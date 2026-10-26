# Window steps

## The three window steps

| Step | What it does |
|---|---|
| `WINDOW_FOCUS` | Brings a window to the front |
| `WINDOW_RESIZE` | Sets a window's width and height |
| `WINDOW_RELOCATE` | Moves a window to a point |

They share one form with three mode buttons, and all three have **Success** and **Failure**
branches — a window can simply not be there.

## How a window step finds its window

A window step names a **window area**: the same area a search looks inside, which finds its window
by process name and title pattern (`CONTAINS`, `EQUALS`, `STARTS_WITH`, `REGEX`). The window is
described once, on the Areas tab, and every step that searches in it or focuses, resizes or moves it
follows when that description changes. Several window steps can name the same area.

The area has to be a window. A monitor, a region inside a window or a browser tab has nothing to
focus, resize or move, and the step is refused with `AREA_NOT_A_WINDOW`. The step acts on the whole
window, title bar and borders included, whatever the area says about its frame.

```
Focus Window    <[ Browser ]>
Resize Window   <[ Browser ]>   size 1280 720
Move Window     <[ Browser ]>   to point <[ Left half ]>
```

## Why resizing a window matters

`WINDOW_RESIZE` is what makes a flow portable.

Fix the window to a known size at the start of a flow, and everything inside it lands in a
reproducible place. Areas defined as offsets inside that window then resolve identically on any
machine, and templates captured at that size need no scaling on a screen with the same Windows
scaling - on one without, they scale by what their area's contents scale with.

A flow that starts with a resize is far more reliable than one that hopes the window is where it
was yesterday.

## Moving a window

`WINDOW_RELOCATE` moves the window to a point. The position is always the window's outer bounds, not
the client area.
