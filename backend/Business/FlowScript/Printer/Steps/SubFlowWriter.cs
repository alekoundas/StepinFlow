using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Text.Steps
{
    internal sealed class SubFlowWriter : BaseStepWriter
    {
        private readonly string _path;

        public SubFlowWriter(FlowStep step, string path) : base(step)
        {
            _path = path;
        }

        protected override void Compose()
        {
            // Sub Flow  <[ path ]>
            WriteKeyword(FlowStepTypeEnum.SUB_FLOW);
            WriteGapUntil(KEYWORD_GAP_UNTIL);
            WriteQuote(_path);
        }
    }
}
