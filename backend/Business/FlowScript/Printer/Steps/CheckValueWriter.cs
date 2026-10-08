using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Text.Steps
{
    internal sealed class CheckValueWriter : BaseStepWriter
    {
        public CheckValueWriter(FlowStep step) : base(step)
        {
        }

        protected override void Compose()
        {
            // Check Value  <[ name ]>   <[ {{Step name}} ]> > <[ 100 ]>
            WriteKeyword(FlowStepTypeEnum.CHECK_VALUE);
            WriteGapUntil(KEYWORD_GAP_UNTIL);
            WriteQuote(Step.Name);
            WriteGap();

            // The step whose result is checked, as the variable that reads it.
            if (Step.FlowStepReference != null)
                WriteQuote("{{" + Step.FlowStepReference.Name + "}}");
            else
                WriteQuote(string.Empty);

            WriteCondition();
        }
    }
}
