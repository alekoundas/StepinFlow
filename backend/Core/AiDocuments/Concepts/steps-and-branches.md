# Steps and branches

## What a step is

A step is one thing a flow does: move the cursor, type, search for an image, run a command, call
another flow. Steps sit in a tree. Order and nesting decide what runs when.

## Which steps have Success and Failure branches

These step types are created with a **Success** and a **Failure** child, and anything that should
run conditionally goes inside one of them:

- `SEARCH_IMAGE`
- `SEARCH_TEXT`
- `CHECK_VALUE`
- `SYSTEM_COMMAND`
- `WINDOW_FOCUS`, `WINDOW_RESIZE`, `WINDOW_RELOCATE`, `WINDOW_CLOSE`

The first three are the **checks** - the steps that exist to decide, and the ones a report is built
from. The rest branch for a different reason: a command that will not run or a window that is not
there is the harness failing, which says nothing about the application being tested.

Every other step type is a leaf or a container, and either succeeds or ends the execution.

Success and Failure are structural. You do not create or delete them, and you cannot move them.
They appear with their parent and disappear with it.

## What happens when a step fails

If the step has branches, its **Failure** branch runs and the flow carries on. That is the flow
working as designed — a search that finds nothing and takes the other path is not an error.

If the step has no branches, the execution ends there.

The execution view shows the difference. A failure that a Failure branch caught is amber and marked
**handled**. The one that ended the execution is red.

## Steps that hold other steps

`LOOP` holds the steps it repeats. `SUCCESS` and `FAILURE` hold the steps that run on that path.
Nothing else holds children directly.

## Disabling a step

A disabled step is not executed, and neither is anything under it: both its branches, or a loop's
whole body. The execution carries on with the next step as if the line were not there, and the step
never appears in the execution's history.

A selected row has a **Disable** button beside Copy, and **Enable** once it is disabled, saved at
once. A disabled row is dimmed with a Disabled chip, and the rows under it are dimmed too. Each step
keeps its own setting, so enabling a parent leaves a step under it that was disabled on its own
still disabled.

What is wrong with a disabled step is not reported and does not stop the flow executing, so a
half-built step can be parked and the rest tried. Its name stays taken, because it is still a line
of the script. A Success, a Failure or a stage marker executes nothing of its own and cannot be
disabled.

The rest of the flow executes as written, and nothing warns about what the step was doing for it. A
Click after a disabled Move clicks wherever the cursor is, and a QA test can pass without a check
that is disabled.

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
