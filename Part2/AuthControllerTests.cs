using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Controllers;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using Xunit;

namespace RaceDayAPI.Tests
{
    public class AuthControllerTests
    {
        private static RaceDayDbContext NewDb()
        {
            var options = new DbContextOptionsBuilder<RaceDayDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new RaceDayDbContext(options);
        }

        private static AuthController NewController(RaceDayDbContext db)
        {
            var controller = new AuthController(db);
            var context = new DefaultHttpContext();
            context.Session = new TestSession();
            controller.ControllerContext = new ControllerContext { HttpContext = context };
            return controller;
        }

        [Fact]
        public async Task Register_ValidParticipant_ReturnsCreated()
        {
            var db = NewDb();
            var controller = NewController(db);

            var dto = new RegisterDto
            {
                Email = "thabo@example.com",
                Password = "Password123",
                Role = "Participant",
                FirstName = "Thabo",
                LastName = "Mokoena",
                DateOfBirth = new DateTime(2001, 5, 14)
            };

            var result = await controller.Register(dto);

            Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(1, db.Users.Count());
            Assert.Equal(1, db.Participants.Count());
        }

        [Fact]
        public async Task Register_DuplicateEmail_ReturnsConflict()
        {
            var db = NewDb();
            var controller = NewController(db);

            var dto = new RegisterDto
            {
                Email = "mary@example.com",
                Password = "Password123",
                Role = "Organiser",
                OrganisationName = "Race Events SA"
            };

            await controller.Register(dto);
            var secondAttempt = await controller.Register(dto);

            Assert.IsType<ConflictObjectResult>(secondAttempt);
        }

        [Fact]
        public async Task Login_CorrectCredentials_ReturnsOkAndSetsSession()
        {
            var db = NewDb();
            var controller = NewController(db);

            await controller.Register(new RegisterDto
            {
                Email = "john@example.com",
                Password = "CorrectPass1",
                Role = "Organiser",
                OrganisationName = "Marathon Masters"
            });

            var result = await controller.Login(new LoginDto { Email = "john@example.com", Password = "CorrectPass1" });

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Organiser", controller.ControllerContext.HttpContext.Session.GetString("Role"));
        }

        [Fact]
        public async Task Login_WrongPassword_ReturnsUnauthorized()
        {
            var db = NewDb();
            var controller = NewController(db);

            await controller.Register(new RegisterDto
            {
                Email = "linda@example.com",
                Password = "RightPassword1",
                Role = "Participant",
                FirstName = "Linda",
                LastName = "Nkosi",
                DateOfBirth = new DateTime(1999, 10, 30)
            });

            var result = await controller.Login(new LoginDto { Email = "linda@example.com", Password = "WrongPassword" });

            Assert.IsType<UnauthorizedObjectResult>(result);
        }
    }
}
