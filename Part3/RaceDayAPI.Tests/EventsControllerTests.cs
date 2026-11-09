using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDayAPI.Controllers;
using RaceDayAPI.DTOs;
using RaceDayAPI.Models;
using Xunit;

namespace RaceDayAPI.Tests
{
    /// <summary>Tests the event endpoints: Organisers can manage their own events, and mistakes are rejected.</summary>
    public class EventsControllerTests
    {
        private static EventCreateDto ValidDto() => new EventCreateDto
        {
            Name = "Soweto Marathon",
            Description = "Community marathon through Soweto",
            EventDate = DateTime.UtcNow.AddMonths(2),
            Location = "Soweto",
            DistanceKm = 42.2m,
            EventType = "Run"
        };

        private static void AssertForbidden(IActionResult result)
        {
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        }

        // ---------- Create ----------

        [Fact]
        public async Task Create_AsOrganiser_ReturnsCreatedAndSavesEvent()
        {
            var db = TestHelpers.NewDb();
            TestHelpers.SeedOrganiser(db, userId: 1, organiserId: 1);
            var controller = TestHelpers.Attach(new EventsController(db), 1, "Organiser");

            var result = await controller.Create(ValidDto());

            Assert.IsType<CreatedAtActionResult>(result);
            var saved = Assert.Single(db.Events);
            Assert.Equal("Soweto Marathon", saved.Name);
            Assert.Equal(1, saved.OrganiserID); // linked to the logged-in Organiser
        }

        [Fact]
        public async Task Create_WithInvalidEventType_ReturnsBadRequest()
        {
            var db = TestHelpers.NewDb();
            TestHelpers.SeedOrganiser(db, 1, 1);
            var controller = TestHelpers.Attach(new EventsController(db), 1, "Organiser");

            var dto = ValidDto();
            dto.EventType = "Swim";

            var result = await controller.Create(dto);

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Empty(db.Events);
        }

        [Fact]
        public async Task Create_WithZeroDistance_ReturnsBadRequest()
        {
            var db = TestHelpers.NewDb();
            TestHelpers.SeedOrganiser(db, 1, 1);
            var controller = TestHelpers.Attach(new EventsController(db), 1, "Organiser");

            var dto = ValidDto();
            dto.DistanceKm = 0;

            var result = await controller.Create(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ---------- Update ----------

        [Fact]
        public async Task Update_OwnEvent_ReturnsOkAndChangesName()
        {
            var db = TestHelpers.NewDb();
            TestHelpers.SeedOrganiser(db, 1, 1);
            TestHelpers.SeedEvent(db, eventId: 1, organiserId: 1);
            var controller = TestHelpers.Attach(new EventsController(db), 1, "Organiser");

            var dto = ValidDto();
            dto.Name = "Renamed Marathon";

            var result = await controller.Update(1, dto);

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Renamed Marathon", db.Events.Single().Name);
        }

        [Fact]
        public async Task Update_AnotherOrganisersEvent_Returns403AndChangesNothing()
        {
            var db = TestHelpers.NewDb();
            TestHelpers.SeedOrganiser(db, userId: 1, organiserId: 1);
            TestHelpers.SeedOrganiser(db, userId: 2, organiserId: 2, organisation: "Marathon Masters");
            TestHelpers.SeedEvent(db, eventId: 1, organiserId: 1); // belongs to Organiser 1
            var controller = TestHelpers.Attach(new EventsController(db), userId: 2, role: "Organiser");

            var dto = ValidDto();
            dto.Name = "Hijacked";

            var result = await controller.Update(1, dto);

            AssertForbidden(result);
            Assert.Equal("Johannesburg City Marathon", db.Events.Single().Name);
        }

        [Fact]
        public async Task Update_MissingEvent_ReturnsNotFound()
        {
            var db = TestHelpers.NewDb();
            TestHelpers.SeedOrganiser(db, 1, 1);
            var controller = TestHelpers.Attach(new EventsController(db), 1, "Organiser");

            var result = await controller.Update(999, ValidDto());

            Assert.IsType<NotFoundObjectResult>(result);
        }

        // ---------- Delete ----------

        [Fact]
        public async Task Delete_OwnEventWithoutEnrolments_ReturnsNoContent()
        {
            var db = TestHelpers.NewDb();
            TestHelpers.SeedOrganiser(db, 1, 1);
            TestHelpers.SeedEvent(db, 1, 1);
            var controller = TestHelpers.Attach(new EventsController(db), 1, "Organiser");

            var result = await controller.Delete(1);

            Assert.IsType<NoContentResult>(result);
            Assert.Empty(db.Events);
        }

        [Fact]
        public async Task Delete_EventWithEnrolments_ReturnsConflictAndKeepsEvent()
        {
            var db = TestHelpers.NewDb();
            TestHelpers.SeedOrganiser(db, 1, 1);
            TestHelpers.SeedParticipant(db, userId: 2, participantId: 1);
            TestHelpers.SeedEvent(db, 1, 1);
            TestHelpers.SeedCategory(db, 1, 1);
            db.Enrolments.Add(new Enrolment { ParticipantID = 1, EventID = 1, CategoryID = 1, Status = "Pending" });
            db.SaveChanges();
            var controller = TestHelpers.Attach(new EventsController(db), 1, "Organiser");

            var result = await controller.Delete(1);

            Assert.IsType<ConflictObjectResult>(result);
            Assert.Single(db.Events);
        }

        [Fact]
        public async Task Delete_AnotherOrganisersEvent_Returns403()
        {
            var db = TestHelpers.NewDb();
            TestHelpers.SeedOrganiser(db, 1, 1);
            TestHelpers.SeedOrganiser(db, 2, 2, "Marathon Masters");
            TestHelpers.SeedEvent(db, 1, 1);
            var controller = TestHelpers.Attach(new EventsController(db), 2, "Organiser");

            var result = await controller.Delete(1);

            AssertForbidden(result);
            Assert.Single(db.Events);
        }

        // ---------- View ----------

        [Fact]
        public async Task GetAll_WithTypeFilter_ReturnsOnlyThatType()
        {
            var db = TestHelpers.NewDb();
            TestHelpers.SeedOrganiser(db, 1, 1);
            TestHelpers.SeedEvent(db, 1, 1, type: "Run", name: "City Marathon");
            TestHelpers.SeedEvent(db, 2, 1, type: "Cycle", name: "Cycle Tour");
            var controller = TestHelpers.Attach(new EventsController(db), null, null); // not logged in

            var result = await controller.GetAll(type: "Cycle", search: null);

            var ok = Assert.IsType<OkObjectResult>(result);
            var items = Assert.IsAssignableFrom<System.Collections.IEnumerable>(ok.Value).Cast<object>().ToList();
            Assert.Single(items);
        }

        [Fact]
        public async Task GetById_MissingEvent_ReturnsNotFound()
        {
            var db = TestHelpers.NewDb();
            var controller = TestHelpers.Attach(new EventsController(db), null, null);

            var result = await controller.GetById(42);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetEnrolments_AnotherOrganisersEvent_Returns403()
        {
            var db = TestHelpers.NewDb();
            TestHelpers.SeedOrganiser(db, 1, 1);
            TestHelpers.SeedOrganiser(db, 2, 2, "Marathon Masters");
            TestHelpers.SeedEvent(db, 1, 1);
            var controller = TestHelpers.Attach(new EventsController(db), 2, "Organiser");

            var result = await controller.GetEnrolments(1);

            AssertForbidden(result);
        }
    }
}
