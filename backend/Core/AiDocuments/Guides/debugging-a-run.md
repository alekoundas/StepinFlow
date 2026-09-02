# Debugging a run

## The execution page

Three panels: the flow's tree on the left with a gutter for breakpoints, the run itself in the
middle, and the selected step's detail on the right. There are also History and Explain tabs.

The panel sizes are remembered.

## Breakpoints

Click the gutter beside a step in the tree to set a breakpoint. The run stops **before** that step
executes.

You can breakpoint any step, including one you have not expanded to — the whole flow is loaded, not
just the visible rows.

## Stepping

**Continue** runs to the next breakpoint or to the end.

**Step into** runs the current step and stops at the first step inside it.

**Step over** runs the current step and everything under it, then stops at the next step beside it.

A breakpoint always wins over a step over. Stepping over a step that contains a breakpoint stops at
the breakpoint.

## Reading the run

Rows are ordered by sequence and indented by depth, so the run reads like a stack trace.

A row marked **handled** in amber failed and its Failure branch took over — the flow working as
designed. The row in red is the one that ended the run. Both being red would make a healthy retry
loop look like a disaster.

`match 2 of 3` is a `FIND_ALL` search working through its hits. Such a row with 0 ms did not search
again; it is being served from the screenshot the first search took.

`pass 4` is a loop on its fourth time round.

## The step detail panel

Selecting a row shows what that step produced: its outcome, how long it took, where it acted, what
it read, what it said, and any exit code or error output.

For an image search it also shows the template images it was looking for, and the screenshot it
searched if the run kept one.

## Screenshots

Whether screenshots are kept depends on the history level chosen for the run and on the per-run
limit in Settings. If a step has none, the panel says so rather than showing nothing.

## Explain

With an AI provider set up, the Explain tab reads the finished run and says what it thinks went
wrong. It can also show exactly what was sent to the model.

A failed step's detail panel offers **Suggest a fix**, which opens the assistant with the step and
run already identified.
