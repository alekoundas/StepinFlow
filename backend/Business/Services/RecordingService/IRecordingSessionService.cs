using Core.Models.Business;

namespace Business.Services.RecordingService
{
    public interface IRecordingSessionService
    {
        bool IsRecording { get; }

        Task<bool> StartAsync(CancellationToken ct = default);

        /// <summary>Stops and returns every action captured, in order.</summary>
        Task<IReadOnlyList<RecordedInput>> StopAsync(CancellationToken ct = default);

        /// <summary>The events of a session that is still open, for a page that reconnects.</summary>
        IReadOnlyList<RecordedInput> GetEvents();

        /// <summary>The screenshot taken for an action, or null when it never had one.</summary>
        byte[]? GetScreenshot(int index);

        /// <summary>Frees the screenshots once the wizard has taken what it needs.</summary>
        void Clear();
    }
}
