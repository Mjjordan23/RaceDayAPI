using Microsoft.AspNetCore.Http;
using RaceDayAPI.Services;

namespace RaceDayAPI.Tests
{
    /// <summary>A stand-in for Azure Blob Storage, so tests never need an Azure account.</summary>
    public class FakeBlobStorage : IBlobStorageService
    {
        /// <summary>When true, uploads fail the way an unconfigured or unreachable Azure would.</summary>
        public bool Unavailable { get; set; }

        /// <summary>The URLs handed out so far.</summary>
        public List<string> Uploaded { get; } = new();

        public Task<string> UploadImageAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
        {
            if (Unavailable) throw new InvalidOperationException("Storage is not configured.");

            var url = $"https://fake.blob.test/raceday-images/{folder}/{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
            Uploaded.Add(url);
            return Task.FromResult(url);
        }

        /// <summary>Builds an in-memory file the way ASP.NET would hand one to a controller.</summary>
        public static IFormFile MakeFile(string fileName = "banner.png", string contentType = "image/png", int size = 1024)
        {
            var stream = new MemoryStream(new byte[size]);
            return new FormFile(stream, 0, size, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };
        }
    }
}
