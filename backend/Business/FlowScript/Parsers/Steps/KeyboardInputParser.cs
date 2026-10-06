using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Binding;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class KeyboardInputParser : BaseStepParser
    {
        public KeyboardInputParser(IReadOnlyList<ScriptToken> tokens) : base(tokens)
        {
        }

        public override FlowStepSchemaBindng Parse()
        {
            // Press  <[ Ctrl+C ]> | Type  <[ text ]>
            ScriptKeyword keyword = ExtractStepKeyword(FlowStepTypeEnum.KEYBOARD_INPUT);

            ExpectKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            string text = ExtractText();
            ExpectKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            ExpectEnd();

            return new FlowStepSchemaBindng()
            {
                Step = new FlowStep()
                {
                    FlowStepType = FlowStepTypeEnum.KEYBOARD_INPUT,
                    KeyboardInputType = keyword.As<KeyboardInputTypeEnum>()!.Value,
                    KeyboardInputText = text
                },
            };
        }
    }
}
