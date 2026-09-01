using System.Drawing;

using Core.Enums;
using Core.Helpers;
using Core.Models.Business;
using Core.Models.Database;

namespace Business.Services.ExecutionService
{
    /// <summary>
    /// Decides what runs next.
    ///
    /// A stack is used for pending steps to be executed:
    /// 1) stack has on top the next step to execute.
    /// 2) Execution stops if stack is empty.
    /// 3) If current step has a sibling, add that to the stack.
    /// 4) if step also has children, add the siblinng and then the first child - so child is executed next(allong with its siblings or childrens) and execution continues with sibling added earlier.
    ///
    /// Loops need nothing special either: a loop's next sibling is itself, so running it again pushes its children again.
    /// </summary>
    public sealed class ExecutionFlowWalker
    {
        private const int _maxSubFlowDepth = 50;// A flow calling a flow calling a flow - count

        private readonly IExecutionCacheService _cache;
        private readonly Stack<PendingStep> _executionStack = new Stack<PendingStep>();                   // Its THE stack
        private readonly Dictionary<int, int> _loopPasses = new Dictionary<int, int>();                   //How many times each loop has come round, keyed by the LOOP step
        private readonly Dictionary<int, PendingMatches> _pendingMatches = new Dictionary<int, PendingMatches>();//Which hit a FIND_ALL search is on, keyed by the search step
        private readonly Dictionary<int, int> _depthByStepId = new Dictionary<int, int>();                //Where each recorded result sits, so leaving a subtree can drop it
        private readonly Dictionary<int, int> _sequenceByDepth = new Dictionary<int, int>();               //The step above on the path being walked, so a child can point at its parent
        private readonly List<ExecutionStep> _matchRepeats = new List<ExecutionStep>();                    //Hits served without executing anything, waiting for the engine to collect them

        private int _subFlowDepth;
        private int _nextSequence;

        public ExecutionFlowWalker(IExecutionCacheService cache)
        {
            _cache = cache;
        }

        public int Depth { get; private set; }


        // ================================================================
        // Public methods
        // ================================================================

        /// <summary>
        /// Fills in everything a worker's result cannot know: which step it was, and where that sits
        /// in the run. The walk is the only thing that knows the second part.
        /// </summary>
        public void PlaceInRun(ExecutionStep executionStep, FlowStep flowStep)
        {
            PlaceInRun(executionStep, flowStep, Depth);
        }

        /// <summary>
        /// Hits that were served without anything being executed - a FIND_ALL search handing out its
        /// second and third match. Nobody ran them, so the engine never saw them come back from
        /// Next, and it collects them here to record and broadcast alongside the rest.
        /// </summary>
        public IReadOnlyList<ExecutionStep> TakeMatchRepeats()
        {
            if (_matchRepeats.Count == 0)
                return [];

            List<ExecutionStep> taken = new List<ExecutionStep>(_matchRepeats);
            _matchRepeats.Clear();

            return taken;
        }

        public FlowStep? Start(int flowId)
        {
            FlowStep? first = FirstChildOfFlow(flowId);
            if (first == null)
                return null;

            _executionStack.Push(new PendingStep(first, 0, 0));
            return Pop();
        }

        public FlowStep? Next(FlowStep step, ExecutionStep result)
        {
            PushContinuation(step, result);
            PushChild(step, result);

            return Pop();
        }


        // ================================================================
        // Private methods
        // ================================================================

        private void PushContinuation(FlowStep step, ExecutionStep result)
        {
            if (step.FlowStepType == FlowStepTypeEnum.GO_TO)
            {
                bool isPushed = PushGoToTarget(step);
                if (isPushed)
                    return;
            }
            if (step.FlowStepType == FlowStepTypeEnum.LOOP && HasAnotherLoop(step))
            {
                _executionStack.Push(new PendingStep(step, Depth, _subFlowDepth));
                return;
            }

            if (OpenPendingMatches(step, result))
            {
                _executionStack.Push(new PendingStep(step, Depth, _subFlowDepth, isMatchRepeat: true));
                return;
            }

            FlowStep? sibling = NextSiblingOf(step);
            if (sibling != null)
                _executionStack.Push(new PendingStep(sibling, Depth, _subFlowDepth));
        }

        private void PushChild(FlowStep step, ExecutionStep result)
        {
            if (step.FlowStepType == FlowStepTypeEnum.SUB_FLOW)
            {
                PushSubFlow(step);
                return;
            }

            if (step.FlowStepType == FlowStepTypeEnum.GO_TO)
                return;

            FlowStep? first = FirstChildOf(step, result);
            if (first != null)
                _executionStack.Push(new PendingStep(first, Depth + 1, _subFlowDepth));
        }

        private bool PushGoToTarget(FlowStep step)
        {
            if (step.FlowStepReferenceId == null)
                return false;

            FlowStep? target = Step(step.FlowStepReferenceId.Value);
            if (target == null)
                return false;

            _executionStack.Push(new PendingStep(target, Depth, _subFlowDepth));
            return true;
        }

        private void PushSubFlow(FlowStep step)
        {
            if (step.SubFlowId == null)
                return;

            // A flow may call itself, and two may call each other - that was deliberate. A stack is
            // not a loop though: it grows, so it needs a ceiling.
            if (_subFlowDepth >= _maxSubFlowDepth)
                throw new InvalidOperationException($"Sub-flows are nested more than {_maxSubFlowDepth} deep at \"{step.Name}\". A flow is probably calling itself with no way out.");

            FlowStep? first = FirstChildOfFlow(step.SubFlowId.Value);
            if (first == null)
                return;

            _executionStack.Push(new PendingStep(first, Depth + 1, _subFlowDepth + 1));
        }

        private FlowStep? Pop()
        {
            while (_executionStack.Count > 0)
            {
                PendingStep pending = _executionStack.Pop();

                // A search that comes back round is not looked at again: its matches came from one
                // screenshot, and searching a second time would find what the flow already handled.
                if (pending.IsMatchRepeat)
                {
                    RepeatForNextMatch(pending);
                    continue;
                }

                Depth = pending.Depth;
                _subFlowDepth = pending.SubFlowDepth;

                ForgetFrom(pending.Depth);
                _depthByStepId[pending.Step.Id] = pending.Depth;

                return pending.Step;
            }

            return null;
        }

        private void RepeatForNextMatch(PendingStep pending)
        {
            PendingMatches matches = _pendingMatches[pending.Step.Id].Advance();
            _pendingMatches[pending.Step.Id] = matches;

            // One execution step per hit, so the walk below reads this hit rather than the first.
            RepeatForMatch(pending.Step, matches, pending.Depth);

            if (matches.HasNext)
            {
                _executionStack.Push(pending);
            }
            else
            {
                // Every hit has been walked, so the search is finally done. What is left goes as
                // well as the continuation: coming back here later means a fresh screenshot.
                _pendingMatches.Remove(pending.Step.Id);

                FlowStep? sibling = NextSiblingOf(pending.Step);
                if (sibling != null)
                    _executionStack.Push(new PendingStep(sibling, pending.Depth, pending.SubFlowDepth));
            }

            FlowStep? first = FirstChildOf(pending.Step, ExecutionStep.Success());
            if (first != null)
                _executionStack.Push(new PendingStep(first, pending.Depth + 1, pending.SubFlowDepth));
        }

        /// <summary>
        /// The next hit as an execution step of its own. The search is not run again - every hit
        /// came from the one screenshot the first pass took - so it carries no duration.
        /// </summary>
        private void RepeatForMatch(FlowStep step, PendingMatches matches, int depth)
        {
            IReadOnlyList<Point>? points = _cache.GetMatchesFrom(step.Id);
            if (points == null || matches.Index >= points.Count)
                return;

            ExecutionStep repeat = new ExecutionStep
            {
                Outcome = StepOutcomeEnum.SUCCESS,
                Location = points[matches.Index],
                MatchIndex = matches.Index,
                MatchCount = matches.Count,
                StartedOn = DateTime.UtcNow,
                DurationMilliseconds = 0,
                ScreenshotFileName = _cache.GetExecutionStepFrom(step.Id)?.ScreenshotFileName, //Every hit came off the one screenshot the search tookEvery hit came off the one screenshot the search took
            };

            PlaceInRun(repeat, step, depth);

            _cache.RecordExecutionStep(step.Id, repeat);
            _matchRepeats.Add(repeat);
        }

        private void PlaceInRun(ExecutionStep executionStep, FlowStep flowStep, int depth)
        {
            executionStep.FlowStepId = flowStep.Id;
            executionStep.Name = flowStep.Name;
            executionStep.FlowStepType = flowStep.FlowStepType;
            executionStep.Depth = depth;
            executionStep.Sequence = _nextSequence++;

            if (flowStep.FlowStepType == FlowStepTypeEnum.LOOP)
                executionStep.LoopPass = _loopPasses.GetValueOrDefault(flowStep.Id);

            // Depth only ever grows by one from the step running, and that step was just recorded at
            // its own depth, so depth - 1 is always fresh. Deeper entries are left over from a
            // subtree already finished and never get read: coming back down to them means recording
            // at the shallower depth first, which overwrites them.
            if (depth > 0 && _sequenceByDepth.TryGetValue(depth - 1, out int parentSequence))
                executionStep.ParentSequence = parentSequence;

            _sequenceByDepth[depth] = executionStep.Sequence;
        }

        /// <summary>
        /// Opens the run through a FIND_ALL search's hits. A step that actually executed always
        /// starts a fresh set - anything left over describes a screenshot that is now stale, which
        /// is what a GO_TO jumping back into an open search would otherwise be handed.
        /// </summary>
        private bool OpenPendingMatches(FlowStep step, ExecutionStep result)
        {
            if (step.SearchMode != SearchModeEnum.FIND_ALL || result.Outcome != StepOutcomeEnum.SUCCESS)
                return false;

            IReadOnlyList<Point>? matches = _cache.GetMatchesFrom(step.Id);
            if (matches == null || matches.Count < 2)
                return false;

            _pendingMatches[step.Id] = new PendingMatches(0, matches.Count);
            return true;
        }

        private bool HasAnotherLoop(FlowStep loop)
        {
            int passes = _loopPasses.GetValueOrDefault(loop.Id) + 1;
            _loopPasses[loop.Id] = passes;

            // Infinite is a feature!
            if (loop.IsLoopInfinite || passes < loop.LoopCount)
                return true;

            _loopPasses.Remove(loop.Id);
            return false;
        }

        /// <summary>
        /// A result can only be read by a step below the one that produced it, so anything recorded
        /// at this depth or deeper belongs to a subtree we have left.
        /// </summary>
        private void ForgetFrom(int depth)
        {
            List<int> gone = _depthByStepId
                .Where(x => x.Value >= depth)
                .Select(x => x.Key)
                .ToList();

            foreach (int flowStepId in gone)
            {
                _depthByStepId.Remove(flowStepId);
                _cache.ForgetExecutionStep(flowStepId);
                _pendingMatches.Remove(flowStepId);
            }
        }

        private FlowStep? Step(int id)
        {
            _cache.StepsById.TryGetValue(id, out FlowStep? step);
            return step;
        }

        /// <summary>
        /// The first step to run inside this one. Success and Failure are never executed - they
        /// only say which way to go - so this reaches past them into the branch itself.
        /// </summary>
        private FlowStep? FirstChildOf(FlowStep step, ExecutionStep result)
        {
            if (!TreeStepHelper.HasBranchChildren(step.FlowStepType))
                return ChildrenOf(step.Id).FirstOrDefault();

            FlowStepTypeEnum wanted = result.Outcome == StepOutcomeEnum.SUCCESS
                ? FlowStepTypeEnum.SUCCESS
                : FlowStepTypeEnum.FAILURE;

            FlowStep? branch = ChildrenOf(step.Id).FirstOrDefault(x => x.FlowStepType == wanted);
            if (branch == null)
                return null;

            return ChildrenOf(branch.Id).FirstOrDefault();
        }

        private FlowStep? FirstChildOfFlow(int flowId)
        {
            return _cache.StepsById.Values
                .Where(x => x.ParentFlowStepId == null && x.FlowId == flowId)
                .OrderBy(x => x.OrderNumber)
                .FirstOrDefault();
        }

        private IEnumerable<FlowStep> ChildrenOf(int parentStepId)
        {
            return _cache.StepsById.Values
                .Where(x => x.ParentFlowStepId == parentStepId)
                .OrderBy(x => x.OrderNumber);
        }

        private FlowStep? NextSiblingOf(FlowStep step)
        {
            IEnumerable<FlowStep> siblings;
            if (step.ParentFlowStepId == null)
                siblings = _cache.StepsById.Values
                    .Where(x => x.ParentFlowStepId == null && x.FlowId == step.FlowId)
                    .OrderBy(x => x.OrderNumber);
            else
                siblings = ChildrenOf(step.ParentFlowStepId.Value);

            return siblings.FirstOrDefault(x => x.OrderNumber > step.OrderNumber);
        }


        // ================================================================
        // Private types
        // ================================================================

        /// <summary>A step waiting to run, and where it sits. Depth is what tells a step over when to stop.</summary>
        private class PendingStep
        {
            public PendingStep(FlowStep step, int depth, int subFlowDepth, bool isMatchRepeat = false)
            {
                Step = step;
                Depth = depth;
                SubFlowDepth = subFlowDepth;
                IsMatchRepeat = isMatchRepeat;
            }

            public FlowStep Step { get; }
            public int Depth { get; }
            public int SubFlowDepth { get; }

            /// <summary>Serves the search's next hit rather than running it. Never executed.</summary>
            public bool IsMatchRepeat { get; }
        }
    }
}
