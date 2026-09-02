# Areas

## What an area is

An area is a rectangle on screen, worked out fresh every time a flow runs. Steps that look at the
screen — image search and read text — search inside an area rather than the whole desktop.

An area is not fixed coordinates. It is a rule for finding a rectangle, which is why a flow built
on one machine can work on another.

## Area types

| Type | What it finds |
|---|---|
| `CUSTOM` | A rectangle you drew. Can sit inside another area. |
| `APPLICATION` | A window, found by process name and title pattern. |
| `MONITOR` | A whole screen. |
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

This is what makes a flow portable. Resize the application window at the start of a flow, and
everything defined inside it lands in the same relative place on any machine.

## Capturing an area

Choose capture, and a transparent overlay covers the screen. Drag a box.

If the area has a parent, the overlay dims everything outside the parent, outlines it, names it,
and stops the drag from leaving it — so you cannot draw a region that would be cropped away when
the flow runs.

The parent is resolved before the overlay opens. An application that is not running stops you
before you draw rather than after.

The offset is worked out for you. You never type coordinates.

## Editing an area that has children

Editing a parent warns about any pixel-positioned children inside it. Children that no longer fit
are detached when you save and keep their position on screen. They are never cropped and never
deleted — the same policy applies when a parent area is deleted.

## Deleting an area a step uses

The step survives and its area reference is cleared. Validation then reports `AREA_MISSING` on that
step.
