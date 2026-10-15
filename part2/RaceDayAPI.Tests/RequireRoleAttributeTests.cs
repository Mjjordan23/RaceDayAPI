using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using RaceDayAPI.Authorization;
using Xunit;

namespace RaceDayAPI.Tests
{
    public class RequireRoleAttributeTests
    {
        private static ActionExecutingContext BuildContext(ISession session)
        {
            var httpContext = new DefaultHttpContext { Session = session };
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            return new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object?>(),
                controller: new object());
        }

        [Fact]
        public void NoSession_ReturnsUnauthorized()
        {
            var context = BuildContext(new TestSession()); // empty session, nobody logged in
            var filter = new RequireRoleAttribute();

            filter.OnActionExecuting(context);

            Assert.IsType<UnauthorizedObjectResult>(context.Result);
        }

        [Fact]
        public void WrongRole_ReturnsForbidden()
        {
            var session = new TestSession();
            session.SetInt32("UserId", 1);
            session.SetString("Role", "Participant");

            var context = BuildContext(session);
            var filter = new RequireRoleAttribute("Organiser"); // action requires Organiser

            filter.OnActionExecuting(context);

            var objectResult = Assert.IsType<ObjectResult>(context.Result);
            Assert.Equal(403, objectResult.StatusCode);
        }

        [Fact]
        public void CorrectRole_AllowsRequestThrough()
        {
            var session = new TestSession();
            session.SetInt32("UserId", 2);
            session.SetString("Role", "Organiser");

            var context = BuildContext(session);
            var filter = new RequireRoleAttribute("Organiser");

            filter.OnActionExecuting(context);

            Assert.Null(context.Result); // no short-circuit result means the request was allowed through
        }
    }
}
