using System.Drawing;

using Core.Enums;
using Core.Models.Business;

namespace Core.Models.Database
{
    /// <summary>
    /// One step, as it ran. Both what a worker returns and what gets written, so a step reading an
    /// earlier result reads this and nothing else.
    ///
    /// Ordered by Sequence and indented by Depth it reads as a tree:
    /// 1) Sequence is the order things happened in, from zero.
    /// 2) ParentSequence is the step this one ran inside - the branch it sits in, the loop pass it
    ///    belongs to, the search hit it was walked for.
    /// 3) Depth is how deep that is, kept rather than counted back up.
    ///
    /// A run holds only the ones still reachable - the walk drops each as it leaves the subtree it
    /// belongs to - so nothing piles up with time.
    /// </summary>
    public class ExecutionStep : BaseDbModel
    {
        // Where it sits in the run
        public int Sequence { get; set; }
        public int? ParentSequence { get; set; } //Not a foreign key - sequences are given out in memory, before anything is written
        public int Depth { get; set; }
        public int? LoopPass { get; set; }       //LOOP only: which pass, from zero


        // Copied and not read through FlowStep, so the row still reads right after a rename or a delete
        public string Name { get; set; } = string.Empty;
        public FlowStepTypeEnum FlowStepType { get; set; }

        public StepOutcomeEnum Outcome { get; set; }
        public DateTime StartedOn { get; set; }
        public int DurationMilliseconds { get; set; }

        public int? ResultLocationX { get; set; }
        public int? ResultLocationY { get; set; }

        /// <summary>FIND_ALL only: which hit this pass was, of how many.</summary>
        public int? MatchIndex { get; set; }
        public int? MatchCount { get; set; }


        // What came back
        public string? Value { get; set; }   //What the step produced, for a step below to read through FlowStepReferenceId
        public string? Message { get; set; } //Why it went the way it did, in a sentence. Mostly only worth setting on a failure


        // SYSTEM_COMMAND
        public int? ExitCode { get; set; }
        public string? Error { get; set; }
        public string? Command { get; set; }

        public string? ScreenshotFileName { get; set; }

        public int ExecutionId { get; set; }
        public Execution Execution { get; set; } = null!;

        public int? FlowStepId { get; set; }
        public FlowStep? FlowStep { get; set; }


        // What the step saw, carried to whatever writes it. Not a column - see ExecutionStepConfiguration
        public ExecutionScreenshot? Screenshot { get; set; }


        // The two location columns as one value. Not a column - see ExecutionStepConfiguration
        public Point? Location
        {
            get
            {
                if (ResultLocationX == null || ResultLocationY == null)
                    return null;

                return new Point(ResultLocationX.Value, ResultLocationY.Value);
            }
            set
            {
                ResultLocationX = value?.X;
                ResultLocationY = value?.Y;
            }
        }


        // ================================================================
        // Public methods
        // ================================================================

        public static ExecutionStep Success(Point? location = null, string? message = null)
        {
            return new ExecutionStep
            {
                Outcome = StepOutcomeEnum.SUCCESS,
                Location = location,
                Message = message,
            };
        }

        public static ExecutionStep Failure(string? message = null)
        {
            return new ExecutionStep
            {
                Outcome = StepOutcomeEnum.FAILURE,
                Message = message,
            };
        }
    }
}
