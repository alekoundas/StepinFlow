# Validation messages

## What validation checks

Validation catches what one step assumes about another — the search a cursor step reads, the branch
a step now sits in. It deliberately does not repeat the form rules, because the form already tells
you a name is required while you type.

This is exactly what a drag and drop breaks quietly, which is why it is checked separately.

Errors are shown as badges on the tree and as messages on each step. An error stops an execution;
a warning and a note do not.

## Flow

**FLOW_HAS_NO_STEPS** — the flow is empty. Add a step.

## Points and step results

**POINT_MISSING** — a move or a drag has nowhere to go. Pick a saved point, or an earlier search
whose result gives one. A click and a scroll never need one: they act where the cursor is, so a
move goes on the step above them.

**STEP_RESULT_MISSING** — the step reads another step's result but none is picked.

**STEP_RESULT_UNREACHABLE** — the step it reads is not above it. A picked step must be above it on
the Success side. A `{{Name}}` written in text may be on either side, because a value is kept
whichever way its step went, but it must still be above: a step's result is kept for the steps
under it and dropped once the flow moves past it. Usually caused by moving one of the two steps, or
by writing `{{Name}}` in a step after it rather than under it. Move the step under the one it reads.

**STEP_RESULT_NOT_GIVEN** — the step it reads keeps nothing to use: a search in
`WAIT_UNTIL_NOT_FOUND`, or a `{{Name}}` naming a step that keeps no value. Only a `SEARCH_TEXT` or a
`SYSTEM_COMMAND` keeps what it read or printed. Pick a different source.

**VARIABLE_UNKNOWN** — a `{{Name}}` names nothing in the flow: no input, no step and not `width` or
`height`. Usually a typo, or a step that was renamed.

## Areas

**AREA_MISSING** — the step searches an area, or acts on a window area, and the area is not set or
was deleted.

## Searches

**NO_TEMPLATES** — an image search has no template images.

**SEARCH_TEXT_MISSING** — a waiting read-text mode has no text to look for.

**OCR_LANGUAGE_MISSING** — no OCR language is picked. A warning: the step executes, reading in the
languages Windows is set to on that machine, which can differ from one machine to the next. A
script does not carry the language, so a text search from a file or a recording starts with none.

## Conditions

**CONDITION_TYPE_MISSING** — no condition is picked.

**CONDITION_VALUE_MISSING** — the condition needs a value and none is set.

**CONDITION_RANGE_INCOMPLETE** — `BETWEEN` needs both ends.

## Commands

**COMMAND_MISSING** — a custom command step has no command.

**COMMAND_PARAMETER_MISSING** — the chosen preset takes a parameter and none is set.

## Windows

**AREA_NOT_A_WINDOW** — a window step names an area that is a monitor, a region or a browser tab.
Only a window can be focused, resized or moved, so pick a window area.

**WINDOW_SIZE_MISSING** — a resize step has no width or height.

## Control

**LOOP_COUNT_MISSING** — a loop is neither infinite nor given a count.

**WAIT_RANGE_INVALID** — the upper bound of a wait is below its lower bound.

**SUB_FLOW_MISSING** — the flow this step ran has been deleted, or was never picked.

**BREAK_TARGET_MISSING** — a Break has no step picked to break out of. A Break goes inside a loop
or a check; at the top of a flow there is nothing to leave.

**BREAK_TARGET_UNREACHABLE** — a Break names a step it does not sit inside: one beside it, one in
the other branch of a check, or a Success or Failure row. Usually the result of a move. Pick one of
the steps it sits inside.

## Notifications

**DISCORD_BOT_MISSING** — a notify step has no bot picked.

**FAILED_STEP_UNREACHABLE** — the step being reported on no longer fails above this one.

## Warnings

**BRANCHES_EMPTY** — a step has Success and Failure branches and both are empty, so the step's
result changes nothing.

**NAME_MISSING** — the step has no name. It still runs; it is just hard to find in an execution.
A cursor, wait, keyboard, window or system action step never has this: the app names it from its
settings.

**SCREEN_COORDINATES** — the step uses an area or a point in screen coordinates: a region drawn
with nothing around it, a point measured from nothing, or anything inside such a region. It works
on this screen and lands somewhere else on any other. Put the region inside a window or a monitor,
or measure the point from one.

**KEYS_NOT_RELEASED** — a `KEYBOARD_INPUT` step holds keys and nothing below it lets go of the same
keys, so they stay down for the rest of the execution and every key typed after them reads as a
shortcut. Add a step that releases them after the steps they are held for. The engine lets go of
anything still held when the execution ends, however it ends.

## Notes

A note is quieter than a warning and never stops an execution. Only a QA test gets them.

**CHECK_CANNOT_FAIL** — a check with no `End Execution` failed anywhere under its Failure branch,
neither its own nor under a check inside it, so failing it changes nothing and the test passes
whichever way it goes. Fine for a search that only decides which way the flow goes, such as closing
a banner when there is one. When the check is meant to prove something, end its Failure branch with
`End Execution` failed. A check under an `End Execution` never gets one: the verdict is decided by
then.
