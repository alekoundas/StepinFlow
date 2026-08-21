using Core.Enums;
using Core.Enums.Business;

namespace Core.Models.Business
{
    public class RecordedInput
    {
        public RecordedInputTypeEnum Type { get; set; }
        
        
        // Cursor
        public int PhysicalX { get; set; }
        public int PhysicalY { get; set; }
        public CursorButtonTypeEnum? CursorButtonType { get; set; }

        // Keyboard
        public KeyCodeEnum? KeyCode { get; set; }
        public string? KeyChar { get; set; }      // human-readable e.g. "A", "Enter"

        // Scroll
        public CursorScrollDirectionTypeEnum? ScrollDirection { get; set; }
        public int ScrollAmount { get; set; }

        // Recording session only. Position in the session, which is also the screenshot key, so
        // the live feed can name an image it never carries.
        public int Index { get; set; }
        public string? WindowTitle { get; set; }
        public bool HasScreenshot { get; set; }
        //public KeyModifierEnum? Modifiers { get; set; }  // Ctrl, Shift, Alt, Meta


        public DateTime CreatedOn { get; set; }  = DateTime.Now;
    }
}
