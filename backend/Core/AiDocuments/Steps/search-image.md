# Search Image step

## What Search Image does

`SEARCH_IMAGE` takes a screenshot of an area and looks in it for one or more template images you
saved earlier. It has **Success** and **Failure** branches.

Where it found something becomes a position that cursor steps below it can use.

It is one of the three **checks** - `SEARCH_IMAGE`, `SEARCH_TEXT`, `CHECK_VALUE` - which are the
steps that decide. A flow holding none of them proves nothing, whatever it does to the screen.

## Search modes

| Mode | What it does |
|---|---|
| `FIND_BEST` | One screenshot, best match wins. Succeeds if anything matched. |
| `FIND_ALL` | One screenshot, then works through every hit in turn. |
| `WAIT_UNTIL_FOUND` | Searches repeatedly until it matches, or the timeout runs out. |
| `WAIT_UNTIL_NOT_FOUND` | Searches repeatedly until it stops matching. |

`WAIT_UNTIL_FOUND` and `WAIT_UNTIL_NOT_FOUND` poll every few hundred milliseconds. A timeout of 0
waits forever, which in an unattended execution means it never gives up.

## Which mode to use, and why it matters for speed

`WAIT_UNTIL_FOUND` costs nothing when the image is already there: the first search runs before any
delay, so it does the same one screenshot and one match that `FIND_BEST` would. Prefer it wherever
the flow is waiting for something that ought to appear.

`FIND_BEST` is for a **branch point** - "which layout am I in", "is the desktop nav there or the
hamburger". That question already has a final answer, so waiting on it buys nothing and costs the
whole timeout on every execution that takes the fallback.

The pattern that gets both right is to wait once on something always present, then branch freely:

```
Wait for the page logo        WAIT_UNTIL_FOUND, timeout 15s
Is the desktop nav there?     FIND_BEST
  Failure: click the hamburger
```

A step with a populated Failure branch and a long timeout is usually a branch point written as a
wait.

## What WAIT_UNTIL_NOT_FOUND actually waits for

It waits for the search to **stop matching**, not for something to disappear. Nothing checks that
the thing was ever there, so if it never matched at all, the step succeeds immediately.

If you need "wait for the spinner to appear and then go away", that is two steps:
`WAIT_UNTIL_FOUND` then `WAIT_UNTIL_NOT_FOUND`.

## How FIND_ALL runs

`FIND_ALL` takes **one** screenshot and works through every hit found in it. The search never runs
a second time. Hits after the first appear in the execution as their own steps with a duration of 0 ms,
because they are served from the screenshot the first search already took.

This matters if the screen changes while you work through the hits — the positions come from the
moment of the first search, not from now.

**Max matches** limits how many hits are worked through, and only appears in this mode.

## Templates and IsRequired

Templates are a list. How they combine depends on whether any are marked required:

- **None marked required** — any one of them matching is enough. This is the case for several
  variants of the same icon.
- **Some marked required** — every required one must be found.

A missing required template fails the search even when others matched, and the failure message
names it. The waiting modes use the same rule: `WAIT_UNTIL_FOUND` waits until every required one
is there, `WAIT_UNTIL_NOT_FOUND` until at least one is gone. **Test now** and an execution decide it
the same way.

## Accuracy

Accuracy is how close a match has to be, between 0 and 1. **Each template has its own** — one
variant of an icon can need a looser bar than another. There is no accuracy on the step.

A new template starts on its mode's default, and changing the step's mode puts every template back
on the new mode's default, because the same number means different things in the two modes.

Raising it reduces false matches and increases misses. Lowering it does the reverse.

A failed search records the best score anything reached and which template reached it. Read the
score against **that template's** accuracy: 0.78 against 0.80 is an accuracy a shade too tight;
0.38 against 0.80 means the template was not on screen at the size it was searched for, and
lowering the accuracy would only make the flow click the wrong thing.

## Match modes

The mode is set on the step and applies to every template in it.

| Mode | Compares | Default accuracy | Use it for |
|---|---|---|---|
| `SHAPE` | the pattern, with each picture's own brightness taken out | 0.80 | nearly everything |
| `SHAPE_AND_BRIGHTNESS` | the pixels as they are | 0.95 | telling states apart - an enabled button from a disabled one |

`SHAPE` ignores how light or dark something is, so it finds a greyed-out button as readily as a
live one. `SHAPE_AND_BRIGHTNESS` sees the difference, which is why it exists.

Its default is high for a reason. A template is mostly background, and background always agrees,
so at 0.80 `SHAPE_AND_BRIGHTNESS` finds a letter on a blank white screen. At 0.95 it does not.

Both compare in grayscale. Colour is never compared.

## How templates survive another screen

Each template records where inside it to click, and the size and DPI of the area it was captured
in. A template cannot be captured until the step has a search area, and the capture is confined to
that area.

When the search runs, the template is scaled once, by a ratio its **area** decides - the area's
"Contents scale with" setting:

| The area scales with | Ratio | For |
|---|---|---|
| Screen DPI | DPI now ÷ DPI it was captured at | browsers, normal apps, the OS - resizing the window does not resize what is in it |
| Area size | the smaller of width now ÷ width then and height now ÷ height then | games - the picture stretches with the window, and gets bars when its shape changes |

A region inside another area follows its parent unless set. Other areas default to Screen DPI,
except an application, which has to be told - a normal app and a game look the same from outside.

There is one attempt at one computed size, not a sweep of sizes. A score far below the accuracy at
a sensible size says the setting is wrong rather than hiding it behind guesses.

If the scaled template would be larger than the area, or under 2 pixels, the search fails with an
error that gives the scale. It does not report "not found".

A template with no recorded size or DPI - captured before they were recorded, or made by the
recording wizard - is searched at the size it was captured, on every screen.

## Known limits

- **Text does not survive a DPI change.** At another Windows scaling, text is drawn again rather
  than enlarged: an icon captured at 100% scores about 0.93 at 125%, a word about 0.67. Find icons
  with Search Image; read words with Search Text.
- **A single-colour template matches everywhere** in `SHAPE` - a picture with no variation scores
  1.0 at every position, so the step succeeds and clicks the first spot. Capture something with
  detail in it.
- **Colour is never compared**, so two states with the same lightness in a different hue look the
  same to both modes.

Templates are stored as PNG, never JPEG. JPEG artifacts wreck normalised template matching.

## Testing a Search Image

**Test now** does the real search against the live screen and reports whether each template was
found and with what score. It clicks nothing.

The form also generates a sentence describing what the step will actually do, which is worth reading
when several settings interact.

## Clicking what a search found

The search does not click. A cursor step below it, inside the **Success** branch, takes the search
result as its position. "Find this and click it" is three steps: the search, a move, and a click.
