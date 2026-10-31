using Business.FlowScript.Diagnostics;
using Business.FlowScript.Models;
using Core.Enums;
using Core.Helpers;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Recording.Generation
{
    /// <summary>
    /// The checks a model added to a recording, which the tester keeps or drops on the Check page.
    /// Told by comparing, not by anything the model wrote: a search nothing moves to is a check
    /// rather than a click's, and one the script before had - the same words, or the same
    /// templates - is the tester's own. One for words is held to the screen, since words no
    /// screenshot of the recording showed would fail the first execution.
    ///
    /// Dropped, a check goes with its Failure branch and what its Success branch holds takes its
    /// place, so the recorded actions a model nested under it stay.
    /// </summary>
    public static class ProposedChecks
    {
        /// <summary>
        /// The checks after has that before did not, in script order. The undecided ones, by name,
        /// are not counted as before's, so a revision still offers them.
        /// </summary>
        public static List<FlowStep> Find(FlowScriptSchema before, FlowScriptSchema after, IReadOnlyCollection<string> undecided)
        {
            HashSet<string> stillProposed = new HashSet<string>(undecided, StringComparer.OrdinalIgnoreCase);
            Dictionary<string, int> known = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (FlowStep check in ChecksOf(before).Where(x => !stillProposed.Contains(x.Name)))
            {
                string key = KeyOf(check);
                known.TryGetValue(key, out int count);
                known[key] = count + 1;
            }

            List<FlowStep> added = [];
            foreach (FlowStep check in ChecksOf(after))
            {
                string key = KeyOf(check);
                if (known.TryGetValue(key, out int count) && count > 0)
                    known[key] = count - 1;
                else
                    added.Add(check);
            }

            return added;
        }

        /// <summary>The check by that name, as long as nothing moves to it.</summary>
        public static FlowStep? Named(FlowScriptSchema schema, string name)
        {
            return ChecksOf(schema).FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Whether a line OCR read on the recording's screenshots satisfies the check's words, as an
        /// execution would test them. Null for a check of a picture, or of words an execution fills in.
        /// </summary>
        public static bool? IsReadOnScreen(FlowStep check, IReadOnlyList<string> lines)
        {
            if (check.FlowStepType != FlowStepTypeEnum.SEARCH_TEXT)
                return null;

            if (VariableTranslator.Names(check.ConditionText).Count > 0 || VariableTranslator.Names(check.ConditionTextEnd).Count > 0)
                return null;

            return lines.Any(x => ConditionEvaluatorHelper.IsSatisfied(RegexHelper.Extract(x, check.ResultExtractPattern), check.ConditionType, check.ConditionText, check.ConditionTextEnd));
        }

        /// <summary>A refusal for each added check whose words no screenshot of the recording showed.</summary>
        public static List<Diagnostic> Unseen(IEnumerable<FlowStep> added, IReadOnlyList<string> lines)
        {
            return added
                .Where(x => IsReadOnScreen(x, lines) == false)
                .Select(x => Diagnostic.Error(DiagnosticCodeEnum.CHECK_TEXT_UNSEEN, 1, 1, $"<[ {x.Name} ]> looks for <[ {x.ConditionText} ]>, which no screenshot of the recording showed, so it would fail the first execution. Add a check only for words a screen: note shows, exactly as it shows them, without the … that ends a line cut short."))
                .ToList();
        }

        public static List<ProposedCheckDto> ToDtos(IEnumerable<FlowStep> checks, string script, IReadOnlyList<string> lines)
        {
            string[] scriptLines = script.Split('\n');
            List<ProposedCheckDto> proposed = [];

            foreach (FlowStep check in checks)
            {
                int line = LineOf(scriptLines, check.Name);

                proposed.Add(new ProposedCheckDto
                {
                    Name = check.Name,
                    Line = line,
                    Text = line > 0 ? scriptLines[line - 1].Trim() : string.Empty,
                    Under = UnderOf(check),
                    IsReadOnScreen = IsReadOnScreen(check, lines),
                });
            }

            return proposed;
        }

        /// <summary>Takes the check out of the flow. Null when it did, else why it could not.</summary>
        public static string? Drop(FlowScriptSchema flow, string name)
        {
            FlowStep? check = Named(flow, name);
            if (check == null)
                return $"The flow no longer has a check called \"{name}\".";

            List<FlowStep> branches = ChildrenOf(flow, check).ToList();
            FlowStep? success = branches.FirstOrDefault(x => x.FlowStepType == FlowStepTypeEnum.SUCCESS);
            FlowStep? failure = branches.FirstOrDefault(x => x.FlowStepType == FlowStepTypeEnum.FAILURE);

            HashSet<FlowStep> going = [check, .. branches];
            if (failure != null)
                going.UnionWith(DescendantsOf(flow, failure));

            FlowStep? reader = flow.Steps.FirstOrDefault(x => !going.Contains(x) && Uses(x, check));
            if (reader != null)
                return $"\"{reader.Name}\" uses what \"{check.Name}\" found, so it cannot be dropped here. Change that step in the editor first.";

            List<FlowStep> lifted = [];
            if (success != null)
                lifted = ChildrenOf(flow, success).ToList();

            List<FlowStep> siblings = ChildrenOf(flow, check.ParentFlowStep).ToList();
            int at = siblings.IndexOf(check);
            siblings.RemoveAt(at);
            siblings.InsertRange(at, lifted);

            for (int i = 0; i < siblings.Count; i++)
            {
                siblings[i].ParentFlowStep = check.ParentFlowStep;
                siblings[i].OrderNumber = i;
            }

            flow.Steps.RemoveAll(going.Contains);
            return null;
        }


        // ================================================================
        // Private methods
        // ================================================================

        // Searches nothing moves to or drags from or to: a click's search is moved to.
        private static IEnumerable<FlowStep> ChecksOf(FlowScriptSchema schema)
        {
            HashSet<FlowStep> moved = schema.Steps
                .Where(x => x.FlowStepType == FlowStepTypeEnum.CURSOR_RELOCATE || x.FlowStepType == FlowStepTypeEnum.CURSOR_DRAG)
                .SelectMany(x => new[] { x.FlowStepReference, x.FlowStepReferenceEnd })
                .OfType<FlowStep>()
                .ToHashSet();

            return schema.Steps.Where(x => (x.FlowStepType == FlowStepTypeEnum.SEARCH_TEXT || x.FlowStepType == FlowStepTypeEnum.SEARCH_IMAGE) && !moved.Contains(x));
        }

        // Words by what they say, however they are spaced; pictures by which templates.
        private static string KeyOf(FlowStep check)
        {
            if (check.FlowStepType == FlowStepTypeEnum.SEARCH_TEXT)
            {
                string words = string.Join(' ', check.ConditionText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
                return $"text {check.ConditionType} {words}";
            }

            IEnumerable<string> templates = check.FlowStepTemplates.Select(x => x.FlowTemplate.Name).Order(StringComparer.OrdinalIgnoreCase);
            return $"image {string.Join(' ', templates)}";
        }

        private static bool Uses(FlowStep step, FlowStep check)
        {
            if (step.FlowStepReference == check || step.FlowStepReferenceEnd == check)
                return true;

            return VariableTranslator.VariableBearingText(step)
                .SelectMany(VariableTranslator.Names)
                .Contains(check.Name, StringComparer.OrdinalIgnoreCase);
        }

        // The step whose Success or Failure branch holds it.
        private static string UnderOf(FlowStep check)
        {
            FlowStep? parent = check.ParentFlowStep;
            if (parent != null && (parent.FlowStepType == FlowStepTypeEnum.SUCCESS || parent.FlowStepType == FlowStepTypeEnum.FAILURE))
                parent = parent.ParentFlowStep;

            return parent?.Name ?? string.Empty;
        }

        // The line that names it first: a name is declared above its first use.
        private static int LineOf(string[] lines, string name)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith('#'))
                    continue;

                int open = line.IndexOf("<[", StringComparison.Ordinal);
                int close = line.IndexOf("]>", StringComparison.Ordinal);
                if (open < 0 || close < open)
                    continue;

                if (string.Equals(line[(open + 2)..close].Trim(), name, StringComparison.OrdinalIgnoreCase))
                    return i + 1;
            }

            return 0;
        }

        private static IEnumerable<FlowStep> ChildrenOf(FlowScriptSchema schema, FlowStep? parent)
        {
            return schema.Steps
                .Where(x => x.ParentFlowStep == parent)
                .OrderBy(x => x.OrderNumber);
        }

        private static IEnumerable<FlowStep> DescendantsOf(FlowScriptSchema schema, FlowStep parent)
        {
            foreach (FlowStep child in ChildrenOf(schema, parent))
            {
                yield return child;

                foreach (FlowStep descendant in DescendantsOf(schema, child))
                    yield return descendant;
            }
        }
    }
}
