# Troubleshooting

## An image search finds nothing

In rough order of likelihood:

**The area is not where you think.** The search only looks inside its area. If the area is anchored
to a window that moved, resized, or is not running, the search is looking at the wrong pixels. Test
the area first, then the search.

**Accuracy is too high.** Anti-aliasing, a different theme, or a slightly different scroll position
all lower the match score. Use **Test now** — it reports the score for each template, so you can see
how close it got. Each template has its own accuracy; a score just under it is this.

**The template did not scale to this screen.** A score far under the accuracy, on a screen other
than the one it was captured on, usually means the area's **Contents scale with** is wrong: Screen
DPI for a browser or a normal app, Area size for a game. A template of text does not survive a
change of Windows scaling at all - read words with Search Text. A template with no captured size or
DPI is never scaled; recapture it.

**The thing is not there yet.** `FIND_BEST` searches once. If the screen is still loading, use
`WAIT_UNTIL_FOUND` with a timeout instead.

## An image search finds the wrong thing

Accuracy is too low, so a roughly-similar region scores above the threshold. Raise it, and use
**Test now** to see what score the correct match gets — set the threshold between the two.

A very small or very plain template matches too many things. Capture more around it. A template
of one flat colour matches everywhere.

It finds the right thing in the wrong state - a disabled button instead of the live one. `SHAPE`
ignores brightness; switch the step to `SHAPE_AND_BRIGHTNESS`.

## A click lands in the wrong place

**The step has no position.** `CURSOR_CLICK` clicks wherever the cursor already is. Clicking
somewhere specific needs a `CURSOR_RELOCATE` first.

**The point is absolute but the window moved.** Anchor the point to an area instead.

**The point is in pixels and the screen's scaling changed.** A point in pixels inside an area that
scales with DPI moves with the monitor's scaling, but only if the DPI it was captured at was
recorded. Capture it again, or use percent.

**The click offset is wrong.** Each template stores where inside it to click. Open the template in
the image editor and check the point.

**The step reads a search that did not run.** If a cursor step reads a search result and the search
took its Failure branch, there is no result. Check the cursor step is inside the search's Success
branch.

## Read Text reads the wrong characters

Use the **Test** button. It shows the full text read and what survived the Keep only expression, so
you can tell a misread from a wrong condition.

Common causes: the area includes too much, the text is small, or the text is anti-aliased against a
busy background. Tightening the area usually helps more than anything else.

If the language is not English, check that the Windows language pack is installed — the dropdown
only offers packs that are.

## A window step does not find its window

The process name is the executable name without `.exe`. The title pattern is matched with the mode
you chose — a title that changes as the app is used will not match `EQUALS`.

Where several windows match, the frontmost one wins.

## The flow works on my machine but not another

This is what areas and points are for. A flow that uses absolute positions is tied to one screen
layout - validation warns about each one with `SCREEN_COORDINATES`.

Start the flow with a `WINDOW_FOCUS` and a `WINDOW_RESIZE`, define areas inside that window, and
anchor points to those areas. Everything then resolves relative to a window of a known size, on any
machine.

Then check what each area's contents scale with. A browser or a normal app keeps its contents' size
when the window changes and only the monitor's scaling moves them - Screen DPI. A game stretches
its picture with the window - Area size. The wrong one makes every template in the area the wrong
size.

## An execution ended and I do not know why

Open the execution. The step that ended it is red and says so. Failures that a Failure branch caught are
amber and marked **handled** — those are the flow working, not the problem.

If screenshots were kept, the failed step has the screenshots leading up to it. Most of them belong to
earlier steps, which is deliberate: the screenshot at the moment of failure usually shows a screen the
thing was never on.

If an AI provider is set up, the **Explain** tab reads the execution and says what it thinks went wrong.

## The execution will not start

Only one execution happens at a time. A second start is refused while one is going.

Check the flow's validation badges too — a flow with errors can still be started, but a step with a
missing area or point will fail when it is reached.

## A loop never ends

That is a supported thing to build. Stop it with the debugger's stop key, or put a step inside the
loop that ends the execution.

## Nothing types where I expect

`KEYBOARD_INPUT` types wherever the keyboard focus already is. Put a `WINDOW_FOCUS` step or a click
before it.
