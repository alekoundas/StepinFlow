# Areas

## What an area is

An area is a rectangle on screen, worked out fresh every time a flow runs. Steps that look at the
screen — image search and read text — search inside an area rather than the whole desktop, and the
window steps — focus, resize and move — act on the window an `APPLICATION` area finds.

An area is not fixed coordinates. It is a rule for finding a rectangle, which is why a flow built
on one machine can work on another.

## Area types

| Type | What it finds |
|---|---|
| `CUSTOM` | A rectangle you drew. Can sit inside another area; with nothing around it, it is fixed screen coordinates. |
| `APPLICATION` | A window, found by process name and title pattern. |
| `MONITOR` | A whole screen. **Primary monitor** is whichever monitor is primary, the one choice that means the same thing on another PC. A named one (`\\.\DISPLAY2`) can be renumbered when monitors are plugged in or out. |
| `BROWSER_TAB` | A tab of a browser, by its address - `tab <[ shop.example.com/cart ]>` after the browser's process in the script. The area is the tab's page. |

## Application areas

An `APPLICATION` area finds a window at runtime by process name plus a title pattern. The pattern
is matched with one of `CONTAINS`, `EQUALS`, `STARTS_WITH` or `REGEX`.

When several windows match, the frontmost one is used. There is no instance index.

The same area is what a window step names, so one description of a window serves the searches
inside it and every step that focuses, resizes or moves it.

**Client area** is an option. With it on, the area covers the window's content and excludes the
title bar and borders, so a window with a different border style does not shift everything inside.
It is on unless the window's line in the script ends with `with frame`.

The **Application** field lists one row per app, with its open windows' titles under it, because
the area matches the app, not one of its windows.

**Started by** says what a `Launch` of the area starts when the app is not open - a command line, or
a link. Picking an open app fills it in: its program file, or for a game in a Steam library
`steam://rungameid/<id>`, since a Steam game started from its own program file restarts through
Steam or refuses. `started by <[ … ]>` on the window's line in the script.

## Browser tab areas

A `BROWSER_TAB` area is a tab of a browser - Chrome, Edge, Brave or Firefox - found by its
**address**, and the area is the tab's page: what is below the address bar, not the browser around
it. The tab whose address starts with the area's is the one, `https://` and `www.` left out on both
sides, so an address pasted from the address bar matches what the address bar shows.

It names its **browser** and no window title. A browser's title is its front tab's, so a title
would only match while the tab was already in front. Every window of the browser is looked in.

Only a window's front tab says its address, which decides what each step does:

| step | a tab behind another |
|---|---|
| a search in the area | not on screen: the search fails, and never switches tabs behind your back |
| `Focus Window`, `Resize Window`, `Move Window` | brought to the front of its window first, then the window acted on |
| `Launch` | brought to the front rather than opened again |

Bringing one to the front tries the front tab of each window first, then each tab in turn, and a
window it is not in goes back to the tab it showed. Several tabs flick past while it looks.

**Started by** is the browser, filled in when an open one is picked. A `Launch` with no tab at the
address starts it with `--new-window` and the address, so the tab does not land among whatever the
browser's last window had open.

The tabs are read through Windows' accessibility interface, the way a screen reader reads them. On
macOS and Linux a tab area does not resolve yet.

## Nesting areas inside other areas

A `CUSTOM` area can sit inside another area, one level deep. The parent can be any type; the child
is always `CUSTOM`, because the other types find their own rectangle and ignore a parent.

One level deep means the parent sits inside nothing, and an area with areas inside it cannot go
inside another. The form only offers areas that sit inside nothing, and hides **Inside** on an area
that has regions inside it. A save or an import that breaks this is refused.

The child is stored as an offset from the parent, either in pixels or as a percentage. When the
flow runs, the parent is resolved first and the child is placed relative to wherever the parent
turned out to be.

Pixels are stored with the DPI they were captured at, so inside a parent that scales with DPI they
grow with the monitor's scaling. Inside a parent that scales with its size, percent is the form
that survives a resize.

This is what makes a flow portable. Resize the application window at the start of a flow, and
everything defined inside it lands in the same relative place on any machine.

## The main area

One area can be marked **Main area** - `IsMain` in the data, `main` on its line in the script. It
is the window the flow works in. A recording is stored relative to it, and a viewport resizes it.

There is at most one, and it always sits inside nothing: the main area is the window itself, never
a region of one. Saving a second area as main is refused until the first is unmarked, and so is an
import with two, or with one inside another area.

A main area that is a `CUSTOM` rectangle on screen is fixed screen coordinates, so the flow works on
the machine it was made on and nowhere else. Make the main area an application window instead.

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
