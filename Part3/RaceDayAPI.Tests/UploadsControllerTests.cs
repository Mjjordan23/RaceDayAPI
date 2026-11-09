using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDayAPI.Controllers;
using Xunit;

namespace RaceDayAPI.Tests
{
    /// <summary>Tests the image upload endpoints and the Organiser dashboard list.</summary>
    public class UploadsControllerTests
    {
        private static void AssertStatus(IActionResult result, int expected)
        {
            var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
            Assert.Equal(expected, objectResult.StatusCode);
        }

        // ---------- banner ----------

        [Fact]
        public async Task UploadBanner_ByOwner_SavesUrlOnEvent()
        {
            var db = TestHelpers.NewDb();
            TestHelpers.SeedOrganiser(db, 1, 1);
            TestHelpers.SeedEvent(db, 1, 1);
            var blobs = new FakeBlobStorage();
            var controller = TestHelpers.Attach(new UploadsController(db, blobs), 1, "Organiser");

            var result = await controller.UploadBanner(1, FakeBlobStorage.MakeFile());

            Assert.IsType<OkObjectResult>(result);
            var saved = db.Events.Single();
            Assert.Equal(blobs.Uploaded.Single(), saved.BannerImageUrl);
        }

        [Fact]
        public async Task UploadBanner_ForAnotherOrganisersEvent_Returns403AndUploadsNothing()
        {
            var db = TestHelpers.NewDb();
            TestHelpers.SeedOrganiser(db, 1, 1);
            TestHelpers.SeedOrganiser(db, 2, 2, "Other Events");
            TestHelpers.SeedEvent(db, 1, 1);
            var blobs = new FakeBlobStorage();
            var controller = TestHelpers.Attach(new UploadsController(db, blobs), 2, "Organiser");

            var result = await controller.UploadBanner(1, FakeBlobStorage.MakeFile());

            AssertStatus(result, StatusCodes.Status403Forbidden);
            Assert.Empty(blobs.Uploaded);
            Assert.Null(db.Events.Single().BannerImageUrl);
        }

        [Fact]
        public async Task UploadBanner_MissingEvent_ReturnsNotFound()
        {
            var db = TestHelpers.NewDb();
            TestHelpers.SeedOrganiser(db, 1, 1);
            var controller = TestHelpers.Attach(new UploadsController(db, new FakeBlobStorage()), 1, "Organiser");

            var result = await controller.UploadBanner(99, FakeBlobStorage.MakeFile());

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Theory]
        [InlineData("notes.pdf", "application/pdf")]
        [InlineData("script.exe", "image/png")]
        [InlineData("photo.png", "text/plain")]
        public async Task UploadBanner_WithWrongFileType_ReturnsBadRequest(string fileName, string contentType)
        {
            var db = TestHelpers.NewDb();
            TestHelpers.SeedOrganiser(db, 1, 1);
            TestHelpers.SeedEvent(db, 1, 1);
            var blobs = new FakeBlobStorage();
            var controller = TestHelpers.Attach(new UploadsController(db, blobs), 1, "Organiser");

            var result = await controller.UploadBanner(1, FakeBlobStorage.MakeFile(fileName, contentType));

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Empty(blobs.Uploaded);
        }

        [Fact]
        public async Task UploadBanner_WithNoFile_ReturnsBadRequest()
        {
            var db = TestHelpers.NewDb();
            TestHelpers.SeedOrganiser(db, 1, 1);
            TestHelpers.SeedEvent(db, 1, 1);
            var controller = TestHelpers.Attach(new UploadsController(db, new FakeBlobStorage()), 1, "Organiser");

            var result = await controller.UploadBanner(1, null!);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UploadBanner_FileOverFiveMegabytes_ReturnsBadRequest()
        {
            var db = TestHelpers.NewDb();
            TestHelpers.SeedOrganiser(db, 1, 1);
            TestHelpers.SeedEvent(db, 1, 1);
            var controller = TestHelpers.Attach(new UploadsController(db, new FakeBlobStorage()), 1, "Organiser");

            var result = await controller.UploadBanner(1, FakeBlobStorage.MakeFile(size: 5 * 1024 * 1024 + 1));

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UploadBanner_WhenStorageIsDown_Returns503AndKeepsOldBanner()
        {
            var db = TestHelpers.NewDb();
            TestHelpers.SeedOrganiser(db, 1, 1);
            TestHelpers.SeedEvent(db, 1, 1);
            var controller = TestHelpers.Attach(new UploadsController(db, new FakeBlobStorage { Unavailable = true }), 1, "Organiser");

            var result = await controller.UploadBanner(1, FakeBlobStorage.MakeFile());

            AssertStatus(result, StatusCodes.Status503ServiceUnavailable);
            Assert.Null(db.Events.Single().BannerImageUrl);
        }

        // ---------- profile picture ----------

        [Fact]
        public async Task UploadProfilePicture_AsParticipant_SavesUrl()
        {
            var db = TestHelpers.NewDb();
            TestHelpers.SeedParticipant(db, 1, 1);
            var blobs = new FakeBlobStorage();
            var controller = TestHelpers.Attach(new UploadsController(db, blobs), 1, "Participant");

            var result = await controller.UploadProfilePicture(FakeBlobStorage.MakeFile("me.jpg", "image/jpeg"));

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal(blobs.Uploaded.Single(), db.Participants.Single().ProfilePictureUrl);
        }

        [Fact]
        public async Task UploadProfilePicture_WithWrongFileType_ReturnsBadRequest()
        {
            var db = TestHelpers.NewDb();
            TestHelpers.SeedParticipant(db, 1, 1);
            var controller = TestHelpers.Attach(new UploadsController(db, new FakeBlobStorage()), 1, "Participant");

            var result = await controller.UploadProfilePicture(FakeBlobStorage.MakeFile("cv.pdf", "application/pdf"));

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Null(db.Participants.Single().ProfilePictureUrl);
        }

        // ---------- organiser dashboard list ----------

        [Fact]
        public async Task GetMine_ReturnsOnlyOwnEvents_WithEnrolmentCounts()
        {
            var db = TestHelpers.NewDb();
            TestHelpers.SeedOrganiser(db, 1, 1);
            TestHelpers.SeedOrganiser(db, 2, 2, "Other Events");
            TestHelpers.SeedParticipant(db, 3, 1);
            TestHelpers.SeedEvent(db, 1, 1, name: "My Marathon");
            TestHelpers.SeedEvent(db, 2, 2, name: "Not Mine");
            TestHelpers.SeedCategory(db, 1, 1);
            db.Enrolments.Add(new RaceDayAPI.Models.Enrolment { ParticipantID = 1, EventID = 1, CategoryID = 1, Status = "Pending" });
            db.SaveChanges();
            var controller = TestHelpers.Attach(new OrganiserEventsController(db), 1, "Organiser");

            var result = await controller.GetMine();

            var ok = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(ok.Value);
            Assert.Contains("My Marathon", json);
            Assert.DoesNotContain("Not Mine", json);
            Assert.Contains("\"EnrolmentCount\":1", json);
        }
    }
}
