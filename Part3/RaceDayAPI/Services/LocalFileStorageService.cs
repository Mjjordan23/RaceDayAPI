namespace RaceDayAPI.Services
{
    /// <summary>
    /// Saves uploaded images into the API's wwwroot/uploads folder and returns a relative URL
    /// such as /uploads/banners/abc123.jpg. The API serves them with UseStaticFiles().
    /// </summary>
    public class LocalFileStorageService : IBlobStorageService
    {
        private readonly string _root;

        public LocalFileStorageService(IWebHostEnvironment env)
        {
            var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
            _root = Path.Combine(webRoot, "uploads");
        }

        public async Task<string> UploadImageAsync(IFormFile file, string folder, CancellationToken ct = default)
        {
            var cleanFolder = folder.Trim('/').Replace("..", string.Empty);
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{extension}";

            var directory = Path.Combine(_root, cleanFolder);
            Directory.CreateDirectory(directory);

            await using var target = File.Create(Path.Combine(directory, fileName));
            await file.CopyToAsync(target, ct);

            return $"/uploads/{cleanFolder}/{fileName}";
        }
    }
}
