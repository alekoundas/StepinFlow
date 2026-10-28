using System.Text.Json.Serialization;

using Core.Enums;
using Core.Enums.Business;

namespace Core.Models.Business
{
    public class RecordedInput
    {
        public RecordedInputTypeEnum Type { get; set; }
        public DateTime CreatedOn { get; set; }


        // Cursor
        public int PhysicalX { get; set; }
        public int PhysicalY { get; set; }
        public CursorButtonTypeEnum? CursorButtonType { get; set; }

        /// <summary> A press, or its release, that the application never got: a recording gesture. </summary>
        public RecordingGestureEnum? Gesture { get; set; }

        // Scroll
        public CursorScrollDirectionTypeEnum? ScrollDirection { get; set; }

        /// <summary> How far the wheel turned, unsigned, in the platform's wheel units. A touchpad sends slices of a notch. </summary>
        public int ScrollRotation { get; set; }

        /// <summary> The platform's wheel units in one notch, as the hook counts them. </summary>
        public int ScrollUnitsPerNotch { get; set; }




        // Keyboard
        public KeyCodeEnum? KeyCode { get; set; }

        /// <summary> human-readable e.g. "A", "Enter" </summary>
        public string? KeyChar { get; set; }

        /// <summary> On a KEY_TYPED input: what the key-down before it typed, as the layout made it - Shift, Caps Lock and AltGr applied. </summary>
        public string? TypedCharacter { get; set; }


        //Recording session only

        /// <summary> Recording session only. Position in the session, which is also the screenshot key, so the live feed can name an image it never carries. </summary>
        public int Index { get; set; }
        public string? WindowTitle { get; set; }
        public bool HasScreenshot { get; set; }

        /// <summary> Which window a press or a scroll landed on. Null with no main area to sort against. </summary>
        public RecordedWindowEnum? Window { get; set; }

        /// <summary> The window pointed at, or the one keys went to. Its title can change while typing; this cannot. </summary>
        [JsonIgnore]
        public WindowHandle WindowHandle { get; set; }
    }
}
