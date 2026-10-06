using Business.FlowScript.Catalogs;
using Business.FlowScript.Diagnostics;
using Business.FlowScript.Lexing;
using Business.FlowScript.Models.Binding;
using Business.FlowScript.Models.Text;
using Business.FlowScript.Parsers;
using Business.FlowScript.Syntax;

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
            }

            parser.Finish();

            if (string.IsNullOrWhiteSpace(flowScriptSchema.FlowName))
                flowScriptSchema.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.FLOW_LINE_MISSING, 1, 1, $"The file has no \"{SyntaxFacts.Keyword(ScriptSymbolEnum.FLOWFIELD_NAME)}\" line, so there is no flow to import."));

            return flowScriptSchema;
        }
    }
}
