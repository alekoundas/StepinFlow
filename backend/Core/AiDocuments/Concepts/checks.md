# Checks

## What a check is

Three step types decide, and they are what makes a recording a test:

| Step | Reads | Produces |
|---|---|---|
| `SEARCH_IMAGE` | the screen, for a template | a position |
| `SEARCH_TEXT` | the screen, with OCR | the text it read |
| `CHECK_VALUE` | a value an earlier step produced | nothing |

Every other step acts. A flow holding no checks proves nothing, whatever it does to the screen — it
clicked some things and finished.

## Fatal or handled

A check failing means one of two very different things, and the difference is what its Failure
branch leads to.

**Fatal** — the failure path reaches an `END_EXECUTION`. The check is an assertion: if it fails,
the flow fails and the application under test is broken.

**Handled** — the Failure branch does something else instead. The check is a question the flow asks
itself: "is the desktop navigation there, or the mobile one?" Failing is a normal path, not a
problem.

The same step type covers both. Only the shape below it says which one it is.

## A check that decides nothing

A check that is neither fatal nor referenced by any later step was never really made. The flow
passes with the application broken, which is the exact failure this model exists to prevent. The
validator raises `CHECK_DECIDES_NOTHING` for it.

Look for this first when asked why a flow passes but the application is wrong.

## Markers group checks into test cases

A `MARKER` names the section that follows it. A section fails if any step beneath it failed, and
each one becomes a test case in the report — so markers turn one pass or fail for a whole flow into
"Sign in is what broke".

## Finding out what a flow verifies

`GetFlowChecks` answers this in one call. For each check it gives the name, the code comment
explaining why it is checked, the marker it falls under, whether it is fatal, and the End Execution
message that says what failing it means.

Use it for "what does this flow test", and before proposing a fix — a fix that removes or weakens a
check makes the flow pass without making the application work.

It is static: it describes the flow, not any execution of it. Which checks actually failed in a
particular execution comes from the execution's steps instead.
