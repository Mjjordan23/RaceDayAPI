using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.Models;

namespace RaceDayAPI.Tests
{
    /// <summary>Shared setup code so each test file stays short and readable.</summary>
    internal static class TestHelpers
    {
        public static RaceDayDbContext NewDb()
        {
            var options = new DbContextOptionsBuilder<RaceDayDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new RaceDayDbContext(options);
        }

        /// <summary>Gives a controller a fake session, as if the given user had logged in.</summary>
        public static T Attach<T>(T controller, int? userId, string? role) where T : ControllerBase
        {
            var context = new DefaultHttpContext();
            var session = new TestSession();
            if (userId is not null) session.SetInt32("UserId", userId.Value);
            if (role is not null) session.SetString("Role", role);
            context.Session = session;
            controller.ControllerContext = new ControllerContext { HttpContext = context };
            return controller;
        }

        public static void SeedOrganiser(RaceDayDbContext db, int userId, int organiserId, string organisation = "Race Events SA")
        {
            db.Users.Add(new User { UserID = userId, Email = $"organiser{userId}@example.com", PasswordHash = "x", Role = "Organiser" });
            db.Organisers.Add(new Organiser { OrganiserID = organiserId, UserID = userId, OrganisationName = organisation, ContactNumber = "0821234567" });
            db.SaveChanges();
        }

        public static void SeedParticipant(RaceDayDbContext db, int userId, int participantId)
        {
            db.Users.Add(new User { UserID = userId, Email = $"participant{userId}@example.com", PasswordHash = "x", Role = "Participant" });
            db.Participants.Add(new Participant
            {
                ParticipantID = participantId,
                UserID = userId,
                FirstName = "Thabo",
                LastName = "Mokoena",
                DateOfBirth = new DateTime(2001, 5, 14),
                PhoneNumber = "0712345678"
            });
            db.SaveChanges();
        }

        public static Event SeedEvent(RaceDayDbContext db, int eventId, int organiserId, string type = "Run", string name = "Johannesburg City Marathon")
        {
            var ev = new Event
            {
                EventID = eventId,
                OrganiserID = organiserId,
                Name = name,
                Description = "Annual city event",
                EventDate = DateTime.UtcNow.AddMonths(1),
                Location = "Johannesburg",
                DistanceKm = 42.2m,
                EventType = type
            };
            db.Events.Add(ev);
            db.SaveChanges();
            return ev;
        }

        public static Category SeedCategory(RaceDayDbContext db, int categoryId, int eventId, string name = "Senior")
        {
            var category = new Category { CategoryID = categoryId, EventID = eventId, Name = name, MinAge = 18, MaxAge = 39 };
            db.Categories.Add(category);
            db.SaveChanges();
            return category;
        }
    }
}
