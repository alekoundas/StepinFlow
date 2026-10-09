using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class KeyboardInputParser : BaseStepParser
    {
        public KeyboardInputParser(IReadOnlyList<ScriptToken> tokens, ScriptScope scope) : base(tokens, scope)
        {
        }

        public override FlowStep Parse()
        {
            // Press  <[ Ctrl+C ]>   [hold | release] | Type  <[ text ]>
            ScriptKeyword keyword = ExtractStepKeyword(FlowStepTypeEnum.KEYBOARD_INPUT);
            KeyboardInputTypeEnum type = keyword.As<KeyboardInputTypeEnum>()!.Value;

            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            string text = ExtractText();
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            FlowStep step = new FlowStep()
            {
                FlowStepType = FlowStepTypeEnum.KEYBOARD_INPUT,
                KeyboardInputType = type,
                KeyboardInputText = text
            };

            // Keys are held or released, never typed text. A press when left out.
            if (type == KeyboardInputTypeEnum.COMBINATION)
                step.KeyboardKeyActionType = ExtractOptionalKeyword<KeyboardKeyActionTypeEnum>() ?? KeyboardKeyActionTypeEnum.PRESS;

            ExpectEnd();

            return step;
        }
    }
}
