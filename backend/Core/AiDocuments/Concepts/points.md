# Points

## What a point is

A point is a single screen position, worked out fresh every time a flow runs. Cursor steps that
carry a position use one, and so does moving a window.

## Anchored and absolute points

A point is either anchored to an area or absolute.

An anchored point is stored as an offset from the area's top-left corner, in pixels or as a
percentage. When the flow runs, the area is resolved first and the point is placed relative to it.
Pixels are stored with the DPI they were captured at, so inside an area that scales with DPI the
point moves with the monitor's scaling.

What a point is measured in follows the area it is measured from. Inside an area whose contents
scale with its size, such as a game that stretches with its window, pixels are not scaled at all,
so the form picks Percent and keeps it there. Inside an area that scales with the screen's DPI it
picks Pixels, since a button keeps its distance from the corner as the window grows, and Percent
can still be chosen for something laid out proportionally.

An absolute point is a fixed screen position. Anchor a point whenever the thing it points at
belongs to a window: an absolute point stops being right the moment the window moves, and on any
other screen. Every step that uses one - or a point inside a region that is itself in screen
coordinates - gets a `SCREEN_COORDINATES` warning.

## Capturing a point

Choose **Capture Location** and then click anywhere on screen. No window opens and nothing is
dimmed — the click itself is the capture. Press Escape to cancel.

This works whether the point is stored in pixels or as a percentage. With an area chosen, the
area must be on screen when you capture, because the offset is measured from where it is now.

## Testing a point

The **Test** button physically moves your mouse to where the point resolves, worked out exactly as
a flow does. It works in the point's own form before it is saved, and beside a saved point in a
Move or a Drag. A point measured from an area added in the same unsaved edit cannot be tested
until the flow is saved. If the mouse lands somewhere unexpected, the point is anchored to the
wrong area or the area is resolving somewhere you did not expect.

A Move or a Drag between saved points has **Test now** in its form, which does the move or the
whole drag for real, as the flow would. A step's result exists only while the flow executes, so a
Move or a Drag that uses one cannot be tested.

## Where a cursor step gets its position

A cursor step that carries a position takes it from one of two places:

- **A saved point** — a named point on the flow, reusable by several steps.
- **An earlier step's result** — a `SEARCH_IMAGE` or `SEARCH_TEXT` above it, whose match position
  becomes the cursor's target.

Only steps reachable through a **Success** branch above the cursor step are offered as a source.
Anything else might not have run by the time the cursor step executes.
