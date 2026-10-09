using Core.Enums.Business;

namespace Business.Recording.Models
{
    /// <summary>
    /// Every keyboard key press.
    /// </summary>
    public sealed class KeyCharacter
    {

        public KeyCodeEnum KeyCode { get; }

        public string Character { get; }

        /// <summary>A letter's capital. Null for any other key.</summary>
        public string? Capital { get; }
        
        public KeyCharacter(KeyCodeEnum keyCode, string character, string? capital = null)
        {
            KeyCode = keyCode;
            Character = character;
            Capital = capital;
        }
    }
}
