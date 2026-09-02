# Check Value step

## What Check Value does

`CHECK_VALUE` branches on what an earlier step produced. It has **Success** and **Failure**
branches and does nothing to the screen.

Use it when a step produced a value and what happens next depends on what that value is.

## Where the value comes from

You pick the source step from a dropdown of `READ_TEXT` and `SYSTEM_COMMAND` steps above this one,
reached through a **Success** branch.

The pick is stored by id, not by name, so renaming the source step cannot break the check.

## Conditions

| Condition | Notes |
|---|---|
| `EQUALS`, `NOT_EQUALS` | Text comparison |
| `CONTAINS`, `NOT_CONTAINS` | Text comparison |
| `MATCHES_REGEX` | Regular expression |
| `IS_EMPTY`, `IS_NOT_EMPTY` | No value field |
| `GREATER_THAN`, `LESS_THAN` | Numeric |
| `BETWEEN` | Numeric, two values |

Value fields only appear when the condition needs them, and `BETWEEN` shows two.

## Numeric comparisons on text that is not a number

A numeric comparison against text that will not parse as a number **fails**. It does not quietly
take the false branch.

That distinction matters: "the number was too small" and "OCR read the number as `S0` instead of
`50`" are different problems, and they should not look the same.
