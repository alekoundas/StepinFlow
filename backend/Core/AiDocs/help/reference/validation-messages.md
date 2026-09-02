# Validation messages

## What validation checks

Validation catches what one step assumes about another — the search a cursor step reads, the branch
a step now sits in. It deliberately does not repeat the form rules, because the form already tells
you a name is required while you type.

This is exactly what a drag and drop breaks quietly, which is why it is checked separately.

Errors are shown as badges on the tree and as messages on each step.

## Flow

**FLOW_HAS_NO_STEPS** — the flow is empty. Add a step.

## Points and step results

**POINT_MISSING** — a cursor step has nowhere to act. Pick a saved point, or an earlier search
whose result gives one.

**STEP_RESULT_MISSING** — the step reads another step's result but none is picked.

**STEP_RESULT_UNREACHABLE** — the step it reads no longer runs above it on a Success path. Usually
caused by moving one of the two steps. Either move it back, or pick a different source.

## Areas

**AREA_MISSING** — the step searches an area, and the area is not set or was deleted.

## Searches

**NO_TEMPLATES** — an image search has no template images.

**SEARCH_TEXT_MISSING** — a waiting read-text mode has no text to look for.

**OCR_LANGUAGE_MISSING** — no OCR language is picked, or the one picked is no longer installed.

## Conditions

**CONDITION_TYPE_MISSING** — no condition is picked.

**CONDITION_VALUE_MISSING** — the condition needs a value and none is set.

**CONDITION_RANGE_INCOMPLETE** — `BETWEEN` needs both ends.

## Commands

**COMMAND_MISSING** — a custom command step has no command.

**COMMAND_PARAMETER_MISSING** — the chosen preset takes a parameter and none is set.

## Windows

**WINDOW_MATCH_MISSING** — a window step has no process name or title pattern, so it cannot find a
window.

**WINDOW_SIZE_MISSING** — a resize step has no width or height.

## Control

**LOOP_COUNT_MISSING** — a loop is neither infinite nor given a count.

**WAIT_RANGE_INVALID** — the upper bound of a wait is below its lower bound.

**SUB_FLOW_MISSING** — the flow this step ran has been deleted, or was never picked.

## Notifications

**DISCORD_BOT_MISSING** — a notify step has no bot picked.

**FAILED_STEP_UNREACHABLE** — the step being reported on no longer fails above this one.

## Warnings

**BRANCHES_EMPTY** — a step has Success and Failure branches and both are empty, so the step's
result changes nothing.

**NAME_MISSING** — the step has no name. It still runs; it is just hard to find in a run.
