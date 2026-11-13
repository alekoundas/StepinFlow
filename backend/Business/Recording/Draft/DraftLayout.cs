using Business.FlowScript.Models;
using Business.Recording.Draft.Models;
using Core.Enums;
using Core.Models.Database;

namespace Business.Recording.Draft
{
    /// <summary>
    /// Where each entry of a draft sits in the flow: its parent and its order. The entries say what
    /// was done, in order; this says how that reads as a tree, and can be asked again once entries
    /// were added or renamed.
    ///
    /// The recording goes under a step, or at the top of the flow, at a position among the steps
    /// already there, which move down one for each step added beside them.
    ///
    /// Everything after a search, up to the next search, goes in that search's Success branch: it
    /// only makes sense once the search found what it waited for. Its Failure branch ends the
    /// execution.
    ///
    /// Searches on one page sit beside each other. The first search on a new page goes in the
    /// Success branch of the search before it, after what that one did, since its click is what
    /// opened the page; a page come back to - a dialog closed, a menu left - goes back beside the
    /// searches of that page. A stage marker is at the top of the flow, and what follows it starts
    /// there again, so a page a stage begins on is never nested under the click before it.
    /// </summary>
    public sealed class DraftLayout
    {
        private readonly FlowScriptSchema _schema;
        private readonly FlowStep? _start;
        private readonly List<FlowStep> _flowSteps;
        private readonly List<FlowStep> _siblings;
        private readonly int _index;

        private readonly Dictionary<FlowStep, int> _childCounts = new Dictionary<FlowStep, int>();
        private int _startCount;

        /// <summary>
        /// For a flow as it is before the recording: child number index of under, or of the flow's
        /// top when under is null, or after the steps already there when index is null.
        /// </summary>
        public DraftLayout(FlowScriptSchema schema, FlowStep? under, int? index)
        {
            _schema = schema;
            _start = under;
            _flowSteps = schema.Steps.ToList();

            // With no position, after what is already there: the setup's Launch step comes first.
            _siblings = _flowSteps.Where(x => x.ParentFlowStep == under).OrderBy(x => x.OrderNumber).ToList();
            _index = Math.Clamp(index ?? _siblings.Count, 0, _siblings.Count);
        }


        // ================================================================
        // Public methods
        // ================================================================

        /// <summary>Puts the entries into the flow's steps, in place of whatever an earlier layout put there.</summary>
        public void Lay(IReadOnlyList<DraftItem> items)
        {
            _schema.Steps.Clear();
            _schema.Steps.AddRange(_flowSteps);

            // Numbered as the tree shows them, so the ones after the position can move down.
            for (int i = 0; i < _siblings.Count; i++)
                _siblings[i].OrderNumber = i;

            _startCount = _index;
            _childCounts.Clear();

            List<Level> levels = [new Level(_start)];
            FlowStep? current = _start;

            foreach (DraftItem item in items)
            {
                switch (item.Kind)
                {
                    case DraftItemKindEnum.STAGE:
                        levels = [new Level(_start)];
                        current = _start;
                        Place(item.Step, _start);
                        break;

                    case DraftItemKindEnum.SEARCH:
                        // A proposed check sits right after what it proves, wherever that is.
                        FlowStep? parent = current;
                        if (!item.IsProposed)
                            parent = LevelFor(levels, item.Page, current).Parent;

                        Place(item.Step, parent);
                        current = Branch(item.Step, FlowStepTypeEnum.SUCCESS);

                        FlowStep failure = Branch(item.Step, FlowStepTypeEnum.FAILURE);
                        foreach (FlowStep step in item.BeforeEnd)
                            Place(step, failure);

                        if (item.End != null)
                            Place(item.End, failure);
                        break;

                    // Done once per hit, or only with what was read, so what comes after it goes
                    // beside it, not in its branch. A read that finds nothing carries on.
                    case DraftItemKindEnum.EVERY:
                    case DraftItemKindEnum.READ:
                        Place(item.Step, current);

                        FlowStep success = Branch(item.Step, FlowStepTypeEnum.SUCCESS);
                        Branch(item.Step, FlowStepTypeEnum.FAILURE);
                        foreach (FlowStep step in item.InSuccess)
                            Place(step, success);
                        break;

                    default:
                        Place(item.Step, current);
                        break;
                }
            }
        }


        // ================================================================
        // Private methods
        // ================================================================

        // The level a search of that page sits at: the one of its page, come back to; the one
        // being filled, which takes its first search's page; or one deeper, in the branch steps go
        // in now. With no page known, where the last search was.
        private static Level LevelFor(List<Level> levels, int? page, FlowStep? current)
        {
            Level top = levels[^1];
            if (page == null)
                return top;

            int seen = levels.FindLastIndex(x => x.Page == page);
            if (seen >= 0)
            {
                levels.RemoveRange(seen + 1, levels.Count - seen - 1);
                return levels[seen];
            }

            if (top.Page == null)
            {
                top.Page = page;
                return top;
            }

            Level deeper = new Level(current) { Page = page };
            levels.Add(deeper);

            return deeper;
        }

        private void Place(FlowStep step, FlowStep? parent)
        {
            step.ParentFlowStep = parent;
            step.OrderNumber = NextOrder(parent);
            _schema.Steps.Add(step);
        }

        private FlowStep Branch(FlowStep search, FlowStepTypeEnum type)
        {
            FlowStep branch = new FlowStep { FlowStepType = type };
            Place(branch, search);

            return branch;
        }

        private int NextOrder(FlowStep? parent)
        {
            if (parent == _start)
            {
                foreach (FlowStep later in _siblings.Skip(_index))
                    later.OrderNumber++;

                return _startCount++;
            }

            _childCounts.TryGetValue(parent!, out int count);
            _childCounts[parent!] = count + 1;

            return count;
        }


        // ================================================================
        // Private types
        // ================================================================

        // Where the searches of one page sit: beside each other, under this parent.
        private sealed class Level
        {
            public Level(FlowStep? parent)
            {
                Parent = parent;
            }

            public FlowStep? Parent { get; }
            public int? Page { get; set; }
        }
    }
}
