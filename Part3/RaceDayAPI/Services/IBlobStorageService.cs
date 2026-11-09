namespace RaceDayAPI.Services
{
    /// <summary>
    /// Stores uploaded images in cloud storage. Controllers depend on this interface,
    /// so tests can use a fake and never need a real Azure account.
    /// </summary>
    public interface IBlobStorageService
    {
        /// <summary>Uploads an image and returns its public URL.</summary>
        /// <param name="file">The image sent by the client.</param>
        /// <param name="folder">A folder prefix inside the container, for example "banners".</param>
        /// <param name="cancellationToken">Cancels the upload.</param>
        Task<string> UploadImageAsync(IFormFile file, string folder, CancellationToken cancellationToken = default);
    }
}
