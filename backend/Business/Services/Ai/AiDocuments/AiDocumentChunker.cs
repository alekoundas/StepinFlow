using Core.Models.Business;

namespace Business.Services.Ai.AiDocuments
{
    /// <summary>
    /// Splits a markdown document into retrievable pieces.
    ///
    /// 1) extract document title by the single '#'.
    /// 2) split document sections by the double '##'.
    /// 3) split document section into paragraphs if it doesnt fit.
    /// 
    /// Every chunk starts with its document title and heading. 
    /// A chunk is read alone, by a model that will not see the page it came from, so it has to say what it is about.
    /// </summary>
    public static class AiDocumentChunker
    {
        private const int _maxCharacters = 2000; //The hard cap of bge-small-en-v1.5. Counts the heading, which is part of what gets embedded.

        public static IReadOnlyList<AiDocumentChunk> Split(string markdown, string source)
        {
            if (string.IsNullOrWhiteSpace(markdown))
                return [];

            // Get all lines of the md file.
            List<string> lines = markdown.Replace("\r\n", "\n").Split('\n').ToList();

            // Find the title (single '#')
            string? heading = lines.FirstOrDefault(x => x.StartsWith("# ", StringComparison.Ordinal));
            string title = heading?[2..].Trim() ?? source; // substring from index 2 to the end.

            // Find all sections (double '##')
            List<Section> sections = SplitIntoSections(lines);

            List<AiDocumentChunk> chunks = new List<AiDocumentChunk>();
            foreach (Section section in sections)
            {
                if (section.Lines.All(x => x.Trim().Length == 0))
                    continue;

                // The heading repeats on every part, so the budget for the body is what is left.
                string header;
                if (section.Heading.Length == 0)
                    header = $"{title}\n\n";
                else
                    header = $"{title} > {section.Heading}\n\n";

                foreach (string part in Pack(section.Lines, _maxCharacters - header.Length))
                    chunks.Add(new AiDocumentChunk(source, title, section.Heading, header + part, chunks.Count));
            }

            return chunks;
        }


        // ================================================================
        // Private methods
        // ================================================================

        // Each "##" heading with the lines under it. Lines stay lines all the way down.
        private static List<Section> SplitIntoSections(List<string> lines)
        {
            List<Section> sections = new List<Section>();

            // The section being filled. A "##" closes it and opens the next.
            string openHeading = string.Empty;
            List<string> openBody = new List<string>();
            bool isInCodeFence = false;

            foreach (string line in lines)
            {
                // Toggle fence on/off.
                if (line.StartsWith("```", StringComparison.Ordinal))
                    isInCodeFence = !isInCodeFence;

                // A "##" inside a fence is code, not a heading.
                bool isHeading = !isInCodeFence && line.StartsWith("## ", StringComparison.Ordinal);

                if (isHeading)
                {
                    // Everything under the previous heading has been read by now, so it is complete.
                    sections.Add(new Section(openHeading, openBody));

                    openHeading = line[3..].Trim(); // substring from index 3 to the end.
                    openBody = new List<string>();
                    continue;
                }

                // The title line itself is carried on every chunk, so it is not part of any body.
                if (!isInCodeFence && line.StartsWith("# ", StringComparison.Ordinal))
                    continue;

                openBody.Add(line);
            }

            // Whatever was still open when the lines ran out.
            sections.Add(new Section(openHeading, openBody));

            return sections;
        }

   
        private static int Length(List<string> lines)
        {
            return lines.Sum(x => x.Length + 1);
        }

        // Paragraphs filled up to the budget. One too big for the budget is cut first, so nothing
        // gets through this larger than a chunk may be.
        private static IEnumerable<string> Pack(List<string> lines, int budget)
        {
            List<string> current = new List<string>();

            foreach (List<string> sectionParagraph in SplitSectionIntoParagraphs(lines))
            {
                foreach (List<string> paragraphPiece in CutParagraph(sectionParagraph, budget))
                {
                    if (current.Count > 0 && Length(current) + Length(paragraphPiece) > budget)
                    {
                        yield return Join(current);
                        current = new List<string>();
                    }

                    current.AddRange(paragraphPiece);
                    current.Add(string.Empty);
                }
            }

            if (Join(current).Length > 0)
                yield return Join(current);
        }

        // Lines back into the one string that gets embedded.
        private static string Join(List<string> lines)
        {
            return string.Join("\n", lines).Trim();
        }

        // A paragraph that fits comes back whole. One that does not - a long table, or a long
        // fenced example - is cut by line, because a line is a table row or a line of code either way.
        private static IEnumerable<List<string>> CutParagraph(List<string> paragraph, int budget)
        {

            // If it fits - return it.
            if (Length(paragraph) <= budget)
            {
                yield return paragraph;
                yield break;
            }

            List<string> current = new List<string>();
            foreach (string line in paragraph)
            {
                if (current.Count > 0 && Length(current) + line.Length + 1 > budget)
                {
                    yield return current;
                    current = new List<string>();
                }

                current.Add(line);
            }

            if (current.Count > 0)
                yield return current;
        }

        // Split text on blank likes.
        private static List<List<string>> SplitSectionIntoParagraphs(List<string> lines)
        {
            List<List<string>> blocks = new List<List<string>>();
            List<string> current = new List<string>();
            bool isInCodeFence = false;

            foreach (string line in lines)
            {
                // Toggle fence on/off.
                if (line.StartsWith("```", StringComparison.Ordinal))
                    isInCodeFence = !isInCodeFence;

                bool isBlankLine = !isInCodeFence && line.Trim().Length == 0;
                if (isBlankLine)
                {
                    if (current.Count > 0)
                    {
                        blocks.Add(current);
                        current = new List<string>();
                    }

                    continue;
                }

                current.Add(line);
            }

            if (current.Count > 0)
                blocks.Add(current);

            return blocks;
        }



        // ================================================================
        // Private types
        // ================================================================

        private sealed record Section(string Heading, List<string> Lines);
    }
}
