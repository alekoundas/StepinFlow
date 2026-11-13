# StepinFlow — Project Reference

> The whole application in one document: what it is for, how it is built, what exists today, and
> what is knowingly unfinished. Written to be read top to bottom by a person joining the project,
> or pasted into a model as context.
>
> `backend/Core/AiDocuments/Reference/flow-format.md` holds the flow script grammar and is kept
> separate on purpose — it is a specification you read while writing a parser, not prose. It sits
> with the AI documents because a model revising a script is given it whole, and the assistant
> searches it. `PLAN.md` is the build order and holds only open work. `TODO.md` is everything
> deferred.
>
> Where a section describes something not built yet, it says so and names the `PLAN.md` phase.
>
> Last synced with the repo: 2026-09-26.

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

Seven projects. The dependency arrows are the design; everything else is detail.

```
App ──→ Transport ──→ Business ──→ DataAccess ──→ Core
 ├────→ Platform.Shared ──────────────────────→ Core
 └────→ Platform.Windows ─────────────────────→ Core
```

| Project | Target | Holds | References |
|---|---|---|---|
| `Core` | `net10.0` | Models, DTOs, enums, ports, pure logic | — |
| `DataAccess` | `net10.0` | `AppDbContext`, configurations, migrations | Core |
| `Platform.Shared` | `net10.0` | What touches the machine the same way on every system: git, OpenCV, the global hook and simulated input | Core |
| `Platform.Windows` | `net10.0-windows` | What touches the machine the Windows way: Win32, WinRT, GDI+ | Core |
| `Business` | `net10.0` | The features: execution, search, the flow script, validation, AI | Core, DataAccess |
| `Transport` | `net10.0` | The IPC pipes, the dispatcher and one handler per action | Business |
| `App` | `net10.0-windows` | Host, DI, composition root | all |

**`Transport` does not reference `Platform.Windows` either.** It is how a request gets in, not
something that touches the machine. A command-line runner arrives in phase 12 as a folder beside
`Ipc/`, not as another project.

Eight test projects sit beside them in `backend/Tests/`, one per production project plus
`Architecture.Tests`, which turns the arrows above into test failures - see §15.

### Inside Business

Feature folders, each named for what it does rather than for what its classes are:

```
Business/
  Executions/    engine, walker, cache, history, Workers/
  Searching/     ImageSearcher - shared by the engine and the editor's Test now
  FlowScript/    Scanner/ Lexing/ Parsers/ Models/ Printer/ Catalogs/ Diagnostics/, importer and exporter
  Flows/         DataService/ - every write to a flow - FlowValidationService/ with its Rules/ -
                 what every write is checked against - and the tree maths a move uses
  Repository/    the flows kept as files in a git repository: GitRepositoryService writes, reads and commits
  Recording/  Notification/  Shell/  AreaPoint/  AppSettings/  Ai/
```

There is no `Services/` level: it claimed everything below it was a service, and most of it was an
engine, a parser or a worker. `Executions` and `AppSettings` are plural because the singular is an
entity, and a namespace named `Business.Execution` would hide the `Execution` class from every file
under `Business`.

**A helper lives with its only consumer.** Used by two or more features, it is shared vocabulary
and goes in `Core/Helpers`; used by one, it sits in that feature's folder. A `Helpers/` subfolder
only once there are more than three.

**`Business` does not reference `Platform.Windows`, or `Platform.Shared`.** They are siblings. `App` is the only project
that knows both exist, because `App` is the composition root and binding a port to an adapter is
its job and nobody else's.

### Ports and adapters

The interfaces for anything outside the process live in `Core/Ports`. The implementations live in
`Platform.Windows`, or in `Platform.Shared` when the code is the same on every system. `Business`
consumes the interface and never sees the implementation.

```
Core/Ports/IScreenshotService        →  Platform.Windows/Screen/ScreenshotService
Core/Ports/IScreenService            →  Platform.Windows/Screen/ScreenService
Core/Ports/IInputSimulatorService    →  Platform.Shared/Input/InputSimulatorService
Core/Ports/IInputRecordService       →  Platform.Shared/Input/InputRecordService
Core/Ports/IMouseService             →  Platform.Windows/Input/MouseService
Core/Ports/IWindowService            →  Platform.Windows/Windowing/WindowService
Core/Ports/IOcrService               →  Platform.Shared/Ocr/PaddleOcrService
Core/Ports/ISystemActionService      →  Platform.Windows/SystemActions/SystemActionService
Core/Ports/IProcessService           →  Platform.Windows/SystemActions/ProcessService
Core/Ports/IOpenCvService            →  Platform.Shared/Vision/OpenCvService
Core/Ports/IGitService               →  Platform.Shared/Git/GitService
Core/Ports/IIpcBroadcastService      →  Transport/Ipc/IpcBroadcastService
```

**The rule for `Core/Ports`: it holds only interfaces that Core and Business cannot implement
themselves.** `IFlowValidationService`, `IExecutionEngine`, `IAppSettingService` and the script's
`IScanner` and `IPrinter` are declared and implemented inside `Business`, so they stay beside their
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

```
Platform.Windows/
  Screen/  Input/  Windowing/  SystemActions/  Native/   — Win32, WinRT, GDI+
```

There is no `Windows/` wrapper folder: the project name already carries the OS. Code that would
move unchanged into a `Platform.Linux` is not here but in `Platform.Shared`, below. OpenCvSharp was
the first of it, in a `Common/` folder of this project until 2026-10-25; what stays of it here is
`OpenCvSharp4.runtime.win`, OpenCV's Windows binaries with no code. Note the trap:
`System.Drawing.Common` is named Common and is **not** — GDI+ has been Windows-only since .NET 6,
so it sits with the rest of the Win32 code.

What this rule deliberately **excludes**: `DiscordNotifier` and the Ollama client are external
*system* adapters, not platform adapters. They sit behind interfaces already and they do not
constrain the target framework, so they stay in `Business`.

### What goes in Platform.Shared

An adapter that touches the machine the same way on Windows, macOS and Linux: git through
LibGit2Sharp, template matching through OpenCvSharp, and the global hook and simulated input through
SharpHook. LibGit2Sharp and SharpHook carry their native library for each system, so nothing has to
be installed; OpenCV's comes from the platform project, `OpenCvSharp4.runtime.win` in
`Platform.Windows`. It targets plain `net10.0`, depends on `Core` alone and takes the banned-API list
every project but `Platform.Windows` takes; the architecture tests hold it to that, and to being the
only project that uses the three libraries.

What the input services cannot do through SharpHook - read where the cursor is, move it in physical
pixels, and know how far a notch of the wheel scrolls - is `IMouseService`, which `Platform.Windows`
implements over `NativeCursor` and `NativeWheel`. It is in `Core/Ports` with the rest, since neither
platform project may reference the other.

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
is the architecture stated as a build error rather than as a paragraph. Both lists also ban what is
not about layering at all:

- **The ambient clock**, because nothing anywhere has a reason to read `DateTime.UtcNow` when a
  `TimeProvider` is injected - which is what lets a test of a ten-second timeout move a fake clock
  instead of waiting.
- **Blocking on a task** - `Task.Result`, `Wait()`, `GetAwaiter()`.
- **`Regex`**, which must be reached through `Core/Helpers/RegexHelper`. Most patterns in this app
  are typed by whoever is authoring a flow, so two things have to hold everywhere and are easy to
  forget once: a half-written pattern must not throw at the caller, and one that backtracks for ever
  must give up rather than compete for CPU with the application being tested. Five call sites each
  had their own answer, and two of them had neither.

A property is banned as `P:System.DateTime.UtcNow`. Written `M:System.DateTime.get_UtcNow` it
silently matches nothing, and a ban that matches nothing looks exactly like a ban nobody has
broken. That was found by dropping a file that broke every ban into `Core` and checking each one
fired. A banned **type** is broad in a useful way: banning `Regex` also catches
`[GeneratedRegex]`, whose generated member has to name the type.

Worth knowing what a ban does not catch: a bare `using System.Text.RegularExpressions;` references
no banned symbol, so it is reported as an unused `using` rather than as a ban.

Two deviations, both `.editorconfig` sections beside every other. `ShellCommandExecutor` holds a `Process`,
which a port would not fix — it launches `cmd.exe` and `powershell.exe` and every entry in its
mode catalogue is a Windows command, so the question is whether the whole executor moves rather
than whether the `Process` is hidden. `RegexHelper` names `Regex`, because it is where the ban
points.

Each names **one file**, not its folder, because every banned symbol shares the one diagnostic id:
switching it off excuses all of them wherever it reaches. Written as `Business/Shell/**.cs` it
quietly let the regex ban off in the one file most likely to break it — the executor is where the
duplicated pattern matching used to live.

**Platform internals can be `internal`.** `Direct3D11Interop` and `NativeCursor` are called only
from inside Platform, as OCR's line finder and readers are only from inside `Platform.Shared`.
They were `public static` solely because one assembly left no other option, and the assembly
boundary makes "this is a private detail of the screenshot adapter" a compiler fact.

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
Core/Helpers/WindowMatcherHelper          Matches(title, processName, WindowQuery) — pure
Platform.Windows/Windowing/WindowService  the P/Invoke, and only that
```

`WindowMatcherHelper` sits in `Core` rather than `Business` because it is part of what the port
promises. `IWindowService.FindWindows(WindowQuery)` is declared in `Core`, so what makes a window
match a query is the port's contract and not one adapter's opinion — otherwise `CONTAINS` could end
up case-sensitive on one platform and not on another. Its only caller today is the Windows adapter,
which is the usual argument for moving a helper to its consumer; it does not apply to something a
second adapter would have to reimplement identically.

Keeping it above the adapter is also what makes it testable with nothing open, including the case
this codebase documents as the reason process name exists at all: `CONTAINS "Notepad"` does match
Notepad++, and adding the process name separates them.

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

`npm run build` publishes the backend with `dotnet publish -c Release -r win-x64 --self-contained
-p:PublishSingleFile=true` into `dist/backend/`, builds the renderer into `dist/frontend/` and
Electron into `dist/electron/`, with `protobuf.proto` beside the code that reads it. Development
compiles Electron to the same place, so every window finds its preload and its page one way
(`electron/app-paths.ts`): Vite's page while developing, the built one once packaged.
`npm run package` then makes the installer in `release/`. The RID is named explicitly; the backend
exe is carried as an electron-builder `extraResources` entry, and only `dist/electron` and
`dist/frontend` go in the archive.

**A release is the Release workflow** (`.github/workflows/release.yml`), a Run button on a Windows
runner. It is asked for patch, minor or major and works out the next number from the newest
`vX.Y.Z` tag. There are no betas: while the major is 0, every release is an early one. The tag is
the one place a version lives: `package.json` in git says `0.0.0-dev`, the workflow stamps it with
`npm version`,
Electron's version for the updater comes from it, and `App.csproj` reads it as
`$(npm_package_version)` for the backend's. Patch is fixes with the script unchanged, minor is
anything new - a script word an older app cannot read included - and major is something that stops
working. The workflow runs the backend tests and the type checks, builds, tags the commit it built,
creates the draft release, and has electron-builder upload to it, with the `latest.yml` that
`electron-updater` reads. The draft comes first because electron-builder starts two uploads at once
and each creates a draft when it finds none, which splits the files between them. The workflow then
checks the tag has one release holding that file, the installer it names and the installer's
blockmap, because electron-builder reports success either way. Publishing the draft is the release.
It is unsigned, so Windows SmartScreen warns on install.

**The auto-updater** is `electron/ipc/services/auto-updater-service.ts`, around `electron-updater`. It
checks at start and every 4 hours against GitHub's latest published release, so a draft is invisible
to it. A newer version downloads by itself, as the still-packed Setup exe, into
`%LOCALAPPDATA%\stepinflow-updater\pending`, fetching only what changed when the blockmap allows it.
The rail says so only while there is something to say (`AutoUpdaterRailBlockComponent`): downloading
with its progress, ready with Restart to update, failed with Try again and the log. On the narrow rail
it is one icon with the words in its tooltip. Restart is off while a flow is executing. It starts the
installer silently and the installer opens the app again; otherwise the installer starts silently when
the app closes, and the app stays closed. Home's Updates card has the version, a pill for what the
updater is doing - when a check last got an answer, Downloading, Restart needed, Update failed - and
Check for updates, which becomes Restart to update once one is ready. The bottom of Settings' section
list shows only the version. A failed check nobody asked for says nothing, since it usually means the
machine is offline, and all of it goes to `main.log`. Started from source the version reads `dev` and
there is nothing to check. macOS needs the app signed first, since Squirrel.Mac refuses an unsigned
update, and on Linux only an AppImage updates itself.

**The icon** is `win.icon` in `package.json`, naming one of the colour schemes in `assets/logo/`.
Each has the logo as SVG, a simpler SVG for small sizes, a PNG, and an `.ico` that uses the simpler
drawing at 24 px and under. macOS and Linux would point `mac.icon` and `linux.icon` at the same
folder. The startup screen and the favicon use `frontend/public/logo.svg`, the README uses
`docs/images/logo.png`, and the rail draws the small one in the theme's colour.

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

**Flow** — one test. A name, a description of what it is for, the screen sizes to test at, the
data columns it takes, and a tree of steps. Identified across machines by `PublicId`, a GUID.

**FlowStep** — one node of the tree. Typed: the type decides which of the wide table's columns mean
anything and which worker executes it. Steps nest; a step that can fail has `Success` and `Failure`
children so a flow handles its own problems rather than stopping.

**FlowArea** — a named rectangle owned by a flow. A window matched by process name and title, a
monitor, or a region inside another area - in pixels or as a fraction of it. Search steps look
inside one, and the window steps focus, resize or move the window one finds. Each says what its
contents **scale with**: the screen's DPI, or its own size (§8).

**Main area** — the window a flow works in: the one `FlowArea` with `IsMain`, always a root. A
recording is stored relative to it.

**FlowPoint** — a named point owned by a flow, measured from an area. Cursor steps aim at one.

**Template image** — the picture a `SEARCH_IMAGE` step looks for. Each carries its own accuracy, the
point inside it to click, and the area size and DPI it was captured at.
Always called a template image or a screenshot; never a "frame" and never a "search image".

**Screenshot** — what the app captures off the screen. An output, per-execution, never checked into
a repository.

**FlowInput** — one input to the flow, `{{name}}` in a step's text, with the value it was recorded
with. A secret's value is kept apart, in the database only.

**Execution** — one complete walk of a flow, with one row of data. The word is
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

A release's Electron starts its own `App.exe`. While developing, Electron connects to the backend
already running in Visual Studio, or starts it with `dotnet run` when given `--spawn-backend`
(`npm run dev:spawn`). A backend Electron starts is given `--parent-pid` and stops once that process
is gone, which a debugger's Stop and a crash would otherwise leave running; its output is streamed
into Electron's log rather than buffered, since a buffer that fills kills the process. F5 in VS Code,
with StepinFlow picked in Run and Debug, builds Electron, starts Vite as a background task, attaches
to the renderer, and stops Vite when the session ends.

**Starting up.** The main window opens at once on `#121212`, Chromium's canvas under the theme's
`color-scheme`, and `index.html` paints `#startup-screen`, which needs no bundle, theme or backend.
The overlay, the image editor and the recording bar load the same page and keep it hidden.
`BackendGateComponent`, around `AppLayout`, holds every page back until the request pipe connects.
`Program.Main` opens the pipes only after the migrations, so that also means the database is ready.
The repository and the docs index are not waited for, since they build in the background.
`backend-status-service.ts` in Electron's main process holds `STARTING`, `READY` or `STOPPED`.

Both pipes keep trying to connect until the backend stops, because a migration can take minutes.
They used to give up after about 30 seconds, and nothing else reconnected the broadcast pipe, so a
slow migration would have left an execution's progress with no way to the page. A request that finds
no connection still fails after those 30 seconds rather than hanging.

Only the process Electron started can end in `STOPPED`, so a backend in Visual Studio can be
restarted while the pages stay up. The screen for it shows the end of the backend's stderr, opens
the log, and offers Try again, which relaunches the whole app rather than joining a new backend to
pages left from the old one. Under `npm run dev:spawn` that also ends the dev session, because
`concurrently -k` takes Vite down when Electron exits.

Opening StepinFlow while it is open brings its window forward. The new copy finds Electron's
single-instance lock taken and leaves before it opens a window or starts a backend, since a second
backend would share the database and the pipe names. The lock lives in Electron's data folder, which
the dev build keeps apart from a release's (`%APPDATA%\Electron` and `%APPDATA%\stepinflow` on
Windows), so the dev build and the installed app can still be open together. Try again is unaffected:
`app.relaunch()` starts the new copy only once the old one has exited.

**Only three protobuf messages exist.** The envelope is protobuf, the body is JSON bytes:

```proto
message IpcRequest   { string action = 1; bytes payload = 2; string correlationId = 3; }
message IpcResponse  { string action = 1; bytes payload = 2; string correlationId = 3; string error = 4; }
message IpcBroadcast { string type = 1;   bytes payload = 2; }
```

`action` is a string like `"FlowStep.update"`, routed by a switch in `Transport/Ipc/IpcDispatcher.cs`
to its handler. `payload` is UTF-8 JSON, camelCase, enums as strings, `ReferenceHandler.IgnoreCycles`.

The dispatcher is written by hand: one switch arm per action, 120 of them, and each handler
registered by name in `Program.cs`.

```
"Flow.get" => await Handler<GetFlowHandler>().HandleAsync(Payload<int>(request), ct),
```

MediatR did this job until its licence changed to one this GPL repository cannot ship under. It
turned out to be barely used - no pipeline behaviour, notification or stream anywhere - so all it
did was resolve a handler from the container, behind 103 one-field message records built so it
had something to match on. Two reflective route tables were built as replacements and both thrown
away: the switch was never the problem, the wrapper types were. What the switch gives that neither
did is the compiler's help - a duplicate action is `CS0152`, a handler whose signature changes
breaks the arm that calls it, and F12 reaches the handler.

**Adding a new DTO never touches the `.proto`.** Every response body is `ResultDto<T>`.

Broadcasts are fire-and-forget, delivered to every BrowserWindow, discriminated by `type`
(`BroadcastTypeEnum` as a string). They carry execution progress, recording progress, model
download progress and answers being written.

The execution page applies execution progress in batches, at most every 100 ms and on a screen
refresh, so a window nobody can see draws nothing. Drawing on every event cost more than a fast flow
left between two, and the page fell further and further behind the execution. The list draws only
the rows in view, newest first: a Firestone execution left on for two hours kept 32,699 steps, and
laying all of them out made the page lag. Scrolled down to read, the rows stay put as new ones
arrive on top. What the page keeps, and how a batch costs the same at any length, is under The
execution page.

**Work longer than a request can wait for is started by the request and reported by broadcast.**
The pipe answers one call at a time and Electron gives up on a call after 30 seconds, so the
request starts the work and returns, the progress goes out as a broadcast, and the result is asked
for once the progress says it is there - Google's long-running operations, Azure's 202 with a
status URL. The result is a request rather than one more broadcast because it can be megabytes,
which every window would be sent, and a window that missed it would never get it.

Hosted services started in `Program.cs`: the request pipe listener, the broadcast pipe listener, and
the SharpHook global hook, which runs for the whole process lifetime. It is an `EventLoopGlobalHook`:
its handlers run on one thread of their own, one at a time and in the order the inputs came, so the
hook itself is never held up and a recording keeps their order.

---

## 5. Data model

SQLite at `PathHelper.GetDatabaseDataPath()/StepinFlowSQLite.db`, migrated on startup with
`dbContext.Database.Migrate()`. Contexts come from `AddPooledDbContextFactory`.

It is in write-ahead-log mode (`UseWriteAheadLog`, asked on every start, kept by the file): a save
goes on the end of `-wal` rather than through a journal made, synced and deleted each time, and a
read never waits for a save. An execution's history saves several times a second, and in the
default mode that was a journal file made and deleted every time. The database is three files, the
`.db`, `-wal` and `-shm`, and a copy taken while the app is open needs all three.

| Table | Purpose |
|---|---|
| `Flows` | The test. Name, `PublicId`, description. |
| `FlowAreas` | Named rectangle owned by a flow, with what it scales with and its DPI. `IsMain` marks the window the flow works in. |
| `FlowPoints` | Named point owned by a flow, with the DPI it was captured at. |
| `FlowInputs` | One input and its recorded default. `IsSecret` means the value never reaches a file, so a secret has no default; its value is `SecretValue`, which only the execution reads. |
| `FlowSteps` | One node of the tree. Wide table, one column set per step type. |
| `FlowTemplates` | A template image of a flow: its file name, the blob and its thumbnail, click point, and captured size and DPI. Steps link to it, so one picture serves every step that looks for it. |
| `FlowStepTemplates` | A step's link to a template, with the step's own accuracy and order. |
| `Executions` | One walk of a flow, with one data row, and the commit it executed at. |
| `ExecutionSteps` | Per-step result within an execution. |
| `AppSettings` | Key/value, defined by `AppSettingCatalog`. |
| `DiscordBots` | Webhook targets for notifications. |

`BaseDbModel` gives every row `Id`, `CreatedOn` and `UpdatedOn`, stamped at save by
`TimestampInterceptor` from the injected `TimeProvider` - an interceptor rather than a
`SaveChanges` override because the context factory is pooled, and a pooled context may only have
the one options constructor. `TemplateThumbnailInterceptor` makes a template's thumbnail the same
way, whenever its picture is added or changed: a capture, an edit, an import and a recording all
leave the tree showing the picture that was saved, and a template saved before thumbnails existed
gets one the next time a save loads it, even if nothing about it changed. Enums are stored as strings (`HasConversion<string>()`) — which means
a migration adding one needs a **parseable** `defaultValue`, not `""`.

A schema change is a migration, and the migration carries the existing data over: a column that
moves takes its values with it, a table that splits moves its rows, and a renamed enum value
updates the rows that hold it. Since 2026-10-23 the database holds flows built by hand - the
Firestone flow among them - so it is never treated as disposable, and "recreate the database" is
never the way past a change. `SharedFlowTemplates` is the example: it moves every picture into
`FlowTemplates` before dropping the old columns. `DataAccess.Tests` fails if an entity changes
without a migration - EF Core 10 refuses `Migrate()` in that state, so the app would refuse to start
as well.

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
| `FlowArea.ParentFlowAreaId` | FlowArea | SetNull |
| `ExecutionStep.FlowStepId` | FlowStep | SetNull |

The `SetNull` group is deliberate: areas, points and referenced steps are **reusable**, so deleting
one must clear the reference rather than delete every step using it. An execution step keeps the
name it ran under, so deleting a step keeps its history. The main area is a flag on the area rather
than a key on the flow, so there is no Flow → FlowArea → Flow cycle to break and deleting the main
area simply leaves the flow without one. `DataAccess.Tests/SchemaTests` pins each of these
against SQLite's own foreign keys, through `ExecuteDelete` rather than EF's change tracker.

---

## 6. Recording

### Today

The global input hook records every click, drag, scroll and keystroke with the pause before it, and
the main area at each click. Nothing becomes a step while recording. After the stop, what each click
hit is found and the clicks on the same picture grouped; the tester goes through it all in Review,
and the plain draft - or a model shaping it - writes it into the flow, which the tester reads on
Check before the editor opens - all below. The template is
cut from the recording's own screenshot, so nothing is captured twice.

A per-action wizard did this until 2026-10-14, asking what each action was for, one at a time, and
building steps in TypeScript. Review and the draft replaced it: the questions it asked are answered
by a rule, and corrected in Review or the editor.

**A new flow is set up before the first click** (`Business/Recording/Setup`, `Recording.setup`):
its name, description, whether it is a QA test (`Flow.IsQaTest`), and what it works in - an
**Application**, an `APPLICATION` area, or a **Website**, a `BROWSER_TAB` area of a browser and an
address, named by the address. Both are the area form's own fields (`WindowMatchFieldsComponent`):
open apps one to a row with their windows' titles under them, StepinFlow's own left out, and a Test
that finds the window and brings it to the front. The flow and its main area are saved in one write,
so the name is reserved, with up to two steps the tester asks for, in this order:

- **A launch.** `Launch opens` the main area, so an execution waits for it, and the area is started
  by the program file of the process that owns the window now - quoted when it has a space, or a
  Steam game's link - so the window has to be open. A website's is its browser, which Launch opens
  at the address when no tab has it.
- **A resize** (`Resize Window <[ Resize window to WxH ]> <[ main ]> size W H`), to a size typed or copied from one of the app's
  open windows by "Measure its windows" - the whole window, as `GetWindowRect` measures it and
  `SetWindowPos` sets it, since the inside the area's Test reports would shrink the window a little on
  every execution. The setup sets the window to it there and then, and the session again at Start
  (`RecordingWindowSize`), since the tester may have changed it while arranging their windows: the
  template images and points are then taken at the size every execution sets. A window that says yes
  and keeps its size - maximised, full screen, fixed - is measured afterwards and refused, saying the
  size it stayed.

The recorded steps go after them. Discard deletes the flow while nothing but the setup is in it.

**Recording steps into a flow that already exists** - the editor's "Record steps here" - is
disabled since 2026-10-15, until how a model's draft lands in an existing flow is settled
(`TODO.md`, Recording). Built and tested, it opens the recording page with the position of the
placeholder it was pressed on, at the flow's top or under a step. The recording is made against the flow's main area; a flow with none is first asked for its
window, in the setup's picker, which saves it as the main area. Review is the same, and the plain
draft is inserted at that position (below).

`Business/Recording/Session` records against the flow's main area, always - a start without a flow is refused:

- **A screenshot from before each press.** It captures the main area four times a second and keeps
  the last three, each stamped when its capture finished. A press takes the newest one stamped
  before it, picked at the hook so a slow drain cannot lose it, and keeps the main area's bounds,
  its DPI and the whole screenshot (`RecordedPress`) - what a click is stored relative to.
- **A website's tab is found by its address once**, at Start, and its browser window followed after
  (`IAreaPointResolver.ResolveTabPage`): a page that goes on to a sign-in on another site is still
  the one being recorded. Matched by its address again only once that window is closed or minimised.
  It is cheaper too: each screenshot reads the browser's controls through UI Automation once, for
  the page, rather than once per browser window for the address and again for the page.
- **Which window a press landed on**, read at the hook from the window under the cursor rather than
  later from whatever is in front: the main window, another window of its application such as a
  Save As dialog, or anything else. A main area that is a monitor or a region has no window, so it
  sorts by where the press landed.
- **Nothing done on StepinFlow's own windows.** Every recording starts with Electron's process id,
  which the page asks Electron for (`electronProcess.getId`) and sends with `Recording.start`; a start
  without one is refused. A press on its windows, or a key while one is in front, is left out. A
  release goes with its press, so a key let go over StepinFlow, or during a pause, is not left held
  for every letter after it to read as a shortcut.
- **Pause and resume**, from the recording bar or its hotkey. A paused session keeps nothing new,
  and the time paused is not read as a wait.

`RecordingSessionService` holds the session - its start, pause and stop, the queue from the hook and
the live list - and what it does beside that is a part of its own, sharing its cache:
`RecordingScreenCapture`, `PressedElementLookups`, `RecordingGestureAsker` and `RecordingProcessor`.
Until 2026-11-11 it was all one class of 1,100 lines.

**Raw input folds into actions in one pass** (`Business/Recording/ActionBuilder`), in the order it
was recorded, so replaying the actions does what the inputs did and only fewer lines say it. Each
action is built from its first input and marks the rest of its own handled - a release, a key's
repeats and key-up, the notches after - so the loop never reads them again and nothing between them
is skipped.
What a key typed is the character the layout made of it - Shift, Caps Lock and AltGr applied, in
whatever language - which the hook raises right after the key-down and the recorder keeps as an
input of its own (`KEY_TYPED`), so nothing already published changes. So Caps Lock is never a step,
and the text is what appeared. The builders, one per kind:

- **A press** is a click, a double click or a drag; **a notch** a scroll, with the notches after it.
  The hook counts in wheel units, which on Windows it multiplies by the lines a notch scrolls in the
  mouse options, so the recorder adds a scroll's units up and divides by one notch's: a touchpad's
  slices make whole notches. Replayed, each notch is a wheel event of its own, 20 ms apart, because
  some apps count wheel events rather than add up their size.
- **A modifier** is a hold where it went down and a release where it came up, with everything between
  them a step of its own: Ctrl+C is Hold Ctrl, Press C, Release Ctrl, and Ctrl+click is Hold Ctrl,
  Click, Release Ctrl. Nothing is worked out about what the modifier meant - the order replays it.
  Shift held over typed text and nothing else is the one exception: its capitals already carry it,
  so it is part of the text instead.
- **A key that is pressed** - one that typed nothing or only a control character like Enter's, or
  any key under a held Ctrl, Alt or Win - is a press of that one key, its repeats counted as the
  time held. AltGr is Ctrl and Alt, so its characters are held and pressed too, for the layout to
  make again.
- **Characters** are read forward as one entry, which starts at the Shift of a first capital: a held
  key types its repeats, a Shift held over text only is passed over, a Backspace takes back what the
  entry typed while some is left, and anything else - a click, a pressed key, another window, a
  resume - ends it. Time does not.

Every action comes out the same shape, a `HumanAction`: the action and every input it was made of,
whose first input places it in the recording and whose last says when it ended - which Review can
later show - and the pauses between them are measured as they come. Each builder adds its own to one
`RecordingReader`, which marks the action's inputs handled as it adds it, and is where every builder
asks what it needs to know about the recording around an input - a key's repeats and key-up, whether
it typed under a held Ctrl - so no builder calls another. What a Shift was held over is the typing
builder's own question, the only one that asks it. The first version's click skipped every input
until its release, a key let go mid-click among them, which left that key held for every letter
after it; a later one folded the cursor and the keyboard apart and merged them back, which needed
rules for what neither could see. Reading everything in one order needs neither.

**The recording bar** (`windows/recording-bar`) is a window of its own: frameless, always on top,
never taking the focus, so a click on it leaves the keyboard with the application, and left out of
every screenshot (`setContentProtection`). Record now opens it at the top of the screen and
minimises the main window, and the recording starts from the bar's Start. The tester arranges their
windows first, and the bar is subscribed before the first `RECORDING_PROGRESS` goes out: a bar
opened after the start would hear nothing until the first click, since a broadcast is never sent
twice. It shows READY, REC or PAUSED with a clock, the action count and the last action, Pause
and Stop with their keys, Stage, and the live list: the inputs folded a few times a second the way the stop
folds them, "Typed 15 characters" rather than every key-down and key-up, typed text counted rather
than shown. The bar only asks. Every change goes out whole as `RECORDING_PROGRESS`, and the
recording page follows a stop or a discard from there, whichever of the bar, a hotkey or the page
made it, and closes the bar.

**Its hotkeys never reach the recording**, whatever window is in front. They are two settings of
their own, F9 and F10 by default, and the same key may also be one of the debugger's, since the
two are never live at once. Stage has no key: it is only on the bar. `RecordingHotkeyReader` reads them at the hook before the filter. A
combination's first keys are held back while they are down, so the Ctrl of Ctrl+F9 is not in the
recording by the time F9 comes, and anything else lets them through first, in order.

**What each click hit is found once the recording stops**, never during it, where it would take the
CPU from the application being recorded (`Business/Recording/Elements/ClickedElementFinder`). For
every click, drag and gesture inside the main area, `Business/Ai/Vision/ElementLocator` runs OmniParser's
detection model over the 640 by 640 pixels of the press's screenshot around the click, at their own
size: the whole screen at full size is six times the work and gigabytes of memory, and shrunk to fit
the model it makes a small icon a third of its size. The model scores every 8, 16 and 32 pixel cell
for an element centred there; the boxes that clear 0.25 are thinned by non-maximum suppression, and
the smallest the click is in is the element, so a click on an icon in a row is the icon. OCR (§8,
Reading text) reads the text on it, and again at twice the size when faint text reads as nothing. When the model
has nothing to say - no model file, nothing under the click, or an element that runs past what it
looked at - a box around the click stands in, 64 pixels at 96 dpi, and its note says why.

About three quarters of a second per click on the CPU, and a second and a half to load the model,
which is let go after: it is hundreds of megabytes the app has no use for between recordings.

**A stop starts that work and returns** (§4). Whichever of the page, the bar or a hotkey stopped
it, the backend works the recording through in stages - finding what each click hit, then which
clicks hit the same picture, then what changed on the screen - and `RECORDING_PROGRESS` carries
the stage, which of how many, and how far through it: "1 of 3 · Finding what you clicked: 44 of
90". A stage that cannot be counted ahead has no total, and the page shows
a bar that moves rather than one sitting at zero. Stages are never added into one percentage: a
click and a model call are not the same unit, and the bar would jump or stall. A stage that fails
costs the review what it would have added, never the recording, and the progress says why. Then
READY, and the page asks `Recording.getResult` for the actions, what each click hit, the groups
and what changed on the screen.

**Which clicks hit the same picture** (`Business/Recording/Elements/ClickedElementGrouper`) is what
a list of coordinates cannot say - "these four clicks were the download icon on four rows" - and
what lets the draft find them all with one template. It compares the clicked elements, never the
crops around the clicks: the middle of a crop around a small icon is mostly what surrounds it, so
the same icon on two rows did not match and two toolbar buttons side by side did. Each element is
looked for inside the other's rectangle widened by six pixels at 96 dpi, from that press's
screenshot, both ways (`IOpenCvService.GroupSameElements`), since the model's boxes shift a little
between screenshots. It is shy on purpose, because an invented group becomes a loop that clicks the
wrong thing and a missed one only a longer draft: only elements the model found, never a box around
the click; never a flat one; never two whose read text differs. A group in different places is
marked moving, the same place clicked again is not.

**What changed on the screen** (`Business/Recording/Screens/RecordedScreenReader`) is read last:
every press's screenshot of the main area whole, by OCR at the press's DPI, every word with its
rectangle - about a second a screenshot on the CPU - and each compared with the one before
(`Business/ScreenText/`). A
press's screenshot is from just before it, so what a click did shows at the next press, and what
the last one did on the screen the recording stopped on: the newest screenshot of the main area at
Stop, taken while the bar was still up, so StepinFlow's own window is never over it, and kept
unencoded only until it is read. Every line read is kept as well, once each. The line finder
can take a row of buttons for one line, "Bag Fellowship Party", so a line is cut wherever two words
are further apart than one and a half times their height. OCR reads a word differently on every
screenshot - Town was "Town", "Towri" and "TOWR" on three of one screen - so two lines are the same
when their words are, wherever they are, or when no more than one letter in three differs and they
sit within a line's height of each other. Numbers never nearly match: a badge going from 1 to 2 is a
change. Equal lines pair nearest first, so of six "Add to cart" buttons the one that became
"Remove" is the one that changed. How much changed is counted in letters, on lines of four letters
or more, because a game counts up numbers on every screenshot: over one Firestone execution the
same screen scored 0 to 0.17, or 0.43 where OCR missed four labels on one of the two, a dialog
opening or closing about 0.42, and the move to another screen 1. Half or more is a new page. Text
found tilted - the game's slanted lettering, by 5.6 degrees - is read straightened, and each word's
rectangle is put back through the tilt. The words are screen data and reach a cloud model only
through the gate (§10); how much changed is a number and goes anywhere.

The same comparison says whether the main area's window took another title between the two presses -
for a browser, another page, which a single-page application can reach with most of its words in
place. Only presses on the main area's own window count, so a click on the taskbar is no page. The
title is screen text; that it changed is not.

**Which page each press was on** comes out of it too (`RecordingResultDto.ScreenPages`): pages
numbered from 1 in the order first seen, a new one where most of the words or the title changed,
and a new one compared with every page before it, so a dialog closed or a menu left is the page it
was opened from, come back to - the same title, where both had one, and not half of the words
changed - and not one more. A number, so it goes anywhere. The draft nests by it (below). And the
**line under each click** is read on the same screenshot (`ClickedElementDto.LineText`): the
smallest line the click is on, else the nearest one the element's rectangle touches, a text field
clicked beside its placeholder; the text read on the element is cut by its rectangle, and this is
not.

**What a press landed on, as its application tells Windows** (`Core/Ports/IAccessibleElementService`,
`Platform.Windows/Accessibility/AccessibleElementService`). Every press is asked of Windows' UI
Automation - what a screen reader is told - at once and off the drain, which every input after it
waits on, and the stop waits up to three seconds for the last answer. The answer is the element's
kind in English whatever the system's language (button, link, text field, check box), its name - its
label, a web page's `aria-label` - whether it is enabled, ticked or a password field, and up to four
named things it sits in, nearest first: `button "Add to cart", in group "Shipping", in window
"Checkout"`. Chromium answers natively since Chrome 138; an application that draws its own pixels, a
game, answers with its window, which says nothing its title does not, so that answer is dropped.
UI Automation's three COM interfaces are declared by hand, slot for slot as far as the ones called,
as the screen capture's are, and every property comes through `GetCurrentPropertyValue` - after
asking whether the element has the pattern at all, since one without the toggle pattern answers
"unticked". Asking turns on an application's accessibility tree if nothing had, which a browser
builds for the whole page, so the first press of a recording can cost it a moment. Review shows it
as "Windows says"; a typed field takes its input's name from it before the text read on the field,
which may be a placeholder or what the field held, and a password field is made secret. The
draft names a press by it before the text read on it, unless it names only what the press sits in
(below). For names and structure only: execution
stays image-based. Windows only: macOS and Linux each want an adapter of their own (`TODO.md`,
Recording).

**Review** comes before anything becomes a step (`features/recording/review/`). Every action is listed
with a checkbox to keep it, the main area at that moment (`Recording.getScreen`: the action's own
press, or for typed text and keys the last click's before it) with where it was clicked, and the
rectangle the template will be cut from, whose corners are dragged to change it. Each action says
what it becomes - wait for the element then click it, click the exact spot, an input or the text
as written, or left out; a click outside the main area starts left out - and typed text is named
from the field clicked before it, secret when that field reads as a password. What was read under a
click, how long the tester waited and the wait that becomes - three times that, at least ten
seconds, at most sixty - and the clicks on the same picture are shown beside it. A gesture shows the
forms its window asks with (below), starting from what the tester answered there. Nothing goes to the
backend until the draft is asked for, which sends it all in one call (`Recording.review`) for the
draft to be built from.

### A recording and a description become a flow

The tester describes the flow and records it once, and reviews the result rather than answering for
each action. A QA test is the same path with more added (below). Every result lands in the editor
and never executes on its own - a model that could execute what it wrote would turn a prompt
injection in OCR'd screen text into a moving mouse.

```
Setup -> Record -> Review -> Draft (rules) ---------------------------------> Import -> Check -> Editor
                                    \-> Outline -> Model -> words, field by field -/
```

`Business/Recording` is a pipeline whose folders are its stages, the way `Business/FlowScript` is:

```
Business/Recording/
  Setup/        RecordingSetupService     the flow, its main area and a Launch step, before the first click
  Session/      RecordingSessionService   start, pause, resume, stop; the queue from the hook; the live list
                RecordingScreenCapture    the main area a few times a second, a tab followed in its window
                PressedElementLookups     each press asked of its application, off the drain
                RecordingGestureAsker     a gesture paused at and asked about
                RecordingProcessor        a stopped recording worked through, stage by stage
                RecordingSessionCache     what all of them remember
  ActionBuilder/ RecordingActionBuilder   events into actions, one builder per kind
  Elements/     ClickedElementFinder      per click: place in the main area, the element's rectangle,
                                          the text on it
                ClickedElementGrouper     the clicks that hit the same picture
  Screens/      RecordedScreenReader      what changed between presses, which page each was on, the
                                          line under each click
                RecordingScreenDescriber  the local model looking at new pages and wordless clicks
  Draft/        RecordingDraftBuilder     the reviewed recording into entries, one builder per kind of
                                          action; RecordingDraft holds what they share
                DraftLayout               where each entry sits: branches, pages, stages
  Generation/   FlowGenerationService     draft, script, scanner, import; the model's words too
                RecordingOutline          the draft as the model reads it: the searches by id
                RecordingWordsService     the model asked for words, in a shape Ollama holds it to
                RecordingWordsApplier     the words put on the draft, field by field
                FlowGenerationSessionService  the model's words and Check's revisions in the
                                              background, by broadcast
Business/Ai/ScriptRevision/
                ScriptRevisionService     a script and an instruction in, a script that reads cleanly out
Business/Ai/AiDocuments/
                FlowExampleIndex          the example flows, picked by meaning
Business/Ai/Vision/
                ElementLocator            phase 6's detection model, with a fallback box
```

**The main area.** Before the first click the tester names the flow, describes it, and picks the
window it works in from the windows open at the time: the flow's main area (`FlowArea.IsMain`,
written `main` in the script). Everything the recorder keeps is relative to it - where a click
landed, the template cut there, the area's size and DPI - which is what makes a recording portable
(§8, Coordinates). A main area placed in screen coordinates says the flow is not portable.

**What the recorder keeps** is the session above. A press on anything outside the main area's
application starts left out.

**The draft is built without a model** (`Business/Recording/Draft/RecordingDraftBuilder`). Each
reviewed action becomes steps by a rule rather than a question, one builder per kind of action as
there is a parser per kind of line, all adding through `RecordingDraft`, which holds what every step
shares: a name unique in the flow, the templates, the points and the inputs. It holds what each step
is, in the order it was done, and never where it sits: `DraftLayout` gives each entry its parent and
its order, and is asked again whenever an entry is renamed or one is added between two others - a
model's stage or check. So nothing a model says can move or reorder what was recorded. The recording
comes after the steps the flow already has - the setup's Launch step first.

- A click waited for is three lines - `Find Image` waiting for the element, `Move to` it, `Click` -
  because every step is a line and a click acts where the cursor is. At its exact spot it is a
  `Move to point` and the click, the point inside the main area at that press, or `on screen` when
  it fell outside it. A drag is one line from the element or its start to where it was let go; a
  scroll a move and the scroll.
- Everything after a search, up to the next search, sits in that search's Success branch, since it
  only makes sense once the element was found. The Failure branch ends the execution, failed, with
  the reason - `Find Invoices found nothing within 10.0s` - because an empty one carries on and
  clicks blind.
- **Searches on one page sit beside each other, and a new page goes under the click that opened
  it**, by which page each press was on (What changed on the screen, above): the first search on a
  new page is in the Success branch of the search before it, after its click; a page come back to -
  a dialog closed, a menu left - goes back beside the searches of that page; a stage marker is at
  the top of the flow and what follows it starts there again. A model was told to do this from the
  notes before 2026-11-11; the recorder has the pages, so it is a rule.
- The template is the element's rectangle as Review left it, cut from the main area at the press,
  with where the click landed inside it and the area's size and DPI. Clicks that hit the same
  picture share one template.
- **Clicks on one picture in several places, one after another**, nothing else kept between them
  and each waited for by its picture - a moving click group (Which clicks hit the same picture,
  above) - are a search waiting for the picture and a `Find All Images` whose Success branch moves
  onto each hit and clicks it (`EveryClickDraftBuilder`): the download icon on every row. Every one
  on the screen is clicked, as many as there are then. The same place clicked again, or the picture
  clicked with something done between, stays clicks of their own. A model was told to fold them
  before; the groups say which, so it is a rule.
- **Or by its words**, which Review offers for a click, a drag or a check: the words read on the
  element, which the tester can change, and the same rectangle as an area of its own inside the main
  area - `Find Invoices area`, placed as a point is, at the press's DPI. The search is a `Wait For
  Text` there. A text search succeeds with its area's centre, so the move goes to the middle of the
  rectangle, which is why for a click it sits on what was clicked. Words left empty go by the
  picture. Read by PaddleOCR, the same on every system (§8, Reading text).
- **A search is named after what was pressed** (`RecordingDraft.TextOf`): as its application named
  it to Windows - `Find Username`, `Find Open Menu` - unless that names what it sits in, a group, a
  page or a pane, rather than the thing; then the words read on it, and when the element is only a
  box around the click, the whole line under the click read on the screenshot, which a box cuts
  through: at 3840 by 2100 a box is 80 pixels and read "andard cked_ou" for `standard_user`; then
  what the local model called it; `Find icon` when there is nothing. Through
  `FlowNameHelper.MakeUnique`, in the one namespace steps, areas, points and inputs share; found by
  its words, from those.
- Typed text is an input with what was typed as its default, and the step types `{{email}}`; a
  secret keeps no value in the draft, and is typed once on the Inputs tab. Typed as written, the
  text is on the line. A key is a `Press`, held and released around a click as it was recorded.
- A pause is no step at all: it sizes the next wait. What was left out in Review is no step either.

The draft is a flow on its own, so with no model configured it is what the tester checks and then
edits, and it prints and reads back with no diagnostics, which a test holds it to.

**The plain draft ships.** Review's "Plain draft, no AI" sends what was decided, then
`Recording.importDraft`: `Business/Recording/Generation/FlowGenerationService` loads the flow's
rows the setup made, builds the draft on them, prints it, and imports the script through the
scanner like any file, with the templates it cut as the images. So a recorded flow, a file and a
model's answer land the one way, and the plain draft is held to what a file is: one that does not
read cleanly leaves the flow as it was and says on which line. Then Check shows it (below).
The import replaces the flow's rows and keeps the row, so its name, description, QA switch and
sub-flow setting stay as the setup left them.

**Not for a QA test**, which is generated by the model only. The setup's QA switch needs AI on
first (§10), Review leaves the plain draft out for one, and
`Recording.importDraft` refuses one, so the rule is not the page's alone. When no answer from the
model reads cleanly, the plain draft is still saved in its place, as for an automation, so the
tester has a flow to fix rather than nothing. Steps recorded into a QA test that already exists are
inserted as a plain draft, as into any flow.

**Into a flow that already existed it is inserted, not imported.** `Recording.insertDraft` builds
the draft on the flow's rows at the editor's position - the steps after it move down, and each
search sits at that position rather than at the top - and reads it back through the scanner the same
way, but writes only the new rows, through `FlowStepDataService.CreateTreeAsync`. An import replaces
every row, and a step replaced loses its link to the executions it was in, which only its name then
keeps. A draft that does not read cleanly writes nothing and says on which line, as an import does.

**A model writes its words, never its script** - Review's "Generate flow". Until 2026-11-11 the model
was handed the plain draft as a script, under the whole of the flow format, and answered with the
whole script revised, three attempts at most. On the tester's `qwen3.5:4b` it never got through: a
step renamed but not the line that moved to it, a point used that the header never declared, two
steps given one name - each attempt fixing one and breaking another, and the plain draft saved.
A small model writes a good name for one step; it does not rewrite a hundred lines with every
reference intact. So the split is now between the words, which are the model's, and the structure
and every reference, which are the code's:

- **The outline** (`RecordingOutline`) is what the model reads: the task in the tester's words, the
  flow's name and kind and the window it works in, then each search by an id of its own - `S1`, `S2`
  - its name, the notes of what the screen said there, and what was done once it was found, `then:
  double click, hold Ctrl, press C, release Ctrl`. Stage markers stand among them as `## name`. An
  input is its name, never its value. No script, no header, no format: a model given only this
  cannot misname a point or an area, because it never writes one.
- **The answer is words by id** (`RecordingWordsService`): for each search a name, a comment saying
  what it is for in the task, and what its failure says, with questions only the tester can answer
  and notes of what to check. Its shape is a JSON schema sent as the response format, which Ollama
  decodes against: every field required and no other, and an id only one the outline has. Thinking
  is off, as for every request to Ollama. Asked again when an answer does not read, twice at most;
  not when the model cannot be reached.
- **Put on the draft field by field** (`RecordingWordsApplier`). A name empty or longer than a line
  keeps the draft's and is said in `NotTaken`; one already used is numbered; a search renamed takes
  its own area along, and a failure message that only named it. A comment goes over the search,
  above the one saying how long the tester waited. The rest of the answer is taken whatever one
  field did, so a mediocre answer still improves most of the flow, and nothing in it can stop the
  import. Measured on `qwen3.5:4b` on the tester's recording of Swag Labs, eight searches: about
  seventeen seconds for the words, every comment and failure message fitting the task, and the names
  kept, since the plain draft's were right (`SwagLabsRecordingTests`, left out of the tests unless
  `-p:Model=true` brings in `Category=Model`).

The request is a few thousand tokens where the script and the format were fifteen thousand, which is
most of the time on a CPU. A cloud model goes through the same path; a bigger model only writes
better words.

**How a finished flow names its searches goes with the outline** (`FlowExamples.WordsOf`): the
example's searches, each with its name, its comment and what its failure says, which a small model
copies better than it follows a description of them, and the instruction to take none of its names.
Which example is picked by meaning (`FlowExampleIndex`): each document in `AiDocuments/Examples` is
embedded once by its title and what it is for, with the bge-small model the assistant's guide index
uses, and the nearest to the flow's name, description and window goes, above a floor; below it, with
a description of fewer than three words, or with no embedding model, the main area's rule picks, the
game for a window that stretches its contents and the website for anything else. An example is added
by adding a file, and a store's most used flows could be indexed the same way. The vectors belong to
the embedding model, so changing the model that answers changes nothing here. "Ask for changes"
takes its example the same way, whole.

**Each example is still a real flow finished by hand**, an AI document in `AiDocuments/Examples` -
what it shows, then its script fenced as ` ```sflw ` - so the assistant searches them too. The
website's also carries the draft as it was recorded (` ```sflw recorded `), with what was wrong with
it, for a person or the assistant, never the request. Firestone is cut to four of its thirteen
notifications, one of each pattern. `FlowExamplesTests` imports every finished example as a file is
imported, validation included, so a format change cannot leave an example the app refuses; the only
issue allowed is a Notify with no Discord bot: a bot is the machine's own, and no example names one.

**The local model looks at the screens first** (`Business/Recording/Screens/RecordingScreenDescriber`),
the generation's first stage, "Looked at the screens with the local model: 3 of 8". It is always
the local model, through `IAiClientFactory.CreateLocalAsync`, even when a cloud model writes the
words: the pictures never leave the machine, and only what it says goes on. It describes in one
sentence the first screen and every one where a new page began, by its words or its title - from
the screenshot, never from the OCR text, which made a small model invent what a screen was for -
and names in two to four words every kept click that neither OCR nor Windows named, from its
cutout, enlarged to 64 pixels at least, with the verb and the full stop a small model adds anyway
taken off. Measured on the tester's `qwen3.5:4b` on a graphics card: about 2.5 seconds a look,
"settings gear" for the Firestone gear and "This is a character skill tree interface showing
attribute points and specialization paths for Tree XLVII in a game." for its research screen. Eight
pages and twelve clicks at most, a look that fails is passed over, and none at all when the local
model takes no pictures, or when a cloud model may not be sent screen content, since what it said
would be withheld. Not at Stop: a plain draft needs none of it, and a model on a CPU is slow.

**The notes of what the screen said** (`Business/Recording/Draft/ScreenNoteWriter`) are lines
starting `screen:` under each search of the outline, from what changed on the screen, what was
pressed and what the local model saw (above) - where a new page began, by its words or by the
window's title, what was on it and what the local model sees it as; what was pressed, as Windows
named it or the local model sees it; and what appeared and went after the click, in the words read,
lines of four letters or more, six at most, each cut at fifty characters with an ellipsis. They are
never in the flow: they are screen text, and a flow is written into a repository (§11). With screen
content not allowed, the notes say where a new page began and what kind of thing was pressed in
what state, and no word read, no title, no name and nothing the local model said; the model is told
the names are placeholders, to name the steps from the task and what was done.

**A QA test is asked a second time** (`AiPromptHelper.ProposeTestParts`), once its searches have
their names: where each part of the journey the task names begins, as stages, and the checks that
turn a replay into a test - after "Add to cart" the button reads "Remove", so a `Wait For Text` for
"Remove" right after that click, its Failure ending the execution. In one request with the names, a
4B model's names got worse, so the two are apart. The code holds both to what it can stand behind:
a stage goes only before a search where no stage begins, so the tester's stand; a check only for
words a screenshot showed appearing after that search's click, quotes and a cut line's ellipsis
taken off, four letters at least - anything else would fail the first execution - in the main area,
after what that search did (`RecordingDraft.AddCheckAfter`). With screen content not allowed there
are no words to check for, and the schema allows no checks. Whatever the answers, a QA test has a
stage, named after the flow when nobody named one, and ends with `End Execution passed`
(`RecordingWordsApplier.MakeTest`) - which the code asked the model for before, and refused an
answer without. A value typed wrong on purpose (below) is typed as it was, since the model writes no
step; the outline shows it as such.

**The checks a model adds are proposed, and the tester keeps or drops each**
(`Business/Recording/Generation/ProposedChecks`). Which checks it added is told by comparing the
saved flow with the plain draft, not by anything the model marks: a search nothing moves to is a
check rather than a click's, and one with the words or the templates of a check the draft had is
the tester's own, however renamed. Each is tested the way an execution tests it against every line
OCR read on the recording, and Check says when no screenshot showed its words. Check lists each with
its line and the search it sits under: Keep only stops offering it, since it is in the flow already;
Drop (`Recording.dropProposedCheck`) takes it out with its Failure branch, and what its Success
branch holds takes its place, then imports the flow again. A check another step reads is not
dropped, and says which step. "Ask for changes" carries the ones not yet decided, by name, so a
revision still offers them even renamed.

**What the model asks, and what of its answer was left out**, are on Check beside its notes: a
question only the tester can answer - "Should it sign in with a different account each time?" - is
answered through Ask for changes or the editor.

A model takes minutes over a draft, so it is work longer than a request can wait for (§4):
`Recording.generateDraft` starts `FlowGenerationSessionService` and returns;
`FLOW_GENERATION_PROGRESS` reports writing the draft, each attempt with the last refusal's lines,
and saving; and `Recording.getGeneratedDraft` has what was saved - which draft, the model's notes,
and the exact request it was sent, its lines that hold text read off the screen marked. The page
draws it as the mockup's Generating board, beside what went to the model and what stayed on the
machine.

**A cloud model is sent text read off the screen only once the tester has read it.** When a cloud
model answers and the request holds any, the generation stops at `WAITING_TO_SEND` with the request
- `ScriptRevisionService.RequestOf`, the one ReviseAsync then sends - and its lines that hold it,
found by `ScreenTextLines`: every note, and every line with the words read on an element - OCR's,
the name Windows had, the words Review searched by - as whole words. `Recording.sendDraftRequest`
lets it go; Cancel sends nothing and saves nothing. The local model is never held, since nothing
leaves the machine, and neither is a cloud model with "Let the cloud model see your screen" off,
unless Review searched by words: those go all the same (`TODO.md`, Recording), so they are shown
first. Lines are found by their words, so the tester's own line with the same words is marked too -
too much rather than too little.

**Check comes before the editor**, for either draft (`features/recording/check/`). What it shows is
read back from the database rather than taken from what was sent: the script, numbered, with the
lines the model added or changed marked against the draft it was given; the tree; the model's notes;
the import's warnings at their lines and what the validator says; and the steps, inputs and
templates counted, a template once however many steps search for it. The script is the script
view (§13) with the draft as its merge original, so what the model wrote is green and the draft's
lines it replaced sit red above it; "Changes from the draft" puts the two side by side with the
unchanged stretches folded. A test holds the saved flow to differ from the draft by what the model
changed and nothing else, so the marks are the model's. Nothing there executes: "Open in the
editor" goes on, and "Discard the flow" deletes it and the recording.

"Ask for changes" sends the flow's script back with what the tester wrote, through the same
`ScriptRevisionService` under `AiPromptHelper.ReviseOnRequest`: make those changes, leave the rest,
and say in a note when a change needs a search no template shows. `Recording.reviseDraft` starts it
in the same session, followed by the same broadcast. An answer that reads cleanly replaces the flow
and becomes what Check shows, marked against the script before it; when none does, the flow is as
it was and Check says why beside the ask. The script goes as the flow holds it, step names
included, so a cloud model without screen content gets the names the plain draft read off the
screen (`TODO.md`, Recording). What it wrote never executes on its own (§10).

**Two gestures while recording**, in either kind of flow. Each is a setting under Recording keys,
Ctrl + click and Ctrl + right click by default; a real Ctrl + click is recorded once they are
rebound. Since 2026-11-09 a gesture pauses the recording and asks what it is for, in a window of its
own over the application (below); with "Ask at once" off (`RECORDING_GESTURE_ASK`), it only marks,
and Review asks, as before.

- **Ctrl + click marks a check at that spot**, where the position matters, which is why it is the
  left button: by its picture, or by its words and a rule they follow - Check Text's contains, is
  exactly, matches a pattern, a number above, below or between, not empty - waiting until it is
  there or until it has gone. Every recorded check waits, so "it must be there" and "wait until it
  appears" are one step. The draft makes it a `Wait For Image`, a `Wait Until No Image`, or a `Wait
  For Text` or `Wait Until No Text` on an area of its own, its Failure ending the execution and what
  came next in its Success branch, as after a click's search
  (`Business/Recording/Draft/Actions/CheckDraftBuilder`). A rule that compares a number or a pattern
  names the check from what was read on the element, not from the rule. A check keeps a template of
  its own: one shared with clicks could be folded into their `Find All Images`.
- **A check can tell Discord** when it succeeds, when it fails, or both, each with a message of its
  own, through one bot. A success's `Notify` is the first step of its Success branch, a failure's
  comes before the `End Execution failed` already in its Failure branch
  (`RecordingDraft.AddToFailure`), each `with result of` the check, so what it found, or why it
  failed, goes with the message and there is nothing to pick for it.
- **Ctrl + right click marks a step that is not a click** where one would have been: a command, an
  application opened, a notification through a bot, an input's value typed, a wait of a
  set time, or a stage begun there - a `Shell`, a `Launch`, a `Notify`, a `Type {{input}}`, a `Wait`,
  or no step, its name beginning the stage at the next search as any action's does
  (`SubstituteDraftBuilder`). Where it was marked does not matter, which is why it is the right
  button. An input the flow does not have yet is added with no value.
- **A notification can read the screen where it was marked** and send what it read: "Read text
  from the screen" on its form, and a pattern for the part to keep. The draft writes a `Wait For
  Text` on an area of its own, the rectangle Review left around what was pressed, that waits until
  it reads anything, its timeout a recorded search's, with the `Notify` in its Success branch `with
  result of` it, since a Notify reports only on a step it sits under (`RecordingDraft.AddRead`,
  `AddToSuccess`). Its Failure branch is empty, so a read that finds nothing sends nothing and
  carries on, and what was done next sits beside it, as after a Find All Images. It is named by the
  words read there with no digits in them, "Read Gold" for "Gold 1,234", or "Read text" for a price
  alone: the value seen while recording changes, and the name heads every message. It is the one
  step marked instead of a click whose place matters, so every right click's press is found as a
  check's is; the window shows the area and what reads in it now, and says it is moved and resized
  in Review. A press outside the main area has nowhere to read, and the notification goes without
  it. In a QA test the read is a check that cannot fail the execution, so validation notes it as one.
- **Asked at once.** The session pauses itself at the gesture's press, as it is drained - the release
  is still held back, since the hook holds back the button rather than the list - notes the window
  in front, and `RECORDING_PROGRESS` carries `Asking` until it is answered: in the progress, sent
  whole, because the question's window opens after it. The bar opens that window
  (`electron/ipc/handlers/recording-gesture-handler.ts`, `windows/recording-gesture/`): frameless, on
  top, left out of screenshots, and focusable where the bar is not, since its forms are typed into.
  The choices one under the other, then the form of the one picked with a way back; the forms are
  the ones Review shows (`features/recording/gesture/`). For either gesture, `ClickedElementFinder.FindAtAsync`
  finds what was pressed while the window is open - the CPU is the tester's while paused - and for a
  check `Recording.testGestureRule` tests the rule against the words read there with
  `ConditionEvaluatorHelper`, the code an execution tests it with. The detector stays loaded for the
  next gesture, and the stop's finding takes each press found already as it was and lets the
  detector go.
- **Add and keep recording, or Decide in Review.** `Recording.decideGesture` keeps the answer by the
  press, or none, resumes, and gives the keyboard back to the window that had it
  (`IWindowService.FocusWindow`); a resume or a stop from the bar or a hotkey meanwhile leaves it for
  Review. There is no Cancel: the gesture marked a moment of the recording, and the application may
  have changed since, losing the keyboard among other things, so the marker stays for Review to keep
  or leave out. `RecordingResultDto.GestureAnswers` carries the answers by action
  (`RecordedActionDto.PressIndex`), Review starts from them and has the last word, and the bar's live
  list says what each was answered as. The time spent asking is no wait: the resume marks it.
- **Neither click reaches the application** - a Ctrl + right click would open its menu, a Ctrl +
  click a link in a new tab. The hook's handlers run on a thread of their own, too late to hold an
  input back, so `Platform.Shared/Input/GestureGlobalHook`, SharpHook's event loop written again
  because that one is sealed, decides on the hook's own thread whether a press is a gesture's
  modifiers and button, holds it and its release back, and marks both. The recorder leaves out the
  modifiers held for it, as it does a hotkey's (`RecordingHotkeyReader`). Paused, nothing is held
  back. Windows and macOS only: on Linux the press reaches the application too (`TODO.md`), the
  question's window can be in a screenshot taken as it closes, since `setContentProtection` does
  nothing there, and Wayland lets no application give another the keyboard back.

**The tester names the stages**, since what they are testing is the one thing no model can work
out. Stage on the bar marks where one begins - paused as well, while the next part is made
ready - as an action of its own (`RecordedActionKindEnum.STAGE`) that takes no time, so the click
after it still waited from the click before. Nothing is typed while recording: the bar never takes
the keyboard from the application, and the click back into it would be recorded. Review asks for
the name, "Delete customer", and offers a stage at each action where the comparison found a new page
(What changed on the screen, above), marked "new page" in the list. A named stage is a stage marker at
the top of the flow over the next search, so what came between stays in the search before; one at
the end closes the flow, where a test says what it proved; one never named, or left out, is none
(`RecordingDraft.BeginStage`). A stage of the tester's is an entry of the draft no
model's words reach: a model can only propose one before a search where none begins (above).

**A QA test adds to it**: stage markers and `End Execution` checks for what the description says
the flow is meant to prove (above), and failure paths written by provoking them rather than
imagining them. The tester types a wrong value on purpose, marks the application's rejection with
Ctrl + click, and in Review makes the typed text **Wrong on purpose**, offered only in a QA test.
The draft types it as written - never an input, which changing its value could put right, and
never a secret - with the comment `# wrong on purpose: the application should reject
it` (`TypingDraftBuilder`), and the model is told to give it a stage of its own, "Rejects a wrong
password", with the check after it as that stage's check. The screen sizes to test at and what
happens when an execution ends wait in `TODO.md`.

### Search mode and timeout

**Every recorded check waits** - `WAIT_UNTIL_STILL` for a picture and `WAIT_UNTIL_FOUND` for text,
never `FIND_BEST`. Reading the tester's speed
is the tempting shortcut and it is wrong both ways: a quick click only means the element was
already on screen on the recording machine, and a slow one is as likely to be someone reading as
the app being slow. Waiting costs nothing when the element is there - the first poll runs before
any delay, so a wait that hits at once is exactly one capture and one match.

The timeout is `max(10s, observed × 3)`, capped at 60 seconds, and always written: a timeout of 0
waits for ever, which in CI eats the whole job. The observed pause is a sample of one; the floor
covers the common case and the multiple catches the outlier. The timeout answers how long before
the step is called failed, not how long the application should take, and CI runners are routinely
two to five times slower than the desktop that recorded the flow, so being generous costs time only
on executions that were going to fail anyway. The step carries a code comment saying why:
`# recorded after a 4.2s wait`. Even where it does not drive the timeout, that is intent a screenshot
cannot carry, and the first thing a model reads when working out why a step got slow.

The recording draft writes it (`RecordingDraft.TimeoutFor`).

Polling is deliberately not as fast as possible. Screenshot plus template match is real CPU, and a
tight loop on a tester's laptop competes with the application being tested.

---

## 7. The flow script

A flow exports to a `.sflw` text file. `flow-format.md` (with the AI documents) is the grammar; this is the shape and the
decisions behind it.

```
Script Language: v1
Flow:    Login and add to cart
Id:      8f14e45f-ea2b-4c3f-9f1a-77f0d2a3b111
Description:
  # Signs in to Swag Labs with the account its inputs name.

Areas:
  <[ Browser ]>       main   window process <[ chrome.exe ]> title contains <[ Swag Labs ]>   scales with dpi   at 120dpi
  <[ Login form ]>    inside <[ Browser ]>   ratio 0.30 0.18  size 0.40 0.40

Inputs:
  <[ username ]>      default <[ standard_user ]>
  <[ password ]>      secret

Templates:
  <[ template-u9d3n.png ]>    click 150 18   captured 922x648 at 120dpi

Steps:

## Sign in

Find Image      <[ Find username field ]>   template <[ template-u9d3n.png ]> accuracy 0.85   in <[ Login form ]>
 Success:
  Move            to <[ Find username field ]>
  Click
  Type            <[ {{username}} ]>
 Failure:
  End Execution   failed  <[ no username field on the login page ]>
```

### Shaped like a compiler

`Business/FlowScript/` is a pipeline, and the folders are its stages:

```
Scanner/      Scanner                                  the stages below, one line at a time
Lexing/       ScriptLineSplitter → ScriptTokenizer     text into lines, a line into tokens
Parsers/      ScriptLineParser → a parser per line     tokens into linked rows
Models/       FlowScriptSchema                         the flow as linked rows, both directions
Printer/      Printer → a writer per line              the model back to text
Catalogs/     ScriptKeywordCatalog                     every word the grammar knows, once
Diagnostics/  Diagnostic, ScriptSyntaxException        what went wrong, and whether it is fatal
              FlowScriptImporter, FlowScriptExporter
```

**One model, both directions.** `FlowScriptSchema` is the flow as the rows the database holds,
linked: a step's area is `step.FlowArea`, its parent `step.ParentFlowStep`, never an id or a name.
The scanner reads a file into it, the exporter loads one out of the database into it - EF links the
rows as they load into one context - and the printer writes it. So the scanner and the printer are
exact mirrors, and the round trip needs no database.

**Every name is declared above its first use**, which a `Break` that only leaves a step it sits
inside keeps true of every reference. So a line is linked the moment it is read: `ScriptScope` holds what the lines above
declared, every parser is handed it, and a parser resolves a name as it reads the quote - a step
comes back already pointing at its area, its point, the step it reads. A name nothing above
declares is `NAME_UNKNOWN` at that quote, and the line reads on, so every unknown name in a file is
reported in one pass. A line is declared only after it is read, so nothing can name itself, and a
name declared twice is `NAME_DUPLICATE` at the second. Areas go one level deep, so an `inside`
naming an area that is itself inside another is `AREA_TOO_DEEP`. The forms offer only a root as a
parent, hide the choice on an area with regions inside it, and refuse the rest on save. This replaced a binder that handed out positions as ids, an importer that swapped those for real
ids one table at a time, and an exporter that built id-to-name dictionaries for the printer: four
translations of one thing, one of which let the last of two duplicate names silently win.

**The tokenizer cuts and never fails.** It splits a line at spaces, keeps a quote whole, and joins
neighbouring words into the longest keyword the catalog has, so `Wait Until No Image` is one token.
A token is a `KEYWORD`, a `QUOTE`, a `NUMBER`, `UNKNOWN`, or the `END_OF_LINE` every line ends with,
and each knows its line and column. Whatever does not belong is still a token, so every error comes
from one place: the parser.

**One parser per kind of line.** `ScriptLineParser` picks it from the section the line is in and
the keyword it opens with - one for each header line, one for each step type - and is the only class
that writes the schema. The grammar of a line is its parser's, and so are the names on it. What the
lines mean together is the orchestrator's: the tree the leading spaces draw, the names each line
declares, a comment belonging to the line below. The parsers share `BaseParser`: `Expect` takes a token that must be there, `Extract`
takes one and returns its value, an `Optional` one never throws, and the index moves in one place.
A line that stops making sense throws `ScriptSyntaxException` at that token, and the Scanner records
it and reads on from the next line, so one typo is one error rather than the end of the report.

**One writer per kind of line, the mirror of the parsers.** `Printer/Header`, `Printer/Steps` and
`Printer/Structure` hold a writer for each parser in the same place under `Parsers/`, so
`AreaParser` reads what `AreaWriter` writes, and changing a line's grammar means two files with the
same stem and a catalog row. Each writer takes its row and writes its line a piece at a time, in
the order it reads - one `WriteKeyword`, `WriteQuote` or `WriteInteger` per piece, as its parser
expects and extracts one token at a time - keyword choice included: a search picks its word by
mode, `Type` or `Press` by input type, `Shell` or `Launch` by mode. Pieces are a space apart unless
a gap (`WriteGap`: three spaces between clauses, two in a few places) or a column
(`WriteGapUntil(KEYWORD_GAP_UNTIL)`) already parts them, so a separator is only written where the
format wants more than one. They share `BaseWriter` - the
pieces, the gaps and the columns the file is laid out in - and the steps' writers `BaseStepWriter`,
with the clauses more than one step writes (`in <[ area ]>`, a target, a condition, a timeout), as
the parsers share `BaseParser` and `BaseStepParser`. `Printer` is the orchestrator, as
`ScriptLineParser` is: the sections, their order, the leading spaces, the blank line above each
top-level step, comments above the line they belong to, and which writer writes a step.

`Printer.Write(FlowScriptSchema) → string` is a pure function, and deterministic: the same flow
writes the same bytes. Children in `OrderNumber` order through their `ParentFlowStep` link, and
areas roots-then-children alphabetically, so a caller cannot hand it rows ordered by chance.

`ScriptKeywordCatalog` holds every word the grammar knows **for both directions** - each row a text
and the value it means: a step type, a condition, a title match, a section header, a clause word
like `inside` or `timeout`, a unit like `ms`. It answers both ways - `Get<TEnum>(text)` for the
value a word means, `GetTextOfKeyword(value)` for the word a value is written as - and the printer,
the parsers and the validator all ask it, so a keyword cannot mean one thing on write and another on
read. A value is read as a catalog word and nothing else: the first reader used
`Enum.TryParse`, which also accepts a number and comma-joined flags, which is how `Press Ctrl+1` once
pressed Ctrl+B and `System 99` parsed as an action that does not exist.

### Everything that makes a template portable travels in the file

```
Templates:
  <[ template-k3x9q.png ]>    click 150 20   captured 922x648 at 120dpi

Steps:
Find Image  <[ Find login ]>   template <[ template-k3x9q.png ]> accuracy 0.9   match shape and colour   in <[ Browser ]>
```

Facts about the picture go in the header - the click point, and the area size and DPI it was
captured at. Decisions about the search go on the step - `accuracy` on each template, `match` for
the mode. The area line carries `scales with` and its DPI. In a script a template is named by its
file, and the step's parser takes the picture from the scope as it reads each template: every step
naming the file links to the one `FlowTemplate` the header line made, with its own `accuracy`, the
way the database holds them. A `required` after a template, from files written when templates could
be AND-ed, is read and ignored.
Every template has a click, so the printer always writes it and the parser always requires it. A
step naming a template with no line in the header is `TEMPLATE_UNKNOWN`, reported at the name.

Before this, export wrote the PNG and nothing else, and import filled the rest with defaults: every
imported flow clicked the top-left corner of every button, and a step with three alternative
templates came back needing all three on screen at once. The byte-identical round trip could not
see it, because the printer never printed those fields - which is why the round trip now also
compares rows.

### Import is transactional

Parse, then replace. Everything before the transaction is pure, so a file with a typo reports
the line and leaves the flow exactly as it was — half a flow is worse than no import, and whoever
hit the error is usually mid-edit.

The rows are added as one graph and saved once: EF inserts them in dependency order and fills every
key from the links. Only the flow row is saved ahead of them, because `RootId` names it by id with
no link to follow. The semantic validator runs on the saved flow, as it does after a save from a
form, so a flow it finds errors in is imported and shows them.

Deleting the old steps does not take their history: an execution step keeps the name it ran under
and its foreign key is `SetNull` rather than cascaded, so the trend for a step survives a re-import
as long as its name does.

A diagnostic carries a code and a severity as well as a line, so a warning is something a flow can
be imported with and an error is not. The import result returns them as two lists, `Errors` and
`Warnings`, and fills `Warnings` on an import that worked too, so a warning reaches the UI rather
than being dropped with the parse.

The template images come in as a dictionary by name rather than a folder path. The folder overload
reads the folder into one, so the recorder can import its crops straight from memory.

### Export and import in the app

**Export** sits beside Execute in the editor's header. Its dialog starts in the app's export folder
(`PathHelper.GetExportDataPath()`, `StepinFlow_v2\Export` under the user's local application data),
Browse picks another through the system's folder dialog, and a script of the same name there is
replaced. It ends on where the files went, with "Show in folder".

**Import** is in the rail, and reads a file before it saves anything. `FlowScript.preview` -
`FlowScriptPreviewService` - reads the file and the folder beside it with the importer's own
`FlowScriptFile`, scans it, and asks the flow data service what the write path would meet, the way
`ReplaceAsync` decides it: the flow with the file's `Id`, which it would replace, and a name another
flow already has, which refuses it. So the page can say, before Import, whether the file is a new
flow, replaces one - whose executions stay and whose script is shown beside the file's, folded to
what differs - or cannot be imported, with every diagnostic marked at its line in the script view
(§13). Templates the file names that are not in the folder beside it are listed; they import
without a picture. The file dialog opens in the export folder, the scripts already there are listed
to pick from (`FlowScript.getExportFolder`), and a file can be dropped on the page, its path asked of
the preload since a page cannot see one. The system dialogs, the file manager and the dropped file's
path are the `files` group of the Electron bridge.

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

The round trip starts from rows the app wrote, so every template in it has a generated name. A file
written by hand is tested on its own: `login-button.png` and `logo.jpg`, imported from disk and
exported again, keep their names - extension included - and their bytes.

A round trip proves `A == B`; it cannot prove either is right. So beside it sits
`SampleFlow.approved.sflw` - a printed flow a person read once and approved, compared on every
build. On a difference the new text is written beside it as `.received.sflw` and the failure names
the first line that changed. It is also the most complete example of the format in the repository.

`GoldenFlow.approved.sflw` goes the other way and starts from the file: every step, mode and field,
with a value and without one, in a flow that reads and would mean nothing if executed. Imported into
a real database and exported again, it has to come back character for character, and imported over
itself it has to leave every row as it was. `GoldenFlowTests` also holds the file to its claim, so a
new keyword, step type, enum value or column fails until the file shows it. To add a case, add the
line where the printer would put it. A file cannot show a column the language has no words for, so
those are named in the test's `NotInTheLanguage`, and each one taken into the language comes off.

### Grammar rules that only emerged from reading real output

**Every piece of text is quoted, and the quotes are `<[` and `]>`**, even when it would read fine
without. The original spec had bare names in aligned columns — a parser cannot tell where such a
name stops. Double quotes came first and needed escaping, and a stray one typed by hand cut a
message short without a word; quotes no text uses mean nothing is escaped, double quotes and
backslashes are ordinary text, and the only rule is that a quote ends at its first `]>`. The forms
refuse `<[` and `]>` as they are typed, and `FlowValidationService` flags them on any step that
arrived another way.

In the code a quote is three tokens - `<[`, the text, `]>` - and every parser reads them as written:
`ExpectKeyword(QUOTE_OPEN)`, `ExtractText()`, `ExpectKeyword(QUOTE_CLOSE)`. A quote that never
closes is then simply a missing `]>` at the end of the line, reported like any other missing word.
Four keywords take the rest of their line as text without quotes, because nothing can follow them:
`Flow:`, `Id:`, `#` and `##`.

**Every word is read, in order.** A line is read in the order the printer writes it and stops at the
first token out of place: `Unexpected "offset", expected "inside" or "on screen".` The expected half
is collected from the catalog as the parser goes, the optional clauses it passed over included, so
no parser writes a message. A word too many and a word missing are both that error, with a column,
so a hand edit that goes wrong is named rather than quietly dropped. Every syntax error is one code,
`TOKEN_UNEXPECTED`: a code per place in the grammar would only repeat what the message says, and
nothing branches on it. The codes left are kinds of problem - too many leading spaces, a template
described twice, a comment with no step below it, a missing `Flow:` line, a flow name that cannot be
a file name, a name nothing above declares, a name declared twice, an area inside one that is
already inside another.

**Unquoted, a word is a keyword or a number**, and a number carries its unit: `800ms`, `120dpi`,
`1920x1080`. A duration is always milliseconds, so it has one spelling. Pairs read like the rest of
the line - `offset 40 8`, `click 60 12`, `ratio 0.30 0.18 size 0.40 0.40` - so no value hides a comma.

**A comment belongs to the line below it**, whatever that line is - a step, a `##` stage or a
`Success:`/`Failure:` branch - because every step type carries `CodeComment`. A branch with nothing
under it is left out of the file unless its comment says why it is empty. A comment above anything
that is not a step is left out with a `COMMENT_UNATTACHED` warning at the comment: handing it to
the next step down would attach a note to a line it was never written above, and failing the
import over a note would refuse a flow that works.

**The description is comments under a heading.** `Description:` sits under `Kind:` and every `#`
directly below it is a line of its text, a blank line a bare `#`. It reuses the comment's spelling
rather than a quote, so it can run to many lines and hold `<[` and `]>` - a quote ends at its first
`]>`. Each line is trimmed as a comment is, so indentation inside a description does not survive.

**A `Notify` of several lines is a quote for each**, `<[ first ]>  <[ second ]>`, a blank line an
empty one. A quote ends with its line, and the form is a text area, so a message with a line break
used to print a line nothing could read and the whole import was refused.

**The main area is a word on its area line**, `main`, after the name. A flag on the area rather than
a key on the flow: the key it replaced, `Flow.AppUnderTestAreaId`, was `NoAction` to break a cascade
cycle, so an import that deleted the old areas would have failed the moment one was set. Two are
`AREA_MAIN_DUPLICATE`; `main inside` is refused by the grammar, as the main area is a root.

**An input carries its recorded value**, `default <[ v ]>`. A secret writes none: a hash cannot be
typed back, and an encrypted value would need its key wherever the flow executes. So `secret` and
`default` never stand on one line. Its value is typed on the Inputs tab and kept in the database,
and an import keeps it for a secret of the same name, since the import writes the inputs anew.

**Every step is a line, and the line says everything the step does.** A click and a scroll act
where the cursor is, so the script writes the position as a step of its own: `Move to <[ X ]>`, then
`Click`. The script once wrote `Click at <[ X ]>` and `Scroll down 3 in <[ area ]>`, and the
validator wanted a point on both, while the worker and the form had always acted at the cursor - so
every click made in the editor was an error and Start refused the flow. Drag stays one line: it is
one gesture, and both its ends are written on it.

**A key held over a click is its own line before and after**, `Press <[ Ctrl ]> hold` and
`Press <[ Ctrl ]> release`, rather than a modifier on the click. The click stays the cursor's, and
a held key is one idea for every step between - a click, a scroll, a drag - the way `Click hold` and
`Click release` already split a press around what happens between. A plain press writes neither
word, so every existing script reads the same.

**A `Find All Images` works through its hits in its own Success branch**, once per hit, and inside
that branch the search's own name is the hit of the current pass: `Move to <[ Find add buttons ]>`.
The grammar used to promise a `Loop each match in` with a `match` keyword the engine never had - a
loop naming a search has no count, so the validator refused it and it made one pass. Building that
loop was rejected: it would have touched the walker, the cache, the worker, two validator rules and
the history to say one more thing, "once, when something was found, before the hits".

**A reference to nothing is an empty quote**: `Move to <[  ]>`, `Break out of <[  ]>`, `Focus Window
<[ name ]> <[  ]>`. It is a step with an error, which the validator reports and Start refuses, but an
import keeps a flow with errors - so the line has to read back as what it was, and the next import or
branch switch shows the same unfinished step. Until 2026-11-07 these were the word `nowhere`, whose
only job was telling the reader the empty value was on purpose, and an empty quote says that too. A
name nothing above declares is still `NAME_UNKNOWN` and refuses the file, since importing it would
erase what the line said.

**A `Launch` with no area takes one command string**, not an executable plus arguments. The model
stores one string; inventing a split it does not have would fail the round trip on the first export.
A launch of an area has the split stored: the program on the area, the arguments on the step.

**`Id:` is in the header.** Identity travels in the file or a clone cannot recognise a flow.

**A blank line goes above each top-level step, and nowhere else.** A marker is written like any
other step, so it gets the same one gap, and none goes inside a loop or a branch, where the leading
spaces already group the lines.

---

## 8. Execution

### The engine

A flow is walked with an **explicit stack**, not recursion. Infinite loops make recursion depth
unbounded, and a stack gives pause, resume and step-into almost for free.

A `Break` leaves a loop or a check it sits inside by unwinding the stack to that step's level: what
is pending inside it is dropped, a loop's next pass and a search's next hit with it, and the walk
carries on with what follows the step it left. It replaced `Go Back`, which jumped to an earlier
step: a retry is now a loop with a count and a Break on success, so it gives up after the count and
the part that repeats is indented under the loop. Going back from inside a branch also left the
step after the target pending once per jump, which ran it again for every retry.

`ExecutionFlowWalker` decides which step runs next and executes nothing - it is handed each step's
result - which is what makes the most intricate code in the repository the cheapest to test.
`ExecutionEngine` drives it: run the worker, place the result, ask for the next step.

Everything an execution needs sits in memory rather than in the database. History is written in
batches, a failed step with the rest, and only if it was asked for — turning history off changes
what gets stored and never what a flow does. **A step's result is kept for the steps under it** and dropped as the walk leaves it,
since 2026-10-26: a location for the Success side, a value for both, because a Read Text or a System
Command keeps what it read or printed either way. The validator holds a `{{name}}` written in text
to the same rule as a step picked from a dropdown. Until then the walker dropped nothing - two lines
in the wrong order - and every result stayed readable until its step executed again, a stale value
from an earlier loop pass included.

`StepWorkerFactory` maps `FlowStepTypeEnum` to an `IStepWorker`. The map is built in
`App/DependencyInjection/ExecutionServiceRegistration.cs` rather than inside the factory, so the
factory needs no container and the whole type-to-worker relationship is on one screen. Workers are
singletons because they hold no state.

### The step types

```
System     WAIT, LOOP, BREAK, SYSTEM_COMMAND, SYSTEM_ACTION, SUB_FLOW,
           NOTIFY, END_EXECUTION, STAGE_MARKER
Input      CURSOR_CLICK, CURSOR_DRAG, CURSOR_SCROLL, CURSOR_RELOCATE,
           WINDOW_FOCUS, WINDOW_RESIZE, WINDOW_RELOCATE, WINDOW_CLOSE,
           KEYBOARD_INPUT
Perception SEARCH_IMAGE, SEARCH_TEXT
Decision   CHECK_VALUE
Hidden     SUCCESS, FAILURE
```

`SEARCH_IMAGE`, `SEARCH_TEXT` and `CHECK_VALUE` are the branching types: each gets `Success` and
`Failure` children. The script shows the *mode* rather than the type, so `SEARCH_IMAGE` in
`WAIT_UNTIL_FOUND` mode writes as `Wait For Image` and in `FIND_ALL` as `Find All Images`.

`SearchModeEnum` is one axis, not two: `FIND_BEST`, `FIND_ALL`, `WAIT_UNTIL_FOUND`,
`WAIT_UNTIL_STILL`, `WAIT_UNTIL_NOT_FOUND`. Acting on every match only ever made sense while looking
once, so it is a mode rather than a flag that would be dead in four cases out of five; waiting until
it stands still only makes sense while waiting for a picture, so it is one too, and text has neither. A `FIND_ALL` search takes one
screenshot, and its Success branch executes once per hit; inside it the search's own result is the
hit of that pass, so a move to the search followed by a click clicks each one. A `WAIT_UNTIL_NOT_FOUND`
search is the opposite: a condition with no location or value, so nothing below can read it.

A `Shell` waits for its command to exit; a `Launch` starts its program through
`IProcessService.Launch` and carries on, since an application never exits on its own. Given its
window (`opens <[ area ]>`), a launch is skipped when that window is already open and otherwise waits
for it - 30 seconds, or the step's timeout - so the next step does not act on a screen that is still
loading. The executor refuses a launch outright, and every mode has a catalog entry, which a test
holds to: Launch had none, and threw the first time one executed.

**An application area says what starts it**, since 2026-11-01: `FlowArea.LaunchCommand`, **Started
by** on its form and `started by <[ … ]>` on its line in the script. A launch of an area starts
that, with its own **Arguments** after it - `FlowStep.ShellArguments`, `Launch <[ Open the shop ]>
opens <[ Browser ]> with <[ --user-data-dir={{temp}} ]>` - which is
where a QA test's fresh profile goes. A launch with no area keeps its whole command
line in `ShellCommandValue`, `Launch <[ Open the notes ]> <[ notepad.exe notes.txt ]>`: two columns,
each meaning one thing, rather than one whose meaning turned on the area. The form shows the one that
applies and empties the other on save. An area started by a link takes no arguments
(`LAUNCH_ARGUMENTS_ON_LINK`), since Steam passes a game's its own way. Picking an open app fills it in
(`Lookup.launchCommand`, `LaunchCommandHelper`): the program file, or `steam://rungameid/<id>` when
the program sits in a Steam library, the id read from the `appmanifest_<id>.acf` that names its
folder, since a Steam game's program started on its own restarts through Steam or refuses. A library
is laid out the same way on every OS. Nothing in the form says to launch only when the app is
missing, because that is always so: launching an open app brings the same one up, or a second
window the area cannot tell from the first. The `LaunchArguments` migration splits each saved launch
of an area the same way, and one starting another program than its area's keeps its command line
and lets go of the area, rather than start something else unseen.

**A window step names a window area**, since 2026-10-26: `Focus Window <[ name ]> <[ Browser ]>`,
`Resize Window <[ name ]> <[ Browser ]> size 1280 720`, `Move Window <[ name ]> <[ Browser ]> to point
<[ X ]>`. The window is described
once, by the area, and the searches inside it and every step that places it follow when its title
changes; several steps share one area. Before, each window step carried its own process, title and
match mode, so the same window was described twice and a renamed title broke the steps but not the
searches. The step keeps `FlowAreaId` and drops those three columns (`WindowStepArea`, which links a
window step to an area of its flow that finds the same window and makes one where none does). The
worker finds the window the way the area does, but whole: a minimised window has no inside to
measure and can still be focused. An `APPLICATION` area is a window and so, since 2026-11-01, is a
`BROWSER_TAB` - a monitor or a region is `AREA_NOT_A_WINDOW` - and an old `process <[ … ]>` line is
refused with a message saying how it is written now.

**A browser tab area is found by its address**, since 2026-11-01: `FlowArea.TabAddress`, `tab <[
address ]>` straight after the browser's process, and the area is the tab's page. It names its
browser and no title, because a browser's title is its front tab's. `IBrowserTabService`
(`BrowserTabService`, UI Automation) reads a window's address bar - the first field with a value
outside a page, a text field in Chromium's browsers and a combo box in Firefox - its tab items in the
order they are drawn, and the page in front, and selects a tab; a page is never walked into. Firefox
marks every page offscreen while another window is in front, so then the largest page is taken, its
tabs' pages all filling the same space; a minimised window has none. `TabAddressHelper` decides: the tab's address starts with the area's at a
boundary, `https://` and `www.` left out of both, since the address bar shows neither.
`BrowserTabFinder` holds the rule for which tab: the resolver takes the front tab of each window of
the browser only, so a search never switches tabs behind the user's back and a tab behind another is
not on screen; Focus, Resize, Move, Close and Launch bring it to the front, the front tabs tried first and
then each tab in turn, a window it is not in put back on the tab it showed. A launch with no tab at
the address starts the area's Started by - the browser - with `--new-window` and the address, which
Chromium's browsers and Firefox all take. `BrowserTabAddress` renamed `TabMatchValue` to `TabAddress`,
dropped `TabMatchOn` - finding a tab by its title went, since a title changes with a notification
count - and cleared the title patterns tab areas had. `tab title contains` is refused. Windows only: macOS has AppleScript for the same, and Linux
AT-SPI, which browsers support unevenly.

**`Close Window` closes the way the window's X does**, since 2026-11-03: `Close Window <[ name ]>
<[ Editor ]> [timeout 10000ms] [then force]`, `WINDOW_CLOSE` beside the other three window steps, with Success
and Failure. `IWindowService.CloseWindow` posts `WM_CLOSE`, so an application can save its session,
let go of its profile lock and remove its own temp files, and `IsWindowOpen` - `IsWindow` and
`IsWindowVisible` - says when it has gone, a window hidden in the tray counting as gone. The wait is
`TimeoutMilliseconds`, 0 being 5 seconds as a launch's 0 is 30. Still open is a failure, usually a
"save changes?" box, unless `FlowStep.IsCloseForced` (`WindowCloseForced`, a column defaulting to
false): then `IProcessService.Kill` ends the window's own process and its tree, found by
`GetProcessId` before the close, never every process of the name the way `Shell KILL_PROCESS`
(`taskkill /F`) does; Explorer and `ApplicationFrameHost`, which own other programs' windows, are
left alone. A `BROWSER_TAB` closes only its tab: the browser is focused with the tab in front, and
only once `FocusWindow` says it is in front is Ctrl+W pressed, since it goes to whatever is; the tab
is gone when the window is or its tab count drops. A tab is never forced - its process is the whole
browser - and `then force` on one is `CLOSE_FORCED_ON_TAB`. Windows only: macOS closes through
Accessibility's close button and wants Cmd+W for a tab, and Linux has `WM_DELETE_WINDOW` under X11
and no general way under Wayland.

**A disabled step is skipped**, since 2026-11-04: `FlowStep.IsDisabled` (`StepIsDisabled`, a column
defaulting to false), and `Disabled` ahead of the step's line in the script, `Disabled Wait  800ms`,
so it is kept in git and shows in a review. A file without the word reads as enabled, and the word
before a `Success:`, a `Failure:` or a `##` is refused, since none executes anything of its own
(`TreeStepHelper.CanBeDisabled`). `TreeStepHelper.WithoutDisabled` leaves out a disabled step and
everything under it, and three things read through it. The engine loads only those steps, so the
walker has no rule of its own and a disabled Sub Flow's flow is not loaded. `StartExecutionHandler`
does not validate that flow. `FlowValidationService` reports nothing about what is skipped, so a
half-built step can be parked and the rest tried. Two things still count a disabled step: its name
stays taken, because the script reads by name, and a branch holding only a disabled step is not
`BRANCHES_EMPTY`. No rule second-guesses what was disabled. The existing ones judge the flow that is
left, so a disabled key release is `KEYS_NOT_RELEASED` and a check whose only End Execution is
disabled is `CHECK_CANNOT_FAIL`. A model writing a recording's words writes no step, so it cannot
disable one, and one revising a script is told to leave the word alone.

A key combination is pressed, held or released (`KeyboardKeyActionType`), the way a click is
single, double, held or released. A held key stays down for the steps after it - a Ctrl+click is
hold Ctrl, click, release Ctrl - so the click never has to know about keys. The input adapter keeps
what steps put down and nothing let go of, and the engine lets go of all of it as an execution ends,
however it ends: a flow that fails between a hold and its release, or a `Click hold` with no
`Click release`, does not leave a key or a button down on the machine.

### Searching the screen

`Business/Searching/ImageSearcher` is the whole look at the screen - the guards, the capture, the
template loop, the click points and the closest score - and both the engine and the editor's
**Test now** call it, so the two can no longer disagree. It knows the screen, OpenCV and templates,
never an execution step, a DTO or the poll loop.

**Test now's Details tune an accuracy by looking.** The matcher keeps, besides the hits, the best
candidate under the bar (`TemplateMatchRequest.RejectedLimit`, one), and the test returns it with
`IsAccepted` false. The dialog (`FlowStepSearchImageTestDialogComponent`) lists the templates with
a 0 to 1 strip each - hit, accuracy, closest wrong place - and draws the picked one on the
screenshot, its hit and near miss cut out beside the template. The accuracy to use sits between the
lowest hit and that near miss, so it is suggested halfway (`image-search-test.ts`); with no hit, just
under the near miss, worded as only right if that box is the thing. One near miss is all the bar
needs: it is the highest wrong score. Under Shape and colour the shapes its checks refused come back
too, drawn dotted with the check that refused them, and a hit carries its colour and contrast; a
refused one is never the near miss, since lowering the bar would not find it. SikuliX's matching preview and Eggplant's Image Doctor answer
the same question; a live slider was left out, since a suggestion and Test again do its job with
less to read. The form renders the dialog itself rather than through the dialog store, which keeps
elements, because **Use** writes to the form (in edit mode only, so Save keeps it) and **Test
again** replaces the result under it. It resizes with the mouse (PrimeReact's `resizable`).

**Two match modes, one accuracy.** Both find candidates with masked `CCoeffNormed` on grayscale,
so the accuracy is the shape score in both and a template starts on 0.80 in either. Shape and
colour then checks each candidate over the bar, and only those (`OpenCvService.IsSameColour`):

| | `SHAPE` | `SHAPE_AND_COLOUR` |
| --- | --- | --- |
| the right template | 1.000 | 1.000, colour 0 apart, contrast 1.00 |
| letter A, screen blank white | 0.000 | 0.000 |
| letter A, only B on screen | 0.523 | 0.523 |
| enabled button, disabled one on screen | **1.000**, found | 1.000, refused: colour 25 apart, 16 allowed |
| the same, template cut 10 pixels wide | **1.000**, found | 1.000, refused: colour 18 apart, 12 allowed |

`OpenCvServiceMatchModeTests` measures every cell on screenshots headless Edge rendered - a letter
in Segoe UI on white, a PrimeReact button enabled and disabled - with each template cut from its
screenshot 4 pixels wide of it, so the table is checked on every test pass rather than claimed.
Shape alone clicks a disabled button: PrimeReact disables one by drawing it at 60% opacity, and a
fade is exactly the change shape is built to forgive. The other four OpenCV methods are gone: the
unnormalised ones return scores in the millions that no 0..1 threshold can judge, and
`CCorrNormed` scores 0.95 against blank grey.

Colour is the distance between the average Lab a and b of the kept pixels, lightness left out,
allowed up to 28% of the template's own colour and never less than 8 (`ColourCheckLimits`).
Contrast is how widely Lab lightness varies over the kept pixels after a blur of sigma 1.5 that only
kept pixels feed, allowed from 0.85 to 1.18 of the template's. A candidate failing either is refused
and the search goes on to the next peak, up to 20 refusals, so a greyed copy with the better shape
cannot hide the live one. The refusals come back as `TemplateMatchOutcome.Refused`, the best across
the templates as `ImageSearchResult.BestRefused`, and a miss says so: `research at 0.97 refused by
its colour, 41 apart where 17 is allowed`.

**Why colour and contrast rather than brightness.** The mode this replaced, Shape and brightness,
was `SqDiffNormed` on grayscale, which mostly measures average brightness. A UI crop is mostly
background and background always agrees, so it found an "A" on an empty screen at 0.80, a phoenix
for a potion in Firestone at 0.96, and the disabled button whenever the template was cut 10 pixels
wide (0.964 against its 0.95). Hue alone is not enough either: the disabled PrimeReact button keeps
its hue within 5 degrees and only loses saturation, so the colour is hue and saturation together.
Colour cannot see a dimmed or faded grey thing, and contrast can. Contrast is measured blurred
because a screenshot at another display scale is softer: a plain spread refused the true match of 9
of 114 templates under a 0.8 pixel blur.

**The limits are fixed, and sized on simulated PCs.** The 114 templates of every flow and the test
assets were compared with copies of themselves changed the way another PC changes a screenshot -
softer from scaling, a game's gamma at 0.85 and 1.15, a colour-managed browser on a wide-gamut
screen either way round, saturation 15% either way - and every one was accepted but one, under the
stronger gamma. Copies changed into another state - 20% fainter on white or on dark, 20% darker,
greyed - were all refused, but for templates with too little colour to grey. A state 15% apart
mostly passes: a 20% one reaches a contrast of 0.84 and a true match on another PC goes down to
0.86, so 20% is about the finest a fixed limit separates. On the 175 Firestone screenshots stored at
the time, the mode agreed with the old one on every step but a close button the old one missed at
0.950. Night light, the monitor's brightness and its calibration do not reach a screenshot, and
neither did HDR: on the development PC an SDR app scored 1.00 under Shape and brightness with HDR on,
at every SDR brightness, against a template captured with it off. A second slider was left out,
since a limit tuned on one PC is the one that fails on the next; Eggplant's own advice is to use its
second tolerance sparingly.

**The migration, `ShapeAndColour`, moves every Shape and brightness step across.** A template on the
old default, 0.95, takes the new one, 0.80. One set stricter keeps a strict shape bar, 0.95, since
it was raised for something the colour may not see: Firestone's Daily missions with its small red
badge, which at the average colour is the same button without it (shape 0.90, colour 8 apart where
17 is allowed). The badge as a template of its own is the lasting fix.

**A step made in the editor starts on Shape and colour**, a recorded one on Shape, since its
template is cut under the cursor and often in its hover state. A script line without `match` is
Shape.

**Accuracy belongs to each template**, because one variant of an icon can need a looser bar than
another. A failure names the template that came closest - `no template matched, closest play hover
at 0.78 - play at 0.80, play hover at 0.90` - because a score read against the wrong template's bar
would pass a threshold it never had to clear.

**A step's templates are alternatives**, tried in list order: the first hit ends the search and
gives the point, and `FIND_ALL` takes every template's hits, the first template's first, up to Max
matches for the step as a whole. The waits follow: `WAIT_UNTIL_FOUND` and `WAIT_UNTIL_STILL` until
any one is there, `WAIT_UNTIL_NOT_FOUND` until none is. There is no AND: two things that must both be
on screen are two steps, the second inside the first one's Success.

**A wait for an image can wait until it stops moving**, since 2026-11-07: `WAIT_UNTIL_STILL`, written
`Wait For Still Image`. The first match is looked at again 100 ms later, and counts only as the same
template within 2 pixels, grown with the scale it matched at, which is a match's jitter rather than
movement; one that moved is held to where it went at the next poll. Only the position counts, so a
glow in place is still and a fade is not caught. On the form it is **Wait until it stops moving**
under Wait until found, ticked whenever that mode is picked, and a recording always uses it.
`Wait For Image` keeps taking the first match, for something that never stops - a game's idle
animation - and every wait saved before it is one. A mode rather than a column, so each line says
which it is and the keyword is still the one place the mode is written.

**A wait finds its area again on every try**, image and text alike, so it follows the window: one
that opens during the wait, moves or changes size is looked at where it is now. Found once before
the wait, a window not open yet failed the step at once, and one that moved was searched where it
used to be. `AreaResolution.Unresolved` says why an area was not found, and the waits act on it:
`NOT_ON_SCREEN` (no window, tab or monitor) is "not yet" to a wait to see it and "gone" to a wait to
see it go, since what was in the window went with it; `NO_SIZE` (minimised, or outside the area it
sits in) is "not yet" to both, since it comes back; `NOT_SET_UP` (deleted, or a region with no size)
fails at once, since no try finds it. A search that cannot be done on a try - a template bigger than
the window is now - is "not yet" too, never "gone", and a wait that gives up says why its last try
could not look. A text wait checks the `{{Name}}` it compares against before the first read, since
one never set is the flow's mistake and no try mends it.

**One computed scale, no sweep.** The area decides it (see Coordinates below), and a template
scaled larger than the screenshot is an error rather than "not found". Nine attempts at one
threshold would be nine chances at a false positive; one attempt fails legibly - "0.62 at 1.25"
says the ratio was wrong.

**Known limits, recorded rather than fixed.** Text does not survive a DPI change - a new DPI
re-renders text rather than scaling it, so a word captured at 100% scores 0.67 at 125% where an
icon scores 0.93: capture icons with Find Image, read words with Search Text. A single-colour
template matches everywhere, because OpenCV defines a template with no variance as a perfect match.
Shape and colour compares the average colour, so a state shown by a small detail - a badge - is not
told apart; the badge is a template of its own. On macOS a screenshot comes in the display's colour
space, often Display P3, which would move the colour of a flow made on a PC; the macOS capture will
have to convert it to sRGB. A template cut from HDR content itself, such as an HDR game, is
tone-mapped into the 8-bit screenshot and has not been tested.

`SEARCH_TEXT` captures the area, reads it with OCR at the area's DPI, optionally keeps the part a
regex captures, and evaluates the condition against it: as text, as a number, or by its length in
characters, counted without the spaces OCR adds at either end.

### Reading text

Since 2026-11-08 text is read by PaddleOCR's PP-OCRv5 models (Apache-2.0) on the ONNX Runtime the
element detector already uses, in `Platform.Shared/Ocr/PaddleOcrService`: the same models and the
same code on Windows, macOS and Linux, where Windows OCR was Windows only, needed a language pack
per language, and asked for administrator permission to install one. Measured against it on pages
whose every word was known, it reads 98% of English words exactly to Windows' 69%, and 97% of Greek
to 89%. Windows read `104.120` as `104.12`, `14:58` as `1458`, and missed the town names on a
Firestone screen. The price is time and memory - below.

- **A line finder** (`TextLineFinder`, `ocr-line-finder.onnx`, the small PP-OCRv5 detector, 4.8 MB)
  answers with a heat map: how sure it is that each pixel is text, marking only the middle of a line
  so lines close together stay apart. Each patch above 0.3 becomes a rectangle, tilted when the text
  is, kept when its mean is 0.6 or more and grown back by area × 1.5 / perimeter; top to bottom,
  then left to right. The big detector took 6 to 8 seconds a screen on a CPU and found nothing on a
  4K page at 7 GB, so it is not offered.
- **A reader** (`TextLineReader`) cuts each line out level, scales it to 48 pixels tall, and guesses
  for every 8 pixels across it which character is there, or none. Repeats merge and the "none"s
  drop; the characters come from the model's `inference.yml` (`OcrCharacterList`), an entry a
  string, since the Chinese list has emoji. One line at a time: six at once was slower.
- **A word's rectangle** runs between the blank columns that part it from its neighbours, found
  from the slices its characters were seen in (`WordEdges`), its top and bottom at the ink: 1 pixel
  out typically and 2 for nine words in ten, as Windows was. A line's words are joined by spaces
  for `ReadAsync`, as Windows joined them.
- **The DPI.** A screen at 150% draws text half as large again, so the line finder looks at it
  shrunk by 1.5: a 4K page read 99.3% of its words in half the time and memory. The reader reads the
  pixels as captured.
- **Memory.** Loaded on first use and kept, about 40 MB for the line finder and Latin. The line
  finder runs without ONNX Runtime's arena, so a read's working memory - hundreds of MB for a whole
  screen, over a gigabyte for a tall page - goes back when it ends; a reader, on one line 48 pixels
  tall, keeps its arena and its speed.
- **Speed**, on an i9-9900K with a game running: a Read Text area 25 to 40 ms where Windows took
  2; a 1753 by 909 game screen about a second where Windows took 70 ms, a 1600 by 1000 page 1.5
  seconds, a 1920 by 1949 web page 3.4. A recording's screens are where it shows.

**Readers.** `Core/Catalogs/OcrReaderCatalog` lists them: Latin - English and the other languages
written in Latin letters - ships with the app and is the default; Greek, East Slavic, Cyrillic,
Chinese and Japanese, Korean, Arabic, Devanagari, Thai, Tamil and Telugu are downloaded from
Settings, about 8 MB each, by `Business/ScreenText/OcrReaderDownloadService` from the app's own
`ocr-v1` release into the `OcrReaders` folder beside the database. A model is checked against the
catalog's SHA-256 before it is kept; the release is filled by `publish-ocr-models.yml` from pinned
Hugging Face revisions against the same hashes, and `npm run models` fetches the line finder and
Latin. `scripts/download-models.mjs` checks every model file it fetches against the SHA-256 its
release lists, and fetches again a file already there that does not match. A step's `OcrLanguage` names its reader - empty is Latin, on every machine, and the step's form
shows Latin picked for it, the field required - and one not
on this machine is an error, `OCR_READER_NOT_DOWNLOADED`, naming the reader to download, so the
flow does not start. Validation asks `IOcrService.GetLanguages` only when the flow reads text. The Greek reader reads English too,
though an English word inside a Greek sentence comes out in Greek letters.

### The matrix

Planned for phase 10. One execution would be one screen size and one CSV row, which means **the
matrix loop sits above the walk and cannot be a step**. Three sizes and ten rows is thirty
executions.

It needs a table for the sizes, a script line for them, `{{width}}` and `{{height}}`, and the size
on each execution. `FlowViewport`, `Sizes:`, those variables and `Execution.ViewportWidth` and
`ViewportHeight` were dropped on 2026-11-07 while nothing used them, to come back under a better
name.

It would run **sequentially**. There is one mouse, one keyboard and one screen. That triples
wall-clock in CI for three sizes and nobody should be surprised by it later.

### Setup and teardown

Each execution is launch, then steps, then teardown. Self-contained and repeatable, which is what
would make a sequential matrix safe.

**Teardown is steps under `End Execution`.** That step no longer stops the walk: the walker drops
everything still pending — the stack holds a sibling for every level walked down to reach it — and
pushes the step's own children, so the cleanup written beneath it runs and the walk ends when that
cleanup does. Close the application, notify, call a webhook: all of them are ordinary steps, which
means they are visible in the tree, in the script and in the execution history, and copy-paste
replicates them across every `End Execution` in a flow.

The verdict is **latched** at the first `End Execution` reached. Cleanup below it is recorded like
anything else but cannot change what the flow already said happened, and a second `End Execution`
under the first is rejected by the validator as unreachable.

### A test that says nothing is not a test that passed

An execution ends one of five ways, and the distinction that matters is the third:

| | |
| --- | --- |
| `COMPLETED` | an `End Execution` said it passed, or an automation reached its end |
| `FAILED` | an `End Execution` said it failed |
| `INCONCLUSIVE` | a QA test reached the end and **nothing ever said** |
| `STOPPED` | somebody pressed stop, or closed the app while it was going |
| `ERRORED` | the harness broke, not the product |

`INCONCLUSIVE` exists because a QA test whose every check failed looks, from the outside, exactly
like one that ran to the end. Reporting that as green is the precise failure this whole model was
built to stop. MSTest and NUnit both carry the same outcome; JUnit writes it as `skipped`, which is
amber in every dashboard rather than green.

It adds no inference. The engine still does not count checks — it reports the structural fact that
no step declared a verdict.

It is a QA test's only, since 2026-10-25 (`Flow.IsQaTest`). An automation is written to do
something rather than to prove it, so one that reaches its end did what it was written to do and is
`COMPLETED`. It fails only where an `End Execution` says so, as a generated one's waits do when they
time out.

An **exception** is the one path with no cleanup, because there is no `End Execution` in scope and
so nothing was authored to run. It ends the whole execution, the viewport matrix included — there
is no next pass to protect, which is what the old flow-level close mode existed for. What that path
gets instead is a notification: see the roadmap.

### Variables

Two kinds of name translate through one helper, `VariableTranslator`:

| written | translates from |
|---|---|
| `{{username}}` | an input: its default, or a secret's saved value |
| `{{Read the total}}` | what a step it sits under read or printed |

**One mechanism**: every text field holds `{{name}}`, since a notify message like `"Login failed:
{{Login error}}"` mixes literal and variable and that mixing is the point. A Type step's form puts
an input in at the end of its text, or adds a new one, but the text stays free. `ExecutionEngine`
loads the value of every input of the flows the execution reaches - the flow started first, so its
value wins over a sub-flow's input of the same name - and `ExecutionCacheService` fills them in
before the steps' results. An empty value is none, and the step says which name has nothing. The
validator catches, at save rather than at execution, a variable that names nothing, a step that
keeps no value, a step this one does not sit under, and an input a step types with no value
(`INPUT_VALUE_MISSING`, on the input, so the Issues tab opens its row). Renaming an input does not
rewrite the steps; the validator says which still use the old name. Extracting a sub-flow copies the
inputs its steps type, a secret's saved value with them, as it copies their areas and points, and
refuses a `{{name}}` of a step that would end up on the other side of the split, as it refuses a
picked one: a sub-flow keeps no value for its caller.

### Coordinates

Everything persisted is in **physical pixels**, and every set of pixels carries the DPI it was
captured at - a template, a child area's offset and size, a point - because each is captured at its
own moment, possibly on a different monitor. The process is Per-Monitor-DPI-V2 aware.
`ScreenMetrics.EnablePerMonitorDpiAwareness` must run before anything else touches a coordinate API
— without it Windows virtualises every rect to 96 DPI and nothing lines up with the capture buffers
or the low-level input hook, both of which are always physical.

What makes a flow authored at 150% work at 100% is the area. Each says what its contents scale
with, and everything inside it inherits the answer:

| `ScalesWith` | for | ratio |
| --- | --- | --- |
| `DPI` | a browser, a native app, the OS | `dpiNow / authoredDpi` - the window's size does not matter |
| `AREA` | a game | `min(widthNow / authoredWidth, heightNow / authoredHeight)` |

One formula for both was the bug this replaced: a browser reflows rather than growing its
contents, so scaling its templates by the window's size shrank them until nothing matched. It
looked right in the demo because maximised 1080p at 100% to 4K at 200% makes both ratios 2.0.
`AREA` takes the **smaller** ratio because a game whose window changes shape adds bars rather than
stretching its art, and it never also applies DPI - the area is measured in device pixels, so a
higher DPI already shows in its width.

A browser tab and a monitor default to DPI. An application asks, because a native app and a game
look identical from outside. A region inside another inherits and can override - the game inside a
browser tab: the tab is `DPI`, the canvas inside it `AREA`. The DPI now is the monitor holding the
largest part of the area - the rule Windows uses for a window - and an empty monitor name means the
primary, which is the portable choice.

Anything placed in screen coordinates - a region with no parent, a point measured from nothing -
gets a `SCREEN_COORDINATES` warning, because no ratio fixes it.

---

## 9. Validation and the fix loop

### Static validation

`FlowValidationService` runs a set of rules in `Rules/` over a flow before it is executed - a flow
tells you what is broken while you are still writing it. The flow list shows each flow's error and
warning counts, and the editor names the step.

| errors - the flow cannot be right | warnings - the flow works, but |
| --- | --- |
| a required field missing - area, point, templates, text, condition, command, window size, loop count, Discord bot | a check whose branches are both empty |
| a step reading a result it does not sit under through Success | a step with no name |
| a `Notify` including the result of a step it does not sit under | anything positioned in screen coordinates |
| a `Break` out of a step it does not sit inside | keys held with nothing below letting them go |
| a sub-flow that does not exist | |
| an `End Execution` under another one, which reads as a decision and is not | |
| `NAME_DUPLICATE` - two steps, areas or points sharing a name | |
| a `{{variable}}` nothing in the flow defines | |
| text holding `<[` or `]>`, which the script could not write | |

`NAME_DUPLICATE` is an error rather than a warning: the script refers to things by name, so a
duplicate cannot round-trip, and it makes two steps share one history trend. Creating a step, an
area or a point picks a free name automatically, so only a manual rename can reach it.

**A QA test gets notes too**, a third severity, `INFO`, quieter than a warning and blocking nothing
(`Rules/FlowQaTestValidator`). `CHECK_CANNOT_FAIL` names a check with no `End Execution` failed
anywhere under its Failure branch, its own or under a check inside it - `FlowCheckListHelper`'s
`IsFatal`, which the assistant's check list reads too. Such a check changes nothing when it fails,
and a suite of them is green whatever the application does. A note rather than a warning, because a
search that only decides which way the flow goes is fine, and the draft already ends every Failure
branch with `End Execution` failed, so it fires only on one somebody removed or on a flow written by
hand. A check under an `End Execution` gets none: the verdict is decided by then. The tree badges it
grey with an info icon, the editor's header and Check count it, the flow list does not, and the
export dialog names the checks before a QA test leaves the app.

**An error stops a run, not a save.** A flow is validated once it is saved - after a form, and
after an import, whose result carries the issues - and a flow with errors stays saved and shows
them. One loader serves every caller: `FlowValidationService.ValidateAsync` takes a flow or a batch,
the flow list's badges come from the batch, and the editor, an import and Start from the same code,
so a badge and a refusal cannot disagree. `StartExecutionHandler` refuses to start it, or any flow that calls it as a sub-flow, before
the engine is asked - the engine runs whatever it is given - and the refusal names the flow, the
step and what is wrong. The Start button reads the same answer, so the two cannot disagree.

**Finding them.** Each issue carries the steps above it, and the list comes in the tree's order, the
flow's own first (`FlowValidationService.Locate`), so nothing has to load the whole tree to place
one. The editor's Issues tab lists a row per step, errors first, with its path. Picking a row opens
the step in the Flow tab's form beside the list, its messages above the form, so the list stays
while each is fixed and a fixed one leaves it; Show in tree opens the branches down to the step. A
name two parts share opens that part's tab. A closed tree row with something wrong under it carries
a dot. The header's count opens the tab, and so does Show the errors beside a Start it refuses.

The rules that relate two steps live once, in `TreeStepHelper`, and three places ask them: the
validator, the form's dropdown, which offers only what the validator accepts, and a drag and drop,
which clears a reference the move would break. A `Break` may leave only a step it sits inside, up to
the top of its flow - never one beside it, and never a Success or Failure row, since leaving a branch
is leaving its check.

`FlowCheckListHelper` and the `GetFlowChecks` AI tool expose the checks a flow contains, so a
question about "what does this flow verify" is answerable without walking the tree by hand. The
validator does not judge them: a check whose failure ends nothing may be a question the flow asks
itself, and a flow may be executed whether it verifies anything or not.

### Planned - phase 9: a recording is not a test until it has validated

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

---

## 10. AI

### Today

**A local model is the base.** Every AI feature runs on a model on this machine through **Ollama**,
behind `Microsoft.Extensions.AI`, and is switched off rather than failing until Ollama answers with
the model chosen in Settings - so is a recording's QA switch, since a QA test is generated by the
model (§6). `AiProviderService.ReadAsync` is that one definition, and it asks Ollama's `/api/tags`
every time (`OllamaService`), because a model chosen once can be deleted and Ollama can be stopped.
**OpenAI** (or anything speaking its API) is added on top, never instead: once it has a model and a
key it answers in the local model's place. A model can be pulled into Ollama from the settings page;
Ollama itself the tester installs for now, and the app fetching and starting it is in `TODO.md`, AI.

**Connecting to Ollama.** Every client that talks to Ollama, OllamaSharp's for the model's answers
included, gets its connection from `OllamaConnectionHelper`: IPv4 addresses first, and 200 ms for a
local one, which answers in well under a millisecond when anything listens. The default address is
`localhost`, which resolves to `::1` before `127.0.0.1`. Ollama listens on `127.0.0.1` only, and
Windows takes two seconds to give up on a refused connection. So each new connection used to take two
seconds: at start, every two minutes as the HttpClient factory renewed its handler (82 of 782 calls in
one log), and on every AI request, since OllamaSharp made a client of its own each time. With Ollama
not installed each call took four, and the Local model card held the request pipe for eight seconds
of every fifteen. macOS and Linux refuse a connection at once, so the wait was only ever Windows'.

**A model needs tools and vision.** Ask answers by calling tools, and what a model is shown of an
execution or a recording is screenshots. Thinking is not needed: every request turns it off
(`OllamaContextChatClient`). `AiModelService` says why a downloaded model falls short
(`AiModelDto.UnusableReason`, from what `/api/show` reports), and both model dropdowns grey it out
with that reason rather than leave it out, since a model that vanished once downloaded would look
like a bug; one chosen before stays shown, with the reason under it. The list to download from
(`AvailableAiModelsHelper`) is hand kept, since Ollama answers only for a model already pulled, and
a test holds every entry to both. Qwen3-VL's plain tags are its thinking models, so the list offers
its instruct one.

**Free memory.** Home's Local model card is always the local model, whatever cloud model answers.
`AiModelService.GetLocalModelStatusAsync` asks `/api/tags` and then `/api/ps` each time, stopping at the
first that goes unanswered, and the card asks every 15 seconds, because Ollama says nothing when it
starts, stops or lets a model go. Free memory
is `POST /api/generate` with the model, `keep_alive: 0` and no prompt. Ollama agrees at once and, if
the model is busy, unloads it when it goes idle, cutting nothing off, another app's request included.
So the reply confirms nothing, and `useLocalModel` shows the button loading and checks `/api/ps` every
second until the model has gone. That stops at 2 minutes, with "Still in use", since a job of several
model calls loads the model again after each one. Such a job gets slower and does not break. The
request can wait behind another call on the request pipe past Electron's 30 seconds and the backend
still acts on it, so the hook ignores how the request ends. `/api/ps` has no busy field, so there is no
"Using" state; counting the app's own calls in `AiClientFactory` would give one, for this app only.

**Ask** is a chat about your own flows and executions, in a panel docked beside the page
(`AiChatPanelComponent`). It pushes the page rather than covering it, lies over it in a window under
1200 px, and stays open from page to page. The model answers by calling tools rather than being
handed a dump: `DbQueryTools` - search flows and steps, read a flow with its areas, points and
templates, list executions and their steps, count outcomes and step types, list what a flow checks -
and `AiDocumentTools`, which searches the app's own documentation. The loop - ask, call a tool, feed
the result back, ask again - is `UseFunctionInvocation()` middleware with a cap on rounds, so there
is no orchestration framework.

An answer takes longer than a request can wait, so `Ai.ask` starts it and returns, and
`AiChatSessionService` says how it goes on `AI_CHAT_EVENT`: each tool in plain words
(`AiToolActivityHelper`), then the text in 40 ms batches, since a local model writes a hundred tokens
a second, then the whole answer. Every event carries the id the page chose for the answer, which is
what Stop names and what keeps a stopped answer's last words off the next one. Several conversations
can answer at once, and Ollama queues them. What a model writes on the way to a tool is narration,
qwen3.5:4b did it with the prompt telling it not to, so the answer is the text after the last tool
call. Ollama's stream ends without throwing when it is cancelled, so `FlowQuestionService` checks the
token once the stream ends, or a stopped answer would say it finished.

What "this" means follows the page. `useAiChatPage` reads it off the address, the composer shows it
as a chip that can be taken off, and `FlowQuestionService` adds a line to the prompt. An execution's
images go with the first question and after that on a click, since an image left in the history is
re-read on every tool round. A name in backticks that a tool returned as a step opens it in the
editor: `DbQueryTools` remembers the steps it returned, under the name they executed with, so the link
never depends on the model writing one. A name two steps share is left as text, since the link could
only guess. Conversations live in the page's store and go at a restart.

**Explain** is a question in that chat, asked from a past execution's page. The model reads the
execution's steps, the closest scores and the images when allowed, and says what went wrong and what
to change.

**Generate flow** shapes a reviewed recording's plain draft into the flow its description asks for,
as a script read back like any import, and Check's **Ask for changes** revises the saved flow the
same way with what the tester wrote (§6).

**The element detector.** OmniParser v2's `icon_detect_v3`, a YOLOv9-E whose weights are MIT,
Copyright Microsoft - the earlier `icon_detect` is AGPL-3.0, inherited from Ultralytics, and is not
used. Microsoft ships it as TorchScript; `publish-element-detector.yml` exports it to ONNX on
GitHub's runner (opset 17, with a height and width that can vary), checks the copy against the
original, and publishes `element-detector.onnx` beside `OmniParser.LICENSE.txt` as the
`element-detector-v1` release, which `npm run models` fetches. The resize, the decoding of its three grids and the suppression are
C# (`ElementDetector`), as OmniParser's `util/yolov9.py` does them in Python. It finds what a
recorded click hit (§6) and reads nothing out of the machine.

**The documentation index.** `backend/Core/AiDocuments/` is markdown written for the model - one
file per step type and concept, troubleshooting, every validation message. `AiDocumentIndexService`
splits it into chunks, embeds them locally with BAAI's bge-small-en-v1.5 (MIT) and
`Microsoft.ML.Tokenizers`, and searches them with USearch. The model is `text-embedder.onnx` and
`text-embedder-vocab.txt`, published by `publish-text-embedder.yml` as the `text-embedder-v1`
release beside `BGE.LICENSE.txt` and fetched by `npm run models`. It is built on the first question and saved, so an app that never asks
anything never loads the model. `OnnxEmbeddingService` runs on the machine, but `AiDocumentTools`
returns raw chunk text — so **embedding is not a privacy layer** and must not be described as one.

### The screen-data gate

`AI_SEND_SCREEN_CONTENT` is a boolean, **default off**. `IAiProviderService.CanSendScreenDataAsync`
is the single chokepoint — the local model always may, a cloud model only on the setting.
`FlowQuestionService` resolves it once per question and hands the answer to both the screenshots and
the tools, so the two cannot disagree. Typed text is redacted from every tool result unless it is
allowed, and `DbQueryTools` never selects the `AppSetting` API key or `DiscordBot.WebhookUrl` in any
projection: the webhook URL *is* the credential and is never logged.

AI-generated flows go into the editor and never execute on their own, because a prompt injection in
text read off the screen would otherwise be code execution.

### The local model reads the screen

The cloud is an addition, not an alternative, which the settings already are (above), and the two
split the jobs.

| | local | cloud |
|---|---|---|
| always present | yes | only when an API key is set |
| sees screenshots, OCR text, typed values | yes | only on opt-in |
| receives what was read off the screen, as notes | — | on opt-in, after a preview |
| job | see, extract, structure | reason, propose, summarise |

Every install gets a working assistant with no configuration, which matters most for the fix loop —
the feature the product turns on cannot be gated behind an API key a customer may never add. It also
means one component touches raw screen data and everything downstream gets derived text.

**Accepted limitation:** a good vision model wants a GPU. A tester's laptop may not have one, and
small CPU models read dense interfaces poorly. That cost is being taken for now rather than designed
around.

**What a screen says, without OmniParser.** Its detector over a whole screen costs seconds and
gigabytes and finds almost nothing on a game (measured 2026-10-30), so it stays on the 640 pixels
around a click. A screen is read by three things instead, cheapest first: OCR over every
press screenshot, each line with its rectangle, compared with the screenshot before - what appeared,
what went, and the window's title; UI Automation's element under each press - role, name, state, and
the dialog or page it sits in - where the application has an accessibility tree, a browser or an
ordinary desktop application and never a game; and the local model, for a click on something with
no words, from its cutout, and for one sentence where the page changed, from the screenshot. All
three built (§6).

**Notes in the script, not a schema.** All of it reaches the model as comments over the searches in
the script it already reads and answers with (§7), in the words read, so a step can be named for
exactly what its button says. The answer replaces them with comments a person would write, and a
check in code holds it to the recorded clicks, typed text and keys, in order.

**This localises leakage; it does not remove it.** The notes are text read off the screen, so one
can be `Welcome, alex@company.com`, and so are the names the draft gives a search from the words
under its click, and the words Review searched by. Those are the places in the script where screen
text appears, which is what makes review possible. How much of a screen changed is a number, and
goes anywhere.

**The payload is shown before it is sent**, which is the actual guarantee, built 2026-10-31 for the
recording's draft (§6): the request a cloud model would be sent waits on the Generating page with
the lines that hold screen text marked, and goes only on Send. Visibility rather than a promise: a
filter that claims to catch everything is worse than a preview that admits it cannot. Ask, Explain
and Check's "Ask for changes" are not shown first yet (`TODO.md`, AI).

---

## 11. Repository and CI

How a flow gets out of the database, into a repository, through a pipeline, and back to whoever has
to work out why it went red.

**Partly built** - phases 8 and 12 to 14. What exists: the script and its transactional import
and export (§7, the editor's Export and the Import page), `PublicId`, the main area, and the
flows kept in a local repository with its discard, restore, branches and remote (phases 14.1 to
14.3, below).

### Every flow is kept in a repository of the app's own

`%LOCALAPPDATA%\StepinFlow_v2\Repository\` (`PathHelper.GetRepositoryPath`) is a git repository the
app makes and owns: every flow as `<Name>.sflw` at the top, its template images in `<Name>/` beside
it - the layout Export writes. `GitRepositoryService` is the only thing that works in it - it
writes the flows, reads the changes and commits - and `IGitService` reaches git.

- **Made at startup.** `HostedGitRepositoryService` hands over to
  `GitRepositoryService.StartBackgroundService`, off the startup thread. `GitRepositoryInitializer`
  makes a repository on `main` when there is none, a `.gitattributes` (`*.sflw text
  eol=lf`, `*.png binary`), a `.gitignore` that keeps out everything but `.sflw`, `.csv` and `.png` -
  a flow's CSV of values is committed, since a secret is never written to it - both put back
  whenever they differ; then every flow is written. Only when it has just
  made the repository does it commit, "Snapshot of N flows", so the first edit shows as a change
  against it rather than inside it. It only reads the database.
- **Every save writes the flow's files.** Each public data-service write calls
  `GitRepositoryService.Changed(flowId)` once it has saved or committed; the service waits for half a
  second of quiet, then writes each changed flow and every flow that calls it - a `Sub Flow` line
  names the file of the flow it calls - and removes the files of flows that are gone or hidden. Not a
  `SaveChangesInterceptor`: several deletes are `ExecuteDelete`, which never reaches one.
- **A flow is written by its export.** `FlowScriptExporter.ExportAsync` writes a file only when its
  bytes differ, and removes a PNG only when the flow names it no more, at the folder's top level,
  so a README survives - the same for an export to a folder of the user's choosing. Before any flow
  is written, the files of a flow renamed or deleted go whole, compared by exact name: a rename
  that changes only case then gets files in the new casing, which Windows and macOS would not give
  it by writing over the old ones, and git and Linux would never see. Nothing is removed while the
  database has no flows at all. One flow that cannot be written costs its own files and is logged.
- **Committing is a button.** `GitRepositoryService.CommitAsync` starts the index from the last
  commit and stages exactly the files picked, so nothing staged before comes along. The author is
  the Settings fields, else the user's git config, else the system's user name with `user@computer`
  for an email, since git refuses a commit without one. The fields are hidden with the remote for
  now: who a commit is by only matters once its history is shared.
- **One thing at a time.** A write, a read and a commit each wait their turn on the service's own
  lock, so a commit never takes a file half written. Writing takes its turn a flow at a time, after
  reading the flow from the database: requests are answered one at a time on one pipe, so a read
  waiting behind the whole startup would hold up every other request.
- **Templates are PNGs.** A template's name ends in lowercase `.png` (`TEMPLATE_NOT_PNG` otherwise),
  and an import reads only the PNGs beside the script, by their bytes as well as their name, so the
  `.gitignore` above can let back `*.png` alone.
- **An execution records what it executed.** Git is the version history; what git cannot answer is
  "what exactly did execution 37 execute". As an execution starts, `ExecutionHistoryService` asks
  `GitRepositoryService.GetFlowVersionAsync`, which writes the flow and every sub-flow it calls
  first - so a save a moment before Execute counts - then answers with the last commit and whether
  any of their files changed since. `Execution` keeps both, and keeps the flow's script as well
  only when the commit cannot say: changes not committed, or no commit. A repository that cannot be
  read costs the record, never the execution. Three nullable columns, so older executions simply
  have none.
- **What the app shows.** The editor header has the branch and its menu, the flow's changes and Commit; the
  commit dialog lists every changed file with this flow's ticked and a message filled in from
  what changed; the Script tab is Script, Changes and History - Script marks each line changed
  since the last commit in its margin (`change-gutter.ts`, a line diff), the other two are
  CodeMirror's merge view with the unchanged lines folded; the Flows page counts each flow's changes; Settings
  has the folder and who commits; a run's header and the runs list show the commit it executed at,
  marked when it executed changes not committed yet. `REPOSITORY_CHANGED` is broadcast after each
  write and commit, and the app reads it all again.

The folder is the app's: a `.sflw` edited in another program is replaced the next time that flow is
saved (`TODO.md`, Git).

**Discard and Restore this version** read a flow back. `GitRepositoryService.ReadFlowAtAsync` finds
the flow's script in a commit by the `Id:` in its header - the file of its name now first, then
every other, so a flow renamed since is found under its old name - and its images in the folder of
that name. `FlowRestoreService` imports them over the flow the way a file is imported, so its row
and executions stay, then writes the flow's files at once. Discard reads the last commit and leaves
nothing to commit; Restore reads an older one and lands as a change. It is a class of its own because
the import saves through the data services, which tell `GitRepositoryService` what they changed -
the service cannot also depend on them - and it still reads the commit through that service.

**Branches.** The branch in the editor header opens a menu of the local branches and a new one. A
new branch starts at the last commit and is checked out at once, so nothing differs and changes not
committed yet come along. A switch is refused while anything is not committed or a flow is
executing. `GitRepositoryService.SwitchBranchAsync` writes every flow first, so a save a moment ago
counts as a change, then compares the two branches' last commits: for each flow whose files differ
it reads the script and images as the other branch has them, and it takes the `Id` of each flow the
other branch does not have from the `Id:` of the scripts that differ, so a flow renamed between
them is one flow. It checks the branch out and hands that to `BranchSwitchService`, holding the
folder until the database has caught up, so the background writer cannot write a flow into it
halfway; when anything fails, the old branch is checked out again. `BranchSwitchService` - a class
of its own for the reason `FlowRestoreService` is - reads every script first, so one that does not
read refuses the switch before anything is saved. Then, in one transaction, it hides the flows the
branch does not have, names the ones it imports by their `Id` for the moment, since a branch may
have swapped two names, and imports them sub-flows first, so a `Sub Flow` line finds the flow it
calls.

A hidden flow is `Flow.IsHidden` (`FlowHidden`, which leaves every existing flow visible): its row
and its executions stay. Nothing lists it - flows, sub-flows, lookups, health, the runs summary, the
AI's search - its name is free for another flow, a `Sub Flow` line does not link to it, and the
writer leaves it out of the folder. A branch that has it imports it back as the same row, since
identity is the `PublicId`: the same flow on two branches is one row, and its executions
accumulate. `REPOSITORY_BRANCH_SWITCH` carries the progress to a dialog that holds the app until the
switch is done, and a flow open in the editor that the branch hides is left for its list.

**The remote.** Settings, Repository has its address, HTTPS only, and a token - a secret setting,
`GIT_REMOTE_TOKEN`, never sent back to the page. The address lives where git keeps it, as `origin`
in the repository's config; an SSH address, a password in the address, or one that does not answer
is refused, and the address it replaced is kept. The token is the password, under any user name or
the one the address names. **Connect** fetches: an empty remote is sent every local branch, and one
with history comes back with what pulling the branch checked out would do, which the person agrees
to first. **Push** sends the branch checked out to the remote's branch of the same name, and is
refused while the remote has commits it does not. **Pull** fetches and shows the commits that arrive
and the ones never pushed that would go; agreed, `GitRepositoryService.PullAsync` keeps those on
`backup/<date time>`, moves the branch to the remote's commit - refused when the remote has moved
since the dialog - and the database follows as for a switch, the same `BranchSwitchService` and the
same dialog, with the branch and its backup put back when an import fails. A branch only the remote
has is in the branch menu, and switching to it makes it here. A fetch and a push go outside the
service's lock, since the network can take seconds and they touch only what the remote has.

### The file is `.sflw`

Git decides binary or text by looking for NUL bytes, not by extension, so a UTF-8 script diffs
correctly with no configuration. What does want configuring is line endings: `*.sflw text eol=lf` in
`.gitattributes`, because flows are authored on Windows and CI runs on Linux, and the round-trip
test compares bytes.

### The file says which version of the language it is in

`Script Language: v1` is the first line the printer writes (`Catalogs/ScriptLanguage`). It is one
counter, raised only when the grammar changes and never by an app release, so updating StepinFlow
does not rewrite the first line of every flow in the repository; the app's own version lives in the
git tag (§2, Building and releasing). The reader upgrades anything older and the writer writes only
the newest. A file in a newer version stops at that line with `SCRIPT_LANGUAGE_NEWER`, which says
the version it needs and the one this app reads up to, rather than reading on into a page of errors
about words it has never heard of. Reading forwards only is the usual rule for documents, and the
right one here: the parser refuses a word it does not know, and a script that drives the mouse is
better refused than half understood. A file without the line is read as v1; from v2 on, a converter
checks the version rather than the shape of a line. Forms from before the first release are not
read: no saved flow used them when they went, on 2026-11-07.

### Every step a line can point at carries its name

A line is named when anything can refer to its step, or when nothing about its settings would name
it: searches, checks, loops, stage markers, window steps, `Launch` and `Shell` (a check or typed text
reads what a command printed, `{{Read it}}`), and `Sub Flow`. A step named by its settings - a click,
a wait, an `End Execution`, a `Notify` - writes none, and is named on import. `Launch`, `Shell`, `Sub
Flow` and the window steps always write theirs, an empty one as `<[  ]>`, and a line without it is
refused. A recording names
the ones it adds (`SubstituteDraftBuilder`), and the `NameUnnamedSteps` migration named the ones
saved without one. Before it, a check reading a command's output exported as `{{System Command}}` with nothing in the
file called that, and the file did not import back. `Notify` writes the step whose result it
includes, `with result of <[ … ]>`; it did not before, and an export lost the pick. Since
2026-11-09 it names its Discord bot too, `through <[ Alerts ]>`, one the header lists under
`Discord Bots:` by name only, as the templates are listed: a bot is a webhook kept on the machine,
so the file carries its name and the import finds it, the same name exactly first, else in any case,
a sub-flow's way. A `through` the header does not list is `DISCORD_BOT_UNDECLARED`, an error. A
name no bot has is `DISCORD_BOT_UNKNOWN`, a warning at its header line, and the import adds a bot by
that name with no webhook, saved with the steps (`DiscordBotDataService.LinkAsync`; its `NameAsync`
is the way back, for an export and for a recording's draft), so they
keep the name through every export and the user only pastes the webhook in under Settings, where
the bot says "No webhook yet"; until then a `Notify` through it sends nothing and says so, and the
flow's validation warns at each one, `DISCORD_BOT_NO_WEBHOOK` - which is what a branch switch, whose
import warnings go nowhere, leaves to say it. A preview adds nothing. The model's revisions may send only through a bot the script it was given lists
(`DISCORD_BOT_NOT_GIVEN`). Before, every export, every branch switch and every recording's draft
lost the bot.

A window step can be pointed at, by a `Break` out of it or a `Notify` reporting it, so its name is
the first thing on its line, read by its own parser as Image Search's is. The file cannot leave a
reader to work it out: the number follows the order the steps were made in, so `Focus window 2` can
sit above `Focus window`, and counting down the file would land the `Break` on the wrong window.
Nothing can point at an `End Execution`: `TreeStepHelper.BreakTargets` leaves it out, because a `Break`
out of one would only skip the rest of its cleanup, the walker having already dropped everything
after it. The other steps named by their settings hold no steps. So none of them needs a name in the
file, and a line that gives one is refused.

### A flow's name is a file name on all three systems

The name becomes the `.sflw` file and its template folder, and a repository is cloned onto Windows,
macOS and Linux. `Core/Helpers/FileNameHelper` keeps one fixed list, Windows', the strictest, rather
than `Path.GetInvalidFileNameChars()`, which is the running machine's and on Linux only `/` and the
null character: none of `< > : " / \ | ? *` or the control characters, no space or dot at either
end, and none of the device names (`CON`, `NUL`, `COM1`...) with or without an extension. A flow's
name is validated against it - by the create, update and extract handlers, by `flow.zod.ts` with the
same rules, and on the script's `Flow:` line as `FLOW_NAME_INVALID`. A step's name stays free text,
so its failure screenshot's file name is cleaned with the same list instead: each refused character
becomes a space and the ends lose dots and spaces. A valid name comes out of that unchanged, so a flow's name
is exactly its file name.

That makes two flows with one name two flows with one file, and on Windows and macOS `Login` and
`login` are one file too. `FlowDataService` refuses a name another flow has, whatever its case, on
every write that names a flow - create, update, extract, and the import as `FLOW_NAME_TAKEN`. It
compares in C# rather than in SQL, because SQLite folds only A to Z.

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
and they are small. Nobody names a template, so each is given its file name when it is created:
`FileNameHelper.GenerateTemplateFileName` draws `template-k3x9q.png`, five random characters from
`0-9a-z`, and `FlowTemplateDataService` draws again while the flow already has it, ignoring case as
Windows and macOS do. It keeps that name for good: the database, the script and the folder all hold
the one string, an import takes it from the file and an export writes it back unchanged. Not the
database id, which every import renumbers, and **not** a content hash. A hash would dedupe
identical images, but it also changes whenever one is edited, so git would record a delete and an
add instead of a modification, and the point of a folder of loose images is that a reviewer can
see which one changed. Two git branches drawing the same name for one flow is a merge conflict,
never a silent duplicate.

Screenshots are per-execution and large, and pile up with every execution until it is deleted. They
travel as build artifacts.

### Results travel as an execution bundle

The CLI runner writes a folder: the JUnit XML, the execution steps as JSON, the flow script that ran,
and the failure screenshots. CI uploads it as a build artifact, which every CI system already does.
The app imports it.

JUnit XML is the interchange format because every CI system already renders it. `<failure>` means
the product is broken; `<error>` means the harness is.

Later the app can fetch that same bundle from the CI provider's API instead of the user downloading
it. A central StepinFlow server that receives results directly is a product decision about becoming a
service, not a technical one, and it should not get decided by accident.

### The window a flow works in is its main area

You cannot resize "a flow", only a window, so the viewport matrix needs a target — and guessing it
from whichever window has focus is exactly the implicit behaviour that breaks on another machine.

`FlowArea` already binds to a window by process name and title, so this needs no new concept: one
root area marked `IsMain`, written `main` in the script. Viewport sizing targets that window, and
a `WINDOW_*` step names it, or any other window area, rather than describing a window itself. The recorder stores what it keeps relative to the same
area, which is what makes a recording portable; a main area in screen coordinates makes a flow that
works on one machine only.

### Secrets live in the local database, never in the repository

Resolution order, most specific first:

```
CI argument  >  environment variable  >  local secrets file  >  stored value
```

`FlowInput.IsSecret` means the value is never stored in any file. A password in a repository is
a leak.

**What is built.** A secret's value is typed once on the Inputs tab and kept in
`FlowInput.SecretValue`. Nothing sends it back: the form only ever sends a new one
(`NewSecretValue`, empty keeps the saved one) and is told only `HasSecretValue`; the exporter's rows,
so the script and what a model is sent, leave it out; the validator loads whether there is one,
never what. Only `ExecutionEngine` reads it. An import writes a flow's inputs anew from a script, so
it keeps the saved value of a secret still called the same - a branch switch, a model's change and
an import of the file again all go through it. A secret a step types with no value saved is an
error on that input, and the flow does not start.

**Not encrypted yet**: the value is plain text in the SQLite file, which only this account can read.
The rest of this section is the plan for phase 8.

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

**Discord notifications** are a `Notify` step, so they go wherever the flow puts one - usually under
a failure branch or an `End Execution`. It can include the result of a step it sits under, either
side, above its own message (`with result of <[ … ]>` on its script line): what an image search found and its accuracy against the one it
needed, or the closest it came; the text a search read; what a command printed. That is read from
what the execution recorded, not from the step's settings. It posts through one queue rate-limited
per bot, so a flow in a retry loop cannot flood a channel. The webhook URL is the credential: it is never logged and never shown
to a model.

**Execution history** records every step's result, duration, location, closest score and message,
in batches: 200 steps or a second, whichever comes first, a failed step with the rest. Writing each
failure straight away was a transaction for four steps in five of a polling loop like Firestone's,
where a search that finds nothing is the usual case. A pause writes what is waiting, so the page
shows every step the execution stopped after.

**Screenshots**: an execution keeps its last ones, 50 unless Settings says otherwise, 100 at most.
They stay in memory - about 200 KB each - older ones letting go of their picture as newer ones
arrive, and are written to disk as the execution ends, each named after the step that took it; the
rows it kept them for are then named after their files. While it goes, the screenshot call answers
from memory (`IExecutionHistoryService.GetKeptScreenshot`). Until 2026-11-12 it kept its first 20:
a 17-hour Firestone execution kept steps 3 to 25, its first three seconds, and nothing of the 495,000
steps after. Writing every screenshot and deleting the oldest would have kept the right ones at a
megabyte a second of disk writes, so they wait in memory instead. An execution the app closes on
before it ends writes none; before, it wrote them and never recorded the folder, which came to the
same.

**Deleting** is by hand: an execution, every execution of a flow but one still going (both from the
Executions page), or a flow with all of its executions (`ExecutionDeleteService`). The steps go in
one statement before the rows they hang off, the screenshots folders after, and then the database
hands the space back to the disk. It is in incremental auto-vacuum since 2026-11-12, switched on at
start, a VACUUM done once (16 s on a 1 GB database); after that a delete moves only what sits past
its holes. On a copy of a 1 GB database, deleting a 17-hour execution took 2.2 s and its space back
0.1 s; a full VACUUM after it would have taken 13 s, and grows with everything kept. Deleting a flow
executed for three weeks, four million steps, took 16 s and 8.5 s, and half as long again when the
cascades did it, which is why calls that delete wait ten minutes before giving up, not thirty
seconds. Nothing is deleted on its own, and there is no limit on the steps an execution keeps.

**Planned - phase 15.** Every check that can fail an execution is a thing worth counting. A flow
that ran a hundred times — fifty at one viewport, fifty at another — with four failures is a
sentence the product should be able to say. So is which checks those four fell into, and which
checks have never failed at all. That is the difference between "the login test is flaky" and "the
login test fails at 390x844 four times in fifty, always on the cart badge check".

---

## 13. Frontend

React 19 and TypeScript 6 on Vite 8, with the React Compiler through Babel; PrimeReact, on
its lara-dark-indigo theme, and PrimeFlex for components; React Router 7; TanStack Query for server state and Zustand for UI state;
React Hook Form with Zod 4 for forms; `react-markdown` for the assistant's answers; CodeMirror 6
for the script view. ESLint 10 with
`typescript-eslint` and the React hooks rules. Electron 45 is the shell, packaged by
electron-builder, with `electron-updater` and `electron-log`.

Feature-based: `features/<name>/{components,hooks,store}`, shared code in `shared/`, Electron-window
pages in `windows/`. The recorder is `features/recording/`: the setup, Review, Generating and
Check, with the recording bar's own window in `windows/recording-bar/`.

**The shell** is the mockup's (`app-layout.tsx`): a rail on the left with Home, Flows, Sub-flows
and Executions, then Import and Ask AI, Settings, and the model the AI features use with where it
runs, a link to its settings. On the editor and an execution it keeps only its icons, since those pages want the
width. Settings shows one card at a time, picked from a list of sections beside it, each at its own
address such as `/settings/ai`, so the model's link opens the AI card. Every page but those two sits in
`PageComponent`: its content grows with the window up to 1200 px for a form - Recording, Settings,
Import - or 1600 px for a list or a dashboard - Home, Flows, Sub-flows, Executions - then stays that
wide in the middle, so a 4K screen does not leave it against the rail. The window has no menu bar on
Windows and Linux: `main.ts` clears Electron's default, and its Ctrl+R reload, Ctrl+Shift+I developer
tools, Ctrl+= zoom and F11 full screen go with it. macOS keeps it, since Cmd+C and Cmd+V reach a text box only through
its Edit menu.

**Home** (`features/home/`) leads with what to do next, then what is in the way of it. Each card reads
what it shows for itself, so none waits for another. Which cards there are depends on whether any flow
exists, so Home shows placeholders until the executions list's summaries say. It used to guess there
were flows: a new install showed Needs attention, then took it away once the summaries came back
empty, and the cards it mounted on the guess put their requests ahead of the summaries on the request
pipe, which answers one at a time.

- The hero is the flow last executed, from the executions list's summaries, which leave out a flow
  the branch hid. Execute opens `/execution/:flowId` and stops there, since starting is that page's
  button. While an execution is going it says View execution. With no flows at all it is Build a
  flow (`/flows/new`), Record a flow (Beta) and Import a flow instead, and Needs attention is left out.
- Needs attention lists the flows validation has something to say about, from `Flow.getHealth`, which
  carries each flow's name for it. The health query used to stay stale for up to five minutes after a
  step was saved, because a step's mutations invalidate `flowStep` and `flowValidation` and never
  `flow`. Now it asks on every page opened, and `useRepositoryEvents` asks again on
  `REPOSITORY_CHANGED`, which the backend sends after writing a flow's files however it was changed.
  That covers the Flows list's badges too.
- Repository reuses `BranchMenuComponent` and `CommitDialogComponent`: an uncommitted change refuses
  a switch, so the way out is on the same card.
- Local model is §"Free memory" under the AI section.
- Updates sits under Needs attention, which takes whatever height the other column leaves, so the
  columns end together; with no flows it goes under Local model. Its last check is `lastCheckedAt`,
  set only by a check that got an answer, so an offline day shows the morning's time. It lives in the
  main process's memory, since a check runs as the app starts. §"Building and releasing" has the rest.

**The script view** is CodeMirror 6, read-only (`shared/components/script-view/`). A
`StreamLanguage` tokenizer colours a line the way the scanner reads one - the step's keyword where
it starts, its options, its `<[ quoted ]>` text, `{{inputs}}`, numbers - from the words of
`ScriptKeywordCatalog`, and colours only: `Scanner.Read` stays the one judge, and what it says
arrives as `@codemirror/lint` marks at their line and column. A line folds what is indented under
it, and a `##` section everything to the next. Given an original, `@codemirror/merge` shows what
changed from it in place; `ScriptMergeComponent` puts two scripts side by side. Its colours are the
theme's variables, so it follows the app. Not Monaco, settled 2026-10-14: it loads from a CDN unless
told not to, wants web workers set up for Vite, and takes its colours as fixed values in script.

Every entity form is a pair — `XFormComponent` (React Hook Form setup, header, footer, submit) and
`XFormFieldsComponent` (fields only, reads `useFormContext`) — with a sibling `x.zod.ts`.

> **Any field missing from the Zod schema is dropped on submit.** Forms submit
> `{ ...defaultValues, ...data }`, so an unvalidated field silently keeps its default. This has
> already cost one debugging session: a form typechecked, looked correct, and quietly discarded two
> new columns because the schema had not been updated.

**Adding a step.** The + in the tree opens `FlowStepPickerComponent`: every type in `FLOW_STEP_CATALOG`
but the two branches, in five sections by the tree's groups - Look at the screen, Mouse and keyboard,
Flow control, Windows, System - each entry wearing its tree row's left edge, tint and icon square. The
catalog is the one list of types: its order is the picker's, and each entry carries the sentence under
its label and the other words the search finds it by. The search has the focus as it opens. Every word
typed has to be in an entry's label, sentence or those words; Enter adds the marked entry - a label
that starts with what was typed, else the first shown - and the arrows move the mark. Each cursor and
window type is an entry of its own and opens the form they share in that mode. A type created with
Success and Failure says "branches". Recent is the last three types picked, kept in `localStorage`.

**A step's form** puts its fields inside `FlowStepFormLayoutComponent`, the mockup's "Step forms"
boards. It reads the values on the form and draws the rest, so no form repeats it:

- **The header:** the step's type, its name, and its mode, coloured by the type's group as in the tree.
  A step named by its settings shows the name it will be saved with.
- **A sentence:** what the step will do. A form can pass its own.
- **"Gives":** what later steps can use from it.
- **"Comment":** the step's `CodeComment`, which the script prints above the step as `#` lines.
- **The script line:** the line the step will be in its flow's script.
- **Test now:** where the step has a test, beside Cancel and Save.

The sentence and "Gives" come from `flow-step-description.ts`. The script line comes from
`FlowStep.getScriptLine`, once typing pauses. `FlowStepScriptLineService` reads by id the areas,
points, steps and sub-flow the step names, and prints with the printer an export uses, so the form
and the file cannot disagree. It also returns those names, which is how the sentence can say
"onto where “Find the email field” matched". Success and Failure have a form too: the comment and a
sentence naming the check they belong to. Every step schema includes `CODE_COMMENT_SCHEMA`, for the
reason the warning above gives.

**Steps named by their settings.** A cursor, wait, keyboard, system action, break, end or notify step
has no Name field, and no name on its script line. `FlowStepNameHelper` names it from its own settings
- "Double right click", "Wait 2s", "Type {{email}}" - whenever it is saved, pasted, recorded or
imported, and `FlowNameHelper.MakeUnique` numbers it like any name: "Click 2", or "Wait 2s (2)" when
the name already ends in a number. Only the step's own settings, never the name of the point or step
it goes to: those can be renamed, and keeping a copy of their name current would mean renaming steps
nobody edited. The tree shows where a Move goes beside its name instead. The form's header gets the
name back with the script line, so it shows what the step will be called before it is saved.

**A window step's name follows its settings until someone types their own.** It can be pointed at, so
it has a required Name field, first on its line. `windowStepName` in `window-modes.ts` fills it in
with what `FlowStepNameHelper.NameOf` says - "Focus window", "Resize window to 1280x720" - and
replaces it when the settings change, as long as it is still that name, empty, or that name with the
number a clash gave it. A recording's resize comes with no name and takes the same one.

**A duplicate name is refused on Save.** `FlowStepDataService.UpdateAsync` refuses a name someone typed
that another step, area, point or input has, on every step that has a Name field, and the form shows
why and stays open. A window step's filled-in name is numbered instead, since nobody chose it. Before,
the save went through and the validator flagged it, but the exported file did not read back, because
`ScriptScope` refuses a name declared twice. Creating a step still numbers a clash rather than
refusing it, since a new step's default name, "Image Search", cannot be told from a typed one.

**A paste lets go of what it cannot reach.** A reference between the copied steps follows the copy. One
to a step outside it is kept when the new place can reach it and cleared when not, the same answer a
drag gets from `TreeStepMoveHelper`, and the validator says the step needs one. It used to be kept,
and a Break pasted above the step it names wrote a line that did not read back.

**Testing a location.** Every Test of a location works it out with `AreaPointResolver`, the way an
execution does: `FlowPoint.resolve` for the values on the location form, measured from the saved
area, and `FlowPoint.getPreview` for a saved location beside a Move or a Drag. A Move or a Drag
between saved locations has Test now: `FlowStep.testCursor` works out both ends with the resolver,
then moves or drags the real cursor in `TestCursorStepHandler`. A step result exists only while the
flow executes, so a step that reads one is not tested. "Measured in" follows the area a location is
measured from: Percent, fixed, in an area that scales with its size, where pixels are not scaled;
Pixels, changeable, in one that scales with DPI.

TanStack Query for server state, keyed `["flow", …]`, `["flowStep", …]`, `["lookup", …]`; mutations
invalidate. Zustand for UI state. Dialogs go through `useDialogStore` rendering into
`DialogRootComponent`.

### The flows list

Flows and Sub-flows are the mockup's first board, one page with the sub-flow flag flipped. A bar
over it has the page's name, the branch and its menu, and **Search flows and steps**: every word
typed has to be in a flow's name or description, or in a step's name, on the branch checked out
(`Flow.search`). Ctrl K puts the cursor in it. A flow found opens in the editor; a step found opens
its flow with the tree opened down to it, the way a "Used by" row does. Below the page's title, chips
filter the list - All, Automation, QA tests, With issues, Not committed; a sub-flow page has no kinds
to filter - and the table has a row a flow: its name, its changes not committed and the first line
of its description, its kind (or for a sub-flow, how many flows call it), its health, its last
execution and when, or when it was last edited when it has none, its steps, and Execute, with Clone,
Settings and Delete in a menu. The list comes whole from `Flow.getLazy`, so the chips filter it here,
with the health and the repository's status the page already has.

### The editor

The workflow page is the mockup's editor boards (8 to 8e). Its header has the way back to Flows or
Sub-flows, the flow's name and kind, whether it would execute - the validator's errors and
warnings - and Export and Execute. Below, the tabs take the whole width. Under them the left side
lists the tab's parts and the right side edits the one picked; a splitter between them remembers its
place.

- **Flow:** the tree, and the step or flow form for the row picked. The flow's form keeps its
  name, description, sizes and callers. **−** and **+** above the tree draw it from 70% to
  130%, and the percentage puts it back. It is CSS `zoom` on the tree rather than a font size,
  because the rows' padding, icons and thumbnails are in rem and would stay put. Like the
  splitter's place, the size is the window's `localStorage` (`workflow-tree-zoom`), kept per
  user on this machine, not in the database.
- **Areas, Locations, Inputs:** a list, regions under their window, each with how many steps use
  it, and the picked one's form. Each is created, saved and deleted on its own
  (`FlowArea.*`, `FlowPoint.*`, `FlowInput.*`), so `Flow.update` no longer touches them. An
  application area's **Application** lists one row an app, its open windows' titles under it,
  because the area matches on the process and a title would promise a window it never picks; the
  field shows the saved app whether it is open or not.
- **Templates:** one tile per picture (`FlowTemplate.getByFlow`), and the picked one's image,
  click point and steps. **Edit image** and **Set click point** save with `FlowTemplate.update`,
  for every step that links to it.
- **Script:** the whole width, the script Export writes (`FlowScript.render`), with Copy.

Every "Used by" comes from `Flow.getStepUses`: for each step, its area, its locations, the
`{{names}}` in its text and its templates. A step in that list opens in the Flow tab, its branches
loaded and opened down to it. The Flow tab is hidden rather than unmounted while another is open,
so the tree keeps its open branches and a half-edited step its edits.

Renaming an input does not rename it in the steps that type it; the validator says which ones still
use the old name. A secret keeps no default, whichever form saves it; its value field is shown as
dots, empty with "Saved" or "Not set yet", and ticking Secret moves the default into it.

### The tree

`DataTreeComponent` renders a PrimeReact `Tree`, lazily loading children on expand.

**Expand all and Collapse all** sit beside the size control. Expand all takes the whole flow in one
query, `FlowStep.getTreeNodesRecursive` as the execution page does, rather than a level at a time:
Firestone has 105 steps with something under them. Collapse all closes every step and leaves the
flow open on its first steps.

**Node keys are namespaced.** Flow ids and FlowStep ids are separate sequences, so a raw id would make
Flow 5 and FlowStep 5 the same node as far as selection and expansion are concerned. Keys are
`flow-{id}` / `step-{id}`, built by `TreeNodeDto.BuildKey` in C# and `buildTreeNodeKey` in TypeScript
— **these two must stay in sync.** `TreeNodeDto.entityId` carries the real id; nothing parses the key.

For the same reason `FlowStep.getTreeNodes` takes `{ id, isFlow }` rather than a bare id.

**A search row shows its template.** The first template's thumbnail takes the icon's place, the
step's icon sits on its corner, and a step with more templates stacks them behind it with "+2". The
row reads the thumbnail from `FlowStepTreeNodeProjection`, never the picture itself.

**The picked row wears a ring** from its step's colour into the accent, with a soft glow.
`TreeSelectionRingComponent` draws it as a masked overlay, so only the ring is painted and the row
shows through. It turns once as the row is picked: a Web Animation on `--tree-ring-angle`, a
property registered from script because the app keeps its styles inline. It stays still when the
system asks for less motion.

**Copy and paste.** A selected step shows Copy, which takes it and everything under it. A bar
over the tree then names what was copied, with × to forget it, and every + has Paste here beside it.
There is one copy at a time. It stays after a paste, and goes when another flow is opened. A Success
or Failure row is not copied on its own; it belongs to its step. Buttons only: no Ctrl+C and Ctrl+V
in the tree.

`FlowStep.paste` reads the copied rows untracked and inserts them with `CreateTreeAsync`'s code,
the recorder's. So a copy gets free names ("Find button 2"), links to the same template images, and
both branch rows, placed at the end of the branch the + is in. References between the copied
steps follow the copy. A reference to a step outside it is kept, and validation says when the new place
cannot reach it.

A copy pastes only into the flow it came from, since its areas, points and inputs are that flow's.
Pasting into another flow would have to bring those along, or ask what to point them at
(`TODO.md`).

**Disable and Enable.** A selected step shows Disable beside Copy, or Enable once it is disabled,
except a Success, a Failure or a stage marker. It saves at once through `FlowStep.setDisabled`
rather than the form's Save, so it works with the form in VIEW and leaves what is being typed in
EDIT alone. `FlowStepDataService.UpdateAsync` keeps the saved value for the same reason: a form open
since before the click holds the old answer. The row is dimmed with a Disabled chip, and
`keysUnderDisabled` dims the rows under it, in the editor, the execution page and the read-only
trees. Dimmed rather than greyed, because a Wait or a Loop is blue-grey already. A disabled step's
form says so in one line with Enable, and the script view greys its line as it does a comment.

### The executions page

`/executions` is every execution on this machine, newest first, in one feed, rather than a flow to
pick before anything shows.

- **Executing now** sits above it: the flow, how long, and the steps so far, from the engine's state
  read every second while it lasts (`ExecutionStateDto.StepCount`). View opens its execution page.
- **The flows**, by when each last executed, with its last ten outcomes and how often it completed.
  All flows comes first and is picked to begin with; a flow picked narrows the tiles and the feed,
  and is in the address (`?flow=`), so coming back from an execution finds it still picked. A
  sub-flow executes inside the flow that calls it, so it is listed once it has executions of its own.
- **Four tiles** over the last 7 days (`Execution.getStats`): executions, completed, failed, stopped.
  For one flow, how long it usually takes - the median of the completed ones, which one left waiting
  all night does not drag - and when it last failed, instead of the last two.
- **The feed** (`Execution.getFeed`), by day, 50 at a time below the last id shown. All, Failed,
  Completed and Stopped, and Inconclusive when there are any, each counted over every day. A row has
  the outcome, the number, the flow, when, how long, the steps, the version and what it kept - the
  camera opens the screenshots' folder - and under a failure the message that ended it, with Explain
  when the AI is on.
- **Stopped is not a verdict.** A stopped or abandoned execution is grey, and "completed" counts only
  the executions that ended with one - completed, failed or errored - so a loop always stopped by hand
  has no verdict rather than 0%.

The step count is counted as the walk goes (`ExecutionEngine.StepCount`), so a stopped or errored
execution keeps it. It used to be written only when the walk reached its end, so every stopped one
said 0; `CountStoppedExecutionSteps` counted those from the steps their history kept, and an
execution abandoned at start-up is counted the same way. The two endpoints mark their times as UTC:
SQLite hands a time back with no zone, which went out with none and was read as local time.

### The execution page

The mockup's "Execution · two hours in, newest on top" board, drawn on a Firestone execution of
32,699 steps. Of those, 28,432 were searches that found nothing, so the page is built to find the
few passes that did:

- **The header:** the flow, the execution's state and number, what it keeps and the commit it
  executed at, with the debugger beside it - or, for a past execution, Explain and Ask.
- **The debugger:** Continue, Pause, Step into, Step over and Stop, each with its key from
  Settings. The keys work whatever window is in front: from an execution's start to its end, and
  only then, the hook raises every key a person presses (`IInputRecordService.PersonKeyChanged`,
  turned on by `StartListeningForKeys`), and `DebuggerHotkeyService` acts on the engine in the
  states the buttons are live in, so the page follows from the engine's events like any other
  change - nothing in the page listens for keys. A flow's own presses never arrive: SharpHook marks
  what SendInput injected as simulated, so a flow that presses F9 cannot pause itself. Start reads
  the keys from Settings for each execution. They still reach the window in front, as the Settings
  page says, and on macOS the F-keys are media keys unless Fn is held.
- **Tiles:** elapsed, steps and their rate, the loop's pass, the passes that found something, and
  the failures handled.
- **Activity:** steps a minute over the whole execution - a second, or ten minutes, whatever keeps
  it to 120 bars - marked where something was found and where it ended.
- **The tree:** how often each step executed, how often a check found, how often a branch was
  taken. Every branch is open but the ones you close, and the step being executed stays in view:
  the branch it is in opens and the panel scrolls it to the middle, though not while the pointer is
  over the panel. The tree is given `onToggle` for this: without it PrimeReact reads `expandedKeys`
  on its first render only, which usually came before the flow had loaded.
- **The list:** newest first. Live while the top is in view, "14 new · back to latest" once
  scrolled down. All, Found and Failed, and Find a step by name or number.
- **The step:** where it executed, what it found against what it needed, its screenshot or why
  there is none, Open in the editor, and Suggest a fix on a failure.

**A loop pass that found nothing folds.** A pass is a loop row and every deeper row after it. It is
quiet when it ran at least one check and none succeeded, and two or more quiet passes of one loop in
a row become one row, "224 passes that found nothing", that opens. The pass still executing never
folds, since it may yet find something, nor the one that ended the execution. "Going the same way as
the pass before" was the other rule weighed: it folds a loop that clicks before it looks, but it
also folds two passes that found the same thing, which are the ones being looked for.

**The overview is counted as the rows arrive.** `ExecutionTally` (`execution-overview.ts`) takes
each batch and keeps the counts, the activity a second at a time and the passes still open; the
tiles, the activity and the tree read a snapshot of it. Worked out again from every row on every
batch, it cost more with each pass: three hours into a Firestone execution of 41,000 rows the
renderer used 670 MB and more CPU than the backend. A live row carries its start and its best match
in the broadcast, so the activity and the found chips are right before the execution ends.

**A live page lets go of what a fold hides.** It keeps its newest 2,000 rows and every pass that
found something. Older quiet passes, of a loop no other loop holds and beside another quiet one,
are kept as a count (`releaseQuietPasses` in `execution-list.ts`), and Show reads their rows back
from the history (`Execution.getSteps`, by sequence, which works while the execution is still
writing). Past 20,000 rows kept the oldest go as well, and the last row says how many. A batch then
costs the same at any length: under 1 ms on 41,000 Firestone rows and under 4 ms on a day's 339,000,
where it took 14 ms and 156 ms. Find a step and the filters look through what is kept, and say so.
A live row has no database id, so the step panel asks for the saved one for its screenshot, exit
code and stderr, once the history has written it.

### Capture flows

**Overlay capture** (for `FlowArea` and templates) opens a fullscreen transparent window per
monitor, all showing a frozen screenshot plus a dimmer, clipping one shared physical selection rect
to their own monitor. Confirm sends the physical absolute rect back.

A template is cut from the screenshots the overlay showed, not grabbed from the screen after
confirming. Windows can still be fading the overlay out by then, and a fresh grab caught the
selection's border round the template's edge. The screenshots are PNG, taken the way an execution
takes its own, so the template matches what a search will see. Electron keeps them while the overlay
is open and, on confirm, sends the selection back with the ones it lies on; `System.cropScreenshots`
cuts it out, across monitors when it spans two.

**F3 captures a template from where the cursor rests.** The screenshot is taken as the overlay
opens, so a click on Add template's "Capture now (F3)" leaves the cursor on the dialog and loses
whatever the application shows on hover. While the dialog's choices show, the global hook is in
hotkey mode (`System.inputRecordHotkeyStart`), whichever window has the keyboard - one clicked
into first to open a menu included - and the first F3 `KEY_DOWN` stops it and captures, as the
click does (`FlowTemplateAddDialogComponent`). Closing the dialog, Reuse a template, or the
capture itself stops it too, and it is stopped before the overlay starts, since the hook listens
in one mode at a time. F3 is not held back, so the window with the keyboard gets it as well: a
browser's find bar can open and show in the picture. The screenshot is a screen copy
(`CopyFromScreen`), which leaves the cursor out.

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

The line is ownership, not purity. `FlowStepDataService.CreateAsync(dbContext, step, templates, ct)`
is async and writes to the database, and it still owns nothing — the caller owns the context and
the transaction — so it is static, while the overload that opens its own context needs the factory
and is not. `IAppSettingService` holds its own factory, so it is a service. That is the whole
distinction, and it is what makes a static method safe to call from anywhere: there is nothing in
it to share, configure or dispose.

**Pure functions stay static; only what holds a dependency is injected.** A pure static function
is the cheapest thing in the repository to test - no fake, no fixture, no container. Wrapping
several in one injected orchestrator would turn a test that needs nothing into one that needs a
fake of all of them.

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
`KeyCatalog` (how keys are named - what the recorder writes as "Ctrl+V" and the execution and the
validation read back into keys, so the round trip cannot break), `OcrReaderCatalog` (every text
reader, its languages and the hash its download is checked against).
`ShellCommandModeCatalog` is the same kind of thing and sits beside the executor in `Business/Shell`,
its only consumer. These are not constants; they are queried, and a `Constants.cs` full of
`public const string` would describe them less accurately than `Catalog` does. The pattern has a
well-known precedent in Roslyn's `SyntaxFacts`.

**Things that genuinely are constants** live in the class that uses them — `WM_CLOSE` in
`WindowService`, the DPI awareness handles in `ScreenMetrics`, the line finder's thresholds in the
internal `TextLineFinder`. There is no `Constants/` folder, for the same reason there is no `Helpers/`
folder in `Business`.

### Backend

- **One handler per action**, one class per file, in `Transport/Ipc/Handlers/<Entity>/` - the
  folder is the part of the action before the dot. A plain class with `HandleAsync`, no base type.
- **As thin as the second caller makes it.** Logic with one caller stays in its handler; a second
  caller, or one that is not a handler, and it moves into its `Business` feature.
- **Every write to a flow goes through its data service**, in `Business/Flows/DataService/`. A
  handler is given `DataService`, which groups `Flow`, `FlowStep`, `FlowArea` and `FlowPoint`, so a
  call names the table it changes: `_dataService.FlowPoint.UpdateAsync(dto, ct)`. The handler maps the dto,
  calls it and maps the result. The rules live there once - names unique across steps, areas and
  points, a check's branch rows, a step's templates, the references a move breaks, validation after
  an import - so a form, the recorder and a file go through the same ones. The rules that span the
  tables are base methods on `BaseDataService`; a rule one table has is private to its service.
- **A data service method opens its own context and saves once**, which EF already makes atomic.
  Only one that saves more than once - an import, an extraction - opens a transaction, written out
  in the method, and every failure returns before its commit so the transaction rolls back as it is
  disposed. One that takes an `AppDbContext` runs inside the caller's, which is how an import or an
  extraction composes several tables in one transaction.
- **A data service speaks rows**: an entity, a row's own DTO for an update, linked rows, or the ids
  of an operation on rows that exist. Never instructions for building rows - the recorder's draft,
  with its temp ids and "under this step's Success branch", is turned into linked rows by its
  handler, and the data service saves them as a tree, the same rules an import's steps get.
- **A check that needs only the request belongs to the handler** - nothing to save, no name given.
  A check that protects stored data stays in the data service even when it needs no database,
  because every caller has to obey it: a step cannot be dropped into itself.
- **Reads stay in the handlers**, which take `IDbContextFactory<AppDbContext>` and own their
  `DbContext`. **There is no generic repository**: EF's `DbSet` *is* the repository, and a layer over
  it would add indirection and remove LINQ. An earlier `IDataService` was removed because it rented a
  context per call and its `SaveChangesAsync()` row count was misread as success; a data service
  returns a result, not a row count.
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

- Explicit types over `var`, and `new TypeName()` rather than a target-typed `new()`.
- No expression-bodied `=>` members.
- An `if`, not a multi-line ternary, inside a block. A ternary is fine in a LINQ projection, an
  object initializer, or when it is short enough to read at a glance.
- `<summary>` on public members only; `//` on private ones.
- Constants are all capitals with underscores: `KEYWORD_GAP_UNTIL`, `WM_CLOSE`.
- Comments explain *why*, not *what*. Names and logic carry the meaning.
- `//===` section banners inside long P/Invoke files.
- A regular expression goes through `RegexHelper`, a clock through the injected `TimeProvider`, and
  neither is a convention: both are build errors (§2).

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
ones, and it is on everywhere again now the handlers implement no MediatR interface.

`IDE0005` reports an unused `using` as an error, because a dead `using` is how a project keeps a
package it stopped using - one in `DiscordNotifier` alone would have kept protobuf-net on
`Business`. It only runs at build when `GenerateDocumentationFile` is on, since a `using` can be
needed by an XML `cref` alone; that flag brings `CS1591`, missing XML comment, which
`.editorconfig` turns off.
EF migrations are exempt by path, because EF writes their usings from a template.

A deviation is recorded three ways, and the width of the record matches the width of the exception:
a severity in `.editorconfig` for a rule everywhere, a path-scoped section for one file or folder,
and a `[SuppressMessage]` with a `Justification` for a single call site. Prefer the narrowest that
does the job — `RS0030` is one diagnostic id for every banned symbol, so a section excuses the whole
list wherever it reaches, and a folder glob reaches further than anyone remembers.

### Priorities

**Correctness → execution speed → memory → clean structure.** In that order, when they conflict.

---

## 15. Tests

1624 tests, all passing, in eight projects under `backend/Tests/` - one
per production project, plus `Architecture.Tests` - and four more that need a live desktop, left out
unless asked for.

| project | tests | what it holds |
| --- | --- | --- |
| `Core.Tests` | 222 | the pure helpers - regular expressions, conditions, variables, names, the tree rules, window matching, command lines - the key names and the recording gestures |
| `Business.Tests` | 1224 | the walker, the engine end to end - what it broadcasts, the `Execution` row, how it ends, one execution at a time, the debugger - the history's batches and the last screenshots it keeps, deleting executions with their folders, the workers, the recorder and what changed on the screen between its screenshots, with OCR's misreadings, what Windows said each press landed on, a gesture asked about while paused with what it pressed found meanwhile and its answer kept for Review, a check's rule and the notifications it sends, what the local model is shown and when it is not, the notes a model is given of it, the request a cloud model waits on the tester for with its screen text marked, the draft named as Windows named each press and nested by page, clicks on one picture folded into one Find All Images, the outline a model reads and its words put on the draft field by field, the tester's stages and their notifications, and the checks it proposes - only for words seen after their click, told from the draft's, and dropped with what they held kept - a real recording of Swag Labs asked of the local model when `-p:Model=true` brings it in, the example flows picked by meaning, the flow script, the searcher, the resolver, validation, the flow-editing rules, the repository's writing and its commits, discard, restore, branch switches, connecting, pushing and pulling; and that nothing AI is on without the local model, and when a cloud one answers in its place; and that Ollama is reached over IPv4 first, and given up on quickly when it is not there |
| `DataAccess.Tests` | 55 | migrations, the model matching them, the AI settings carried to their new keys and back, timestamps, template thumbnails, every delete rule, and a delete's space handed back to the disk |
| `Architecture.Tests` | 11 | the layering in §2 as failing tests |
| `Transport.Tests` | 40 | Start refusing a flow, or a sub-flow it calls, that has errors; the Move and Drag Test; what each step uses; the steps a Move or a Check Value is offered; cutting a template from the screenshots the overlay showed; the header's search over flows and steps |
| `Platform.Windows.Tests` | 1 + 5 | every P/Invoke in the assembly bound to its library and never called, so a misspelt entry point fails by name; and, traited `Desktop`, real monitors, the window in front, the window list, the cursor, and UI Automation asked about a window of the test's own - a button, a ticked check box, a password field and a disabled button in a group |
| `Platform.Shared.Tests` | 70 | PaddleOCR with the real models on lines drawn straight, tilted and at 150%, each read word for word where it was drawn, a reader not downloaded, a reader's character list with an empty entry and an escape only YAML has refused, and the blank columns a word's edges go to; git through the real libgit2, in a folder of its own: changes, commits of only what was picked, a flow's history, what a commit changed, a `.gitignore` that lets back only some files, branches made, checked out and compared, a push one repository fetches and is reset to, through a bare repository standing in for the server; and OpenCV: grouping clicked elements, a template with erased pixels beside a flat patch, on drawn screenshots, the thumbnail of a template, and the match-mode table in §8 on real PNGs in `Assets/` |
| `App.Tests` | 1 | the AutoMapper profile: what a form's DTO may not set on an entity |

```bash
npm run test:backend
npm run coverage:backend
```

### The stack

**xUnit v3 and Shouldly**, on Microsoft.Testing.Platform - each test project is its own executable
rather than a dll in a shared runner, which matters with OpenCV, SharpHook and ONNX Runtime all
carrying native bits. TUnit was weighed and passed over: its mutation-testing runner is in preview
and Fine Code Coverage cannot drive it, in exchange for start-up speed this suite would not notice.
FluentAssertions was ruled out because version 8 is a paid licence for commercial use, and a
routine upgrade would put it into a GPL repository. **ArchUnitNET** for the layering;
`FakeTimeProvider` for the clock.

**Fakes are written by hand.** Each port's fake records what it was told as a readable line -
`"move 200,80"`, `"press LeftCtrl+LeftShift+T"` - so an assertion reads like the flow, and throws on
anything the test did not arrange, so an unplanned call fails loudly. A fake recording lines shows a
reader what the ports bought; `Received()` shows them a mocking library.

**The database is real SQLite in memory, one per test**, built by the real migrations - not EF's
in-memory provider, which is not relational, enforces no foreign key, and would leave every delete
rule in §5 unverified. The one trick: an in-memory SQLite database lives inside its connection, so
the test opens one, holds it, and hands EF the connection rather than a connection string.

**Names are sentences** - `A_missing_required_template_fails_the_search_even_when_others_match` -
and the class names the subject. One class per subject, so
`Executions/Workers/` holds one file per worker rather than four files grouping them by kind. A
class called `SimpleWorkerTests` or `InputWorkerTests` names a bucket, and the `// ====` banners
inside it were doing the work a class name should do - the same mistake as `Parser.Steps.cs`, where
the dot was the symptom and the partial was the thing. It also means a missing file is a visible
gap - which is how the last untested worker got its tests.

### What the wiring needed

- **`backend/global.json` opts `dotnet test` into Microsoft.Testing.Platform**, which the .NET 10 SDK
  requires for xUnit v3. It is read by the `dotnet` CLI before anything builds, so it cannot live in
  code. The switches changed with it: `dotnet test --solution backend.slnx`, and test options after
  `--`.
- **One `Directory.Build.props`.** Test projects are recognised by the `.Tests` suffix and get their
  packages and settings there, and inherit everything else - analyzers, warnings as errors and both
  banned lists. A test reaching for `DateTime.UtcNow` fails like anything else, and no rule has
  needed relaxing for test code.
- **xUnit's generated `Main` blocks on a task**, which the banned list refuses.
  `XUNIT_GENERATED_DISABLE_WARNINGS` silences warnings in that generated file and nowhere else.
- **Exit code 8, "no tests ran", is not a failure**, so a project whose tests are all filtered out
  passes.
- **A test that needs a live desktop is traited `Category=Desktop`**, and `Directory.Build.props`
  filters the trait out, so a headless machine never meets one.
  `dotnet test --solution backend.slnx -p:Desktop=true` brings them in.
- **The engine's tests wait rather than poll.** The engine keeps the execution's task as
  `Completion`, and the broadcast fake hands back a task for an event - a pause at a given step -
  so a test waits for exactly what it needs and no sleep loop decides how long that is.
- **The snapshot is twenty lines rather than Verify.** Verify 33 fails the build until a project
  declares sponsorship, a licence or an exemption - a statement about the project that is not a
  test suite's to make. `ApprovedFile.ShouldMatch` compares the printed sample flow with
  `SampleFlow.approved.sflw`.

### What they found

Each of these was found by a layer's first run, and none had been reported:

- **`Press Ctrl+1` pressed Ctrl+B.** The key parser used `Enum.TryParse`, which reads `"1"` as the
  enum member at position 1. The recorder writes `Num1`, which is why nobody saw it; a hand- or
  AI-written script did. Its twin was in the script parser: `System 99` was accepted as a system
  action that does not exist.
- **`Wait Until No Image` and `Wait Until No Text` took their Failure branch when the thing went
  away** - exactly when they should succeed - and failed again when it timed out still there. Since
  the wait modes were written.
- **Results were never forgotten.** The walker recorded where a result sat and then dropped that
  same record, so every result stayed readable until its step executed again. Left open while the
  rule was decided, fixed 2026-10-26 - see §8.
- **A start that failed while loading left the engine claimed.** `StartAsync` claims the engine,
  then loads the steps and writes the `Execution` row; a throw there - a database error, or the
  request cancelled - never let go, and every later start was refused as "already running" until
  the app restarted. Layer 5, 2026-10-26.
- **The P/Invoke probe never checked the five calls that move or close windows.** It resolved each
  with `Marshal.Prelink` on the method a `LibraryImport` declares, which is a generated managed
  wrapper rather than the native call, so there was nothing to bind. Planting `PostMessage` for
  `PostMessageW` passed the probe; the test that replaced it binds the generated calls themselves
  and names the missing export. 2026-10-29.

Proving the tests bite: planting two bugs in `ImageSearcher` - the required rule off, the larger
scale ratio instead of the smaller - each failed exactly the test written for it and nothing else.
Taking the engine's fix back out failed its test and no other. Mapping shape-and-brightness onto
the removed `CCorrNormed` failed five cells of the match-mode table.
Adding a column to an entity without a migration failed every `DataAccess` test. A throwaway
architecture rule that is false today failed and named every class that broke it.

### The architecture tests

Six layer rules, one per project, each naming what it may not depend on; no OpenCvSharp,
SharpHook or LibGit2Sharp outside `Platform.Shared`; `Core` using nothing but the framework; and no `Services`
namespace coming back into `Business`. Today no forbidden edge can even be written - each would be a
reference cycle or a reference to a Windows framework - so they first bite when somebody gives a
project a Windows target to reach the machine, which is exactly how `Business` was before the split.

### Coverage, and why it is not one number

**30.7% of lines overall.** Per project: DataAccess 82.5%, Business 50.9%, Core 38.6%, and App,
Transport and Platform.Windows 0%. Measured over the six production assemblies only - libraries
that ship debug information would otherwise be counted as ours - and without the generated
migrations, which the tests run in full just by building a database and which alone took the total
to 65%.

The target is per project, and the table is the architecture diagram: `Core` near total, because
it is pure decisions and has no excuse; `Business` decision code high - the walker, the script, the
validators, the searcher; orchestration moderate and by integration test; `Platform.Windows` near
zero **on purpose**, because it is the part that touches the machine, with the handful of tests that
need a real screen labelled and kept out of CI. A blanket 100% buys tests for property getters and
catches nothing. The number worth publishing is a mutation score over the walker and the script,
because that is a claim about whether the tests detect defects rather than which lines ran.

Not tested yet: the frontend. There is no Playwright and will not be: this product *is* a UI
automation tool, and its end-to-end test is a flow script that tests StepinFlow.

---

## 16. Status

In active development, not released.

Working: the flow builder, the recorder with Review and its plain draft, image search that survives another monitor
and DPI, OCR, sub-flows, validation, Discord notifications, the execution engine with breakpoints,
step into and step over, execution history with the screenshots leading up to its end, the flow script in both
directions, flows kept and committed in a local git repository, and the AI assistant on a local Ollama model
with OpenAI on top. 1129 backend tests.

`PLAN.md` holds the open build order. `TODO.md` holds everything deferred.

### Known gaps

- No inputs from CSV, no viewport matrix, no CLI runner - phases 8, 10 and 12.
- The 5.7 forms - template capture and an area's "Contents scale with" - are verified by the build
  and tests but have not been clicked through by hand.
- `ShellCommandValue` can hold a credential in a command line. It is authored rather than read off the
  screen, so it is not currently redacted for AI. Flagged in `TODO.md` rather than folded in silently.

---

## Licence

GPL-3.0-or-later. Copyright (C) 2026 Alex Psihogios.
