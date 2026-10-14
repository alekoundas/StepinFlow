# Recording a flow

## What the recorder does

The recorder watches what you do and turns it into a list of actions you then answer questions
about. It does not guess what the steps should be.

A flow is created before recording starts, so every form has a real flow to attach areas and points
to while you work.

## What is and is not recorded

Clicks, drags, scrolls and typing are recorded. A press and release become one click; a burst of
typing becomes one entry; a long gap becomes a pause.

Cursor movement on its own is not recorded. Recording every movement would produce thousands of
events in a couple of minutes and bury the ones that matter.

When you press a mouse button, the screen around the pointer is captured — the screenshot you were
looking at when you decided to click, rather than whatever the click then changed. How much is
captured is a setting.

## The recording bar

Record now minimises StepinFlow and opens a small bar at the top of the screen. Arrange your windows,
then press Start on the bar. While recording, the bar shows whether it is recording or paused, a
clock, the last few actions, and Pause and Stop. Closing the bar before Start records nothing and
brings StepinFlow back. Nothing you do on the bar is recorded, and it never appears in a
screenshot. Pause and Stop also have keys, F9 and F10 by default, which work whatever window is in
front and are never recorded; they are under Recording keys in Settings. Typed text shows in the
bar as a count, "Typed 15 characters", never as the text.

## What each click hit

When the recording stops, StepinFlow looks at the screenshot from before each click and finds the
element under it — the button, field or icon — and reads the text on it. It takes about a second
per click, and the recording page shows how far it has got. Each click then shows that element and
its text, marked "found by the model" or "box around the click". A box around the click means the
model had nothing to say: no element was under the click, the element was bigger than the area it
looked at, or the model file is not installed.

## Turning actions into steps

For each recorded action the wizard asks two things:

**What should this become?** A click could be a plain cursor click, an image search that finds the
thing first, or only a check that something is on screen. Only you know which.

**Where does it go?** After the last step, inside its Success branch, or back out of the branch.

One action can produce several steps. "Find this image then click it" becomes a search, a move and a
click, with the last two inside the search's Success branch and the move pointing at the search.

## The tree while you work

The tree beside the wizard grows as you answer, with a dashed row for the action you are on.
Nothing is drawn ahead of your answers, because placement is your decision.

## Going back

Going back to an earlier action discards everything after it, with a warning first. Placement
cascades — later steps can sit inside the one you are changing — so they cannot be kept.

## What you fill in

Only what the recording could not know: a name, the crop for a template, the search area, the text
to type. Not the step's whole form; the rest is filled in when the flow is saved and can be edited
afterwards in the workflow editor.

## Saving

Saving writes everything in one transaction: steps, any new points, the Success and Failure
branches, and the ordering.
