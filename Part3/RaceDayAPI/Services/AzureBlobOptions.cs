namespace RaceDayAPI.Services
{
    /// <summary>Settings read from the "AzureBlob" section of configuration.</summary>
    public class AzureBlobOptions
    {
        /// <summary>The storage account connection string. Keep it out of GitHub (user secrets or an environment variable).</summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>The container that holds all images. It is created automatically if it does not exist.</summary>
        public string ContainerName { get; set; } = "raceday-images";
    }
}
