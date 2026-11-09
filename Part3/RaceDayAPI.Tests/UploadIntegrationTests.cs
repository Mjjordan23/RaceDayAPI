using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace RaceDayAPI.Tests
{
    /// <summary>
    /// End-to-end checks for image uploads and the Organiser dashboard list,
    /// going through sessions and the role filter like the real web app will.
    /// </summary>
    public class UploadIntegrationTests : IDisposable
    {
        private readonly ApiFactory _factory = new();

        public void Dispose() => _factory.Dispose();

        private async Task<HttpClient> LoggedInOrganiser(string email = "org@example.com")
        {
            var client = _factory.CreateClient();
            await client.PostAsJsonAsync("/api/auth/register", new
            {
                email, password = "Password123", role = "Organiser", organisationName = "Race Events SA", contactNumber = "0821234567"
            });
            var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123" });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            return client;
        }

        private async Task<HttpClient> LoggedInParticipant(string email = "runner@example.com")
        {
            var client = _factory.CreateClient();
            await client.PostAsJsonAsync("/api/auth/register", new
            {
                email, password = "Password123", role = "Participant",
                firstName = "Thabo", lastName = "Mokoena", dateOfBirth = "2001-05-14"
            });
            var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123" });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            return client;
        }

        private static async Task<int> CreateEvent(HttpClient organiser)
        {
            var res = await organiser.PostAsJsonAsync("/api/events", new
            {
                name = "Soweto Marathon", description = "Community marathon", eventDate = DateTime.UtcNow.AddMonths(2),
                location = "Soweto", distanceKm = 42.2, eventType = "Run"
            });
            Assert.Equal(HttpStatusCode.Created, res.StatusCode);
            var json = JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement;
            return json.EnumerateObject().First(p => p.Name.Equals("eventID", StringComparison.OrdinalIgnoreCase)).Value.GetInt32();
        }

        private static MultipartFormDataContent ImageForm(string fileName = "pic.png", string contentType = "image/png")
        {
            var file = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
            file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            var form = new MultipartFormDataContent();
            form.Add(file, "file", fileName);
            return form;
        }

        [Fact]
        public async Task Organiser_UploadsBanner_AndEventShowsTheUrl()
        {
            var organiser = await LoggedInOrganiser();
            var eventId = await CreateEvent(organiser);

            var upload = await organiser.PostAsync($"/api/events/{eventId}/banner", ImageForm());
            var details = await organiser.GetStringAsync($"/api/events/{eventId}");

            Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
            Assert.Single(_factory.Blobs.Uploaded);
            Assert.Contains(_factory.Blobs.Uploaded[0], details);
        }

        [Fact]
        public async Task Participant_CannotUploadBanner_Returns403()
        {
            var organiser = await LoggedInOrganiser();
            var eventId = await CreateEvent(organiser);
            var participant = await LoggedInParticipant();

            var response = await participant.PostAsync($"/api/events/{eventId}/banner", ImageForm());

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Empty(_factory.Blobs.Uploaded);
        }

        [Fact]
        public async Task NotLoggedIn_CannotUploadBanner_Returns401()
        {
            var response = await _factory.CreateClient().PostAsync("/api/events/1/banner", ImageForm());

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task BannerUpload_WithPdf_Returns400()
        {
            var organiser = await LoggedInOrganiser();
            var eventId = await CreateEvent(organiser);

            var response = await organiser.PostAsync($"/api/events/{eventId}/banner", ImageForm("doc.pdf", "application/pdf"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Participant_UploadsProfilePicture_AndProfileShowsTheUrl()
        {
            var participant = await LoggedInParticipant();

            var upload = await participant.PostAsync("/api/users/me/profile-picture", ImageForm("me.jpg", "image/jpeg"));
            var profile = await participant.GetStringAsync("/api/users/me");

            Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
            Assert.Contains(_factory.Blobs.Uploaded[0], profile);
        }

        [Fact]
        public async Task Organiser_CannotUploadProfilePicture_Returns403()
        {
            var organiser = await LoggedInOrganiser();

            var response = await organiser.PostAsync("/api/users/me/profile-picture", ImageForm());

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task EventsMine_ForOrganiser_ListsOwnEvents()
        {
            var organiser = await LoggedInOrganiser();
            await CreateEvent(organiser);

            var response = await organiser.GetAsync("/api/events/mine");
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Soweto Marathon", body);
            Assert.Contains("enrolmentCount", body);
        }

        [Fact]
        public async Task EventsMine_ForParticipant_Returns403()
        {
            var participant = await LoggedInParticipant();

            var response = await participant.GetAsync("/api/events/mine");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
