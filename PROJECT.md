# StepinFlow — Project Reference

> Working reference for the app: what it is, how it is built, what exists today, and what is
> knowingly broken or unfinished. Written to be pasted into an AI prompt as context, or skimmed
> to recall the business.
>
> Last synced with the repo: 2026-08-07.

---

## 1. What it is

A Windows desktop **workflow automation bot**. The user builds a **Flow** out of ordered,
nestable **FlowSteps** (click here, wait, loop, search the screen for an image, type text …) and
then executes it. The app drives the real mouse and keyboard and reads the real screen, so a flow
automates any application, not just a browser.

Two halves:

- **Authoring** — a tree of steps on the right, a per-step-type form on the left.
- **Execution** — the same page replays the flow live, showing each step's state and result.

---

## 2. Stack

| Layer | Tech |
|---|---|
| Shell | Electron (main + preload, multiple BrowserWindows) |
| UI | React + Vite + TypeScript, PrimeReact + PrimeFlex |
| State | Zustand (UI state), TanStack Query (server state) |
| Forms | React Hook Form + Zod (`zodResolver`) |
| Routing | `react-router-dom`, `createHashRouter` |
| Backend | .NET 10 console host (`Host.CreateApplicationBuilder`), MediatR, AutoMapper |
| Data | EF Core + SQLite (file-backed, `AddPooledDbContextFactory`) |
| Input | SharpHook (global hook + event simulation) |
| Capture | Direct3D11 / Windows.Graphics.Capture |
| IPC | Two named pipes, protobuf-net ↔ protobufjs |

The .NET process is **DPI-unaware by design** (see §5).

---

## 3. Process model and IPC

Three processes:

```
Electron main  ──named pipe "stepinflow-request"──►  .NET host   (request/response)
      ▲                                                   │
      │        ──named pipe "stepinflow-broadcast"─────────┘       (server → client push)
      │
  React renderer(s) via contextBridge (preload)
```

**Only three protobuf messages exist** — the envelope is protobuf, the body is JSON bytes:

```proto
message IpcRequest   { string action = 1; bytes payload = 2; string correlationId = 3; }
message IpcResponse  { string action = 1; bytes payload = 2; string correlationId = 3; string error = 4; }
message IpcBroadcast { string type = 1;   bytes payload = 2; }
```

- `action` is a string like `"FlowStep.update"`, routed by a switch in
  `backend/App/Ipc/IpcDispatcher.cs` to a MediatR request.
- `payload` is UTF-8 JSON (camelCase, enums as strings, `ReferenceHandler.IgnoreCycles`).
  **Adding a new DTO never touches the .proto.**
- Every response body is `ResultDto<T>` (`isSuccess`, `data`, `errorMessage`, `errors`).
- Broadcasts are fire-and-forget, delivered to **all** BrowserWindows, discriminated by `type`
  (`BroadcastTypeEnum` as a string).

Electron IPC channels live in `electron/shared/channels.ts`; shared TS types in
`electron/shared/types.ts` (imported directly by the React code via relative path).

**Backend hosted services** (`Program.cs`): request pipe listener, broadcast pipe listener, and the
SharpHook global hook — the hook runs for the whole process lifetime from startup.

---

## 4. Database

SQLite file at `PathHelper.GetDatabaseDataPath()/StepinFlowSQLite.db`. EF migrations are applied on
startup (`dbContext.Database.Migrate()`). 13 migrations so far (`InitialMigration` … `InitialMigration12`).

### Tables

| Table | Purpose |
|---|---|
| `Flow` | The workflow. Name, OrderNumber. |
| `SubFlow` | Reusable sub-workflow. Modelled, **no UI yet**. |
| `FlowStep` | One node of the flow tree. Wide table, one column set per step type. |
| `FlowSearchArea` | Named reusable **rectangle** owned by a Flow. |
| `FlowLocation` | Named reusable **point** owned by a Flow. |
| `FlowStepImage` | Template image + match settings for IMAGE_SEARCH. Blob lives here, off `FlowStep`. |
| `Execution` | One run of a Flow. **Modelled, not implemented.** |
| `ExecutionStep` | Per-step result of a run. **Modelled, not implemented.** |

`BaseDbModel` gives every row `Id` + `CreatedOn`.

### The wide-table decision

`FlowStep` holds **every** field for **every** step type, all nullable/defaulted, and
`FlowStepType` is the discriminator. This is deliberate:

- SQLite stores a NULL column as ~1 byte of record header and **zero payload bytes**, so ~30 unused
  columns per row cost almost nothing.
- The executor loads a whole step in one row with no joins.
- The DTO is flat, so the form binds straight to it with no mapping layer.

The form shows only the fields its `flowStepType` uses.

### FlowStep relationships

| FK | Points at | On delete |
|---|---|---|
| `FlowId` | Flow | Cascade |
| `SubFlowId` | SubFlow | Cascade |
| `ParentFlowStepId` | FlowStep (`ChildrenFlowSteps`) | Cascade |
| `FlowSearchAreaId` | FlowSearchArea | **SetNull** |
| `FlowLocationId` | FlowLocation (`FlowSteps`) | **SetNull** |
| `FlowLocationEndId` | FlowLocation (`EndFlowSteps`) | **SetNull** |
| `FlowStepReferenceId` | FlowStep (`FlowStepReferences`) | **SetNull** |
| `FlowStepReferenceEndId` | FlowStep (`FlowStepReferencesEnd`) | **SetNull** |

The `SetNull` group is deliberate: search areas, locations and referenced steps are **reusable**, so
deleting one must clear the reference rather than delete every step using it.

Indexes: `RootId`, `(FlowId, OrderNumber)`, `(ParentFlowStepId, OrderNumber)`.

### RootId

`FlowStep.RootId` denormalises the owning Flow/SubFlow id onto every descendant so a whole tree can
be fetched with one `WHERE RootId = ?` instead of a recursive CTE. Used by the ancestor lookup and
intended for the executor's single-query load.

---

## 5. Coordinate spaces — read this before touching anything positional

This is the single most error-prone area of the app.

- The .NET process is **DPI-unaware on purpose**, so multi-monitor screenshots stitch correctly.
- Consequence: `GetMonitorInfo` returns **DPI-virtualised** ("logical") coordinates, while
  `EnumDisplaySettings`/DEVMODE returns **real device pixels** ("physical").
- `MonitorInfo` carries both: `Bounds` (logical) and `PhysicalBounds` (physical).

**Everything persisted is in PHYSICAL pixels** — `FlowSearchArea`, `FlowLocation`, and the
coordinates SharpHook's global hook reports.

**Cursor movement does not go through SharpHook.** `Business/Helpers/CursorHelper.cs` calls
`SendInput` with `MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK`, normalised to `[0,65535]` over
the virtual desktop, from inside a thread that temporarily sets
`DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2` and restores it before returning. The per-thread
context is what lets the rest of the process stay DPI-unaware while the move speaks physical pixels.

`OverlayCapturePage.tsx` documents its own contract at the top: selection state is physical
absolute; logical is used only at the edges (mouse input → broadcast, broadcast → render clip).

> ⚠ **Unverified:** the SendInput normalisation has not been tested on a machine with a monitor at
> non-100% scale. Test: set a display to 150%, capture a FlowLocation, press **Test**, confirm the
> cursor lands on the same pixel.

---

## 6. Reusable positional entities — the portability story

The problem: a flow authored on one PC has hard-coded screen coordinates and breaks on another.

The answer is to never let a step store raw coordinates. Steps point at a **named, reusable entity
owned by the Flow**, resolved at runtime. Moving a flow to a new machine means re-capturing a handful
of named entities; every step keeps working.

### FlowSearchArea — a rectangle

Selected in a dropdown by IMAGE_SEARCH / TEXT_SEARCH. `FlowSearchAreaTypeEnum`:

| Type | Stores | Resolved at runtime by |
|---|---|---|
| `CUSTOM` | `LocationX/Y/Width/Height` | used as-is |
| `APPLICATION` | `AppWindowName` | finding the window and taking its rect |
| `MONITOR` | `MonitorUniqueId` | looking the monitor up |

Authored with the **overlay capture window** (drag a rectangle).

### FlowLocation — a point

Same idea one dimension down: `Name`, `LocationX`, `LocationY`, `FlowId`. Used by cursor steps.

Authored **in place**, without opening a window (see §9).

Both are edited as `useFieldArray` collections inside the **Flow form**, side by side, and both grids
show a **"Used By"** count of the steps referencing them. The count is computed in SQLite by the same
query that loads the Flow (`GetFlowHandler` is a projection, not `Include` + map). Deleting an entry
that is in use warns with the count first.

---

## 7. Flow step types

`FlowStepTypeEnum`:

```
System : WAIT, LOOP, GO_TO, RUN_CMD, SUB_FLOW, VARIABLE_CONDITION, NOTIFICATION_EMAIL
Input  : CURSOR_CLICK, CURSOR_RELOCATE, CURSOR_DRAG, CURSOR_SCROLL,
         WINDOW_FOCUS, WINDOW_RESIZE, WINDOW_RELOCATE, KEYBOARD_INPUT
Search : IMAGE_SEARCH, TEXT_SEARCH
Hidden : SUCCESS, FAILURE   (control-flow children, not user-selectable)
```

### Implemented

| Type | Fields used | Notes |
|---|---|---|
| `WAIT` | `name`, `waitForMilliseconds` | |
| `LOOP` | `name`, `loopCount`, `isLoopInfinite` | mutually exclusive, enforced in the form; can have children |
| `CURSOR_CLICK` | `name`, click action, `cursorButtonType`, start point | |
| `CURSOR_RELOCATE` | `name`, start point | move without clicking |
| `CURSOR_DRAG` | `name`, `cursorButtonType`, start point, end point | |
| `CURSOR_SCROLL` | `name`, `cursorScrollDirectionType`, `loopCount` (notches) | no point |

`SUCCESS` / `FAILURE` / `LOOP` are the only droppable (child-accepting) node types in the tree.

### Not implemented

`GO_TO`, `RUN_CMD`, `SUB_FLOW`, `VARIABLE_CONDITION`, `NOTIFICATION_EMAIL`, `WINDOW_*`,
`KEYBOARD_INPUT`, `IMAGE_SEARCH`, `TEXT_SEARCH`. Model columns exist for most of them
(`RunCommand`, `ConditionText/Type`, `WindowName/Height/Width`, `KeyboardInputText/Type`,
`LocationX/Y/EndX/EndY` for the WINDOW_* steps).

---

## 8. Cursor steps — the design

**Four separate `FlowStepType` values, one shared form.**

The alternative considered was a single `CURSOR` step type plus a mode enum. Four types won because:

- the executor stays a **flat dispatch** (`Dictionary<FlowStepTypeEnum, IFlowStepExecutor>`) instead
  of a nested switch;
- tree icons, labels and the type-picker grid stay per-type with no special-casing;
- Zod validation is per-type;
- the four values already existed — zero migration.

The UI merges them: one card in the type picker, then **four mode buttons** (Click / Move / Drag /
Scroll) that rewrite `flowStepType`. Fields below change per mode. Switching mode in EDIT is allowed;
on submit the fields belonging to the other modes are explicitly cleared so a scroll step never
carries a stale click action.

Files: `frontend/src/features/flow-step/components/forms/cursor/`
(`FlowStepCursorFormComponent`, `…FormFieldsComponent`, `…LocationFieldsComponent`,
`cursor-modes.ts`, `flow-step-cursor.zod.ts`).

### Point resolution

Each cursor point has two possible sources, chosen by a boolean:

| `isLocationCustom` | Source | Field |
|---|---|---|
| `true` | a saved **FlowLocation** on the Flow | `flowLocationId` |
| `false` | the **result of an ancestor step** | `flowStepReferenceId` |

`CURSOR_DRAG` has the whole set twice: `isLocationEndCustom`, `flowLocationEndId`,
`flowStepReferenceEndId`.

`flowStepReferenceId` holds the id of an **ancestor** `IMAGE_SEARCH` / `TEXT_SEARCH` step. At
execution time the executor takes the latest `ExecutionStep` row for that step and uses its
`ResultLocationX/Y`. Only ancestors are offered — anything off the parent chain may not have run yet
when the cursor step executes. `Lookup.flowStep` implements this: one query by `RootId`, then walk
the parent chain in memory, nearest first.

---

## 9. Custom Electron windows and capture flows

### Overlay capture (used by FlowSearchArea)

1. Caller invokes `OVERLAY_OPEN_CAPTURE_WINDOW`.
2. Electron opens a **fullscreen transparent window per monitor**, all loading `/overlay-capture`.
3. Backend takes a screenshot per monitor (`System.captureForOverlay`) and starts
   `System.inputRecordOverlayStart`, which broadcasts mouse/key events as `OVERLAY_MOUSE_EVENT`.
4. Every window renders the frozen screenshot + a dimmer, clipping the shared physical selection
   rect to its own monitor.
5. Confirm → renderer sends the **physical absolute rect** back; Electron closes all overlay windows
   and resolves the caller's promise. Escape cancels.

### Point capture (used by FlowLocation) — no window

Deliberately **not** a window. Click **Capture Location**, then click anywhere on screen:

1. `System.inputRecordPointCaptureStart` puts the always-running global hook into point-capture mode,
   broadcasting as `POINT_CAPTURE_EVENT`.
2. `use-capture-point.ts` resolves a promise on the first `BUTTON_DOWN`.
   It listens for **BUTTON_DOWN, not BUTTON_UP** — the press that armed the capture happened before
   recording started, so its release is the only stale event that can arrive.
3. `KEY_UP` + `Escape` cancels; so does clicking the button again; unmount tears the session down.

> The click is **not swallowed** — SharpHook observes, it does not suppress. That is what allows
> picking a point inside a live app, but it also means the click reaches whatever is underneath.
> Suppressing would need a low-level hook returning non-zero.

A **Test** button next to any location calls `System.moveCursor` and physically moves the cursor
there, so the user can confirm the point before saving.

### Image editor

Opens a window at `/image-editor` with a PNG, used to produce the IMAGE_SEARCH template.
Implemented: zoom, pan, toggleable pixel grid with adjustable opacity and hover X/Y readout,
minimap, rectangular crop, freehand/polygonal lasso crop, eraser (pixels → transparent),
undo/redo stack with thumbnail history.
Actions: crop-and-apply-as-new-background, and erase pixels to transparent.

---

## 10. Backend conventions

- **One handler per action**, MediatR, in `Business/Ipc/Handlers/<Entity>/`.
- Handlers take `IDbContextFactory<AppDbContext>` and own their own `DbContext`.
  There is **no generic repository / data service** — an earlier `IDataService` was removed because
  it rented a context per call and its `SaveChangesAsync()` row count was misread as success/failure.
- **Reads**: `AsNoTracking()`, and project straight into the DTO when the shape is known
  (`GetFlowHandler`, tree queries, lookups) so counts and joins happen in SQLite in one round trip.
- **Updates**: load the tracked entity, then
  `dbContext.Entry(entity).CurrentValues.SetValues(dto)` — copies scalars and FKs only, so the
  navigations the client round-tripped back cannot overwrite unrelated rows, and `CreatedOn` survives.
- **Deletes**: `ExecuteDeleteAsync()`.
- **Child collections** (Flow → search areas / locations) are synced **by hand**, matching on `Id`:
  update matched, insert `Id == 0`, delete missing. AutoMapper must never assign a collection onto a
  tracked entity — it deletes and re-inserts every row, changing ids and breaking every `FlowStep`
  that referenced one.
- **AutoMapper**: entity → DTO maps may carry navigations; **DTO → entity maps ignore every
  navigation and `CreatedOn`**.
- Enums are stored **as strings** (`HasConversion<string>()`).

### Services

| Service | Role |
|---|---|
| `IInputService` / `InputService` | `MoveCursor` (via `CursorHelper`), click, scroll, keyboard. Adds a settle delay between move and press because targets process the move asynchronously. |
| `IInputRecordService` / `InputRecordService` | The global hook. Handlers subscribe **once at construction** and gate on a mode int (`None`/`All`/`Overlay`/`PointCapture`) set with `Interlocked.CompareExchange`, so only one recording runs at a time and subscriptions can't drift. Events go to a **bounded** channel (`DropOldest`, 4096) plus an optional broadcast. |
| `IScreenshotService` | `Capture(rect)`, `CaptureVirtualScreen`, `CaptureSearchArea`, `CaptureAppWindow`, `CaptureMonitor`. |
| `IWindowsGraphicsCaptureService` | D3D11 / Windows.Graphics.Capture backend. |

Helpers: `ScreenHelper` (monitors, logical vs physical bounds), `AppWindowHelper` (enumerate/find
windows, `[ThreadStatic]` title buffer), `CursorHelper` (DPI-correct SendInput), `Direct3D11Helper`.

---

## 11. Frontend conventions

- **Feature-based**: `features/<name>/{components,hooks,store}`, shared code in `shared/`,
  Electron-window pages in `windows/`.
- Every entity form is a pair: `XFormComponent` (RHF setup, header, footer, submit) +
  `XFormFieldsComponent` (fields only, reads `useFormContext`), with a sibling `x.zod.ts`.
- **Any field missing from the Zod schema is dropped on submit** — forms submit
  `{ ...defaultValues, ...data }`, so an unvalidated field silently keeps its default.
- TanStack Query for server state, keyed `["flow", …]`, `["flowStep", …]`, `["lookup", …]`;
  mutations invalidate.
- Zustand for UI state (`workflow-store` holds the selected tree node, the step type being added,
  the tree refresh trigger, and the root flow id).
- Dialogs go through `useDialogStore` (`openForm`/`closeAll`) rendering into `DialogRootComponent`.
- Reusable form controls in `shared/components/form/` all bind via `useController`.

### The tree

`DataTreeComponent` renders a PrimeReact `Tree`, lazily loading children on expand.

**Node keys are namespaced.** Flow ids and FlowStep ids are separate sequences, so a raw id makes
Flow 5 and FlowStep 5 the same node as far as selection and expansion are concerned. Keys are
`flow-{id}` / `step-{id}`, built by `TreeNodeDto.BuildKey` (C#) and `buildTreeNodeKey` (TS) — **these
two must stay in sync**. `TreeNodeDto.entityId` carries the real id; nothing parses the key.

For the same reason `FlowStep.getTreeNodes` takes `{ id, isFlow }`, not a bare id:
`isFlow: true` → `WHERE FlowId = id AND ParentFlowStepId IS NULL` (the flow's root steps),
`isFlow: false` → `WHERE ParentFlowStepId = id`.

Each expanded node appends a synthetic "New item" node (random UUID key, `isNew: true`) that opens
the step-type picker.

### Routes (`createHashRouter`)

`/`, `/flows`, `/flows/new`, `/flows/:id/{view,edit,clone}`, `/workflow/:id`,
plus the window routes `/overlay-capture`, `/overlay-preview`, `/image-editor`.

---

## 12. IPC action catalogue

```
Flow            create update delete get getLazy getTreeNodes
FlowStep        create update delete get getLazy getTreeNodes
FlowSearchArea  create update delete get getLazy
FlowLocation    create update delete get
FlowStepImage   create get
SubFlow         create update delete get
Lookup          window monitor flowStep flowLocation
System          takeScreenshot captureForOverlay moveCursor
                inputRecordAllStart/Stop
                inputRecordOverlayStart/Stop
                inputRecordPointCaptureStart/Stop
```

---

## 13. Known issues and inconsistencies

### 🔴 Cursor enum split is currently inconsistent across layers

There are now **two** discriminators for the same concept and the click action does not round-trip.

| Layer | Fields |
|---|---|
| Entity `FlowStep` | `CursorType` (CLICK/MOVE/SCROLL/DRAG), `CursorButtonActionType` (SINGLE/DOUBLE/HOLD/RELEASE), `CursorButtonType`, `CursorScrollDirectionType` |
| `FlowStepDto` (C#) | `CursorType`, `CursorButtonType`, `CursorScrollDirectionType` — **`CursorButtonActionType` missing** |
| `FlowStepDto` (TS) | `cursorActionType`, `cursorButtonActionType` (both typed as the CLICK/MOVE/SCROLL/DRAG enum), `cursorButtonType`, `cursorScrollDirectionType` — **no `cursorType`** |
| Cursor form / Zod | discriminates on `flowStepType`; binds the **"Click Action"** dropdown to `cursorActionType`, which now offers CLICK/MOVE/SCROLL/DRAG |

Consequences: the click action cannot be saved or loaded; the "Click Action" dropdown shows the
wrong four options; `CursorType` never arrives from the form.

Decision still open: **either** keep the four `FlowStepType` values and delete `CursorTypeEnum`
(the form already discriminates on `flowStepType`), **or** keep `CursorType` and collapse the four
step types into one. Not both. Recommended: drop `CursorTypeEnum`, rename the click-action field to
`CursorButtonActionType` consistently in all four layers.

Also: `backend/Core/Enums/Cursor/CursorActionTypeEnum.cs` declares a type named `CursorTypeEnum` —
filename and type disagree.

### 🟠 Not built

- **The whole execution engine.** `Execution` / `ExecutionStep` are modelled but have no handlers,
  no IPC actions and no executor. See §14.
- `SubFlow` CRUD is wired but has no UI; `GetSubFlowTreeNodeHandler.cs` is entirely commented out.
- `GetLazyFlowStepImageQuery`, `UpdateFlowStepImageCommand`, `DeleteFlowStepImageCommand` are
  declared with no handler and no dispatcher entry (unreachable, so no runtime error).

### 🟡 Smaller

- `Execution.Status` / `ExecutionStep.Status` are `string`, unlike every other enum.
- `Lookup.window` returns window **titles** only. Titles mutate constantly
  ("Document1 - Word" → "Report - Word"), so a saved `AppWindowName` goes stale. Should key on
  process name + an optional title pattern; `SystemWindow.ProcessName` already exists but is unused.
- Window matching is `title.Contains(x)`, first `EnumWindows` hit wins, no z-order preference —
  "Notepad" matches "Notepad++".
- `CursorActionTypeEnum.HOLD_CLICK` without a matching `RELEASE_CLICK` leaves the physical button
  down. The executor must force-release held buttons and modifiers on completion, failure and abort.
- Overlay capture uses JPEG. Fine for display; **must not** feed an IMAGE_SEARCH template — JPEG
  artifacts wreck template matching.
- Dead copy-paste files: `features/flow-search-area/hooks/use-flow-step.ts` and
  `features/flow-search-area/store/flow-step-store.ts` are duplicates of the flow-step versions.
- `WorkflowContentComponent` still switches per step type twice (ADD branch and VIEW/EDIT branch).
  At 17 step types this becomes ~700 lines. A single registry
  (`Record<FlowStepTypeEnum, {label, icon, form: lazy(...), defaults}>`) would collapse it and
  code-split the forms.
- Every form repeats a `setTimeout(() => trigger(), 0)` on mount to sync `isValid`.

---

## 14. Execution engine — decisions to make before building it

Not implemented. The design intent:

- **Load the whole flow in one query** using `RootId`, build the tree in memory, execute with zero
  DB round trips in the hot path.
- **Explicit stack, not recursion** — `Stack<Frame>` of `{ stepId, childIndex, iteration }`.
  Infinite `LOOP` + `GO_TO` make recursion depth unbounded, and an explicit stack gives
  pause / resume / step-into for free, which the workflow page wants.
- **Do not write an `ExecutionStep` row per step** — for fast steps the INSERT dominates. Keep run
  state in memory, stream progress over the existing broadcast pipe, persist only the `Execution`
  header, checkpoints and failures (or batch-flush).
- **Budget + panic key.** `GO_TO` plus infinite `LOOP` can never terminate. Wire a step budget and a
  global panic key (Esc/F12) to cancel — the global hook is already running, so it is nearly free.
- **Dry-run mode** that logs resolved coordinates instead of clicking. Makes the whole
  FlowLocation/FlowSearchArea portability model debuggable.
- One executor class per step type resolved from DI
  (`Dictionary<FlowStepTypeEnum, IFlowStepExecutor>`), sharing an injected point resolver.
  Never a giant switch.
- Consider storing IMAGE_SEARCH templates as **files** with a path in the DB rather than blobs in
  `FlowStepImage`, so no query accidentally drags megabytes of PNG along.

---

## 15. Conventions worth stating to an AI

- Priorities, in order: **correctness → execution/load speed → memory → clean structure.**
- Prefer one round trip and a projection over `Include` + AutoMapper for read paths.
- Prefer adding a nullable column on `FlowStep` over a new child table, unless the thing needs to be
  **named and shared** across steps (that is what `FlowSearchArea` and `FlowLocation` are for).
- Naming: `XComponent.tsx`, `XFormComponent` / `XFormFieldsComponent` / `x.zod.ts`,
  `XHandler.cs`, `XDto`, `XEnum`. Handlers are one class per file under `Handlers/<Entity>/`.
- C# style in this repo: explicit types over `var`, `//` section banners, comments explaining *why*.
