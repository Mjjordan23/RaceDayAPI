using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Controllers;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Models;
using Xunit;

namespace RaceDayAPI.Tests
{
    public class EnrolmentsControllerTests
    {
        private static RaceDayDbContext SeededDb()
        {
            var options = new DbContextOptionsBuilder<RaceDayDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var db = new RaceDayDbContext(options);

            var user = new User { UserID = 1, Email = "thabo@example.com", PasswordHash = "x", Role = "Participant" };
            var participant = new Participant { ParticipantID = 1, UserID = 1, FirstName = "Thabo", LastName = "Mokoena", DateOfBirth = new DateTime(2001, 5, 14) };

            var organiserUser = new User { UserID = 2, Email = "john@example.com", PasswordHash = "x", Role = "Organiser" };
            var organiser = new Organiser { OrganiserID = 1, UserID = 2, OrganisationName = "Race Events SA" };

            var ev = new Event
            {
                EventID = 1,
                OrganiserID = 1,
                Name = "Johannesburg City Marathon",
                Description = "Annual city marathon",
                EventDate = DateTime.UtcNow.AddMonths(1),
                Location = "Johannesburg",
                DistanceKm = 42.2m,
                EventType = "Run"
            };
            var category = new Category { CategoryID = 1, EventID = 1, Name = "Senior", MinAge = 18, MaxAge = 39 };

            db.Users.AddRange(user, organiserUser);
            db.Participants.Add(participant);
            db.Organisers.Add(organiser);
            db.Events.Add(ev);
            db.Categories.Add(category);
            db.SaveChanges();

            return db;
        }

        private static EnrolmentsController NewController(RaceDayDbContext db, int userId)
        {
            var controller = new EnrolmentsController(db);
            var context = new DefaultHttpContext();
            var session = new TestSession();
            session.SetInt32("UserId", userId);
            session.SetString("Role", "Participant");
            context.Session = session;
            controller.ControllerContext = new ControllerContext { HttpContext = context };
            return controller;
        }

        [Fact]
        public async Task Enrol_ValidCategory_CreatesEnrolment()
        {
            var db = SeededDb();
            var controller = NewController(db, userId: 1);

            var result = await controller.Enrol(eventId: 1, new EnrolmentCreateDto { CategoryId = 1 });

            Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(1, db.Enrolments.Count());
        }

        [Fact]
        public async Task Enrol_SameEventTwice_ReturnsConflict()
        {
            var db = SeededDb();
            var controller = NewController(db, userId: 1);

            await controller.Enrol(eventId: 1, new EnrolmentCreateDto { CategoryId = 1 });
            var secondAttempt = await controller.Enrol(eventId: 1, new EnrolmentCreateDto { CategoryId = 1 });

            Assert.IsType<ConflictObjectResult>(secondAttempt);
            Assert.Equal(1, db.Enrolments.Count()); // still only one recorded
        }

        [Fact]
        public async Task Enrol_CategoryFromDifferentEvent_ReturnsBadRequest()
        {
            var db = SeededDb();
            db.Events.Add(new Event
            {
                EventID = 2,
                OrganiserID = 1,
                Name = "Cape Town Fun Walk",
                Description = "Family fun walk",
                EventDate = DateTime.UtcNow.AddMonths(2),
                Location = "Cape Town",
                DistanceKm = 10m,
                EventType = "Walk"
            });
            db.SaveChanges();

            var controller = NewController(db, userId: 1);

            // CategoryId 1 belongs to EventID 1, not EventID 2
            var result = await controller.Enrol(eventId: 2, new EnrolmentCreateDto { CategoryId = 1 });

            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}
