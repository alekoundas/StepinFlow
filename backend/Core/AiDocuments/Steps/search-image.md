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
| `WAIT_UNTIL_STILL` | Searches repeatedly until it matches in the same place twice, or the timeout runs out. |
| `WAIT_UNTIL_FOUND` | Searches repeatedly until it matches, or the timeout runs out. |
| `WAIT_UNTIL_NOT_FOUND` | Searches repeatedly until it stops matching. |

The three waiting modes poll every few hundred milliseconds. A timeout of 0 waits forever, which in
an unattended execution means it never gives up.

Each try finds the search area again, so a wait follows its window: one that opens during the wait
is searched once it is there, and one that moves or changes size is searched where it is now. A try
that cannot look - no window yet, a minimised one, a template bigger than the window is now - counts
as neither found nor gone, and the next try looks again. A wait that gives up says why its last try
could not look. An area that no try can find - deleted, or a region with no size - fails the step at
once.

## Which mode to use, and why it matters for speed

`WAIT_UNTIL_STILL` costs little when the image is already there: the first search runs before any
delay, and one more look 100 ms later shows it standing still. Prefer it wherever the flow is
waiting for something that ought to appear.

`FIND_BEST` is for a **branch point** - "which layout am I in", "is the desktop nav there or the
hamburger". That question already has a final answer, so waiting on it buys nothing and costs the
whole timeout on every execution that takes the fallback.

The pattern that gets both right is to wait once on something always present, then branch freely:

```
Wait for the page logo        WAIT_UNTIL_STILL, timeout 15s
Is the desktop nav there?     FIND_BEST
  Failure: click the hamburger
```

A step with a populated Failure branch and a long timeout is usually a branch point written as a
wait.

## Waiting until it stops moving

`WAIT_UNTIL_STILL`, written `Wait For Still Image`, is the wait to use, and a recorded wait is always
one. Its first match is looked at again 100 ms later and counts only if the same template is in the
same place: within 2 pixels, more at a larger scale, which is the jitter of a match rather than
movement. If it moved, the next poll is held to where it went. So a click below it lands where the
thing came to rest, not where a slide-in was passing through.

```
Wait For Still Image  <[ Play button shows ]>   template <[ template-k2p7d.png ]> accuracy 0.8   in <[ App ]>   timeout 5000ms
Wait For Image  <[ Ticker shows ]>   template <[ template-m4q8z.png ]> accuracy 0.8   in <[ App ]>   timeout 5000ms
```

`WAIT_UNTIL_FOUND`, written `Wait For Image`, takes the first match. It is for something that never
stops: a bobbing icon, a scrolling ticker. A still wait on one waits out its timeout and fails with
"found but still moving when the time ran out". A glow or a pulse in place is still, because only
the position counts. A fade-in is not caught, since a fading thing does not move: raise that
template's accuracy instead.

Text has no still wait: `Wait For Text` reads words, and a click does not go to them.

## What WAIT_UNTIL_NOT_FOUND actually waits for

It waits for the search to **stop matching**, not for something to disappear. Nothing checks that
the thing was ever there, so if it never matched at all, the step succeeds immediately.

If you need "wait for the spinner to appear and then go away", that is two steps:
`WAIT_UNTIL_FOUND` then `WAIT_UNTIL_NOT_FOUND`.

When the window of the area closes, what it waited to see go went with it, so the step succeeds:
"No longer on screen: No window matches ...". A minimised window is not closed, and is waited out.

It is a condition and nothing more: there is no position to give, so no Move or Drag can go to
it. The dropdown does not offer it, and the validator reports a step still pointing at it.

## How FIND_ALL runs

`FIND_ALL` takes **one** screenshot and works through every hit found in it. The search never runs
a second time. Hits after the first appear in the execution as their own steps with a duration of 0 ms,
because they are served from the screenshot the first search already took.

This matters if the screen changes while you work through the hits — the positions come from the
moment of the first search, not from now.

Its Success branch executes once per hit, and inside that branch the search's own name is the hit
of the current pass. So a `CURSOR_RELOCATE` to the search, then a `CURSOR_CLICK`, clicks every hit
in turn. There is no separate loop for it; a step that should happen once after all the hits goes
after the search, not inside it.

**Max matches** limits how many hits are worked through, and only appears in this mode. It is the
step's total, not each template's: the hits are taken in the order of the template list, so the
first template's come first, and two templates that match the same spot are two hits.

## Several templates

A step's templates are alternatives: any one of them matching is enough. This is for several
variants of the same thing - a button and its hover state, an icon in two themes. They are tried
from the top of the list, and the first that matches ends the search and gives the point, so the
likeliest goes first. `FIND_ALL` tries them all and takes every hit, the first template's first.
With none found, the step fails once, so its Failure branch executes once.

The waiting modes follow: `WAIT_UNTIL_FOUND` and `WAIT_UNTIL_STILL` wait until any one is there,
the still one until the same one is there twice, and `WAIT_UNTIL_NOT_FOUND` until none is. **Test now** and an execution decide it the same way.

To act only when two different things are both on screen, use two steps: the second search inside
the first one's Success branch.

## Templates are shared

A template image belongs to the flow, and steps link to it. **Add template** captures a new one, or
reuses one the flow already has, picked by its picture. A pasted step links to the pictures of the
step it copies rather than copying them.

The picture, its click point and where it was captured are shared: editing them through one step,
or from the editor's Templates tab, changes them for every step that links to it, and the form asks
first when other steps use it. **Edit a copy for this step** gives one step a picture of its own.
The accuracy is each step's own. A template goes when the last step stops using it.

## Accuracy

Accuracy is how close a match has to be, between 0 and 1. **Each template a step searches for has
its own**, on that step — one variant of an icon can need a looser bar than another, and two steps
sharing a picture can search for it at different bars. There is no accuracy on the step.

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

A template with no recorded size or DPI - captured before they were recorded - is searched at the
size it was captured, on every screen.

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

**Details** opens what the test found, one template at a time. Each row has the template's score
against its accuracy and a strip from 0 to 1 marking its hit, its accuracy, and the closest place
on the screenshot that scored under it. Picking a row draws them on the screenshot - the hit solid,
with where it would click, and the closest place dashed - and shows the template beside both, so
"what did it nearly match instead?" is answered by looking. It suggests an accuracy: halfway between
the hit and the closest wrong place, or, when nothing was found, just under the closest place - which
is only right if that place is the thing. **Use** writes it to the step's form, in edit mode, and
Save keeps it. When the hit and the closest wrong place score close together, a tighter crop of the
template helps more than any accuracy. **Test again** tests from inside the dialog. The dialog
resizes with the mouse and maximises; the screenshot zooms to a box clicked, pans by dragging, and
zooms with Ctrl and the wheel.

The form also generates a sentence describing what the step will actually do, which is worth reading
when several settings interact.

## Clicking what a search found

The search does not click. A cursor step below it, inside the **Success** branch, takes the search
result as its position. "Find this and click it" is three steps: the search, a move, and a click.
