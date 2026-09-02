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

Cover: `READ_TEXT` with a Keep only expression, then `CHECK_VALUE` with a numeric comparison, and
why a non-numeric read fails rather than taking the false branch.

## Retrying something that sometimes fails

Cover: a `LOOP` around a search, breaking out on success, and how the run view distinguishes a
handled retry from a real failure.

## Working through a list of items

Cover: `FIND_ALL`, that every hit comes from one screenshot, and what that means when the screen
changes as you work through them.

## Reusing part of a flow

Cover: Extract to sub-flow, what gets copied versus moved, and why references are checked first.

## Being told when a flow fails

Cover: a `NOTIFY` step in a Failure branch, reporting on the parent step, and what an image search
failure attaches.
