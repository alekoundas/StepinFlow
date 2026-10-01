using Business.FlowScript.Diagnostics;
using Business.FlowScript.Syntax;
using Business.FlowScript.Models.Text;
using Business.FlowScript.Models.Binding;

namespace Business.FlowScript.Scanner
{
    /// <summary>
    /// Read the .sflw and extract everything a Flow needs.
    ///
    /// The mirror of Printer.cs.
    ///
    /// An unreadable line is recorded and skipped rather than thrown.
    /// So one typo reports one error instead of hiding the rest of errors below it.
    /// </summary>
    public sealed class Scanner : IScanner
    {
        private enum ScriptSection
        {
            Flow,
            Areas,
            Points,
            Inputs,
            Templates,
            Steps,
        }


        // ================================================================
        // Public methods
        // ================================================================

        public FlowScriptSchema Read(string script)
        {
            FlowScriptSchema document = new FlowScriptSchema();
            IReadOnlyList<ScriptLine> lines = ScriptTokenizer.Read(script); // Script -> Lines + tokens.

            ScriptSection? currentSection = ScriptSection.Flow; // Script always starts with Flow fields.
            Dictionary<int, int> lastIndexAtIndent = new Dictionary<int, int>();
            List<string> pendingComments = new List<string>(); // CodeComment of the step bellow.
            int order = 0;

            // Use the Parsers to parse each line.
            foreach (ScriptLine line in lines)
            {
                if (line.Tokens.Count == 0)
                    continue;

                if (line.IsComment)
                {
                    pendingComments.Add(line.TextAfterHash);
                    continue;
                }


                // Current section.
                switch (line.Raw.Trim())
                {
                    case "Areas:":
                        currentSection = ScriptSection.Areas;
                        break;
                    case "Points:":
                        currentSection = ScriptSection.Points;
                        break;
                    case "Inputs:":
                        currentSection = ScriptSection.Inputs;
                        break;
                    case "Templates:":
                        currentSection = ScriptSection.Templates;
                        break;
                    case "Steps:":
                        currentSection = ScriptSection.Steps;
                        break;
                    default:
                        break;
                }


                if (currentSection == ScriptSection.Flow)
                {
                    FlowParser.ReadFlowField(document, line);
                    continue;
                }

                switch (currentSection)
                {
                    case ScriptSection.Areas:
                        FlowParser.ReadArea(document, line);
                        break;

                    case ScriptSection.Points:
                        FlowParser.ReadPoint(document, line);
                        break;

                    case ScriptSection.Inputs:
                        FlowParser.ReadCsvColumns(document, line);
                        break;

                    case ScriptSection.Templates:
                        FlowParser.ReadTemplate(document, line);
                        break;

                    case ScriptSection.Steps:
                        StepParser.Read(document, line, lastIndexAtIndent, pendingComments, ref order);
                        break;

                    default:
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(document.FlowName))
                document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.FLOW_LINE_MISSING, 1, 1, "The file has no \"Flow:\" line, so there is no flow to import."));

            return document;
        }


        // ================================================================
        // Private methods
        // ================================================================
    }
}
