# Image Search step

## What Image Search does

`IMAGE_SEARCH` takes a screenshot of an area and looks in it for one or more template images you
saved earlier. It has **Success** and **Failure** branches.

Where it found something becomes a position that cursor steps below it can use.

## Image Search modes

| Mode | What it does |
|---|---|
| `FIND_BEST` | One screenshot, best match wins. Succeeds if anything matched. |
| `FIND_ALL` | One screenshot, then works through every hit in turn. |
| `WAIT_UNTIL_FOUND` | Searches repeatedly until it matches, or the timeout runs out. |
| `WAIT_UNTIL_NOT_FOUND` | Searches repeatedly until it stops matching. |

`WAIT_UNTIL_FOUND` and `WAIT_UNTIL_NOT_FOUND` poll every few hundred milliseconds. A timeout of 0
waits forever.

## What WAIT_UNTIL_NOT_FOUND actually waits for

It waits for the search to **stop matching**, not for something to disappear. Nothing checks that
the thing was ever there, so if it never matched at all, the step succeeds immediately.

If you need "wait for the spinner to appear and then go away", that is two steps:
`WAIT_UNTIL_FOUND` then `WAIT_UNTIL_NOT_FOUND`.

## How FIND_ALL runs

`FIND_ALL` takes **one** screenshot and works through every hit found in it. The search never runs
a second time. Hits after the first appear in the run as their own steps with a duration of 0 ms,
because they are served from the screenshot the first search already took.

This matters if the screen changes while you work through the hits — the positions come from the
moment of the first search, not from now.

**Max matches** limits how many hits are worked through, and only appears in this mode.

## Templates and IsRequired

Templates are a list. How they combine depends on whether any are marked required:

- **None marked required** — any one of them matching is enough. This is the case for several
  variants of the same icon.
- **Some marked required** — every required one must be found.

## Accuracy

Accuracy is how close a match has to be, between 0 and 1. It is set on the step, and any single
template can override it.

Raising it reduces false matches and increases misses. Lowering it does the reverse. A template
that matches a blank region is usually accuracy set too low.

## How templates survive a different screen size

Each template records the click offset — where inside the image to click — and the size of the area
it was captured in. At match time the template is scaled by the ratio between that recorded size
and the area's size now, so a template captured on a 1080p screen still matches at 4K.

If nothing matches at the expected size, a multi-scale sweep tries sizes either side before giving
up.

Templates are stored as PNG, never JPEG. JPEG artifacts wreck normalised template matching.

## Testing an Image Search

**Test now** runs the real search against the live screen and reports whether each template was
found and with what score. It clicks nothing.

The form also generates a sentence describing what the step will actually do, which is worth reading
when several settings interact.

## Clicking what an Image Search found

The search does not click. A cursor step below it, inside the **Success** branch, takes the search
result as its position. "Find this and click it" is three steps: the search, a move, and a click.
