# How execution works

## One run at a time

There is one mouse, one keyboard and one screen, so only one flow runs at a time. Starting a second
run while one is going is refused rather than queued.

## How the runner decides what is next

The runner keeps a stack of pending steps rather than calling itself recursively. For each step it
pops the top of the stack, pushes what should happen after that step, then pushes the step's first
child. Running continues until the stack is empty.

`SUCCESS` and `FAILURE` nodes are never executed. The runner reaches past them into whichever
branch the result calls for.

## How loops run

A loop's continuation is the loop itself, so nothing special is needed to repeat it. When the last
step inside a loop finishes, the loop is back on top of the stack and starts its next pass.

## How GO_TO runs

`GO_TO` pushes its target as the continuation rather than as a child. A jump backwards therefore
does not grow the stack on every pass, which it would if the target were pushed as a child.

## How sub-flows return

Returning from a sub-flow is the stack unwinding, so it needs no machinery. Nesting is limited to a
depth of 50, which is what stops runaway recursion.

## What a run keeps while it is running

- The results a step below can still read. These are dropped as the walk leaves the branch they
  belong to.
- Every hit from a `FIND_ALL` search.
- The last few screenshots.

All of it is bounded by how deep the tree is, not by how long the run has been going, so a run
lasting three weeks holds no more than one lasting three seconds.

## What a run leaves behind

History has three levels: nothing, steps only, or steps and screenshots.

Turning history off changes what is **stored**, never what a flow **does**. The runner keeps the
same values in memory either way, so a step still reads the step above it.

## Screenshots

Screenshots work like a dashcam. Nothing is written while a flow is going well. When a step fails,
the last few frames are written out, each named after the step that took it — most of them belong
to steps that ran earlier, which is the point: the frame at the moment of failure usually shows a
screen the thing was never on.

The number of frames kept is a setting.

## The debugger

Breakpoints, pause, continue, step into and step over.

**Step over** remembers the depth it started at and runs until the walk is back at or above that
depth. A breakpoint always wins over a step over — if you step over a step containing a breakpoint,
the run stops at the breakpoint.

## Reading the run

The run list is ordered by sequence and indented by depth, so it reads like a stack trace. Each row
shows what ran, whether it succeeded, and how long it took.

A row with `match 2 of 3` is a `FIND_ALL` search working through its hits. A row like that with 0 ms
did not search again — it is being served from the screenshot the first search took.
