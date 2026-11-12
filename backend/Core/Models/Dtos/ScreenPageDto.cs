namespace Core.Models.Dtos
{
    /// <summary>
    /// Which page of the main area a press was on, by the text OCR read and the window's title:
    /// pages are numbered from 1 in the order first seen, and the same number again is a page come
    /// back to - a dialog closed, a menu left. A number, so it goes anywhere.
    /// </summary>
    public class ScreenPageDto
    {
        /// <summary>The press's action, by its index among the recording's actions.</summary>
        public int ActionIndex { get; set; }

        public int Page { get; set; }
    }
}
