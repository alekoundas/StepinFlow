using Business.FlowScript.Catalogs;
using Business.FlowScript.Models;
using Business.FlowScript.Models.Text;

namespace Business.FlowScript.Parsers.Header
{
    /// <summary>
    /// A bot the flow sends through, by the name it has in Settings. Only the name: the webhook is
    /// the credential, and the rest is the bot's, shared by every flow on the machine.
    /// </summary>
    internal sealed class DiscordBotParser : BaseParser<DiscordBotReference>
    {
        public DiscordBotParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override DiscordBotReference Parse()
        {
            // <[ Alerts ]>
            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            DiscordBotReference bot = new DiscordBotReference() { Name = ExtractText() };
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            ExpectEnd();

            return bot;
        }
    }
}
