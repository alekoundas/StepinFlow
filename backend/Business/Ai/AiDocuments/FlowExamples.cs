using Business.FlowScript.Models;
using Business.FlowScript.Scanner;
using Core.Enums;
using Core.Models.Database;

namespace Business.Ai.AiDocuments
{
    /// <summary>
    /// Real flows, finished by hand, that go with a script to the model so it sees how a finished
    /// flow reads - its names, comments and branches - rather than only being told. Each is one of
    /// the AI documents in Examples, so the assistant searches them too, and its finished script is
    /// the block fenced as <c>```sflw</c>.
    ///
    /// Picked by a rule rather than by meaning: with a handful to choose from, what the flow works
    /// in says which fits, and an example only teaches whole, never as a chunk.
    /// </summary>
    public static class FlowExamples
    {
        public const string GAME = "example-firestone.md";
        public const string WEBSITE = "example-crude-oil-price.md";

        // A block fenced as ```sflw recorded is the same flow before it was fixed.
        public const string FINISHED = "sflw";

        private const string FENCE = "```";

        // Enough to show the pattern; more is only longer to read on a CPU.
        private const int MAXIMUM_EXAMPLE_SEARCHES = 8;


        // ================================================================
        // Public methods
        // ================================================================

        /// <summary>
        /// The game's for a flow whose window stretches its contents, as a game does; the
        /// website's for any other, which reads text and keeps its size as a browser does.
        /// </summary>
        public static string DocumentFor(FlowArea? mainArea)
        {
            if (mainArea != null && mainArea.Type == FlowAreaTypeEnum.APPLICATION && mainArea.ScalesWith == ScalesWithEnum.AREA)
                return GAME;

            return WEBSITE;
        }

        /// <summary>What goes with the instruction: the example, and what it is there for.</summary>
        public static string ForRequest(FlowArea? mainArea)
        {
            return ForRequest(DocumentFor(mainArea));
        }

        /// <summary>What goes with the instruction: that example, and what it is there for.</summary>
        public static string ForRequest(string document)
        {
            string script = ScriptOf(AiDocumentsReader.ReadDocument(document), FINISHED);

            return $"""
                An example: a finished flow a person built by hand for the same kind of window, to show how
                names, comments and branches read once a flow is done. It is another flow, not the one to
                revise: never take its steps, areas, points or templates into this one.

                {FENCE}
                {script}
                {FENCE}
                """;
        }

        /// <summary>
        /// How a finished flow names its searches, says what each is for and what a failed wait
        /// says: what a recording's words are written like, which a small model copies better than
        /// it follows a description of it. Another flow's, so the model is told to take nothing of it.
        /// </summary>
        public static string WordsOf(string document)
        {
            FlowScriptSchema schema = new Scanner().Read(ScriptOf(AiDocumentsReader.ReadDocument(document), FINISHED));

            List<string> lines = [];
            foreach (FlowStep search in schema.Steps.Where(x => x.FlowStepType == FlowStepTypeEnum.SEARCH_IMAGE || x.FlowStepType == FlowStepTypeEnum.SEARCH_TEXT).Take(MAXIMUM_EXAMPLE_SEARCHES))
            {
                string line = $"- {search.Name}";

                string comment = string.Join(' ', search.CodeComment.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
                if (comment.Length > 0)
                    line += $": {comment}";

                FlowStep? failure = schema.Steps.FirstOrDefault(x => x.ParentFlowStep == search && x.FlowStepType == FlowStepTypeEnum.FAILURE);
                FlowStep? end = schema.Steps.FirstOrDefault(x => failure != null && x.ParentFlowStep == failure && x.FlowStepType == FlowStepTypeEnum.END_EXECUTION);
                if (end != null && end.Message.Length > 0)
                    line += $" Not found: {end.Message}";

                lines.Add(line);
            }

            if (lines.Count == 0)
                return string.Empty;

            return $"""
                How a finished flow people built by hand names its searches, says what each is for, and
                what it says when one is never found. It is another flow, for another task: write this one's
                words the same way, never its names.

                {string.Join("\n", lines)}
                """;
        }

        /// <summary>The lines of the first block fenced as this, without its fences.</summary>
        public static string ScriptOf(string document, string fencedAs)
        {
            List<string> lines = document.Replace("\r\n", "\n").Split('\n').ToList();

            int start = lines.FindIndex(x => x.TrimEnd() == FENCE + fencedAs);
            if (start < 0)
                throw new InvalidOperationException($"The example has no block fenced as {fencedAs}.");

            int end = lines.FindIndex(start + 1, x => x.TrimEnd() == FENCE);
            if (end < 0)
                throw new InvalidOperationException($"The example's {fencedAs} block is never closed.");

            return string.Join("\n", lines.Skip(start + 1).Take(end - start - 1));
        }
    }
}
