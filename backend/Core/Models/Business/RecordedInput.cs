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

        // Scroll
        public CursorScrollDirectionTypeEnum? ScrollDirection { get; set; }
        public int ScrollAmount { get; set; }




        // Keyboard
        public KeyCodeEnum? KeyCode { get; set; }

        /// <summary> human-readable e.g. "A", "Enter" </summary>
        public string? KeyChar { get; set; }

        /// <summary>
        /// Applied only on  KEY_TYPED.
        /// Some KEY_DOWN also emmit a KEY_TYPED used to see what was actually typed.
        /// </summary>
        public string? TypedCharacter { get; set; }


        //Recording session only

        /// <summary> Recording session only. Position in the session, which is also the screenshot key, so the live feed can name an image it never carries. </summary>
        public int Index { get; set; }
        public string? WindowTitle { get; set; }
        public bool HasScreenshot { get; set; }
        //public KeyModifierEnum? Modifiers { get; set; }  // Ctrl, Shift, Alt, Meta


        public DateTime CreatedOn { get; set; }
    }
}
