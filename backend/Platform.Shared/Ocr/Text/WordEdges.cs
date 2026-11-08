namespace Platform.Shared.Ocr.Text
{
    /// <summary>
    /// Where a word starts and ends on its line from the pixels. 
    /// The reader says roughly where each character is, the slices of the line can show the edges between words.
    /// The blank columns between two characters are different words.
    /// </summary>
    internal static class WordEdges
    {
        /// <summary>
        /// Columns holding text: far from the line's background, the crop's median grey. The
        /// threshold follows the contrast, so faint grey text on white still counts.
        /// </summary>
        public static bool[,] InkOf(byte[] gray, int width, int height)
        {
            byte[] sorted = (byte[])gray.Clone();
            Array.Sort(sorted);
            int background = sorted[sorted.Length / 2];

            int[] differences = new int[gray.Length];
            for (int i = 0; i < gray.Length; i++)
                differences[i] = Math.Abs(gray[i] - background);

            int[] sortedDifferences = (int[])differences.Clone();
            Array.Sort(sortedDifferences);
            int strongest = sortedDifferences[(int)(sortedDifferences.Length * 0.99)];
            int threshold = Math.Max(25, (int)(strongest * 0.4));

            bool[,] ink = new bool[height, width];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                    ink[y, x] = differences[y * width + x] > threshold;
            }

            return ink;
        }

        public static bool[] ColumnsOf(bool[,] ink)
        {
            int height = ink.GetLength(0);
            int width = ink.GetLength(1);
            bool[] columns = new bool[width];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height && !columns[x]; y++)
                    columns[x] = ink[y, x];
            }

            return columns;
        }

        /// <summary>The first column with text from the start of the line up to a column, or -1.</summary>
        public static int FirstInk(bool[] columns, int to)
        {
            for (int x = 0; x <= Math.Min(to, columns.Length - 1); x++)
            {
                if (columns[x])
                    return x;
            }

            return -1;
        }

        /// <summary>The last column with text from a column to the end of the line, or -1.</summary>
        public static int LastInk(bool[] columns, int from)
        {
            for (int x = columns.Length - 1; x >= Math.Max(0, from); x--)
            {
                if (columns[x])
                    return x;
            }

            return -1;
        }

        /// <summary>
        /// The widest run of blank columns between two columns: the space between a word's last
        /// character and the next word's first. Start and end inclusive, or (-1, -1) when the two
        /// touch.
        /// </summary>
        public static (int Start, int End) WidestGap(bool[] columns, int from, int to)
        {
            from = Math.Clamp(from, 0, columns.Length - 1);
            to = Math.Clamp(to, 0, columns.Length - 1);

            int bestStart = -1;
            int bestEnd = -1;
            int runStart = -1;
            for (int x = from; x <= to; x++)
            {
                if (columns[x])
                {
                    runStart = -1;
                    continue;
                }

                if (runStart < 0)
                    runStart = x;

                if (bestStart < 0 || x - runStart > bestEnd - bestStart)
                {
                    bestStart = runStart;
                    bestEnd = x;
                }
            }

            return (bestStart, bestEnd);
        }

        /// <summary>The first and last rows with text between two columns, or the whole height when there are none.</summary>
        public static (int Top, int Bottom) RowsOf(bool[,] ink, int left, int right)
        {
            int height = ink.GetLength(0);
            int width = ink.GetLength(1);
            int top = -1;
            int bottom = -1;
            for (int y = 0; y < height; y++)
            {
                for (int x = Math.Max(0, left); x <= Math.Min(right, width - 1); x++)
                {
                    if (!ink[y, x])
                        continue;

                    if (top < 0)
                        top = y;
                    bottom = y;
                    break;
                }
            }

            if (top < 0)
                return (0, height - 1);

            return (top, bottom);
        }
    }
}
