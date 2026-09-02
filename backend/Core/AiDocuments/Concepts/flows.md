# Flows and sub-flows

## What a flow is

A flow is a named tree of steps, plus the areas and points those steps refer to. Running a flow
walks the tree from the top and does what each step says.

Everything a flow needs travels with it: its steps, its areas, its points, and the template images
its searches look for. Nothing is stored globally, so two flows never interfere with each other.

## What a sub-flow is

A sub-flow is a flow meant to be called by another flow rather than started on its own. It has the
same tree, the same areas and points, the same recorder and the same editor as a normal flow. The
only difference is where it appears and how it is started.

Sub-flows are listed on their own page, separate from the flows you start yourself.

## Turning a flow into a sub-flow

A normal flow shows a **Make this a sub-flow** button. A sub-flow shows **Used by**, listing the
flows that call it.

Promotion is one way. It is a button rather than a checkbox because a checkbox reads as something
you can undo, and undoing it would leave every caller pointing at a flow that can no longer be
called.

## Calling a sub-flow

A `SUB_FLOW` step runs another flow and then carries on with the next step beside it. It is a leaf:
it has no Success or Failure branches of its own.

Nesting is allowed, and so are cycles — a flow may call itself. There is no cycle detection,
because a self-call with an exit condition is recursion rather than a mistake, and an infinite loop
is already something you can build on purpose. Instead the runner stops at a nesting depth of 50.

## What a sub-flow cannot see

A sub-flow cannot read results from the flow that called it. Steps refer to each other by id, and
those references only resolve inside one flow, so a sub-flow is self-contained by construction.

If a sub-flow needs a value from its caller, the value has to be something the sub-flow can obtain
itself — read from the screen, or read from the clipboard.

## Deleting a flow that is used as a sub-flow

The `SUB_FLOW` step survives and its reference is cleared. Validation then reports
`SUB_FLOW_MISSING` on that step. The step is never deleted along with the flow it pointed at,
because that would silently remove work from a flow you were not looking at.
