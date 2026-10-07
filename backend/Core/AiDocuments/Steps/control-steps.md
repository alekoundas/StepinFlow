# Wait, Loop, Go Back, Sub-Flow, End Execution and Stage Marker

## Wait

`WAIT` pauses. It takes a duration in milliseconds and, optionally, an upper bound.

Setting both makes the pause a random length between the two. A bot that pauses for exactly 500 ms
every time is recognisably a bot; a varying pause is not.

`WAIT` is a leaf and has no branches.

## Loop

`LOOP` repeats the steps inside it, either a fixed number of times or forever.

Steps to repeat go **inside** the loop. A loop with nothing inside it does nothing.

An infinite loop is a supported thing to build, not a mistake. Stop it with the debugger, or put a
step inside it that ends the execution.

Each pass is labelled in the execution view, so a failure on pass 40 is distinguishable from one on pass 1.

## Go Back

`GO_BACK` returns to a step that already ran on the way here and carries on from there.

The steps it can go back to are its earlier siblings, its parent, the parent's earlier siblings,
and so on up to the top of the flow. Never a step below it, never one in the other branch of a
check above it, and never one inside an earlier block: a loop or a check that already finished is
gone back to as a whole, not into. The form's **Go back to** dropdown offers only those steps,
nearest first, the validator reports anything else, and moving a step so its target falls off that
chain clears the target.

Going back repeatedly does not accumulate anything, so a Go Back can run indefinitely without
growing.

`GO_BACK` is a leaf. Anything after it in the same branch never runs.

## Sub-Flow

`SUB_FLOW` runs another flow and then carries on with the next step beside it. It is a leaf with no
branches of its own.

The form shows the invoked flow's tree read-only under **What this runs**, which expands and
collapses, and clicking a step opens a view-only dialog. The sub-flow's steps are not merged into
the calling flow's tree — they belong to a different flow and moving them from here would not mean
anything.

Nesting is allowed and so is a flow calling itself. The runner stops at a nesting depth of 50.

## End Execution

`END_EXECUTION` stops the flow where it stands and stamps the verdict. It is a leaf with no
branches, and nothing after it runs.

It carries **as success** — false by default, because a step added and left half configured should
not quietly turn a broken execution green — and a **message**, which is the sentence a report and a
failure notification show. A failure needs one; a pass does not.

Without any `END_EXECUTION`, a flow ends when it runs out of steps and reports that it completed.
That is not the same as passing: a flow whose every check failed but whose failure branches all
carried on will still report COMPLETED. If a check failing should fail the execution, its Failure
branch has to reach one of these.

Cleanup belongs above it, which is why it is a step and not a setting on the check:

```
Failure:
  click Log out
  notify "checkout failed"
  End Execution, as success = false, "could not complete the order"
```

## Stage Marker

`STAGE_MARKER` names the stage that follows it. It does nothing at execution time.

A stage is everything from one stage marker to the next. It fails if any step in it failed and
passes otherwise, and each one becomes a test case in the report — so stages are what turn a single
pass or fail for a whole flow into "Sign in is what broke".

Stages do not nest and do not indent the steps in them. Name them after what a person would say
they were doing, because those names end up in front of everyone reading the report.
