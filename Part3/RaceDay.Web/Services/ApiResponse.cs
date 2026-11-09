using System.Net;
using System.Text.Json;

namespace RaceDay.Web.Services
{
    /// <summary>What came back from the API: the status code, the raw JSON, and helpers to read it.</summary>
    public class ApiResponse
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private readonly string? _messageOverride;

        public ApiResponse(HttpStatusCode status, string body, string? messageOverride = null)
        {
            Status = status;
            Body = body;
            _messageOverride = messageOverride;
        }

        public HttpStatusCode Status { get; }
        public string Body { get; }

        public bool IsSuccess => (int)Status >= 200 && (int)Status < 300;
        public bool IsUnauthorized => Status == HttpStatusCode.Unauthorized;

        /// <summary>A short, friendly message that is safe to show to a user.</summary>
        public string Message
        {
            get
            {
                if (_messageOverride is not null) return _messageOverride;

                try
                {
                    using var doc = JsonDocument.Parse(Body);
                    if (doc.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        if (doc.RootElement.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                            return message.GetString() ?? string.Empty;

                        // Automatic validation errors look like { "errors": { "Field": [ "message" ] } }
                        if (doc.RootElement.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                        {
                            foreach (var field in errors.EnumerateObject())
                            {
                                if (field.Value.ValueKind == JsonValueKind.Array && field.Value.GetArrayLength() > 0)
                                    return field.Value[0].GetString() ?? string.Empty;
                            }
                        }
                    }
                }
                catch (JsonException)
                {
                    // Not JSON: fall through to the generic messages below.
                }

                return Status switch
                {
                    HttpStatusCode.BadRequest => "Some of the details are not valid. Please check and try again.",
                    HttpStatusCode.Unauthorized => "Please log in to continue.",
                    HttpStatusCode.Forbidden => "You do not have permission to do that.",
                    HttpStatusCode.NotFound => "That could not be found.",
                    HttpStatusCode.Conflict => "That conflicts with existing data.",
                    _ => "Something went wrong. Please try again."
                };
            }
        }

        /// <summary>Reads the JSON body as one object. Returns null if it is empty or not the expected shape.</summary>
        public T? Data<T>() where T : class
        {
            if (string.IsNullOrWhiteSpace(Body)) return null;
            try { return JsonSerializer.Deserialize<T>(Body, JsonOptions); }
            catch (JsonException) { return null; }
        }

        /// <summary>Reads the JSON body as a list. Returns an empty list if it is empty or not the expected shape.</summary>
        public List<T> List<T>()
        {
            if (string.IsNullOrWhiteSpace(Body)) return new List<T>();
            try { return JsonSerializer.Deserialize<List<T>>(Body, JsonOptions) ?? new List<T>(); }
            catch (JsonException) { return new List<T>(); }
        }
    }
}
