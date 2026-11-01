using Business.FlowScript.Catalogs;
using Business.FlowScript.Diagnostics;
using Business.FlowScript.Lexing;
using Business.FlowScript.Models;
using Business.FlowScript.Models.Text;
using Business.FlowScript.Parsers;

namespace Business.FlowScript.Scanner
{
    /// <summary>
    /// Read the .sflw and extract the schema a Flow needs.
    ///
    /// The mirror of Printer.cs.
    ///
    /// An unreadable line is recorded and skipped. A line error shouldnt hide any errors bellow.
    /// </summary>
    public sealed class Scanner : IScanner
    {
        // ================================================================
        // Public methods
        // ================================================================

        public FlowScriptSchema Read(string script)
        {
            FlowScriptSchema flowScriptSchema = new FlowScriptSchema();
            ScriptLineParser parser = new ScriptLineParser(flowScriptSchema);

            // Script -> lines -> tokens -> the schema.
            foreach (ScriptLine line in ScriptLineSplitter.Split(script))
            {
                line.Tokens = ScriptTokenizer.Tokenize(line);

                // A line that stops making sense throws at that token, and the rest of the file is still read.
                try
                {
                    parser.Parse(line);
                }
                catch (ScriptSyntaxException error)
                {
                    flowScriptSchema.Diagnostics.Add(error.Diagnostic);
                }

                // The rest would be read as errors, none of which says the one thing that matters.
                if (flowScriptSchema.ScriptLanguageVersion > ScriptLanguage.CURRENT_VERSION)
                {
                    flowScriptSchema.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.SCRIPT_LANGUAGE_NEWER, line.Number, 1,
                        $"The file is in script language {ScriptLanguage.VERSION_PREFIX}{flowScriptSchema.ScriptLanguageVersion}, and this StepinFlow reads up to {ScriptLanguage.VERSION_PREFIX}{ScriptLanguage.CURRENT_VERSION}. Update StepinFlow to open it."));

                    return flowScriptSchema;
                }
            }

            parser.Finish();

            // A name already refused is not also missing.
            bool isNameRefused = flowScriptSchema.Diagnostics.Any(x => x.Code == DiagnosticCodeEnum.FLOW_NAME_INVALID);
            if (string.IsNullOrWhiteSpace(flowScriptSchema.Flow.Name) && !isNameRefused)
                flowScriptSchema.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.FLOW_LINE_MISSING, 1, 1, $"The file has no \"{ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.FLOWFIELD_NAME)}\" line, so there is no flow to import."));

            return flowScriptSchema;
        }
    }
}
