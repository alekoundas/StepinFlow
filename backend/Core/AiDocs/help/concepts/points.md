# Points

## What a point is

A point is a single screen position, worked out fresh every time a flow runs. Cursor steps that
carry a position use one, and so does moving a window.

## Anchored and absolute points

A point is either anchored to an area or absolute.

An anchored point is stored as an offset from the area's top-left corner, in pixels or as a
percentage. When the flow runs, the area is resolved first and the point is placed relative to it.
An absolute point is a fixed screen position.

Anchor a point whenever the thing it points at belongs to a window. An absolute point stops being
right the moment the window moves.

## Capturing a point

Choose **Capture Location** and then click anywhere on screen. No window opens and nothing is
dimmed — the click itself is the capture. Press Escape to cancel.

This works whether the point is stored in pixels or as a percentage.

## Testing a point

The **Test** button physically moves your mouse to where the point resolves. If the mouse lands
somewhere unexpected, the point is anchored to the wrong area or the area is resolving somewhere
you did not expect.

## Where a cursor step gets its position

A cursor step that carries a position takes it from one of two places:

- **A saved point** — a named point on the flow, reusable by several steps.
- **An earlier step's result** — an `IMAGE_SEARCH` or `READ_TEXT` above it, whose match position
  becomes the cursor's target.

Only steps reachable through a **Success** branch above the cursor step are offered as a source.
Anything else might not have run by the time the cursor step executes.
