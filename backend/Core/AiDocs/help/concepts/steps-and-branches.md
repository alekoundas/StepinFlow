# Steps and branches

## What a step is

A step is one thing a flow does: move the cursor, type, search for an image, run a command, call
another flow. Steps sit in a tree. Order and nesting decide what runs when.

## Which steps have Success and Failure branches

These step types are created with a **Success** and a **Failure** child, and anything that should
run conditionally goes inside one of them:

- `IMAGE_SEARCH`
- `READ_TEXT`
- `CHECK_VALUE`
- `SYSTEM_COMMAND`
- `WINDOW_FOCUS`, `WINDOW_RESIZE`, `WINDOW_RELOCATE`

Every other step type is a leaf or a container, and either succeeds or ends the run.

Success and Failure are structural. You do not create or delete them, and you cannot move them.
They appear with their parent and disappear with it.

## What happens when a step fails

If the step has branches, its **Failure** branch runs and the flow carries on. That is the flow
working as designed — a search that finds nothing and takes the other path is not an error.

If the step has no branches, the run ends there.

The run view shows the difference. A failure that a Failure branch caught is amber and marked
**handled**. The one that ended the run is red.

## Steps that hold other steps

`LOOP` holds the steps it repeats. `SUCCESS` and `FAILURE` hold the steps that run on that path.
Nothing else holds children directly.

## How steps refer to each other

A step never refers to another step by name. It refers by id, so renaming a step cannot break
anything that reads it.

A step can only read the result of a step that is above it and reached through a **Success**
branch. Anything else might not have run. The dropdown that offers the choices, the validator, and
the drag-and-drop preview all use the same rule, so what you are offered is exactly what will still
be valid after you move things.

## Moving steps

The tree supports drag and drop. A line between rows reorders; an outlined row drops inside; red
means the move is not allowed and says why.

Before a move completes it asks the backend for a preview and then confirms — telling you how many
children come along, and which steps would lose the search result they point at. Reparenting can
break a reference silently, and a step whose reference has gone clicks in the wrong place rather
than reporting anything.

## Variables

There are none. Steps pass values by referring to each other by id rather than by writing to named
variables, which means a reference cannot be broken by a rename and an invalid one cannot be
expressed at all.
