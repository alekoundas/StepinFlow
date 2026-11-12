# How execution works

## One execution at a time

There is one mouse, one keyboard and one screen, so only one flow runs at a time. Starting a second
execution while one is going is refused rather than queued.

## How the runner decides what is next

The runner keeps a stack of pending steps rather than calling itself recursively. For each step it
pops the top of the stack, pushes what should happen after that step, then pushes the step's first
child. Running continues until the stack is empty.

`SUCCESS` and `FAILURE` nodes are never executed. The runner reaches past them into whichever
branch the result calls for.

A disabled step and everything under it are not loaded, so the runner goes on to the next step as
if the line were not there.

## How loops run

A loop's continuation is the loop itself, so nothing special is needed to repeat it. When the last
step inside a loop finishes, the loop is back on top of the stack and starts its next pass.

## How BREAK runs

`BREAK` leaves the step it names as if that step had just finished with nothing left inside it.
Everything still pending inside it is dropped from the stack, a loop does no more passes and a
`FIND_ALL` search serves no more hits; the walk carries on with whatever follows the step it left.
Every loop it leaves on the way starts counting from its first pass the next time the walk reaches
it.

## How sub-flows return

Returning from a sub-flow is the stack unwinding, so it needs no machinery. Nesting is limited to a
depth of 50, which is what stops runaway recursion.

## What an execution keeps while it is going

- The results a step below can still read. These are dropped as the walk leaves the branch they
  belong to.
- Every hit from a `FIND_ALL` search.
- Its last few screenshots.

All of it is bounded, by how deep the tree is and by the screenshot setting, never by how long the
execution has been going, so an execution lasting three weeks holds no more than one lasting three
seconds.

## What an execution leaves behind

History has three levels: nothing, steps only, or steps and screenshots.

Turning history off changes what is **stored**, never what a flow **does**. The engine keeps the
same values in memory either way, so a step still reads the step above it.

Steps are written in batches while the execution goes, a second behind it at most. Nothing is
deleted on its own: an execution, or every execution of a flow, is deleted from the Executions page,
and deleting a flow deletes its executions. Their screenshots go with them, and the database gives the
space back to the disk.

## Screenshots

An execution keeps its **last** screenshots. They stay in memory, older ones making way for newer
ones, and are written to disk as the execution ends, each named after the step that took it. A long
execution keeps what led up to its end — the step that failed, or the moment it was stopped — not how
it started. In a loop that searches several times a second, that is its last seconds.

An execution the app closed on before it ended writes none.

How many are kept is the setting **Screenshots kept per execution**: 50 unless changed.

## The debugger

Breakpoints, pause, continue, step into and step over.

**Step over** remembers the depth it started at and runs until the walk is back at or above that
depth. A breakpoint always wins over a step over — if you step over a step containing a breakpoint,
the execution stops at the breakpoint.

## Reading the execution

The execution list is ordered by sequence and indented by depth, so it reads like a stack trace. Each row
shows what ran, whether it succeeded, and how long it took.

A row with `match 2 of 3` is a `FIND_ALL` search working through its hits. A row like that with 0 ms
did not search again — it is being served from the screenshot the first search took.
