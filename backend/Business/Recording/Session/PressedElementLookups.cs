using System.Drawing;
using Core.Models.Business;
using Core.Models.Dtos;
using Core.Ports;

namespace Business.Recording.Session
{
    /// <summary>
    /// Every press asked of its application, through the system's accessibility interface, what it
    /// landed on - a button, a link, a text field, with its name. Asked at once, before the screen
    /// changes, and off the drain, which every input after it waits on: an application asked about
    /// its elements can take its time. The stop waits a moment for the last answer.
    /// </summary>
    public sealed class PressedElementLookups
    {
        // Long enough for the last press's question to an application to come back, and no longer:
        // a busy application costs the review what it would have said, never the review.
        private static readonly TimeSpan WAIT = TimeSpan.FromSeconds(3);

        // Says nothing a window's title does not: what an application that draws its own pixels answers with.
        private static readonly HashSet<string> UNSAID_ROLES = ["", "window", "pane", "title bar"];

        private readonly IAccessibleElementService _accessibleElementService;
        private readonly RecordingSessionCache _cache;
        private readonly TimeProvider _timeProvider;

        // Each press's question, and which recording asked: one that answers after the next
        // recording began is not that recording's.
        private readonly List<Task> _lookups = new List<Task>();
        private readonly Lock _lookupsLock = new Lock();
        private int _recordingNumber;

        public PressedElementLookups(IAccessibleElementService accessibleElementService, RecordingSessionCache cache, TimeProvider timeProvider)
        {
            _accessibleElementService = accessibleElementService;
            _cache = cache;
            _timeProvider = timeProvider;
        }


        // ================================================================
        // Public methods
        // ================================================================

        /// <summary>A new recording: an answer still on its way is no longer kept.</summary>
        public void Reset()
        {
            Interlocked.Increment(ref _recordingNumber);

            lock (_lookupsLock)
                _lookups.Clear();
        }

        public void Ask(int index, RecordedInput press)
        {
            Point point = new Point(press.PhysicalX, press.PhysicalY);
            int recordingNumber = Volatile.Read(ref _recordingNumber);

            Task lookup = Task.Run(() =>
            {
                try
                {
                    AccessibleElement? element = _accessibleElementService.ElementAt(point);
                    bool isSaid = element != null && !UNSAID_ROLES.Contains(element.Role);

                    if (isSaid && recordingNumber == Volatile.Read(ref _recordingNumber))
                        _cache.AddPressedElement(index, element!);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Recording] Could not ask what a press landed on: {ex.Message}");
                }
            });

            lock (_lookupsLock)
                _lookups.Add(lookup);
        }

        /// <summary>The last press's question may still be on its way.</summary>
        public async Task AwaitAsync(CancellationToken ct)
        {
            Task[] lookups;
            lock (_lookupsLock)
                lookups = _lookups.ToArray();

            try
            {
                await Task.WhenAll(lookups).WaitAsync(WAIT, _timeProvider, ct);
            }
            catch (TimeoutException)
            {
                Console.Error.WriteLine("[Recording] An application took too long to say what a press landed on.");
            }
        }

        /// <summary>By the action each press became, its name and what it sits in as they read: document "Swag Labs".</summary>
        public List<PressedElementDto> ElementsOf(IReadOnlyList<RecordedActionDto> actions)
        {
            List<PressedElementDto> elements = new List<PressedElementDto>();

            foreach (RecordedActionDto action in actions.Where(x => x.ScreenshotIndex != null))
            {
                AccessibleElement? element = _cache.GetPressedElement(action.ScreenshotIndex!.Value);
                if (element == null)
                    continue;

                elements.Add(new PressedElementDto
                {
                    ActionIndex = action.Index,
                    Role = element.Role,
                    Name = element.Name,
                    IsEnabled = element.IsEnabled,
                    IsChecked = element.IsChecked,
                    IsPassword = element.IsPassword,
                    Inside = element.Inside.Select(x => $"{x.Role} \"{x.Name}\"").ToList(),
                });
            }

            return elements;
        }
    }
}
