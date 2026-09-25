# StepinFlow — Project Reference

> The whole application in one document: what it is for, how it is built, what exists today, and
> what is knowingly unfinished. Written to be read top to bottom by a person joining the project,
> or pasted into a model as context.
>
> `FLOW-FORMAT.md` holds the flow script grammar and is kept separate on purpose — it is a
> specification you read while writing a parser, not prose. `PLAN.md` is the build order.
> `TODO.md` is everything deferred.
>
> Last synced with the repo: 2026-09-25.

---

## 1. What it is and who it's for

StepinFlow turns a QA tester's recording into a test that runs on every build.

A tester records what they do to an application. The recording becomes a flow: a tree of typed
steps that clicks, types, waits, reads the screen and branches on what it finds. That flow then
runs unattended — at several screen sizes, against several rows of data — and reports which checks
passed.

There are no selectors, no DOM and no code. Everything works from what is on screen: a template
image, or text read by OCR. So it does not matter whether the application under test is a website,
a desktop program, or something nobody has ever written an automation library for.

**Who it is for.** A QA tester who knows the application and does not write code. Secondarily the
developer who has to work out why the pipeline went red, and who will read the test as a file in a
pull request rather than opening the app at all.

### What makes it different

Record-and-replay tools have existed for twenty years and testers do not trust them, because a
recording breaks the first time anything moves and nobody can tell why. Three things answer that:

**A recording is not a test until it has validated.** A freshly recorded flow has been seen working
once, by the person who recorded it, on their machine. That is not the same as being a test, and
the app says so rather than letting a green suite prove nothing.

**A failure is diagnosed, not just reported.** The screenshots, the templates it was looking for,
the flow as text, and a model that has read the customer's own requirements all go into explaining
what went wrong.

**The test is a file in the customer's repository.** Reviewable in a pull request by someone who
has never opened the app, with `git log` and `git blame` as its version history.

---

## 2. Architecture

### The layout

Six projects. The dependency arrows are the design; everything else is detail.

```
App ──→ Transport ──→ Business ──→ DataAccess ──→ Core
 └────→ Platform.Windows ─────────────────────→ Core
```

| Project | Target | Holds | References |
|---|---|---|---|
| `Core` | `net10.0` | Models, DTOs, enums, ports, pure logic | — |
| `DataAccess` | `net10.0` | `AppDbContext`, configurations, migrations | Core |
| `Platform.Windows` | `net10.0-windows` | Everything that touches the machine | Core |
| `Business` | `net10.0` | The features: execution, search, the flow script, validation, AI | Core, DataAccess |
| `Transport` | `net10.0` | The IPC pipes, the dispatcher and one handler per action | Business |
| `App` | `net10.0-windows` | Host, DI, composition root | all |

**`Transport` does not reference `Platform.Windows` either.** It is how a request gets in, not
something that touches the machine. A command-line runner arrives in phase 12 as a folder beside
`Ipc/`, not as another project.

### Inside Business

Feature folders, each named for what it does rather than for what its classes are:

```
Business/
  Executions/    engine, walker, cache, history, Workers/
  Searching/     ImageSearcher - shared by the engine and the editor's Test now
  FlowScript/    Syntax/ Binding/ Text/ Diagnostics/, importer and exporter
  Flows/         editing a flow's tree: moves, template sync, name lookup
  Validation/    FlowValidationService, Rules/
  Recording/  Notification/  Command/  AreaPoint/  AppSettings/  Ai/
```

There is no `Services/` level: it claimed everything below it was a service, and most of it was an
engine, a parser or a worker. `Executions` and `AppSettings` are plural because the singular is an
entity, and a namespace named `Business.Execution` would hide the `Execution` class from every file
under `Business`.

**A helper lives with its only consumer.** Used by two or more features, it is shared vocabulary
and goes in `Core/Helpers`; used by one, it sits in that feature's folder. A `Helpers/` subfolder
only once there are more than three.

**`Business` does not reference `Platform.Windows`.** They are siblings. `App` is the only project
that knows both exist, because `App` is the composition root and binding a port to an adapter is
its job and nobody else's.

### Ports and adapters

The interfaces for anything outside the process live in `Core/Ports`. The implementations live in
`Platform.Windows`. `Business` consumes the interface and never sees the implementation.

```
Core/Ports/IScreenshotService        →  Platform.Windows/Screen/ScreenshotService
Core/Ports/IScreenService            →  Platform.Windows/Screen/ScreenService
Core/Ports/IInputService             →  Platform.Windows/Input/InputService
Core/Ports/IInputRecordService       →  Platform.Windows/Input/InputRecordService
Core/Ports/IWindowService            →  Platform.Windows/Windowing/WindowService
Core/Ports/IOcrService               →  Platform.Windows/Ocr/OcrService
Core/Ports/ISystemActionService      →  Platform.Windows/SystemActions/SystemActionService
Core/Ports/IProcessService           →  Platform.Windows/SystemActions/ProcessService
Core/Ports/IOpenCvService            →  Platform.Windows/Common/Vision/OpenCvService
Core/Ports/IIpcBroadcastService      →  Transport/Ipc/IpcBroadcastService
```

**The rule for `Core/Ports`: it holds only interfaces that Core and Business cannot implement
themselves.** `IFlowValidationService`, `IExecutionEngine`, `IAppSettingService` and the script's
`IParser` and `IPrinter` are declared and implemented inside `Business`, so they stay beside their
implementations. Sweeping every interface into `Ports` would make the folder mean "interfaces"
again and the name would stop carrying information.

### What goes in Platform.Windows

> **A file belongs in Platform if it touches the machine rather than the model** — a `DllImport`, a
> `using Windows.*`, or a type wrapping a native handle (GDI+ `Bitmap`, OpenCvSharp `Mat`,
> SharpHook's global hook).

Falsifiable form: *would removing it let the project drop `-windows` or a `.runtime.win` package?*

A window handle crosses the port as `WindowHandle`, a `readonly record struct` over an `nint`. It
is the opaque pointer of C, with the compiler enforcing what the comment used to ask for: nothing
outside the adapter can interpret one, a monitor handle cannot be passed where a window is wanted,
and the Linux XID - 32 bits rather than a pointer - has one place to live instead of every call
site.

The folders inside it split on a second axis — whether the native code is OS-specific:

```
Platform.Windows/
  Screen/  Input/  Windowing/  Ocr/  SystemActions/  Native/   — Win32, WinRT, GDI+
  Common/Vision/                                               — native, identical everywhere
```

There is no `Windows/` wrapper folder: the project name already carries the OS. What the split
still has to say is which code would move unchanged into a `Platform.Linux`, and that is
`Common/`.

It exists for OpenCvSharp. That is native code, so it is not `Business`; it is byte-identical on
Linux, so it does not belong beside the Win32. Without `Common` it would be homeless. Note the
trap: `System.Drawing.Common` is named Common and is **not** — GDI+ has been Windows-only since
.NET 6, so it sits with the rest of the Win32 code.

What this rule deliberately **excludes**: `DiscordNotifier` and the Ollama client are external
*system* adapters, not platform adapters. They sit behind interfaces already and they do not
constrain the target framework, so they stay in `Business`.

### Why this boundary exists

Before the split, nine files carried a hard Windows dependency and those nine were the entire
reason the 16,000-line `Business` project targeted `net10.0-windows`. Every other line — the
validator, the script writer, the execution walker, the AI services — had no idea what OS it was
on. Everything else that touched `System.Drawing` used only `Rectangle`, `Point` and `Size`, which
live in `System.Drawing.Primitives`, ship with the framework, and are cross-platform.

Splitting gives two things that are not available from folders and convention:

**Business cannot reach native code.** `AppWindowHelper.Focus(...)` inside the execution walker used
to compile, because it was a `public static` class in the same assembly. Now the type does not
exist as far as the Business compiler is concerned — `CS0103` for a bare name, `CS0246` for a
qualified one. A hard error either way, and checked by dropping a probe file into `Business`
that names a Platform type: it fails to compile.

The target framework alone would be a tripwire rather than a gate, and it is worth being honest
about the difference. A hand-written `[DllImport("user32.dll")]` compiles fine in a plain `net10.0`
project; P/Invoke is not gated by the framework, it just fails at runtime on a platform without the
library. The same is true of `System.Diagnostics.Process`, which is cross-platform and so slips
past the compiler entirely while being exactly the kind of machine-touching the boundary exists to
stop.

**So the rule the framework cannot express is written down and enforced instead.**
`Microsoft.CodeAnalysis.BannedApiAnalyzers` reads two lists:

```
backend/BannedSymbols.txt                  every project except Platform.Windows
backend/Platform.Windows/BannedSymbols.txt Platform.Windows, which inherits nothing
```

`Process`, `DllImport` and `LibraryImport` are banned in the first and legal in the second, which
is the architecture stated as a build error rather than as a paragraph. Both ban the ambient clock,
because nothing anywhere has a reason to read `DateTime.UtcNow` when a `TimeProvider` is injected.

One deviation, recorded in `.editorconfig` beside every other: `Business/Command`,
which a port would not fix — it launches `cmd.exe` and `powershell.exe` and every entry in its
preset catalogue is a Windows command, so the question is whether the whole runner moves rather
than whether the `Process` is hidden.

**Platform internals can be `internal`.** `Direct3D11Interop`, `NativeCursor` and
`OcrLanguageCatalog` are called only from inside Platform. They were `public static` solely
because one assembly left no other option, and the assembly boundary makes "this is a private
detail of the screenshot adapter" a compiler fact.

Two could not follow. `ScreenMetrics` stays public because `App` calls
`EnablePerMonitorDpiAwareness` at startup, before a container exists.
`IWindowsGraphicsCaptureService` stays public because registration lives in `App` by house rule,
so `App` has to be able to name the type.

**The verification:** `Business` drops the `System.Drawing.Common` package reference entirely and
still compiles, so the domain is genuinely GDI-free. `Rectangle`, `Point` and `Size` come from
`System.Drawing.Primitives`, which ships with the framework.

### Splitting the machine from the decision

Moving a file to Platform is the moment to ask what is actually in it. Most of the native helpers
have two things tangled together.

`AppWindowHelper` is the clearest case. Its 343 lines are half `EnumWindows` / `GetWindowRect` /
`PostMessage` — the machine — and half matching a `WindowQuery` against a list of windows by process
name, title pattern and `TitleMatchModeEnum`. The second half is pure logic that has nothing to do
with Windows, and before the split it could not be tested without Chrome actually running.

```
Core/Ports/IWindowService                 FindWindows, GetWindowBounds, Focus, Move, Resize, Close
Core/Helpers/WindowMatcherHelper          Matches(SystemWindow, WindowQuery) — pure
Platform.Windows/Windowing/WindowService  the P/Invoke, and only that
```

`WindowMatcherHelper` sits in `Core` rather than `Business` because the adapter needs it too — it
is the filter inside the enumeration. That is the shape to expect: a rule both sides share
belongs below both of them. It is checked against a hand built list of `SystemWindow` with
nothing open, including the case this codebase documents as the reason process name exists at
all: `CONTAINS "Notepad"` does match Notepad++, and adding the process name separates them.

Ask the same question of all nine: *what here is the machine, and what here is a decision?* The
decisions go up, the machine stays down.

And the corollary — **most of Platform needs no interface at all.** `Direct3D11Interop` is called
only by `WindowsGraphicsCaptureService`, both inside Platform. It is an implementation detail, not a
port. Only the things `Business` calls get an interface.

### Linux, and why it is not close

Linux is on the roadmap, not in the plan. Under **X11** the port is close to one-for-one:

| Windows | X11 / EWMH |
|---|---|
| `EnumWindows` | `_NET_CLIENT_LIST` |
| `GetWindowText` | `_NET_WM_NAME` |
| `GetWindowThreadProcessId` | `_NET_WM_PID` |
| `GetWindowRect` / `ClientToScreen` | `XGetWindowAttributes`, `XTranslateCoordinates`, `_NET_FRAME_EXTENTS` |
| `SetForegroundWindow` | `_NET_ACTIVE_WINDOW` |
| `SetWindowPos` | `XMoveResizeWindow` |
| `PostMessage(WM_CLOSE)` | `_NET_CLOSE_WINDOW` |

Under **Wayland** most of it is forbidden by design — a client cannot enumerate or control another
client's windows, and that is the security model rather than a missing feature. What exists is
compositor-specific: `wlr-foreign-toplevel-management` on wlroots compositors gives title, activate
and close but **not geometry, move or resize**; KDE has its own protocol; GNOME's Mutter implements
no foreign-toplevel protocol at all. Screen capture goes through PipeWire and `xdg-desktop-portal`,
which typically prompts the user for consent per session. Input synthesis needs `libei` or
`/dev/uinput` permissions. Wayland is the default on Ubuntu 21.04+, Fedora and RHEL 9.

**X11 is a port; Wayland is a different product.** For a tool whose premise is deterministic
viewport sizes and pixel-accurate matching, losing precise geometry and gaining a consent dialog is
not a rough edge. The ports make Linux *possible* and make the feasibility answerable on day one —
implement `IWindowService` and find out — but they do not make it cheap.

### Building and releasing

Project references resolve at **build** time, not runtime. Nothing detects the current OS.

```bash
npm run build
```

runs `dotnet publish -c Release -r win-x64 --self-contained /p:PublishSingleFile=true` into
`dist/backend/`, builds the renderer, then packages with electron-builder. The RID is named
explicitly; the backend exe is carried as an electron-builder `extraResources` entry.

Per-RID publishing is not avoidable here even in principle — the native dependencies (OpenCV,
libuiohook) ship different binaries per platform, so there is no single artifact that runs on both.

When Linux arrives there are two routes, both resolved at build time. Either multi-target one
project (`<TargetFrameworks>net10.0-windows;net10.0</TargetFrameworks>` with MSBuild excluding
`Windows/**` from the portable one, which is what MAUI does with its `Platforms/` folder), or split
into `Platform.Windows` and `Platform.Linux` with a RID-conditional `ProjectReference`. The second
is simpler and is why the project is named `Platform.Windows` rather than `Platform` from the start.

One thing that route has to solve: `App` currently targets `net10.0-windows`, and an unsuffixed
project cannot reference a platform-suffixed one. So a Linux build needs `App` multi-targeted, or
two thin entry projects over a shared host. That is a real cost and it is not being paid yet.

---

## 3. Concepts and vocabulary

The words below mean exactly one thing in this codebase and in the UI.

**Flow** — one test. A name, the application it tests, the screen sizes to test at, the data
columns it takes, and a tree of steps. Identified across machines by `PublicId`, a GUID.

**FlowStep** — one node of the tree. Typed: the type decides which of the wide table's columns mean
anything and which worker executes it. Steps nest; a step that can fail has `Success` and `Failure`
children so a flow handles its own problems rather than stopping.

**FlowArea** — a named rectangle owned by a flow. Either fixed coordinates, a monitor, or a window
matched by process name and title. Search steps look inside one.

**FlowPoint** — a named point owned by a flow. Cursor steps aim at one.

**Template image** — the picture a `SEARCH_IMAGE` step looks for. Always called a template image or
a screenshot; never a "frame" and never a "search image".

**Screenshot** — what the app captures off the screen. An output, per-execution, never checked into
a repository.

**FlowViewport** — one screen size the flow is tested at.

**FlowCsvColumn** — one input to the flow. Ten rows of CSV means ten executions.

**Execution** — one complete walk of a flow, at one viewport, with one row of data. The word is
always "execution"; "run" is not used in this codebase.

**Marker** — a named divider in the step tree with no behaviour, which becomes a `##` heading in
the script.

---

## 4. Process model and IPC

Three processes, two named pipes.

```
 Electron main  ──"stepinflow-request"────►  .NET host      request / response
       ▲                                          │
       │        ──"stepinflow-broadcast"──────────┘         server → client push
       │
  React renderer(s) via contextBridge (preload)
```

The .NET host owns the database, the screen, the mouse and the keyboard. Electron is the shell and
the bridge; the React renderer never talks to .NET directly.

**Only three protobuf messages exist.** The envelope is protobuf, the body is JSON bytes:

```proto
message IpcRequest   { string action = 1; bytes payload = 2; string correlationId = 3; }
message IpcResponse  { string action = 1; bytes payload = 2; string correlationId = 3; string error = 4; }
message IpcBroadcast { string type = 1;   bytes payload = 2; }
```

`action` is a string like `"FlowStep.update"`, routed by a switch in `Transport/Ipc/IpcDispatcher.cs`
to its handler. `payload` is UTF-8 JSON, camelCase, enums as strings, `ReferenceHandler.IgnoreCycles`.

The dispatcher is written by hand: one switch arm per action, every handler registered by name in
`Program.cs`. MediatR did the same job until its licence changed to one this GPL repository cannot
ship under, and a switch is also the more readable of the two - every route is on one screen, and
an unknown action is an explicit arm rather than a missing registration found at runtime.
**Adding a new DTO never touches the `.proto`.** Every response body is `ResultDto<T>`.

Broadcasts are fire-and-forget, delivered to every BrowserWindow, discriminated by `type`
(`BroadcastTypeEnum` as a string). They carry execution progress, recorded input events and model
download progress.

Hosted services started in `Program.cs`: the request pipe listener, the broadcast pipe listener, and
the SharpHook global hook, which runs for the whole process lifetime.

---

## 5. Data model

SQLite at `PathHelper.GetDatabaseDataPath()/StepinFlowSQLite.db`, migrated on startup with
`dbContext.Database.Migrate()`. Contexts come from `AddPooledDbContextFactory`.

| Table | Purpose |
|---|---|
| `Flows` | The test. Name, `PublicId`, the app under test, what to do when it ends. |
| `FlowAreas` | Named rectangle owned by a flow. |
| `FlowPoints` | Named point owned by a flow. |
| `FlowViewports` | One screen size to test at. |
| `FlowCsvColumns` | One input column. `IsSecret` means the value never reaches a file. |
| `FlowSteps` | One node of the tree. Wide table, one column set per step type. |
| `FlowStepTemplates` | Template image + match settings. The blob lives here, off `FlowStep`. |
| `FlowStepLastGoodScreenshotHistories` | What the screen looked like when a step last worked. |
| `Executions` | One walk of a flow, at one viewport, with one data row. |
| `ExecutionSteps` | Per-step result within an execution. |
| `AppSettings` | Key/value, defined by `AppSettingCatalog`. |
| `DiscordBots` | Webhook targets for notifications. |

`BaseDbModel` gives every row `Id` and `CreatedOn`. Enums are stored as strings
(`HasConversion<string>()`) — which means a migration adding one needs a **parseable**
`defaultValue`, not `""`.

### Flow identity: `PublicId`

`Flow.PublicId` is a GUID, generated once at creation, carried in the script file and copied onto
every later version of that flow.

The integer `Id` is unique to one machine's database; a repository is cloned into many. Without a
stable id in the file, a fresh clone cannot tell "a new version of the login flow" from "a second
flow that happens to be called login". It is also what lets an edited flow become a new row while
old executions stay valid — they still point at the same logical test, so history accumulates
instead of fragmenting.

It is called `PublicId` rather than `Guid` because `Guid` names the C# type, not the meaning.

### The wide-table decision

`FlowStep` holds every field for every step type, all nullable, with `FlowStepType` as the
discriminator.

SQLite stores a NULL column as about a byte of record header and zero payload bytes, so thirty
unused columns per row cost almost nothing. The executor loads a whole step in one row with no
joins, and the DTO is flat so the form binds straight to it. `FlowStepFieldCatalog` is the list of
which columns mean anything for which type — written with `nameof`, so renaming a column breaks the
build rather than quietly dropping the field from everything that reads it.

### `RootId`

`FlowStep.RootId` denormalises the owning flow id onto every descendant, so a whole tree loads with
one `WHERE RootId = ?` instead of a recursive CTE.

### Delete behaviour

| FK | Points at | On delete |
|---|---|---|
| `FlowId` | Flow | Cascade |
| `ParentFlowStepId` | FlowStep | Cascade |
| `FlowAreaId` | FlowArea | SetNull |
| `FlowPointId` / `FlowPointEndId` | FlowPoint | SetNull |
| `FlowStepReferenceId` / `…EndId` | FlowStep | SetNull |
| `Flow.AppUnderTestAreaId` | FlowArea | NoAction |

The `SetNull` group is deliberate: areas, points and referenced steps are **reusable**, so deleting
one must clear the reference rather than delete every step using it. `AppUnderTestAreaId` is
`NoAction` to break a cascade cycle — Flow → FlowArea → Flow.

---

## 6. Recording

Before the first click is captured, the tester answers three questions in a setup form:

**What is this flow called.** It has to be unique.

**What does it test, and how is it opened.** An application, a browser, a new tab. A **Test** button
tries the opening there and then, so a wrong command is found before a recording is wasted on it.
Screen sizes are added here too, one dialog per size.

**What should happen to that application when an execution ends** — leave it, close the window, kill
the process.

None of this is a step. It is configuration on the flow, so it stays editable afterwards and the
recorder is not the only way to set it.

Then recording starts and the system tracks input. **Pausing is part of authoring**: the tester can
pause, type a wrong value on purpose, resume, and record what the application does when it rejects
it. That is how failure paths get written — by provoking them rather than imagining them.

### The two gestures

**Ctrl + left click** asks what should be checked at that spot. Does this text or image need to
exist? Should the flow wait until it appears, or until it goes away? The click position matters,
which is why it is the left button.

**Ctrl + right click** asks what should happen there instead of a click: run a command, open another
application, or take a value from a CSV column. Position does not matter for any of those, which is
why it is the right button. Choosing a column offers the ones this flow already has, or defines a
new one with the recorded value as its default — which is why the CSV is per-flow and its template
is generated from the flow.

### Search mode, timeout and poll rate

Every recorded click becomes a `SEARCH_IMAGE` step, and how it searches depends on how long the
tester waited before clicking.

A quick click becomes `FIND_BEST` — the element was already there. A click after a visible pause
becomes `WAIT_UNTIL_FOUND`, because the pause is evidence the tester was waiting for something. The
recorded wait plus headroom becomes the timeout, so a step that took 4.2 seconds gets a timeout
derived from 4.2 seconds rather than a flat default. A flat ten-second default would make every
failing branch on a slow viewport ten seconds slower, forever.

The step carries a code comment recording why: `# recorded after a 4.2s wait`.

Polling is deliberately not as fast as possible. Screenshot plus template match is real CPU, and a
tight loop on a tester's laptop competes with the application being tested.

---

## 7. The flow script

A flow exports to a `.sflw` text file. `FLOW-FORMAT.md` is the grammar; this is the shape and the
decisions behind it.

```
Flow:    Login and add to cart
Id:      8f14e45f-ea2b-4c3f-9f1a-77f0d2a3b111
Sizes:   1920x1080, 390x844

Areas:
  "Browser"       window process "chrome.exe" title contains "Swag Labs"   scales with dpi   at 120dpi
  "Login form"    inside "Browser"   ratio 0.30 0.18  0.40 0.40

Templates:
  "username-field.png"    click 150,18   captured 922x648 at 120dpi

Steps:

## Sign in

Find Image      "Find username field"   template "username-field.png" accuracy 0.85   in "Login form"
  Success:
    Click           at "Find username field"
    Type            "{{username}}"
  Failure:
    End Execution   failed  "no username field on the login page"
```

### Shaped like a compiler

`Business/FlowScript/` is a pipeline, and the folders are its stages:

```
Syntax/       Lexer → Parser → StepParser        text, and nothing but text
Binding/      Binder → BoundFlow                 names become ids
Text/         Printer                            the model back to text
Diagnostics/  Diagnostic                         what went wrong, and whether it is fatal
              FlowScriptImporter, FlowScriptExporter
```

The split that matters is the one a compiler is built around: **the parser never resolves a name
and the binder never touches text.** A cursor step can aim at a check written below it, so nothing
can be resolved until everything has been read — and because binding needs no database, the round
trip is testable without one.

`Printer.Write(BoundFlow) → string` is a pure function, and deterministic: the same flow writes the
same bytes. Branch order from `OrderNumber`, areas roots-then-children alphabetically, and
`BoundFlow` builds its own child lookup so a caller cannot hand it steps ordered by chance.

`SyntaxFacts` holds every word the grammar knows **in both directions** — the keyword a step is
written as and the step a keyword means, the words for a condition, a title match, a scroll
direction and a button, and all of those read back. One file, because two is how a keyword comes to
mean one thing on write and another on read.

### Import is transactional

Parse, bind, then replace. Everything before the transaction is pure, so a file with a typo reports
the line and leaves the flow exactly as it was — half a flow is worse than no import, and whoever
hit the error is usually mid-edit.

Deleting the old steps does not take their history: an execution step keeps the name it ran under
and its foreign key is `SetNull` rather than cascaded, so the trend for a step survives a re-import
as long as its name does.

A diagnostic carries a code and a severity as well as a line, so a warning is something a flow can
be imported with and an error is not.

### The round trip is the acceptance test

Export, import, export again, byte identical — over a flow using both search kinds, all four search
modes, every placement form, branches, a loop, a section, a comment and cleanup under
`End Execution`. It runs two ways: purely, and through a real database with template bytes written
to disk and read back - where the imported rows are also compared with the originals field by
field, because identical bytes cannot see a field the printer never prints. See
`backend/Tests/Business.Tests/FlowScript/`.

It earned that status on its first run by finding a writer bug: `Scroll` emitted `in match`, because
the writer used the point-target fragment for its `in` clause and that falls through to "match" when
a scroll names neither a point nor a step — while the format means an area. A line no parser could
read, found the moment something tried to read one.

### Four grammar rules that only emerged from reading real output

**Every name is quoted**, even when it would read fine without. The original spec had bare names in
aligned columns — a parser cannot tell where such a name stops. One rule is easier to parse back
than a rule about which names need it.

**`Launch` takes one command string**, not an executable plus arguments. The model stores one
string; inventing a split it does not have would fail the round trip on the first export.

**`Id:` is in the header.** Identity travels in the file or a clone cannot recognise a flow.

**A blank line separates top-level steps, but never two.** A marker already leaves one behind it,
and a heading followed by empty space reads as a section with nothing in it.

---

## 8. Execution

### The engine

A flow is walked with an **explicit stack**, not recursion. Infinite loops and `Go To` make
recursion depth unbounded, and a stack gives pause, resume and step-into almost for free.

Everything an execution needs sits in memory and is dropped as the walk leaves it behind, so a flow
running for three weeks holds no more than one running for three seconds. History is written in
batches and only if it was asked for — turning history off changes what gets stored and never what a
flow does.

`StepWorkerFactory` maps `FlowStepTypeEnum` to an `IStepWorker`. The map is built in
`App/DependencyInjection/ExecutionServiceRegistration.cs` rather than inside the factory, so the
factory needs no container and the whole type-to-worker relationship is on one screen. Workers are
singletons because they hold no state.

### The step types

```
System     WAIT, LOOP, GO_TO, SYSTEM_COMMAND, SYSTEM_ACTION, SUB_FLOW,
           NOTIFY, END_EXECUTION, MARKER
Input      CURSOR_CLICK, CURSOR_DRAG, CURSOR_SCROLL, CURSOR_RELOCATE,
           WINDOW_FOCUS, WINDOW_RESIZE, WINDOW_RELOCATE, KEYBOARD_INPUT
Perception SEARCH_IMAGE, SEARCH_TEXT
Decision   CHECK_VALUE
Hidden     SUCCESS, FAILURE
```

`SEARCH_IMAGE`, `SEARCH_TEXT` and `CHECK_VALUE` are the branching types: each gets `Success` and
`Failure` children. The script shows the *mode* rather than the type, so `SEARCH_IMAGE` in
`WAIT_UNTIL_FOUND` mode writes as `Wait For Image` and in `FIND_ALL` as `Find All Images`.

`SearchModeEnum` is one axis, not two: `FIND_BEST`, `FIND_ALL`, `WAIT_UNTIL_FOUND`,
`WAIT_UNTIL_NOT_FOUND`. Acting on every match only ever made sense while looking once, so it is a
mode rather than a flag that would be dead in three cases out of four.

### The matrix

`Execution` carries `ViewportWidth`, `ViewportHeight` and `CsvRowIndex`. One execution is one
viewport and one CSV row, which means **the matrix loop sits above the walk and cannot be a step**.
Three sizes and ten rows is thirty executions.

It runs **sequentially**. There is one mouse, one keyboard and one screen. That triples wall-clock
in CI for three sizes and nobody should be surprised by it later.

### Setup and teardown

Each viewport pass is launch, then steps, then teardown. Self-contained and repeatable, which is
what makes a sequential matrix safe.

**Teardown is steps under `End Execution`.** That step no longer stops the walk: the walker drops
everything still pending — the stack holds a sibling for every level walked down to reach it — and
pushes the step's own children, so the cleanup written beneath it runs and the walk ends when that
cleanup does. Close the application, notify, call a webhook: all of them are ordinary steps, which
means they are visible in the tree, in the script and in the execution history, and copy-paste
replicates them across every `End Execution` in a flow.

The verdict is **latched** at the first `End Execution` reached. Cleanup below it is recorded like
anything else but cannot change what the flow already said happened, and a second `End Execution`
under the first is rejected by the validator as unreachable.

### A flow that says nothing is not a flow that passed

An execution ends one of five ways, and the distinction that matters is the third:

| | |
| --- | --- |
| `COMPLETED` | an `End Execution` said it passed |
| `FAILED` | an `End Execution` said it failed |
| `INCONCLUSIVE` | the walk reached the end and **nothing ever said** |
| `STOPPED` | somebody pressed stop |
| `ERRORED` | the harness broke, not the product |

`INCONCLUSIVE` exists because a flow whose every check failed looks, from the outside, exactly like
a flow that ran to the end. Reporting that as green is the precise failure this whole model was
built to stop. MSTest and NUnit both carry the same outcome; JUnit writes it as `skipped`, which is
amber in every dashboard rather than green.

It adds no inference. The engine still does not count checks — it reports the structural fact that
no step declared a verdict. A recorded flow is inconclusive by default, because the recorder
deliberately adds no `End Execution`, and that is the honest reading of it.

An **exception** is the one path with no cleanup, because there is no `End Execution` in scope and
so nothing was authored to run. It ends the whole execution, the viewport matrix included — there
is no next pass to protect, which is what the old flow-level close mode existed for. What that path
gets instead is a notification: see the roadmap.

### Variables

Three kinds of name translate through one helper, `VariableTranslator`:

| written | translates from |
|---|---|
| `{{username}}` | a CSV column |
| `{{width}}`, `{{height}}` | the viewport being executed |
| `{{Read the total}}` | what an earlier step produced |

There are **two substitution mechanisms**, because there are two different needs. A keyboard step in
column mode holds a `FlowCsvColumnId` — a foreign key, not text — because the whole value is one
column, and a foreign key is what makes renaming a column safe. Free text fields hold `{{name}}`
variables, because a notify message like `"Login failed: {{Login error}}"` mixes literal and
variable and that mixing is the point. Renaming a column rewrites the variables across that flow's
steps in one transaction; a validator rule catches a variable that names nothing, at save rather
than at execution. The writer emits `{{username}}` either way, so the script reads the same.

### Coordinates

Everything persisted is in **physical pixels**, and every set of pixels carries the DPI it was
captured at - a template, a child area's offset and size, a point. The process is
Per-Monitor-DPI-V2 aware. `ScreenHelper.EnablePerMonitorDpiAwareness` must run before anything else
touches a coordinate API — without it Windows virtualises every rect to 96 DPI and nothing lines up
with the capture buffers or the low-level input hook, both of which are always physical.

What makes a flow authored at 150% work at 100% is the area. Each says what its contents scale
with: **DPI** for a browser or a normal app, whose contents keep their size when the window changes,
or **its own size** for a game, whose picture stretches. The DPI now is the monitor holding the
largest part of the area - the rule Windows uses for a window. A template is scaled once, by that
one ratio; there is no sweep of sizes. Anything placed in screen coordinates - a region with no
parent, a point measured from nothing - gets a validation warning, because no ratio fixes it.
`PLAN.md` phase 5.7 has the reasoning and the measurements.

---

## 9. Validation and the fix loop

**A freshly recorded flow is not allowed to run in CI.**

A **Validate** button executes the flow and stops at the first `END_EXECUTION`. If it passes, the
flow is marked ready and viewports can be added. Each viewport then has to pass validation of its
own, because a flow has to work at that size to be worth running there.

If it fails, the app asks a model for a fix, giving it:

1. the flow exported as a script
2. the application's own documentation
3. the customer's documents — requirements, acceptance criteria, whatever they have
4. common issues and their solutions
5. scripts of flows that already validate

The app applies the proposed fix and executes again. After a bounded number of attempts — ten is the
working number — it stops and asks the tester rather than editing forever. When the tester answers,
the app re-executes, takes fresh screenshots where it needs them, regenerates templates, and carries
on from where validation stopped.

This is the loop the whole product turns on: the difference between a recorder that produces a
brittle script and a tool that produces a test somebody trusts.

### Static validation

`FlowValidationService` orchestrates rules in `Rules/` and runs before any of that — a flow tells
you what is broken before you execute it. `FlowCheckProjection` and the `GetFlowChecks` AI tool
expose the same information to a model: every check a flow contains, so a question about "what does
this flow verify" is answerable without walking the tree by hand.

`NAME_DUPLICATE` is an error rather than a warning: a duplicate name cannot round-trip through the
script and makes two steps share one history trend.

---

## 10. AI

### The local model is always on. The cloud is an addition, not an alternative.

| | local | cloud |
|---|---|---|
| always present | yes | only when an API key is set |
| sees screenshots, OCR text, typed values | yes | only on opt-in |
| receives the local model's structured findings | — | always |
| job | see, extract, structure | reason, propose, summarise |

Every install gets a working assistant with no configuration, which matters most for the fix loop —
the feature the product turns on cannot be gated behind an API key a customer may never add. It also
means one component touches raw screen data and everything downstream gets derived text.

**Accepted limitation:** a good vision model wants a GPU. A tester's laptop may not have one, and
small CPU models read dense interfaces poorly. That cost is being taken for now rather than designed
around.

### The screen-data gate

`AI_SEND_SCREEN_CONTENT` is a boolean, **default off**. `IAiProviderService.MaySendScreenDataAsync`
is the single chokepoint — local always may, a cloud provider only on the setting. Typed text is
redacted on three paths, and `DbQueryTools` never selects the `AppSetting` API key or
`DiscordBot.WebhookUrl` in any projection: the webhook URL *is* the credential and is never logged.

AI-generated flows go into the editor and never execute on their own.

### Structured output, not prose

The local model returns a schema:

```
elements:    type, label, x, y, width, height, state     (OmniParser produces these)
screenState: normal | loading | modal | error
notes:       short strings — what looks wrong, what is covering what
```

**This localises leakage; it does not remove it.** `label` and `notes` are both text read off the
screen, so a label can be `Welcome, alex@company.com`. The schema means there are exactly two fields
where screen text can appear rather than an unbounded paragraph, which is what makes review
possible.

### The payload is shown before it is sent

Which is the actual guarantee. The cloud query is assembled from local findings and displayed first,
with `label` and `notes` highlighted as the fields carrying screen text. Visibility rather than a
promise: a filter that claims to catch everything is worse than a preview that admits it cannot.

### Documents

`AiDocumentIndexService` embeds markdown chunks locally with ONNX and searches them with USearch.
`OnnxEmbeddingService` runs on the machine, but `AiDocumentTools` returns raw chunk text — so
**embedding is not a privacy layer** and must not be described as one.

---

## 11. Repository and CI

How a flow gets out of the database, into a repository, through a pipeline, and back to whoever has
to work out why it went red.

### The file is `.sflw`

Git decides binary or text by looking for NUL bytes, not by extension, so a UTF-8 script diffs
correctly with no configuration. What does want configuring is line endings: `*.sflw text eol=lf` in
`.gitattributes`, because flows are authored on Windows and CI runs on Linux, and the round-trip
test compares bytes.

### Git is the version history. The execution carries what ran.

Once a flow is a text file, `git log`, `git blame` and `git show HEAD~20:...` are the version
history, with diffs and pull requests no in-app feature would match. Building a second history in
the database would duplicate that, and only for customers who use a repository at all.

What git cannot answer is "what exactly ran in execution 37". So the execution stores the flow script
as text — a few KB — alongside the branch and commit it ran at. That reproduces the execution
exactly, renders read-only in the app, and works whether or not the customer uses git.

### Templates are inputs and live in the repository. Screenshots are outputs and never do.

A template is part of the test definition, like a snapshot in a unit test: a flow from three months
ago cannot execute without the images it was written against. They sit in the folder beside the flow
and they are small. They are named after the template, hyphenated, with a number appended on a
collision - **not** by content hash. A hash would dedupe identical images, but it also changes
whenever one is edited, so git would record a delete and an add instead of a modification, and
the point of a folder of loose images is that a reviewer can see which one changed.

Screenshots are per-execution, large, and grow without bound. They travel as build artifacts.

### Results travel as an execution bundle

The CLI runner writes a folder: the JUnit XML, the execution steps as JSON, the flow script that ran,
and the failure screenshots. CI uploads it as a build artifact, which every CI system already does.
The app imports it.

JUnit XML is the interchange format because every CI system already renders it. `<failure>` means
the product is broken; `<error>` means the harness is.

Later the app can fetch that same bundle from the CI provider's API instead of the user downloading
it. A central StepinFlow server that receives results directly is a product decision about becoming a
service, not a technical one, and it should not get decided by accident.

### The app under test is the flow's root area

You cannot resize "a flow", only a window, so the viewport matrix needs a target — and guessing it
from whichever window has focus is exactly the implicit behaviour that breaks on another machine.

`FlowArea` already binds to a window by process name and title, so this needs no new concept: one
field on `Flow` naming the root area that is the application under test, plus the command that
launches it. Viewport sizing targets that window; every other `WINDOW_*` step is untouched.

### Secrets live in the local database, never in the repository

Resolution order, most specific first:

```
CI argument  >  environment variable  >  local secrets file  >  stored value
```

`FlowCsvColumn.IsSecret` means the value is never stored in any file. A password in a repository is
a leak.

Stored values are encrypted at rest under a master password. Not DPAPI — that is Windows-only and
this has to work on Linux. Plain AES-256, composed from .NET's own primitives:

| stored in the clear | what it is |
|---|---|
| salt | 16 random bytes, per installation |
| wrapped data key | a random 32-byte key, encrypted with the password-derived key |
| each value | nonce, tag and ciphertext, with a fresh 12-byte nonce every time |

`Rfc2898DeriveBytes` turns the master password into a key, deliberately slowly, because a password is
short and guessable where a key is not. `AesGcm` encrypts and authenticates, so a tampered value
fails loudly instead of decrypting to noise. Both live in `System.Security.Cryptography` and behave
identically on Windows and Linux.

The wrapped data key is what makes changing the master password cheap: derive a new key, re-wrap the
same data key, and every stored value is untouched. It also removes the need for a separate password
verifier — a wrong password simply fails to unwrap, and that failure is the check.

Two consequences that are features rather than bugs. Forgetting the password means the stored values
are unrecoverable, so there is a "reset stored values" path. And **CI never decrypts anything**,
because a pipeline resolves from environment variables — a headless execution that can block on a
password prompt is a broken CI story.

### Sub-flows always resolve to the latest version

Not pinned. A fix to a shared sub-flow reaches every flow that calls it, which is the point of having
one.

### Switching branches hides flows, it never deletes them

The working tree is the truth; the database is a cache plus execution history. On a branch switch the
app rescans the flows folder and shows what the working tree contains. Anything else is hidden,
because its executions still matter.

This only holds together because identity is `PublicId`: the same flow on two branches is one family,
so its executions accumulate rather than fragmenting. Each execution records the branch and commit,
which is what turns "why does this fail in CI but not locally" into an answerable question.

### Flows appear in the folder structure the repository has

Adding a folder in the app adds a folder in the repository; adding a flow inside it writes the script
there. Script changes are visible in the app — including what a model changed when asked to fix
something, so a tester can see the edit before trusting it.

---

## 12. Reporting and notifications

Every check that can fail an execution is a thing worth counting.

A flow that ran a hundred times — fifty at one viewport, fifty at another — with four failures is a
sentence the product should be able to say. So is which checks those four fell into, and which checks
have never failed at all. That is the difference between "the login test is flaky" and "the login
test fails at 390x844 four times in fifty, always on the cart badge check".

**Discord notifications** post to a webhook when a step fails, with the reason and the template images
it was looking for, rate-limited per bot so a flow in a retry loop cannot flood a channel.

**Failure screenshots**: nothing is written while a flow goes well. A failure writes out the last few
frames leading up to it, each named after the step that took it.

---

## 13. Frontend

Feature-based: `features/<name>/{components,hooks,store}`, shared code in `shared/`, Electron-window
pages in `windows/`.

Every entity form is a pair — `XFormComponent` (React Hook Form setup, header, footer, submit) and
`XFormFieldsComponent` (fields only, reads `useFormContext`) — with a sibling `x.zod.ts`.

> **Any field missing from the Zod schema is dropped on submit.** Forms submit
> `{ ...defaultValues, ...data }`, so an unvalidated field silently keeps its default. This has
> already cost one debugging session: a form typechecked, looked correct, and quietly discarded two
> new columns because the schema had not been updated.

TanStack Query for server state, keyed `["flow", …]`, `["flowStep", …]`, `["lookup", …]`; mutations
invalidate. Zustand for UI state. Dialogs go through `useDialogStore` rendering into
`DialogRootComponent`.

### The tree

`DataTreeComponent` renders a PrimeReact `Tree`, lazily loading children on expand.

**Node keys are namespaced.** Flow ids and FlowStep ids are separate sequences, so a raw id would make
Flow 5 and FlowStep 5 the same node as far as selection and expansion are concerned. Keys are
`flow-{id}` / `step-{id}`, built by `TreeNodeDto.BuildKey` in C# and `buildTreeNodeKey` in TypeScript
— **these two must stay in sync.** `TreeNodeDto.entityId` carries the real id; nothing parses the key.

For the same reason `FlowStep.getTreeNodes` takes `{ id, isFlow }` rather than a bare id.

### Capture flows

**Overlay capture** (for `FlowArea`) opens a fullscreen transparent window per monitor, all showing a
frozen screenshot plus a dimmer, clipping one shared physical selection rect to their own monitor.
Confirm sends the physical absolute rect back.

**Point capture** (for `FlowPoint`) is deliberately not a window. The always-running global hook is
put into point-capture mode and resolves on the first `BUTTON_DOWN` — not `BUTTON_UP`, because the
press that armed the capture happened before recording started, so its release is the only stale
event that can arrive.

> The click is **not swallowed**. SharpHook observes, it does not suppress. That is what allows
> picking a point inside a live application, but it also means the click reaches whatever is
> underneath.

**Image editor** opens at `/image-editor` to produce a template: zoom, pan, pixel grid, minimap,
rectangular and lasso crop, eraser to transparency, undo/redo with thumbnail history.

---

## 14. Conventions

### Naming

> **A `Helper` is a static class that owns nothing.** It holds no state and no injected
> dependency: everything it needs arrives as an argument, including a `DbContext` when it needs
> one. Anything that owns something is a service.

The line is ownership, not purity. `FlowNameLookupHelper.TakenAsync(dbContext, flowId, ct)` is
async and touches the database, and it is still a helper — the caller owns the context and the
transaction, and the helper just asks a question with it. `IAppSettingService` holds its own
factory, so it is a service. That is the whole distinction, and it is what makes a helper safe to
call from anywhere: there is nothing in it to share, configure or dispose.

What `Helpers/` must not become is the folder where anything without a home lands. Native interop
is the case that went wrong once: `AppWindowHelper` and `Direct3D11Helper` were the OS API
surface wearing the name, and they moved to `Platform.Windows` in the split.

A few classes keep an agent noun where it says more than the suffix would — `VariableTranslator`,
`FlowStructureHasher`, `FlowStepTreeNodeProjection`. They follow the same ownership rule.

### Catalogs and constants

Two different things, two homes.

**`Core/Catalogs/`** holds structured tables that answer a question: `AppSettingCatalog` (every
setting's label, description, default, min and max — read by the loader *and* the settings page so
the two cannot disagree), `FlowStepFieldCatalog` (which columns mean anything for which step type),
`CommandPresetCatalog`. These are not constants; they are queried, and a `Constants.cs` full of
`public const string` would describe them less accurately than `Catalog` does. The pattern has a
well-known precedent in Roslyn's `SyntaxFacts`.

**`Constants/`** holds things that genuinely are constants — the OCR installable-tag list, `WM_CLOSE`,
the DPI awareness handles, recorder poll rates.

Neither belongs under `Helpers/`.

### Backend

- **One handler per action**, one class per file, in `Transport/Ipc/Handlers/<Entity>/` - the
  folder is the part of the action before the dot. A plain class with `HandleAsync`, no base type.
- **As thin as the second caller makes it.** Logic with one caller stays in its handler; a second
  caller, or one that is not a handler, and it moves into its `Business` feature.
- Handlers take `IDbContextFactory<AppDbContext>` and own their `DbContext`. **There is no generic
  repository.** A handler *is* the transaction boundary and EF's `DbSet` *is* the
  repository; a repository layer over `DbContext` would add indirection and remove LINQ. An earlier
  `IDataService` was removed because it rented a context per call and its `SaveChangesAsync()` row
  count was misread as success.
- **Reads** use `AsNoTracking()` and project straight into the DTO when the shape is known, so counts
  and joins happen in SQLite in one round trip.
- **Updates** load the tracked entity then `Entry(entity).CurrentValues.SetValues(dto)` — scalars and
  FKs only, so round-tripped navigations cannot overwrite unrelated rows and `CreatedOn` survives.
- **Deletes** use `ExecuteDeleteAsync()`.
- **Child collections are synced by hand**, matching on `Id`: update matched, insert `Id == 0`, delete
  missing. AutoMapper must never assign a collection onto a tracked entity — it deletes and
  re-inserts every row, changing ids and breaking every `FlowStep` that referenced one.
- **AutoMapper**: entity → DTO maps may carry navigations; DTO → entity maps ignore every navigation
  and `CreatedOn`.

### C# style

- Explicit types over `var`.
- No expression-bodied `=>` members.
- `<summary>` on public members; `//` on private ones.
- Comments explain *why*, not *what*. Names and logic carry the meaning.
- `//===` section banners inside long P/Invoke files.

### Rules a person has to remember are rules already broken

The house style is enforced by the compiler wherever it can be. `backend/Directory.Build.props`
decides which rules run — analyzers on, `EnforceCodeStyleInBuild`, and `TreatWarningsAsErrors` with
the NuGet audit codes exempt, because a CVE published overnight against a transitive package is
news rather than a reason nobody can build. `backend/.editorconfig` decides what each rule says,
and it is the only one of the two that can be scoped to a folder.

Roughly 350 warnings on the day it went on, and none now. Two thirds of those were three rules
arguing with a deliberate convention rather than finding a defect: `CA1707` wanted the underscores
out of `KILL_PROCESS`, `CA1711` wanted the `Enum` suffix off `FlowStepTypeEnum`, and `CA1725`
wanted MediatR's `cancellationToken` in place of the house `ct`. Each was turned off with the reason
written beside it. The last was off only under the handlers, because elsewhere it caught six real
ones, and it is on everywhere again now the handlers implement no MediatR interface. `IDE0005`
reports an unused `using` as an error.

A deviation is recorded three ways, and the width of the record matches the width of the exception:
a severity in `.editorconfig` for a rule everywhere, a path-scoped section for a folder, and a
`[SuppressMessage]` with a `Justification` for a single call site.

### Priorities

**Correctness → execution speed → memory → clean structure.** In that order, when they conflict.

---

## 15. Status

In active development, not released.

Working: the flow builder, the recorder, image search, OCR, sub-flows, notifications, the execution
engine with breakpoints and step-into, execution history, validation, the flow script in both
directions, and the AI assistant with local and cloud providers.

`PLAN.md` holds the build order and which phases have landed. `TODO.md` holds everything
deferred.

### Known gaps

- The flow script round-trips: writer, parser, binder and a transactional importer, with the
  byte-identical round trip verified both purely and through a database. Two gaps remain inside it -
  a `Sub Flow` step imports with no target, and `FlowValidationService` does not yet run on import.
- No CSV template is generated yet, and no `.gitignore` entry is written for the secrets file.
- Export has no button. `Flow.export` is reachable over IPC but nothing in the UI calls it.
- `RunCommandValue` can hold a credential in a command line. It is authored rather than read off the
  screen, so it is not currently redacted for AI. Flagged in `TODO.md` rather than folded in silently.

---

## Licence

GPL-3.0-or-later. Copyright (C) 2026 Alex Psihogios.
