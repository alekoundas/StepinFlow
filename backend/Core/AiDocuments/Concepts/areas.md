# Areas

## What an area is

An area is a rectangle on screen, worked out fresh every time a flow runs. Steps that look at the
screen — image search and read text — search inside an area rather than the whole desktop.

An area is not fixed coordinates. It is a rule for finding a rectangle, which is why a flow built
on one machine can work on another.

## Area types

| Type | What it finds |
|---|---|
| `CUSTOM` | A rectangle you drew. Can sit inside another area; with nothing around it, it is fixed screen coordinates. |
| `APPLICATION` | A window, found by process name and title pattern. |
| `MONITOR` | A whole screen. **Primary monitor** is whichever monitor is primary, the one choice that means the same thing on another PC. A named one (`\\.\DISPLAY2`) can be renumbered when monitors are plugged in or out. |
| `BROWSER_TAB` | Modelled but not implemented. The resolver reports that it is not supported yet. |

## Application areas

An `APPLICATION` area finds a window at runtime by process name plus a title pattern. The pattern
is matched with one of `CONTAINS`, `EQUALS`, `STARTS_WITH` or `REGEX`.

When several windows match, the frontmost one is used. There is no instance index.

**Client area** is an option. With it on, the area covers the window's content and excludes the
title bar and borders, so a window with a different border style does not shift everything inside.

## Nesting areas inside other areas

A `CUSTOM` area can sit inside another area, one level deep. The parent can be any type; the child
is always `CUSTOM`, because the other types find their own rectangle and ignore a parent.

The child is stored as an offset from the parent, either in pixels or as a percentage. When the
flow runs, the parent is resolved first and the child is placed relative to wherever the parent
turned out to be.

Pixels are stored with the DPI they were captured at, so inside a parent that scales with DPI they
grow with the monitor's scaling. Inside a parent that scales with its size, percent is the form
that survives a resize.

This is what makes a flow portable. Resize the application window at the start of a flow, and
everything defined inside it lands in the same relative place on any machine.

## What an area's contents scale with

Every area says what makes the things inside it bigger or smaller on another screen - **Contents
scale with** on the form, `ScalesWith` in the data. It decides how every template searched in the
area is scaled.

| Setting | For | What it follows |
|---|---|---|
| `DPI` - Screen DPI | browsers, normal apps, the OS | the monitor's scaling. Resizing the window moves things but does not resize them. |
| `AREA` - Area size | games | the area's size. The picture stretches with the window, and gets bars when its shape changes. |

Left unset, a region inside another area follows its parent, and any other area follows the DPI.
An **application** has no default and must be told, because a normal app and a game look the same
from outside. A game running inside a browser is the case for the two to differ: the browser tab
scales with DPI, the game region inside it with its size.

The wrong setting makes every template in the area the wrong size on another screen, which shows up
as a search scoring far below its accuracy.

## Screen coordinates

A `CUSTOM` area with no parent - and a region inside one - is fixed screen coordinates. It is right
on the screen it was drawn on and somewhere else on any other. Every step that uses one gets a
`SCREEN_COORDINATES` warning. Put it inside a window or a monitor instead.

## Capturing an area

Choose capture, and a transparent overlay covers the screen. Drag a box.

If the area has a parent, the overlay dims everything outside the parent, outlines it, names it,
and stops the drag from leaving it — so you cannot draw a region that would be cropped away when
the flow runs.

The parent is resolved before the overlay opens. An application that is not running stops you
before you draw rather than after.

The offset is worked out for you, and so is the DPI it was captured at. You never type
coordinates.

## Editing an area that has children

Editing a parent warns about any pixel-positioned children inside it. Children that no longer fit
are detached when you save and keep their position on screen. They are never cropped and never
deleted — the same policy applies when a parent area is deleted.

## Deleting an area a step uses

The step survives and its area reference is cleared. Validation then reports `AREA_MISSING` on that
step.
