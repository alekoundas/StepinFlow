# Window steps

## The four window steps

| Step | What it does |
|---|---|
| `WINDOW_FOCUS` | Brings a window to the front |
| `WINDOW_RESIZE` | Sets a window's width and height |
| `WINDOW_RELOCATE` | Moves a window to a point |
| `WINDOW_CLOSE` | Closes a window, or a browser tab |

They share one form with four mode buttons, and all four have **Success** and **Failure**
branches — a window can simply not be there.

## How a window step finds its window

A window step names a **window area**: the same area a search looks inside, which finds its window
by process name and title pattern (`CONTAINS`, `EQUALS`, `STARTS_WITH`, `REGEX`). The window is
described once, on the Areas tab, and every step that searches in it or focuses, resizes, moves or
closes it follows when that description changes. Several window steps can name the same area.

A browser tab area works too: its window is the browser window holding the tab, and the tab is
brought to the front of it first, the front tabs tried before each tab in turn.

The area has to be a window or a tab. A monitor or a region inside a window has nothing to
focus, resize, move or close, and the step is refused with `AREA_NOT_A_WINDOW`. The step acts on the
whole window, title bar and borders included, whatever the area says about its frame.

```
Focus Window    <[ Browser ]>
Resize Window   <[ Browser ]>   size 1280 720
Move Window     <[ Browser ]>   to point <[ Left half ]>
Close Window    <[ Editor ]>   timeout 10000ms   then force
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

## Closing a window

`WINDOW_CLOSE` asks the window to close, the way clicking its X does, and waits for it to go: 5
seconds, or the step's `timeout`. The application gets to save its session and remove its own temp
files. A window that hides itself in the tray counts as gone. Success means the window went.
Failure means no window matched, or it was still open when the wait ran out - usually because it is
asking whether to save.

With `then force`, a window still open after the wait has its program ended: that one program and
whatever it started, found from the window itself, never every program of the same name. Nothing
is saved, and every other window of that program goes with it. A program that holds other
programs' windows too, such as Windows' own shell, is never ended.

A browser tab area closes only its tab, the way Ctrl+W does, once the browser is in front with the
tab in front of it. If the browser will not come to the front, no key is pressed and the step
fails, since Ctrl+W would close whatever is in front instead. A tab is never forced, because its
program is the whole browser: `then force` on one is `CLOSE_FORCED_ON_TAB`. A flow that wants to end
its browser starts its own, with its own profile, and names that window as an application area.

`Run KILL_PROCESS` is the other way to close something. It ends every program of a name at once,
with nothing saved, so it suits a program left over from a crash rather than a window to close.

A close goes at the end of the flow, or under an `End Execution` as cleanup, so the next execution
starts with the window freshly launched rather than wherever the last one left it.
