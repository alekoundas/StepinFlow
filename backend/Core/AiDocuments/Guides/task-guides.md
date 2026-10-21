# Task guides

> **These are stubs.** Each needs writing from a real flow that has actually been built and run.
> The headings mark what is worth covering; the content should come from doing it, not from
> describing it in the abstract.

## Automating a desktop application

Cover: focus and resize the window first, define areas inside it, capture templates at that size,
and why that order makes the flow portable.

## Waiting for something to load

Cover: `WAIT_UNTIL_FOUND` versus a fixed `WAIT`, choosing a timeout, and what to put in the Failure
branch.

## Reading a number off the screen and acting on it

Cover: a single `SEARCH_TEXT` with a Keep only expression and a numeric condition - the read and
the comparison are one step now - and why a non-numeric read fails rather than taking the false
branch. Then when a second `CHECK_VALUE` is still worth adding: asking another question of the same
read without going back to the screen.

## Retrying something that sometimes fails

Cover: a `LOOP` around a search, breaking out on success, and how the execution view distinguishes a
handled retry from a real failure.

## Working through a list of items

Cover: `FIND_ALL`, that every hit comes from one screenshot, and what that means when the screen
changes as you work through them.

## Reusing part of a flow

Cover: Extract to sub-flow, what gets copied versus moved, and why references are checked first.

## Repeating a step somewhere else in the flow

Select the step in the tree and press **Copy**. The copy takes everything under the step, and a bar
over the tree names it. **Paste here** beside any + puts a copy at the end of that branch; the copy
stays, so it can go in several places. A pasted step is named apart from the one it copies ("Find
button 2"), since the script refers to steps by name.
Its templates become files of its own. A step inside the copy that read another step inside it
reads the copy's. One that read a step outside it still reads that step, and validation marks it
when the new place cannot reach it.

A copy goes only into the flow it was copied from. Another flow has its own areas, points and
inputs, so to reuse steps across flows, extract them to a sub-flow instead.

## Saying why a step is there

Every step's form, Success and Failure included, ends with **Why it is here**: the step's comment,
written above it in the script as `#` lines. The form shows that line as it will print. It is the
first thing read when a failure is explained, so say what you expect at that point and what it
means if it fails, rather than what the step does, which its line already says.

## Sharing a flow as a file

A flow's **Export**, beside Run in the editor, writes it as a `.sflw` script with its templates in a
folder of the same name beside it. The folder starts as the app's export folder; Browse picks
another, and a script of the same name there is replaced.

**Import**, in the menu, reads a `.sflw` file and says what importing it would do before anything
is saved: a new flow, when no flow has the file's `Id`; the flow with that `Id` replaced, when one
has - its executions are kept, and the page shows it beside the file; or nothing, when the file does
not read cleanly or another flow already has its name. Its errors and warnings are marked at their
lines in the script. A template the file names that is not in the folder beside it is imported
without a picture, and a secret input has no value in the file: it is asked for when the flow
executes.

## Being told when a flow fails

Cover: a `NOTIFY` step in a Failure branch, reporting on the parent step, and what a `SEARCH_IMAGE`
failure attaches.
