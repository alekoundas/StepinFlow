using System.Drawing;
using Business.ScreenText.Models;
using Core.Models.Business;

namespace Business.ScreenText
{
    /// <summary>
    /// OCR's words into the lines a person reads as one thing. The Windows engine puts every word on
    /// a row into one line - "Bag Fellowship Party" across three buttons - so a line is cut wherever
    /// two words are further apart than one and a half times their height.
    /// </summary>
    public static class ScreenLineBuilder
    {
        private const float GAP_PER_HEIGHT = 1.5f;

        public static List<ScreenLine> Build(IReadOnlyList<OcrWord> words)
        {
            List<ScreenLine> lines = new List<ScreenLine>();
            List<OcrWord> run = new List<OcrWord>();

            foreach (OcrWord word in words.Where(x => !string.IsNullOrWhiteSpace(x.Text)))
            {
                bool isNewLine = run.Count > 0 && (word.LineIndex != run[^1].LineIndex || IsApart(run[^1].Bounds, word.Bounds));
                if (isNewLine)
                {
                    lines.Add(Join(run));
                    run.Clear();
                }

                run.Add(word);
            }

            if (run.Count > 0)
                lines.Add(Join(run));

            return lines;
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static bool IsApart(Rectangle before, Rectangle word)
        {
            int gap = Math.Max(word.Left - before.Right, before.Left - word.Right);
            int height = Math.Max(before.Height, word.Height);

            return gap > height * GAP_PER_HEIGHT;
        }

        private static ScreenLine Join(List<OcrWord> words)
        {
            Rectangle bounds = words[0].Bounds;
            foreach (OcrWord word in words.Skip(1))
                bounds = Rectangle.Union(bounds, word.Bounds);

            return new ScreenLine
            {
                Text = string.Join(' ', words.Select(x => x.Text.Trim())),
                Bounds = bounds,
            };
        }
    }
}
