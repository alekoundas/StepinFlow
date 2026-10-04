using Business.FlowScript.Catalogs;
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
        // ================================================================
        // Public methods
        // ================================================================

        public FlowScriptSchema Read(string script)
        {
            FlowScriptSchema document = new FlowScriptSchema();
            IReadOnlyList<ScriptLine> lines = ScriptTokenizer.Read(script); // Script -> Lines + tokens.

            ScriptSymbolEnum? currentSection = null; // Null until the first header: the Flow fields come before any.
            List<string> pendingComments = new List<string>(); // CodeComment of the step bellow.

            // Use the Parsers to parse each line.
            foreach (ScriptLine line in lines)
            {
                if (line.Tokens.Count == 0)
                    continue;

                // Extract CodeComment.
                if (SyntaxFacts.IsComment(line))
                {
                    pendingComments.Add(line.RawAfter(SyntaxFacts.Symbol(ScriptSymbolEnum.COMMENT)));
                    continue;
                }

                // Extract section from script and skip this line.
                ScriptSymbolEnum? header = SyntaxFacts.ReadSectionHeader(line);
                if (header != null)
                {
                    currentSection = header;
                    continue;
                }

                // Call the parsers.
                switch (currentSection)
                {
                    case null:
                        FlowParser.ReadFlowField(document, line);
                        break;

                    case ScriptSymbolEnum.AREAS:
                        FlowParser.ReadArea(document, line);
                        break;

                    case ScriptSymbolEnum.POINTS:
                        FlowParser.ReadPoint(document, line);
                        break;

                    case ScriptSymbolEnum.CSV_COLUMNS:
                        FlowParser.ReadCsvColumns(document, line);
                        break;

                    case ScriptSymbolEnum.TEMPLATES:
                        FlowParser.ReadTemplate(document, line);
                        break;

                    case ScriptSymbolEnum.STEPS:
                        StepParser.Read(document, line, pendingComments);
                        break;

                    default:
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(document.FlowName))
                document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.FLOW_LINE_MISSING, 1, 1, $"The file has no \"{SyntaxFacts.Symbol(ScriptSymbolEnum.FLOWFIELD_NAME)}\" line, so there is no flow to import."));

            return document;
        }


        // ================================================================
        // Private methods
        // ================================================================
    }
}
