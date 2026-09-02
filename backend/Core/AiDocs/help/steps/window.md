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

The match lives on the step itself: process name, title pattern, and how to match the title
(`CONTAINS`, `EQUALS`, `STARTS_WITH`, `REGEX`).

This is deliberately not an area. A window step cares about a window, not about a rectangle inside
one.

## Why resizing a window matters

`WINDOW_RESIZE` is what makes a flow portable.

Fix the window to a known size at the start of a flow, and everything inside it lands in a
reproducible place. Areas defined as offsets inside that window then resolve identically on any
machine, and templates captured at that size match without scaling.

A flow that starts with a resize is far more reliable than one that hopes the window is where it
was yesterday.

## Moving a window

`WINDOW_RELOCATE` moves the window to a point. The position is always the outer frame, not the
client area.
