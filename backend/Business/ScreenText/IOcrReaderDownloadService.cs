namespace Business.ScreenText
{
    public interface IOcrReaderDownloadService
    {
        /// <summary>
        /// Downloads a text reader into the app's data folder and checks it, so a step can read
        /// with it. Null when it worked, or what went wrong.
        /// </summary>
        Task<string?> DownloadAsync(string language, CancellationToken ct = default);
    }
}
