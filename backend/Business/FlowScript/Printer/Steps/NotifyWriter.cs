using Business.FlowScript.Catalogs;
using Business.FlowScript.Models;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Text.Steps
{
    internal sealed class NotifyWriter : BaseStepWriter
    {
        private readonly DiscordBotReference? _discordBot;

        public NotifyWriter(FlowStep step, DiscordBotReference? discordBot) : base(step)
        {
            _discordBot = discordBot;
        }

        protected override void Compose()
        {
            // Notify  <[ message ]>  <[ its next line ]>   [through <[ a bot ]>]   [with result of <[ a step ]>]
            WriteKeyword(FlowStepTypeEnum.NOTIFY);
            WriteGapUntil(KEYWORD_GAP_UNTIL);
            WriteMessage();

            if (_discordBot != null)
            {
                WriteGap();
                WriteKeyword(ScriptSymbolEnum.THROUGH);
                WriteQuote(_discordBot.Name);
            }

            if (Step.FlowStepReference == null)
                return;

            WriteGap();
            WriteKeyword(ScriptSymbolEnum.WITH_RESULT_OF);
            WriteQuote(Step.FlowStepReference.Name);
        }


        // ================================================================
        // Private methods
        // ================================================================

        // A quote ends with its line, so a message of several lines is a quote for each, closer
        // together than a clause so they read as one.
        private void WriteMessage()
        {
            string[] lines = Step.Message.ReplaceLineEndings("\n").Split('\n');

            WriteQuote(lines[0]);

            foreach (string line in lines.Skip(1))
            {
                WriteGap(2);
                WriteQuote(line);
            }
        }
    }
}
