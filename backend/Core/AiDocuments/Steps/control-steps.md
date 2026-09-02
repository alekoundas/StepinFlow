# Wait, Loop, Go To and Sub-Flow

## Wait

`WAIT` pauses. It takes a duration in milliseconds and, optionally, an upper bound.

Setting both makes the pause a random length between the two. A bot that pauses for exactly 500 ms
every time is recognisably a bot; a varying pause is not.

`WAIT` is a leaf and has no branches.

## Loop

`LOOP` repeats the steps inside it, either a fixed number of times or forever.

Steps to repeat go **inside** the loop. A loop with nothing inside it does nothing.

An infinite loop is a supported thing to build, not a mistake. Stop it with the debugger, or put a
step inside it that ends the run.

Each pass is labelled in the run view, so a failure on pass 40 is distinguishable from one on pass 1.

## Go To

`GO_TO` jumps to another step and continues from there.

Jumping backwards is the normal use, and it does not accumulate anything as it repeats — a backward
jump can run indefinitely without growing.

`GO_TO` is a leaf. Anything after it in the same branch never runs.

## Sub-Flow

`SUB_FLOW` runs another flow and then carries on with the next step beside it. It is a leaf with no
branches of its own.

The form shows the invoked flow's tree read-only under **What this runs**, which expands and
collapses, and clicking a step opens a view-only dialog. The sub-flow's steps are not merged into
the calling flow's tree — they belong to a different flow and moving them from here would not mean
anything.

Nesting is allowed and so is a flow calling itself. The runner stops at a nesting depth of 50.
