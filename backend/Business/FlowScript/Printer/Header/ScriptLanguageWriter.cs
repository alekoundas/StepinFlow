using Business.FlowScript.Catalogs;

namespace Business.FlowScript.Text.Header
{
    internal sealed class ScriptLanguageWriter : BaseWriter
    {
        protected override void Compose()
        {
            // Script Language: v1
            WriteKeyword(ScriptSymbolEnum.FLOWFIELD_SCRIPT_LANGUAGE);
            WriteText(ScriptLanguage.VERSION_PREFIX + ScriptLanguage.CURRENT_VERSION);
        }
    }
}
