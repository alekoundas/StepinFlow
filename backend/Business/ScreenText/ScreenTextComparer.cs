using System.Drawing;
using System.Text;
using Business.ScreenText.Models;

namespace Business.ScreenText
{
    /// <summary>
    /// What changed between two screenshots, by their text: the lines that appeared, the lines that
    /// went, and how much of the text that is.
    ///
    /// OCR does not read a word the same way twice - Town was "Town", "Towri" and "TOWR" on three
    /// screenshots of one screen - so two lines are the same when their words are, wherever they
    /// are, or when their letters nearly are and they sit in the same place. Numbers are never
    /// nearly the same: a badge going from 1 to 2 is a change, though a number OCR missed on one
    /// side is no difference. Equal lines pair nearest first, so of six "Add to cart" buttons the
    /// one that became "Remove" is the one that changed.
    ///
    /// How much changed is counted in letters, and only on lines of four letters or more: a game
    /// counts up numbers on every screenshot, and a word under four letters is as often a misread
    /// icon as a word.
    /// </summary>
    public static class ScreenTextComparer
    {
        // Under four letters, one misread letter is another word.
        private const int MINIMUM_LETTERS = 4;
        private const int LETTERS_PER_MISREAD = 3;

        public static ScreenTextChange Compare(IReadOnlyList<ScreenLine> before, IReadOnlyList<ScreenLine> after)
        {
            List<Read> was = Reads(before);
            List<Read> now = Reads(after);

            List<Pairing> pairings = new List<Pairing>();
            for (int i = 0; i < was.Count; i++)
            {
                for (int j = 0; j < now.Count; j++)
                {
                    bool isEqual = was[i].Key == now[j].Key;
                    if (isEqual || IsNearly(was[i], now[j]))
                        pairings.Add(new Pairing(i, j, isEqual, Distance(was[i].Line.Bounds, now[j].Line.Bounds)));
                }
            }

            bool[] isWasPaired = new bool[was.Count];
            bool[] isNowPaired = new bool[now.Count];

            foreach (Pairing pairing in pairings.OrderByDescending(x => x.IsEqual).ThenBy(x => x.Distance))
            {
                if (isWasPaired[pairing.Before] || isNowPaired[pairing.After])
                    continue;

                isWasPaired[pairing.Before] = true;
                isNowPaired[pairing.After] = true;
            }

            List<Read> appeared = now.Where((x, i) => !isNowPaired[i]).ToList();
            List<Read> gone = was.Where((x, i) => !isWasPaired[i]).ToList();

            int total = was.Sum(x => x.Size) + now.Sum(x => x.Size);
            int changed = appeared.Sum(x => x.Size) + gone.Sum(x => x.Size);

            return new ScreenTextChange
            {
                Appeared = appeared.Select(x => x.Line).ToList(),
                Gone = gone.Select(x => x.Line).ToList(),
                ChangedShare = total == 0 ? 0 : (double)changed / total,
            };
        }


        // ================================================================
        // Private methods
        // ================================================================

        // A line with no letter or digit, a rule or a bullet, is no text.
        private static List<Read> Reads(IReadOnlyList<ScreenLine> lines)
        {
            List<Read> reads = new List<Read>();

            foreach (ScreenLine line in lines)
            {
                string key = KeyOf(line.Text);
                if (key.Length == 0)
                    continue;

                string letters = new string(key.Where(char.IsLetter).ToArray());
                string digits = new string(key.Where(char.IsDigit).ToArray());
                int size = letters.Length >= MINIMUM_LETTERS ? letters.Length : 0;
                reads.Add(new Read(line, key, letters, digits, size));
            }

            return reads;
        }

        // Lower case, letters and digits, one space wherever anything else stood between them.
        private static string KeyOf(string text)
        {
            StringBuilder key = new StringBuilder();
            bool isBreak = false;

            foreach (char character in text.ToLowerInvariant())
            {
                if (!char.IsLetterOrDigit(character))
                {
                    isBreak = true;
                    continue;
                }

                if (isBreak && key.Length > 0)
                    key.Append(' ');

                key.Append(character);
                isBreak = false;
            }

            return key.ToString();
        }

        // A misread letter in every three, the same numbers where both have some, and where the other was.
        private static bool IsNearly(Read was, Read now)
        {
            if (was.Letters.Length < MINIMUM_LETTERS || now.Letters.Length < MINIMUM_LETTERS)
                return false;

            if (was.Digits.Length > 0 && now.Digits.Length > 0 && was.Digits != now.Digits)
                return false;

            int longer = Math.Max(was.Letters.Length, now.Letters.Length);
            int allowed = (longer + LETTERS_PER_MISREAD - 1) / LETTERS_PER_MISREAD;
            if (EditDistance(was.Letters, now.Letters) > allowed)
                return false;

            return IsSamePlace(was.Line.Bounds, now.Line.Bounds);
        }

        // Within a line's height of where the other was.
        private static bool IsSamePlace(Rectangle was, Rectangle now)
        {
            int margin = Math.Max(was.Height, now.Height);
            Rectangle around = Rectangle.Inflate(was, margin, margin);

            return around.IntersectsWith(now);
        }

        private static double Distance(Rectangle was, Rectangle now)
        {
            double x = (was.Left + was.Right - now.Left - now.Right) / 2d;
            double y = (was.Top + was.Bottom - now.Top - now.Bottom) / 2d;

            return Math.Sqrt(x * x + y * y);
        }

        // Levenshtein: the fewest letters added, removed or changed to turn one into the other.
        private static int EditDistance(string a, string b)
        {
            int[] previous = new int[b.Length + 1];
            int[] current = new int[b.Length + 1];

            for (int j = 0; j <= b.Length; j++)
                previous[j] = j;

            for (int i = 1; i <= a.Length; i++)
            {
                current[0] = i;

                for (int j = 1; j <= b.Length; j++)
                {
                    int changeCost = a[i - 1] == b[j - 1] ? 0 : 1;
                    current[j] = Math.Min(Math.Min(previous[j] + 1, current[j - 1] + 1), previous[j - 1] + changeCost);
                }

                (previous, current) = (current, previous);
            }

            return previous[b.Length];
        }


        // ================================================================
        // Private types
        // ================================================================

        private sealed record Read(ScreenLine Line, string Key, string Letters, string Digits, int Size);
        private sealed record Pairing(int Before, int After, bool IsEqual, double Distance);
    }
}
