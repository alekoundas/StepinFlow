<div align="center">

<!-- SCREENSHOT 1 — optional logo. 120x120 PNG, transparent background.
     Put it at docs/images/logo.png and uncomment:
<img src="docs/images/logo.png" width="120" height="120" alt="StepinFlow">
-->

# StepinFlow

**Record a test once. Run it on every build, at every screen size, against real data.**

A QA tester records what they do to an application. The recording becomes a test that runs
unattended in CI and reports which checks passed — with no selectors, no DOM and no code.

</div>

<!-- SCREENSHOT 2 — THE HERO. The most important image in this file.
     The workflow page: step tree on one side, the form for the selected step on the other.
     Pick a flow with 8-15 steps including a branch, so the tree shows real structure.
     Full window, ~1600px wide. Save as docs/images/workflow.png -->

![The flow builder](docs/images/workflow.png)

---

## What it is

StepinFlow works from **what is on screen** — a template image, or text read by OCR — rather than
from a selector or an accessibility tree. So it does not care whether the thing under test is a
website, a desktop program, or something nobody has written an automation library for.

A test is a **flow**: a tree of typed steps that clicks, types, waits, reads the screen and branches
on what it finds. You build it in the UI or record it, and it exports to a text file you can review
in a pull request.

**Why it exists.** Record-and-replay tools have existed for twenty years and testers do not trust
them, because a recording breaks the first time anything moves and nobody can tell why. Three things
answer that here:

- **A recording is not a test until it has validated.** The app executes it once and says so, rather
  than letting a green suite prove nothing.
- **A failure is diagnosed, not just reported** — with the screenshots, the templates it was looking
  for, the flow as text, and a model that has read your own requirements.
- **The test is a file in your repository**, reviewable by someone who has never opened the app.

---

## Features

### Record

- **Setup first** — what the flow is called, what it tests and how to open it, what to do when it ends
- **A Test button on the launch command**, so a wrong one is caught before a recording is wasted on it
- **Pause mid-recording** to type a wrong value on purpose and capture how the app rejects it — that is
  how failure paths get written
- **Ctrl + left click** to add a check at that spot: must this exist, wait until it appears, wait until it goes
- **Ctrl + right click** to do something other than click: run a command, or take the value from a CSV column

### Build

- **Visual flow tree** — drag steps to reorder or renest, with the move validated before it happens
- **Reusable areas and points** — name a rectangle or a location once, reference it from any step
- **Sub-flows** — extract part of a flow and call it from anywhere; changes reach every caller
- **Live validation** — a flow tells you what is broken before you execute it
- **AI assistant** — local model always on, cloud provider optional, screen data off by default

### See the screen

- **Image search** — OpenCV template matching, multi-scale so a resized window still matches
- **Four search modes** — best match, every match, wait until found, wait until gone
- **Read text** — Windows OCR over a region, with a regex to pull out the part you want
- **Areas that follow a window** — anchor a region to an application window and the coordinates stay
  correct wherever the user drags it

### Execute and debug

- **Breakpoints** — click the gutter beside any step
- **Step into / step over** — step over executes a whole sub-flow and stops after it
- **Pause and continue** mid-execution
- **Live view** — every step as it happens, indented by how deep it ran
- **Execution history** — per-step duration, result and location
- **Failure screenshots** — nothing is written while a flow goes well; a failure writes the last few
  frames leading up to it, each named after the step that took it

<!-- SCREENSHOT 3 — the debugger, mid-execution or paused on a breakpoint.
     Show the toolbar (Continue / Step into / Step over active), a breakpoint dot in the tree,
     and the list with a few finished steps. This is the feature nothing else here has.
     Save as docs/images/debugger.png -->

![Executing a flow](docs/images/debugger.png)

### Ship results

- **Data-driven** — a flow's inputs are CSV columns, and it executes once per row
- **Viewports** — the same recording executes at every screen size you add
- **Secrets** — marked columns never reach a file; CI resolves them from the environment
- **Discord notifications** on failure, rate-limited so a retry loop cannot flood a channel

---

## Step types

Twenty-one step types in five groups. Any step that can fail has **Success** and **Failure**
branches, so a flow handles its own problems rather than stopping.

| Group | Step | What it does |
|---|---|---|
| **Control** | Wait | Pause, for a fixed time or a random range |
| | Loop | Repeat its children a number of times, or forever |
| | Go To | Jump to another step |
| | Sub-Flow | Execute another flow and come back |
| | End Execution | Finish, passed or failed, with a reason |
| | Marker | A named divider — becomes a heading in the script |
| **Input** | Cursor Click | Click at a point, a found image, or an earlier step's result |
| | Cursor Drag | Drag between two locations |
| | Cursor Scroll | Scroll at a location |
| | Cursor Relocate | Move the cursor without clicking |
| | Keyboard Input | Type text, a CSV column, or send key combinations |
| **Window** | Window Focus | Bring an application window to the front |
| | Window Resize | Resize a window |
| | Window Relocate | Move a window |
| **Perception** | Search Image | Find a template image on screen |
| | Search Text | OCR a region, optionally extracting with a regex |
| **Decision** | Check Value | Test what an earlier step produced |
| **System** | System Command | Run a shell command and check its exit code |
| | System Action | Sleep, lock, shut down and similar |
| | Notify | Post a message to Discord |

Every step can be positioned from a **named point**, a **found image**, or **another step's result**
— which is what makes a flow survive the window moving.

---

## How it works

Three processes, talking over two named pipes:

```
 Electron main  ──"stepinflow-request"────►  .NET host      request / response
       ▲                                          │
       │        ──"stepinflow-broadcast"──────────┘         server → client push
       │
  React renderer
```

The **.NET host** owns the database, the screen, the mouse and the keyboard. **Electron** is the
shell and the bridge; the React renderer never talks to .NET directly. The IPC envelope is protobuf
and the body is JSON, so adding a new DTO never touches the `.proto`.

### The execution engine

A flow is walked with an **explicit stack**, not recursion — infinite loops and `Go To` make
recursion depth unbounded, and a stack gives pause, resume and step-into almost for free.

Everything an execution needs sits in memory and is dropped as the walk leaves it behind, so a flow
running for three weeks holds no more than one running for three seconds.

### The backend layering

```
App ──────→ Business ──→ DataAccess ──→ Core
 └────────→ Platform.Windows ─────────→ Core
```

`Business` holds the domain — validation, the script writer, the execution walker — and **cannot
reach native code**, because it does not reference `Platform.Windows`. The ports it calls
(`IScreenshotService`, `IInputService`, `IWindowService` …) are declared in `Core` and bound to
their Windows adapters in `App`, the composition root. That boundary is what keeps the domain
testable without a screen, and what a Linux port would slot into.

See [PROJECT.md](PROJECT.md) for the full architecture.

---

## Tech stack

| Layer | Technology |
|---|---|
| Shell | Electron, multiple BrowserWindows |
| UI | React 19, TypeScript, Vite (rolldown) |
| Components | PrimeReact + PrimeFlex |
| State | Zustand (UI), TanStack Query (server) |
| Forms | React Hook Form + Zod |
| Backend | .NET 10, MediatR, AutoMapper |
| Data | EF Core 10 + SQLite |
| Input | SharpHook — global hook and event simulation |
| Vision | OpenCvSharp4 template matching, `Windows.Media.Ocr` |
| Capture | Direct3D11 / `Windows.Graphics.Capture` |
| AI | Ollama or OpenAI, ONNX embeddings, USearch vector index |
| IPC | Named pipes, protobuf-net ↔ protobufjs |

**DPI-aware, everything in physical pixels** — a flow authored on a 150% display executes correctly
on a 100% one.

---

## Getting started

### Requirements

- Windows 10 (build 22621) or newer
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) 20 or newer

### Run in development

```bash
git clone https://github.com/alekoundas/StepinFlow.git
cd StepinFlow
npm run install:full
npm run dev
```

That starts the Vite dev server, the .NET host and Electron together. The SQLite database is created
on first run and migrations are applied at startup.

### Build a release

```bash
npm run build
```

Publishes the backend self-contained for `win-x64`, builds the renderer, and packages everything with
electron-builder into `release/`.

### Useful scripts

| Script | What it does |
|---|---|
| `npm run dev` | Everything, with hot reload |
| `npm run dev:no-api` | UI only, no .NET host |
| `npm run lint` | ESLint over the renderer |
| `npm run protobuf:generate` | Regenerate the protobuf bindings |

---

## Documentation

| File | What is in it |
|---|---|
| [PROJECT.md](PROJECT.md) | The whole application — architecture, data model, execution, AI, CI |
| [FLOW-FORMAT.md](FLOW-FORMAT.md) | The `.sflw` flow script grammar |
| [PLAN.md](PLAN.md) | Build order, and what has landed |
| [TODO.md](TODO.md) | Everything deferred |

---

## Status

In active development, and not yet released. The builder, recorder, image search, OCR, sub-flows,
notifications, execution engine and AI assistant all work.

The flow script **writer** is done and verified; the **parser** is not, so nothing round-trips yet.
Linux is on the roadmap — the port boundary exists for it, but Wayland makes it a larger job than a
straight port. See [PROJECT.md §2](PROJECT.md) for why.

---

## Licence

[GNU General Public License v3.0 or later](LICENSE).

Copyright (C) 2026 Alex Psihogios.

StepinFlow is free software: you can redistribute it and modify it under the terms of the GPL as
published by the Free Software Foundation, either version 3 or (at your option) any later version.
It is distributed in the hope that it will be useful, but **without any warranty** — without even the
implied warranty of merchantability or fitness for a particular purpose.

In short: fork it, change it, use it. If you distribute a modified version, that version has to be
open under the same licence.
