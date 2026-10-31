# Debugging an execution

## The execution page

At the top: the flow, the execution's state and number, what it keeps and the commit it executed
at, with the debugger beside it. Under that, tiles with the elapsed time, the steps and their rate,
the loop's pass, the passes that found something and the failures handled, then an activity strip:
steps a minute over the whole execution, marked where something was found and where it ended.

Below, three panels: the flow's tree on the left with a gutter for breakpoints, the execution itself
in the middle, and the selected step's detail on the right. The panel sizes are remembered.

The tree shows how often each step executed. A check shows how often it found something instead,
and a Success or Failure row how often it was taken. The step executing now has a ring around it.

## Breakpoints

Click the gutter beside a step in the tree to set a breakpoint. The execution stops **before** that step
executes.

You can breakpoint any step, including one you have not expanded to — the whole flow is loaded, not
just the visible rows.

## Stepping

**Continue** runs to the next breakpoint or to the end.

**Step into** runs the current step and stops at the first step inside it.

**Step over** runs the current step and everything under it, then stops at the next step beside it.

A breakpoint always wins over a step over. Stepping over a step that contains a breakpoint stops at
the breakpoint.

Each button shows its key, set under Settings, Debugger keys: F5 Continue, F11 Step into, F10 Step
over, F9 Pause and F8 Stop until changed. The keys work while StepinFlow has focus, not while
another window does.

## Reading the execution

Rows are newest first and indented by depth, so the step that just executed is always the top row
and the execution reads like a stack trace. Scrolled down to read an older row, it stays where it is
as new rows arrive, and the panel says how many came since, with a way back to the latest.

A row marked **handled** in amber failed and its Failure branch took over — the flow working as
designed. The row in red is the one that ended the execution. Both being red would make a healthy retry
loop look like a disaster.

`found 0.95` is a check that succeeded, with the best match its search scored.

`hit 2 of 3` is a `FIND_ALL` search working through its hits. Such a row shows no time: it did not
search again, it is being served from the screenshot the first search took.

`pass 4` is a loop on its fourth time round. A Break's row says which step it broke out of.

**Passes that found nothing are folded.** A loop pass that ran at least one check, and none of them
succeeded, is quiet; two or more in a row become one row, such as "224 passes that found nothing".
Show opens it. The pass still executing, and the one that ended the execution, never fold.

All, Found and Failed narrow the rows, and Find a step finds them by name or number. Neither folds.

## The step detail panel

Selecting a row shows what that step produced: whether it found, was handled or ended the execution,
where it executed (the loop pass or branch it sat in, which selects that row), how long it took,
where it acted, what it read, what it said, and any exit code or error output.

For an image search it also shows its best match against the accuracy it needed, the template images
it was looking for, and the screenshot it searched if the execution kept one.

**Open in the editor** opens the step in its flow, a sub-flow's step in the sub-flow.

## Screenshots

Whether screenshots are kept depends on the history level chosen for the execution and on the per-execution
limit in Settings, Execution, which keeps an execution's first screenshots. If a step has none, the
panel says which of those it was. While an execution is under way, its rows come from the broadcast,
which carries no screenshot; the kept ones show once it has ended.

## Explain

With AI on, a past execution's page offers **Explain this execution** and **Ask about this
execution**, which open the assistant with the execution already identified.

A failed step's detail panel offers **Suggest a fix**, which opens the assistant with the step and
execution already identified.
