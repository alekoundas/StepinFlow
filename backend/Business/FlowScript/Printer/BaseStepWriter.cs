using Business.FlowScript.Catalogs;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Text
{
    /// <summary>
    /// Reusable clauses FlowSteps writes.
    /// </summary>
    internal abstract class BaseStepWriter : BaseWriter
    {
        protected BaseStepWriter(FlowStep step)
        {
            Step = step;
        }

        protected FlowStep Step { get; }


        // ================================================================
        // Protected methods
        // ================================================================

        /// <summary>
        /// Write a FlowPoint.
        /// </summary>
        protected void WriteTarget(FlowPoint? point, FlowStep? reference)
        {
            // point <[ X ]> | <[ a step ]> | nowhere - neither, so a flow saved with that error still round-trips.

            if (point != null)
            {
                WriteKeyword(ScriptSymbolEnum.POINT);
                WriteQuote(point.Name);
                return;
            }

            if (reference != null)
            {
                WriteQuote(reference.Name);
                return;
            }

            WriteKeyword(ScriptSymbolEnum.NOWHERE);
        }

        /// <summary>
        /// Write a FlowArea.
        /// </summary>
        protected void WriteArea()
        {
            //    in <[ area ]>, or nothing.

            if (Step.FlowArea == null)
                return;

            WriteGap();
            WriteKeyword(ScriptSymbolEnum.IN);
            WriteQuote(Step.FlowArea.Name);
        }

        /// <summary>
        /// Write a Condition.
        /// </summary>
        protected void WriteCondition()
        {
            // is <[ x ]>, between <[ 1 ]> and <[ 9 ]>, is empty, ... or nothing.

            if (Step.ConditionType == null)
                return;

            WriteKeyword(Step.ConditionType.Value);

            switch (Step.ConditionType)
            {
                case ConditionTypeEnum.IS_EMPTY:
                case ConditionTypeEnum.IS_NOT_EMPTY:
                    break;

                case ConditionTypeEnum.BETWEEN:
                    WriteQuote(Step.ConditionText);
                    WriteKeyword(ScriptSymbolEnum.AND);
                    WriteQuote(Step.ConditionTextEnd);
                    break;

                default:
                    WriteQuote(Step.ConditionText);
                    break;
            }
        }

        /// <summary>
        /// Write Timeout.
        /// </summary>
        protected void WriteWaiting()
        {
            //    timeout 10000ms |    no timeout - only a search that waits has either.

            bool isWaiting = Step.SearchMode == SearchModeEnum.WAIT_UNTIL_FOUND || Step.SearchMode == SearchModeEnum.WAIT_UNTIL_NOT_FOUND;
            if (!isWaiting)
                return;

            WriteGap();

            // Zero is "for ever", and writing "timeout 0ms" would read as "give up at once".
            if (Step.TimeoutMilliseconds > 0)
            {
                WriteKeyword(ScriptSymbolEnum.TIMEOUT);
                WriteMilliseconds(Step.TimeoutMilliseconds);
            }
            else
            {
                WriteKeyword(ScriptSymbolEnum.NO_TIMEOUT);
            }
        }
    }
}
