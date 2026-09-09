# Search Text step

## What Search Text does

`SEARCH_TEXT` reads text inside an area using Windows OCR, decides whether it says what it should,
and hands what it read to steps below it. It has **Success** and **Failure** branches.

It works off the raw screen capture with no image encoding in between.

It is one of the three **checks** - `SEARCH_IMAGE`, `SEARCH_TEXT`, `CHECK_VALUE` - which are the
steps that decide. A flow holding none of them proves nothing, whatever it does to the screen.

## There is no separate read step

There used to be a `READ_TEXT` that read without deciding. It is gone, and nothing replaces it: a
read that nothing checks is a check that was never made, and its result could quietly be an empty
string that later steps went on to use.

Reading and deciding are now one step. Where you would once have read and then tested the result,
use a condition of **is not empty** - that is the old read, with the failure made visible.

## Search Text modes

Three of the four search modes are offered:

| Mode | What it does |
|---|---|
| `FIND_BEST` | Reads the area once and decides. |
| `WAIT_UNTIL_FOUND` | Reads repeatedly until the condition holds. |
| `WAIT_UNTIL_NOT_FOUND` | Reads repeatedly until the condition stops holding. |

`FIND_ALL` is not offered. Reading gives one block of text and no positions, so there is nothing to
act on one at a time.

**Every mode evaluates the condition**, including `FIND_BEST`. There is no mode that succeeds merely
because something was read.

## OCR languages

The language is a dropdown of the language packs Windows actually has installed, so you cannot pick
one Windows cannot read. English works out of the box.

More languages can be installed from Settings, which uses the Windows language pack installer.

## Keep only — narrowing before deciding

**Keep only** is a regular expression applied to what was read, keeping the first capture group.
It runs **before** the condition is checked.

That ordering is what lets a screen be asked a numeric question. Keep `total: (\d+)` and the
condition is tested against `42`, not against the whole paragraph the OCR returned — so
**is greater than** means what it says.

If the expression does not match, the result is empty.

## Conditions

All of them apply, because the condition is tested against what Keep only left rather than against
a whole block of text:

| Condition | Notes |
|---|---|
| `EQUALS`, `NOT_EQUALS` | Text comparison |
| `CONTAINS`, `NOT_CONTAINS` | Text comparison |
| `MATCHES_REGEX` | Regular expression |
| `IS_EMPTY`, `IS_NOT_EMPTY` | No value field |
| `GREATER_THAN`, `LESS_THAN` | Numeric |
| `BETWEEN` | Numeric, two values |

A numeric comparison against text that will not parse as a number **fails** rather than quietly
taking the false branch. "The number was too small" and "OCR read `S0` instead of `50`" are
different problems and should not look the same.

## What later steps read

What Keep only left is kept **whether the condition passed or failed**, and steps below can
reference it. A failure that says what was actually on screen is worth far more than one that only
says it failed.

A `CHECK_VALUE` below a `SEARCH_TEXT` can therefore ask a second question of the same read without
reading the screen again.

## Testing a Search Text

The **Test** button shows everything that was read **and** what survived the Keep only expression,
then whether the condition holds — not just a pass or a fail.

This is what diagnoses most OCR problems. Seeing `Loqin` where you expected `Login` tells you
immediately that the text is being misread and the condition is fine.
