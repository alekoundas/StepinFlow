# Cursor steps

## The four cursor steps

| Step | What it does | Carries a position? |
|---|---|---|
| `CURSOR_RELOCATE` | Moves the cursor | Yes |
| `CURSOR_CLICK` | Clicks where the cursor already is | No |
| `CURSOR_DRAG` | Presses, moves, releases | Yes — a start and an end |
| `CURSOR_SCROLL` | Scrolls where the cursor already is | No |

They share one form with four mode buttons. Switching mode clears the fields belonging to the mode
you left.

## Why clicking is two steps

Only **relocate** and **drag** carry a position. Click and scroll act wherever the cursor already
is.

So "click here" is two steps: a `CURSOR_RELOCATE` to the position, then a `CURSOR_CLICK`. This is
deliberate — it means a click after an image search does not need to know anything about the search,
and a sequence of clicks in one place needs only one move.

## Where a cursor step gets its position

Relocate and drag take their position from one of two sources:

- **A saved point** on the flow.
- **An earlier step's result** — an `IMAGE_SEARCH` or `READ_TEXT` above it, reached through a
  Success branch.

Drag has both sources twice: one for where the drag starts and one for where it ends.

Only steps above the cursor step, reached through a **Success** branch, are offered. Anything else
might not have run.

## Click options

Button: left, right or middle. Action: single click, double click, hold, or release.

Hold and release are separate actions so you can hold a button down, do something else, and release
later.

## Scrolling

Scroll takes a direction and a number of notches. The notch count is the same field the loop count
uses.

## Why the cursor is moved by the app rather than by the recorder

Cursor movement during a run is generated directly by StepinFlow rather than replayed through the
input hook. On a machine with scaled or mixed-DPI monitors, replaying recorded movement lands
clicks in the wrong place.
