using Business.FlowScript.Catalogs;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Text.Steps
{
    internal sealed class NotifyWriter : BaseStepWriter
    {
        public NotifyWriter(FlowStep step) : base(step)
        {
        }

        protected override void Compose()
        {
            // Notify  <[ message ]>  <[ its next line ]>   [with result of <[ a step ]>]
            WriteKeyword(FlowStepTypeEnum.NOTIFY);
            WriteGapUntil(KEYWORD_GAP_UNTIL);
            WriteMessage();

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
