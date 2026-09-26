<div align="center">

<!-- SCREENSHOT 1 — optional logo. 120x120 PNG, transparent background.
     Put it at docs/images/logo.png and uncomment:
<img src="docs/images/logo.png" width="120" height="120" alt="StepinFlow">
-->

# StepinFlow

**Record a test once. Run it on every build, at every screen size, against real data.**

A QA tester records what they do to an application. The recording becomes a test that runs
unattended and reports which checks passed — with no selectors, no DOM and no code.

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

- **A recording is not a test until it has validated.** A flow that never says whether it passed is
  reported as *inconclusive*, not green — a suite of those proves nothing, and the app says so.
- **A failure is diagnosed, not just reported** — with the screenshots, the template that came
  closest and by how much, and a model that can read your flows and their history.
- **The test is a file in your repository**, reviewable by someone who has never opened the app.

---

## Features

### Record

- **Record once, decide afterwards** — every click, drag, scroll and keystroke is captured with the
  pause before it and a screenshot at each click
- **A wizard turns each action into steps**: click here, find this image and then click it, only check
  it is on screen, wait until something appears, type text or send key presses
- **Templates cropped from the recording itself**, so nothing is captured twice

### Build

- **Visual flow tree** — drag steps to reorder or renest, with the move validated before it happens
- **Reusable areas and points** — name a window, a monitor or a region once, reference it from any step
- **Sub-flows** — extract part of a flow and call it from anywhere; changes reach every caller
- **Live validation** — a flow tells you what is broken before you execute it, and the flow list shows
  every flow's error and warning count
- **Teardown is steps** — cleanup written under `End Execution` runs whether the flow passed or failed
- **A flow script** — the whole flow as a `.sflw` text file, exported and imported with a byte-identical
  round trip

### See the screen

- **Image search** — OpenCV template matching with two modes: *shape*, and *shape and brightness* for
  telling an enabled button from a disabled one
- **Accuracy per template**, and templates that are alternatives or all **required**
- **Four search modes** — best match, every match, wait until found, wait until gone
- **Read text** — Windows OCR over a region, with a regex to pull out the part you want
- **Portable across screens** — each area says whether its contents scale with the screen's DPI (a
  browser, an app) or with its own size (a game), and every captured pixel carries the DPI it was
  captured at. A template captured at 150% is found at 100%, and in a window that is not maximised
- **Areas that follow a window** — anchor a region to an application window and the coordinates stay
  correct wherever the user drags it

### Execute and debug

- **Breakpoints** — click the gutter beside any step
- **Step into / step over** — step over executes a whole sub-flow and stops after it
- **Pause and continue** mid-execution
- **Live view** — every step as it happens, indented by how deep it ran
- **Execution history** — per-step result, duration, location and closest score
- **Failure screenshots** — nothing is written while a flow goes well; a failure writes the last few
  screenshots leading up to it, each named after the step that took it
- **Discord notifications** from a Notify step, rate-limited so a retry loop cannot flood a channel

<!-- SCREENSHOT 3 — the debugger, mid-execution or paused on a breakpoint.
     Show the toolbar (Continue / Step into / Step over active), a breakpoint dot in the tree,
     and the list with a few finished steps. This is the feature nothing else here has.
     Save as docs/images/debugger.png -->

![Executing a flow](docs/images/debugger.png)

### Ask

- **Ask about your flows** — a chat that answers by querying your own flows, executions and the app's
  documentation through tool calls, rather than being handed a dump
- **Explain an execution** — what failed, the closest match, and what to change
- **Ollama on your machine or OpenAI** — screen content never goes to a cloud provider unless you
  switch it on, and credentials are never shown to a model

---

## Step types

Twenty step types in six groups. Any step that can fail has **Success** and **Failure** branches, so
a flow handles its own problems rather than stopping.

| Group | Step | What it does |
|---|---|---|
| **Control** | Wait | Pause, for a fixed time or a random range |
| | Loop | Repeat its children a number of times, or forever |
| | Go To | Jump to another step |
| | Sub-Flow | Execute another flow and come back |
| | End Execution | Finish, passed or failed, with a reason — and run the cleanup written under it |
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
and the body is JSON, so adding a new DTO never touches the `.proto`. Requests are routed by a
hand-written switch — one line per action, so a duplicate route is a compile error and F12 reaches
the handler.

### The execution engine

A flow is walked with an **explicit stack**, not recursion — infinite loops and `Go To` make
recursion depth unbounded, and a stack gives pause, resume and step-into almost for free.

The walker that decides which step comes next executes nothing: it is handed each step's result. So
the most intricate code in the repository is also the cheapest to test.

### The backend layering

```
App ──→ Transport ──→ Business ──→ DataAccess ──→ Core
 └────→ Platform.Windows ─────────────────────→ Core
```

`Business` holds the domain — validation, the flow script, the execution walker, the searcher — and
**cannot reach native code**, because it does not reference `Platform.Windows`. The ports it calls
(`IScreenshotService`, `IInputService`, `IWindowService` …) are declared in `Core` and bound to
their Windows adapters in `App`, the composition root. `Transport` is how a request gets in and
cannot reach the machine either. That boundary is what keeps the domain testable without a screen,
and what a Linux port would slot into.

It is enforced three ways rather than by convention: the target framework, a banned-API list that
makes `Process` and `DllImport` a build error outside `Platform.Windows`, and architecture tests.

See [PROJECT.md](PROJECT.md) for the full architecture.

---

## Tests

**365 backend tests**, and three of the layers found a real bug on their first run — `Press Ctrl+1`
pressing Ctrl+B, `Wait Until No Image` failing exactly when the image went away, and `System 99`
parsing as a system action that does not exist.

- **Architecture tests** turn the layering above into failing tests
- **Hand-written fakes** for every port, recording what they were told as readable lines —
  `"move 200,80"`, `"press LeftCtrl+LeftShift+T"` — so a test reads like the flow it drives
- **A real SQLite database per test**, built by the real migrations, and a test that fails if an
  entity changes without one
- **The flow script** round-trips byte for byte and row for row, against an approved sample file a
  person has read
- **Coverage reported per project, not as one number** — the domain high, the code that touches the
  machine near zero on purpose

```bash
npm run test:backend
npm run coverage:backend
```

---

## Tech stack

| Layer | Technology |
|---|---|
| Shell | Electron 40, electron-builder, electron-updater |
| UI | React 19, TypeScript, Vite (rolldown), React Compiler, React Router 7 |
| Components | PrimeReact + PrimeFlex |
| State | Zustand (UI), TanStack Query (server) |
| Forms | React Hook Form + Zod |
| Backend | .NET 10, a hand-written IPC dispatcher, AutoMapper |
| Data | EF Core 10 + SQLite |
| Input | SharpHook — global hook and event simulation |
| Vision | OpenCvSharp4 template matching, `Windows.Media.Ocr` |
| Capture | `Windows.Graphics.Capture` over Direct3D11 |
| AI | Microsoft.Extensions.AI with tool calling, OllamaSharp, OpenAI |
| Docs search | ONNX Runtime embeddings, Microsoft.ML.Tokenizers, USearch vector index |
| IPC | Named pipes, protobuf-net ↔ protobufjs |
| Tests | xUnit v3 on Microsoft.Testing.Platform, Shouldly, ArchUnitNET, FakeTimeProvider |
| Coverage | coverlet + ReportGenerator |
| Code quality | .NET analyzers with warnings as errors, BannedApiAnalyzers, ESLint 9 + typescript-eslint |

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

`install:full` installs both npm projects and downloads the embedding model the assistant's
documentation search uses. `dev` starts the Vite dev server, the .NET host and Electron together.
The SQLite database is created on first run and migrations are applied at startup.

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
| `npm run test:backend` | Every backend test project |
| `npm run coverage:backend` | The tests with coverage, as an HTML report in `backend/coveragereport/` |
| `npm run lint` | ESLint over the renderer |
| `npm run protobuf:generate` | Regenerate the protobuf bindings |
| `npm run model:download` | Fetch the embedding model, if it is missing |

---

## Documentation

| File | What is in it |
|---|---|
| [PROJECT.md](PROJECT.md) | The whole application — architecture, data model, execution, search, AI, tests |
| [FLOW-FORMAT.md](FLOW-FORMAT.md) | The `.sflw` flow script grammar |
| [SampleFlow.approved.sflw](backend/Tests/Business.Tests/FlowScript/SampleFlow.approved.sflw) | A flow script using most of the grammar, checked by a test |
| [PLAN.md](PLAN.md) | What is left to build, in order |
| [TODO.md](TODO.md) | Everything deferred |

---

## Status

In active development, and not yet released. The builder, recorder, image and text search,
sub-flows, validation, notifications, the execution engine with its debugger, and the AI assistant
all work.

The flow script round-trips both ways — export, import, export again, byte identical — but has no
button in the UI yet. CSV inputs, the viewport matrix and the CI runner are next on the roadmap.

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
