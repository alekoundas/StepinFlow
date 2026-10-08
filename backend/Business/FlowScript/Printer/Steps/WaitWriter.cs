using Business.FlowScript.Catalogs;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Text.Steps
{
    internal sealed class WaitWriter : BaseStepWriter
    {
        public WaitWriter(FlowStep step) : base(step)
        {
        }

        protected override void Compose()
        {
            // Wait  800ms   [to 1200ms]
            WriteKeyword(FlowStepTypeEnum.WAIT);
            WriteGapUntil(KEYWORD_GAP_UNTIL);
            WriteMilliseconds(Step.WaitForMilliseconds);

            if (Step.WaitForMillisecondsMax > Step.WaitForMilliseconds)
            {
                WriteKeyword(ScriptSymbolEnum.TO);
                WriteMilliseconds(Step.WaitForMillisecondsMax);
            }
        }
    }
}
