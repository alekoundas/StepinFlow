using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers.Steps
{
    internal sealed class WindowCloseParser : BaseStepParser
    {
        public WindowCloseParser(IReadOnlyList<ScriptToken> tokens, ScriptScope scope) : base(tokens, scope)
        {
        }

        public override FlowStep Parse()
        {
            // Close Window  <[ name ]>   <[ Notepad ]>   timeout 10000ms   then force
            ExpectKeyword(FlowStepTypeEnum.WINDOW_CLOSE);
            FlowStep step = new FlowStep() { FlowStepType = FlowStepTypeEnum.WINDOW_CLOSE };

            step.Name = ExtractName();
            step.FlowArea = ExtractWindowArea();

            if (ExpectOptionalKeyword(ScriptSymbolEnum.TIMEOUT))
                step.TimeoutMilliseconds = ExtractMilliseconds();

            step.IsCloseForced = ExpectOptionalKeyword(ScriptSymbolEnum.THEN_FORCE);

            ExpectEnd();

            return step;
        }
    }
}
