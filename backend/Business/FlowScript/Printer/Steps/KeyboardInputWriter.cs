using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Text.Steps
{
    internal sealed class KeyboardInputWriter : BaseStepWriter
    {
        public KeyboardInputWriter(FlowStep step) : base(step)
        {
        }

        protected override void Compose()
        {
            // Press  <[ Ctrl+C ]> | Type  <[ text ]>
            KeyboardInputTypeEnum type = KeyboardInputTypeEnum.TEXT;
            if (Step.KeyboardInputType == KeyboardInputTypeEnum.COMBINATION)
                type = KeyboardInputTypeEnum.COMBINATION;

            WriteKeyword(FlowStepTypeEnum.KEYBOARD_INPUT, type);
            WriteGapUntil(KEYWORD_GAP_UNTIL);
            WriteQuote(Step.KeyboardInputText);
        }
    }
}
