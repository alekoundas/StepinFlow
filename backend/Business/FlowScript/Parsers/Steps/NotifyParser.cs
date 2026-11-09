using Business.FlowScript.Catalogs;
using Business.FlowScript.Models;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class NotifyParser : BaseStepParser
    {
        public NotifyParser(IReadOnlyList<ScriptToken> tokens, ScriptScope scope) : base(tokens, scope)
        {
        }

        public override FlowStep Parse()
        {
            // Notify  <[ message ]>  <[ its next line ]>   [through <[ a bot ]>]   [with result of <[ a step ]>]
            ExpectKeyword(FlowStepTypeEnum.NOTIFY);

            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            List<string> lines = [ExtractText()];
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            while (ExpectOptionalKeyword(ScriptSymbolEnum.QUOTE_OPEN))
            {
                lines.Add(ExtractText());
                ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);
            }

            FlowStep step = new FlowStep() { FlowStepType = FlowStepTypeEnum.NOTIFY, Message = string.Join("\n", lines) };

            // Held on the step only until the line parser moves it into the schema, for the import to find.
            // An empty quote is a step saved with no bot, which the validator reports.
            if (ExpectOptionalKeyword(ScriptSymbolEnum.THROUGH))
            {
                ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
                ScriptToken at = CurrentToken;
                string name = ExtractText();
                ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

                DiscordBotReference? bot = null;
                if (name.Length > 0)
                    bot = Scope.DiscordBot(name, at);

                if (bot != null)
                    step.DiscordBot = new DiscordBot() { Name = bot.Name };
            }

            if (ExpectOptionalKeyword(ScriptSymbolEnum.WITH_RESULT_OF))
                step.FlowStepReference = ExtractStepReference();

            ExpectEnd();

            return step;
        }
    }
}
