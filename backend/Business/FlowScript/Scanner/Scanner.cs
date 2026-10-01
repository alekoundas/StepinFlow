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
            List<string> pendingComments = new List<string>(); // CodeComment of the step bellow.

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


                // Extract section from script and skip this line.
                string rawText = line.Raw.Trim();
                if (rawText == "Areas:")
                {
                    currentSection = ScriptSection.Areas;
                    continue;
                }
                else if (rawText == "Points:")
                {
                    currentSection = ScriptSection.Points;
                    continue;
                }
                else if (rawText == "Inputs:")
                {
                    currentSection = ScriptSection.Inputs;
                    continue;
                }
                else if (rawText == "Templates:")
                {
                    currentSection = ScriptSection.Templates;
                    continue;
                }
                else if (rawText == "Steps:")
                {
                    currentSection = ScriptSection.Steps;
                    continue;
                }

                // Call the parsers.
                switch (currentSection)
                {
                    case ScriptSection.Flow:
                        FlowParser.ReadFlowField(document, line);
                        break;

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
                        StepParser.Read(document, line, pendingComments);
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
