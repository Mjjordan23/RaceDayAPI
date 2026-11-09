using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RaceDay.Web.Services
{
    /// <summary>
    /// The only class that talks to the RaceDay API. Controllers call it and never touch HTTP,
    /// the database or Azure directly.
    ///
    /// The API remembers who is logged in with a session cookie. The browser never sees that cookie:
    /// this service keeps it in the web app's own session and sends it on every call for that user.
    /// </summary>
    public class ApiService
    {
        private const string ApiCookieKey = "ApiCookie";
        private const string ApiSessionCookieName = ".AspNetCore.Session=";
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _http;
        private readonly IHttpContextAccessor _accessor;

        public ApiService(HttpClient http, IHttpContextAccessor accessor)
        {
            _http = http;
            _accessor = accessor;
        }

        private ISession Session => _accessor.HttpContext!.Session;

        public Task<ApiResponse> GetAsync(string url) => SendAsync(HttpMethod.Get, url, null, null);

        public Task<ApiResponse> PostAsync(string url, object? body = null) => SendAsync(HttpMethod.Post, url, body, null);

        public Task<ApiResponse> PutAsync(string url, object body) => SendAsync(HttpMethod.Put, url, body, null);

        public Task<ApiResponse> DeleteAsync(string url) => SendAsync(HttpMethod.Delete, url, null, null);

        /// <summary>Sends an image to an upload endpoint as multipart/form-data in a field called "file".</summary>
        public Task<ApiResponse> PostFileAsync(string url, IFormFile file)
        {
            var form = new MultipartFormDataContent();
            var fileContent = new StreamContent(file.OpenReadStream());
            if (MediaTypeHeaderValue.TryParse(file.ContentType, out var contentType))
                fileContent.Headers.ContentType = contentType;
            form.Add(fileContent, "file", file.FileName);
            return SendAsync(HttpMethod.Post, url, null, form);
        }

        /// <summary>Logs in at the API. On success the API's session cookie is kept for later calls.</summary>
        public Task<ApiResponse> LoginAsync(string email, string password)
        {
            Session.Remove(ApiCookieKey); // always start a fresh API session
            return PostAsync("api/auth/login", new { email, password });
        }

        /// <summary>Forgets the API session cookie (used when the user logs out).</summary>
        public void ForgetApiSession() => Session.Remove(ApiCookieKey);

        private async Task<ApiResponse> SendAsync(HttpMethod method, string url, object? body, HttpContent? content)
        {
            using var request = new HttpRequestMessage(method, url);

            if (content is not null)
                request.Content = content;
            else if (body is not null)
                request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");

            var cookie = Session.GetString(ApiCookieKey);
            if (!string.IsNullOrEmpty(cookie))
                request.Headers.TryAddWithoutValidation("Cookie", cookie);

            try
            {
                using var response = await _http.SendAsync(request);
                var text = await response.Content.ReadAsStringAsync();
                RememberApiSessionCookie(response);
                return new ApiResponse(response.StatusCode, text);
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                // The API is switched off or unreachable: show a friendly message instead of crashing.
                return new ApiResponse(HttpStatusCode.ServiceUnavailable, string.Empty,
                    "The RaceDay service is not reachable right now. Please try again shortly.");
            }
        }

        private void RememberApiSessionCookie(HttpResponseMessage response)
        {
            if (!response.Headers.TryGetValues("Set-Cookie", out var values)) return;

            foreach (var value in values)
            {
                if (value.StartsWith(ApiSessionCookieName, StringComparison.Ordinal))
                {
                    Session.SetString(ApiCookieKey, value.Split(';')[0]);
                    return;
                }
            }
        }
    }
}
