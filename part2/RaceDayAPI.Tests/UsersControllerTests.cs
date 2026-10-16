using Microsoft.AspNetCore.Mvc;
using RaceDayAPI.Controllers;
using RaceDayAPI.DTOs;
using Xunit;

namespace RaceDayAPI.Tests
{
    /// <summary>Tests the profile endpoints for both roles.</summary>
    public class UsersControllerTests
    {
        [Fact]
        public async Task GetMe_AsParticipant_ReturnsOwnProfile()
        {
            var db = TestHelpers.NewDb();
            TestHelpers.SeedParticipant(db, userId: 1, participantId: 1);
            var controller = TestHelpers.Attach(new UsersController(db), 1, "Participant");

            var result = await controller.GetMe();

            var ok = Assert.IsType<OkObjectResult>(result);
            var profile = Assert.IsType<UserProfileDto>(ok.Value);
            Assert.Equal("Participant", profile.Role);
            Assert.Equal("Thabo", profile.FirstName);
            Assert.Null(profile.OrganisationName); // belongs to the other role
        }

        [Fact]
        public async Task UpdateMe_AsParticipant_ChangesOnlyTheFieldsSent()
        {
            var db = TestHelpers.NewDb();
            TestHelpers.SeedParticipant(db, 1, 1);
            var controller = TestHelpers.Attach(new UsersController(db), 1, "Participant");

            var result = await controller.UpdateMe(new UpdateProfileDto { PhoneNumber = "0799999999" });

            Assert.IsType<OkObjectResult>(result);
            var saved = db.Participants.Single();
            Assert.Equal("0799999999", saved.PhoneNumber);
            Assert.Equal("Thabo", saved.FirstName); // untouched
        }

        [Fact]
        public async Task UpdateMe_AsOrganiser_ChangesOrganisationName()
        {
            var db = TestHelpers.NewDb();
            TestHelpers.SeedOrganiser(db, 1, 1);
            var controller = TestHelpers.Attach(new UsersController(db), 1, "Organiser");

            var result = await controller.UpdateMe(new UpdateProfileDto { OrganisationName = "Cape Cycle Club" });

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Cape Cycle Club", db.Organisers.Single().OrganisationName);
        }

        [Fact]
        public async Task UpdateMe_WithBlankFirstName_ReturnsBadRequest()
        {
            var db = TestHelpers.NewDb();
            TestHelpers.SeedParticipant(db, 1, 1);
            var controller = TestHelpers.Attach(new UsersController(db), 1, "Participant");

            var result = await controller.UpdateMe(new UpdateProfileDto { FirstName = "   " });

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Thabo", db.Participants.Single().FirstName);
        }
    }
}
