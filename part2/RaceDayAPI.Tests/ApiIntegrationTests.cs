using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace RaceDayAPI.Tests
{
    /// <summary>
    /// End-to-end tests that call the real HTTP endpoints, including sessions and the
    /// [RequireRole] filter. Each HttpClient has its own cookie jar, so one client = one logged-in user.
    /// </summary>
    public class ApiIntegrationTests : IDisposable
    {
        private readonly ApiFactory _factory = new();

        public void Dispose() => _factory.Dispose();

        // ---------- helpers ----------

        private static int Id(JsonElement element, string name)
        {
            foreach (var p in element.EnumerateObject())
                if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) return p.Value.GetInt32();
            throw new InvalidOperationException($"Property '{name}' not found in: {element}");
        }

        private static async Task<JsonElement> Json(HttpResponseMessage response)
        {
            var text = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(text).RootElement.Clone();
        }

        private async Task<HttpClient> LoggedInOrganiser(string email = "org@example.com", string org = "Race Events SA")
        {
            var client = _factory.CreateClient();
            var reg = await client.PostAsJsonAsync("/api/auth/register", new
            {
                email, password = "Password123", role = "Organiser", organisationName = org, contactNumber = "0821234567"
            });
            Assert.Equal(HttpStatusCode.Created, reg.StatusCode);
            var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123" });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            return client;
        }

        private async Task<HttpClient> LoggedInParticipant(string email = "runner@example.com")
        {
            var client = _factory.CreateClient();
            var reg = await client.PostAsJsonAsync("/api/auth/register", new
            {
                email, password = "Password123", role = "Participant",
                firstName = "Thabo", lastName = "Mokoena", dateOfBirth = "2001-05-14", phoneNumber = "0712345678"
            });
            Assert.Equal(HttpStatusCode.Created, reg.StatusCode);
            var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123" });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            return client;
        }

        private static object EventBody(string name = "Soweto Marathon") => new
        {
            name, description = "Community marathon", eventDate = DateTime.UtcNow.AddMonths(2),
            location = "Soweto", distanceKm = 42.2, eventType = "Run"
        };

        private static async Task<int> CreateEvent(HttpClient organiser, string name = "Soweto Marathon")
        {
            var res = await organiser.PostAsJsonAsync("/api/events", EventBody(name));
            Assert.Equal(HttpStatusCode.Created, res.StatusCode);
            return Id(await Json(res), "eventID");
        }

        private static async Task<int> CreateCategory(HttpClient organiser, int eventId)
        {
            var res = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories",
                new { name = "Senior", minAge = 18, maxAge = 39, distanceKm = 42.2 });
            Assert.Equal(HttpStatusCode.Created, res.StatusCode);
            return Id(await Json(res), "categoryID");
        }

        // ---------- not logged in (401) ----------

        [Theory]
        [InlineData("GET", "/api/users/me")]
        [InlineData("GET", "/api/enrolments/me")]
        [InlineData("GET", "/api/results/me")]
        [InlineData("POST", "/api/events")]
        [InlineData("POST", "/api/auth/logout")]
        public async Task ProtectedEndpoints_WhenNotLoggedIn_Return401(string method, string url)
        {
            var client = _factory.CreateClient();
            var request = new HttpRequestMessage(new HttpMethod(method), url);
            if (method == "POST") request.Content = JsonContent.Create(EventBody());

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task BrowsingEvents_WhenNotLoggedIn_IsAllowed()
        {
            var response = await _factory.CreateClient().GetAsync("/api/events");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // ---------- wrong role (403) ----------

        [Fact]
        public async Task Participant_CannotCreateEvent_Returns403()
        {
            var participant = await LoggedInParticipant();

            var response = await participant.PostAsJsonAsync("/api/events", EventBody());

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Participant_CannotDeleteEvent_Returns403()
        {
            var organiser = await LoggedInOrganiser();
            var eventId = await CreateEvent(organiser);
            var participant = await LoggedInParticipant();

            var response = await participant.DeleteAsync($"/api/events/{eventId}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Organiser_CannotEnrol_Returns403()
        {
            var organiser = await LoggedInOrganiser();
            var eventId = await CreateEvent(organiser);
            var categoryId = await CreateCategory(organiser, eventId);

            var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Participant_CannotCaptureResults_Returns403()
        {
            var organiser = await LoggedInOrganiser();
            var eventId = await CreateEvent(organiser);
            var categoryId = await CreateCategory(organiser, eventId);
            var participant = await LoggedInParticipant();
            var enrol = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
            var enrolmentId = Id(await Json(enrol), "enrolmentID");

            var response = await participant.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results",
                new { finishTime = "01:30:00", finishPosition = 1 });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Organiser_CannotViewParticipantOnlyEndpoints_Returns403()
        {
            var organiser = await LoggedInOrganiser();

            Assert.Equal(HttpStatusCode.Forbidden, (await organiser.GetAsync("/api/enrolments/me")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await organiser.GetAsync("/api/results/me")).StatusCode);
        }

        [Fact]
        public async Task Organiser_CannotEditAnotherOrganisersEvent_Returns403()
        {
            var owner = await LoggedInOrganiser("owner@example.com", "Owner Events");
            var eventId = await CreateEvent(owner);
            var other = await LoggedInOrganiser("other@example.com", "Other Events");

            var response = await other.PutAsJsonAsync($"/api/events/{eventId}", EventBody("Hijacked"));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // ---------- main flow ----------

        [Fact]
        public async Task Enrolling_TwiceInTheSameEvent_Returns409()
        {
            var organiser = await LoggedInOrganiser();
            var eventId = await CreateEvent(organiser);
            var categoryId = await CreateCategory(organiser, eventId);
            var participant = await LoggedInParticipant();

            var first = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
            var second = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });

            Assert.Equal(HttpStatusCode.Created, first.StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }

        [Fact]
        public async Task Enrolling_WithCategoryFromAnotherEvent_Returns400()
        {
            var organiser = await LoggedInOrganiser();
            var eventA = await CreateEvent(organiser, "Event A");
            var eventB = await CreateEvent(organiser, "Event B");
            var categoryOfB = await CreateCategory(organiser, eventB);
            var participant = await LoggedInParticipant();

            var response = await participant.PostAsJsonAsync($"/api/events/{eventA}/enrolments", new { categoryId = categoryOfB });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Organiser_SeesEnrolmentsForOwnEvent_AndParticipantSeesOwnEnrolments()
        {
            var organiser = await LoggedInOrganiser();
            var eventId = await CreateEvent(organiser);
            var categoryId = await CreateCategory(organiser, eventId);
            var participant = await LoggedInParticipant();
            await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });

            var forOrganiser = await Json(await organiser.GetAsync($"/api/events/{eventId}/enrolments"));
            var forParticipant = await Json(await participant.GetAsync("/api/enrolments/me"));

            Assert.Equal(1, forOrganiser.GetArrayLength());
            Assert.Equal(1, forParticipant.GetArrayLength());
        }

        [Fact]
        public async Task CapturingResult_ThenParticipantSeesItInResultsMe()
        {
            var organiser = await LoggedInOrganiser();
            var eventId = await CreateEvent(organiser);
            var categoryId = await CreateCategory(organiser, eventId);
            var participant = await LoggedInParticipant();
            var enrol = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
            var enrolmentId = Id(await Json(enrol), "enrolmentID");

            var capture = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results",
                new { finishTime = "03:45:10", finishPosition = 12 });
            var duplicate = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results",
                new { finishTime = "03:50:00", finishPosition = 13 });
            var mine = await Json(await participant.GetAsync("/api/results/me"));

            Assert.Equal(HttpStatusCode.Created, capture.StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
            Assert.Equal(1, mine.GetArrayLength());
        }

        [Fact]
        public async Task DeletingEventWithEnrolments_Returns409_NotAServerError()
        {
            var organiser = await LoggedInOrganiser();
            var eventId = await CreateEvent(organiser);
            var categoryId = await CreateCategory(organiser, eventId);
            var participant = await LoggedInParticipant();
            await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });

            var response = await organiser.DeleteAsync($"/api/events/{eventId}");

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        // ---------- profile ----------

        [Fact]
        public async Task Profile_GetThenUpdate_ReturnsNewValues()
        {
            var participant = await LoggedInParticipant();

            var update = await participant.PutAsJsonAsync("/api/users/me", new { phoneNumber = "0799999999" });
            var profile = await Json(await participant.GetAsync("/api/users/me"));

            Assert.Equal(HttpStatusCode.OK, update.StatusCode);
            Assert.Equal("0799999999", profile.GetProperty("phoneNumber").GetString());
            Assert.Equal("Participant", profile.GetProperty("role").GetString());
        }

        // ---------- auth ----------

        [Fact]
        public async Task Logout_EndsTheSession()
        {
            var participant = await LoggedInParticipant();

            var logout = await participant.PostAsync("/api/auth/logout", null);
            var after = await participant.GetAsync("/api/users/me");

            Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, after.StatusCode);
        }

        [Fact]
        public async Task Register_RetryAfterValidationError_DoesNotReturn409()
        {
            var client = _factory.CreateClient();
            var bad = await client.PostAsJsonAsync("/api/auth/register",
                new { email = "retry@example.com", password = "Password123", role = "Participant" }); // missing names + DOB
            var good = await client.PostAsJsonAsync("/api/auth/register", new
            {
                email = "retry@example.com", password = "Password123", role = "Participant",
                firstName = "Anele", lastName = "Dlamini", dateOfBirth = "1999-01-01"
            });

            Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
            Assert.Equal(HttpStatusCode.Created, good.StatusCode);
        }

        [Fact]
        public async Task Login_WithWrongPassword_Returns401()
        {
            await LoggedInParticipant("wrongpw@example.com");
            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "wrongpw@example.com", password = "WrongPassword" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
