using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;

namespace Business.FlowScript.Parsers.Header
{
    /// <summary>
    /// The version of the script language the file is written in.
    /// </summary>
    internal sealed class ScriptLanguageParser : BaseParser<int>
    {
        public ScriptLanguageParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override int Parse()
        {
            // Script Language: v1
            ExpectKeyword(ScriptSymbolEnum.FLOWFIELD_SCRIPT_LANGUAGE);

            int version = ExtractVersion();

            ExpectEnd();

            return version;
        }
    }
}
