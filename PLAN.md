# Build order

What gets built, and why in this order. `PROJECT.md` says what the product does and how it is put
together; `TODO.md` is everything deferred that is not part of this plan.

Ordering is by dependency first and value second. Where both allowed a choice, the thing that makes
an earlier phase honest wins over the thing that makes a later one possible.

> **Reconstructed on 2026-09-16.** The original was written but never committed, and was lost in a
> revert along with the `Platform.Windows` sources. The phase list and ordering are intact. The
> per-phase notes on the finished work are not the original prose — they were rebuilt by reading
> the code, so they say what is there rather than what it felt like to write. Everything marked
> `[x]` below was checked against the repository, not recalled.

---

## Foundations

### 0. Close the two leaks

- [x] **One chokepoint.** `IAiProviderService.CanSendScreenDataAsync` - local always may, a cloud
      provider only on the setting. `ExecutionRunExplainService` had its own copy of that rule; it
      now calls this one.
- [x] **Screenshots gated.** `FlowQuestionService` resolves the rule once per question and passes
      it to both the pictures and the tools, so the two cannot disagree.
- [x] **Typed text redacted**, and the flag reaches `DbQueryTools` through its constructor rather
      than being re-derived inside it.

`AI_SEND_SCREEN_CONTENT` is a boolean and defaults to off. The `AppSetting` api key and
`DiscordBot.WebhookUrl` are never selected in any `DbQueryTools` projection: the webhook url *is*
the credential.

Not done, and deliberately: `RunCommandValue` can hold a credential in a command line, but it is
authored rather than read off the screen, and "which flows run curl" is a fair question. Flagged in
`TODO.md` rather than folded in silently.

### 1. Name uniqueness

Before the script, because the script refers to a step, an area and a point **by name**. Two things
sharing a name cannot round trip.

- [x] **`FlowNameHelper`** in Core - `MakeUnique` returns the first free "name 2", "name 3", and
      `Duplicates` finds names taken more than once. Case insensitive, trims, and a blank name is
      not a duplicate of another blank one.
- [x] **`FlowNameLookupHelper`** - the names already in use in a flow, across steps, areas, points
      and csv columns, which share one namespace. Shared by the create paths so a step saved one at
      a time and a whole draft saved at once cannot disagree about what counts as taken. `SUCCESS`
      and `FAILURE` rows are excluded: they are structural and nothing refers to them by name.
- [x] **`NAME_DUPLICATE`, an error rather than a warning.** A duplicate cannot round trip and makes
      two steps share one history trend. Creation avoids it automatically, so only a manual rename
      can reach it, and the fix is to rename.

### 2. Variable substitution

- [x] **`VariableTranslator`** in Core - `Translate`, `Names`, `VariableBearingText`,
      `DescribeUntranslated`, `ViewportNames`. `{{{{` escapes a literal brace.
- [x] **`VariableTranslationResult`** carries the text, what was left untranslated, and whether it
      translated at all - so a worker can fail with "this names nothing" instead of typing
      `{{username}}` into the application.
- [x] **`IExecutionCacheService.TranslateVariables`** builds the name to value map from the steps
      and the results so far.

Three kinds of name go through one helper: a csv column, the viewport being executed
(`{{width}}`, `{{height}}`), and what an earlier step produced.

### 3. The app under test, setup and teardown

- [x] **`Flow.AppUnderTestAreaId`** names the root area that is the application being tested.
      Sizing a viewport needs a window to size, and guessing it from whatever has focus is what
      breaks on another machine. `DeleteBehavior.NoAction`, to break the Flow to FlowArea to Flow
      cascade cycle.
- [x] ~~**`AppCloseModeEnum`** - `LEAVE`, `CLOSE_WINDOW`, `KILL_PROCESS`.~~ **Reversed on
      2026-09-21**, along with `ExecutionEngine.CloseAppUnderTestAsync` and the `AppUnderTestCloser`
      that briefly replaced it. Both are gone, and so is the column. Teardown is steps under
      `End Execution` - see phase 4.7.

      The reason recorded here was that `END_EXECUTION` stops the walk where it stands, so a
      failed pass would leave the application open. True of the walk as it was written; not a law.
      The walker now drops everything pending when it reaches an `End Execution` and pushes that
      step's own children, so cleanup written under it runs and the walk ends when it does. The
      remaining case - an exception, where no `End Execution` is in scope and nothing is authored
      to run - ends the whole execution including the rest of the viewport matrix, so there is no
      next pass to protect.
- [x] **`FlowViewport`** - width, height, order, owned by the flow. Configuration rather than a
      step, so it stays editable after recording.

Launch stays a **step**, not a flow property: `FLOW-FORMAT.md` writes it as one and
`RunCommandPresetEnum.LAUNCH_APP` already existed.

Still open, and belonging with the recorder rather than here - see phase 7:

- [ ] Recording asks up front which application and how to open it.
- [ ] Recording asks at the end what to do with it.

---

## The script

### 4. The writer

Before the parser, because it is easier and because reading a real flow as text is the only way to
find out whether the grammar reads well. It was - four things only showed up once a real flow was
on screen, and all four changed the grammar rather than the code.

- [x] **`Flow.PublicId`**, which the header carries as `Id:`. Identity across machines: an int is
      unique to one database and a repository is cloned into many. Named `PublicId` rather than
      `Guid` because `Guid` names the C# type, not the meaning. Unique index, and the migration
      backfills existing rows with version 4 UUIDs - the generated one defaulted every row to
      `Guid.Empty` and would have failed the index on any database holding two flows.
- [x] **`FlowScriptWriter`** - header, areas, points, inputs, then steps with branches indented,
      `#` comments from `CodeComment` and `##` sections from markers.
- [x] **`FlowScriptKeywords`** maps a step and its search mode to one word - "Wait For Image"
      rather than "Search Image ... wait until found" - and **`Words`** maps enum members to what a
      reviewer reads. Kept apart from the writer so the parser can read the same tables backwards.
- [x] **Deterministic**: branch order from `OrderNumber`, areas roots-then-children each
      alphabetical, children built inside `FlowScriptSource` rather than by the caller. The round
      trip compares bytes, so anything ordered by chance fails it.
- [x] Empty branches are left out. `Success:` with nothing under it says nothing.

What reading the output changed in `FLOW-FORMAT.md`:

- **Every name is quoted**, in the header as well as the steps. The spec had bare names lined up in
  columns, which a parser cannot read back - it would have to guess where `Login form inside
  Browser` stops being a name.
- **`Launch` takes one command string**, not an executable plus `args`. The model stores one
  string, and inventing a split the model does not have would fail the round trip.
- **Sub-flow paths are `.sflw`**, which the spec still wrote as `.flow`.
- **`Id:`** joined the header, which the technical decisions described but the grammar never showed.

Then the caller, so that the writer runs against a real flow rather than one built in memory:

- [x] **`FlowScriptExporter`** loads the flow, orders it, resolves template file names and hands
      the lot to the writer. `RenderAsync` returns the text alone, which is what the phase 9 fix
      loop needs; `ExportAsync` also writes the files. Registered, routed as `Flow.export`, and
      handled by `ExportFlowHandler`.
- [x] **Templates to a folder beside the script**, named after the template rather than by
      content hash. A hash would dedupe identical images but changes whenever one is edited, so
      git would record a delete and an add rather than a modification - throwing away the reason
      the images are loose files at all. `FLOW-FORMAT.md` and `PROJECT.md` section 11 disagreed
      on this; the grammar won, because its stated rationale is the one a hash breaks.
- [x] **Written with LF endings**, because the round trip compares bytes and CI runs on Linux.

Running the writer against a flow loaded from the database rather than built in memory was the
point of doing this before the parser, and it earned its keep: template names came out with
spaces in them, which is friction in every shell and CI file that touches a repository path.
Now hyphenated.

Still open:

- [ ] The CSV template beside it, plus a `.gitignore` entry for the secrets file.
- [ ] A button. `Flow.export` is reachable over IPC; nothing in the UI calls it yet.

### 4.5. The project split

Structural rather than script work, but it landed here: every service written from the parser
onwards is born in the right project rather than moved later.

Nine files carried a hard Windows dependency and those nine were the entire reason a 16,000 line
`Business` targeted `net10.0-windows`. Everything else that touched `System.Drawing` used only
`Rectangle`, `Point` and `Size`, which ship with the framework and are cross platform.

- [x] **`Platform.Windows`**, `net10.0-windows`, referencing `Core` only. No `Windows/` wrapper
      folder - the project name already carries the OS, so the top level is
      `Screen/ Input/ Windowing/ Ocr/ SystemActions/ Native/`, plus `Common/` for native code that
      is identical everywhere, which is where OpenCvSharp goes. No `Linux/` folder until something
      goes in it.
- [x] **Ports to `Core/Ports`** - nine of them, including a new `IWindowService` and `IScreenService`
      that came out of `AppWindowHelper` and `ScreenHelper`. Only interfaces that `Core` and
      `Business` cannot implement themselves; `IFlowValidationService` and `IExecutionEngine` stay
      beside their implementations.
- [x] **`Business` drops to `net10.0`** and loses the `System.Drawing.Common`, `OpenCvSharp4` and
      `SharpHook` package references. It still compiles, so the domain is genuinely GDI free - that
      was the acceptance test for this phase and it passed. Checked a second way, by dropping a
      probe file into `Business` that names a Platform type: it does not compile.
- [x] **Split the machine from the decision.** `AppWindowHelper` was half P/Invoke and half matching
      a `WindowQuery` by process name and title. The matching became `WindowMatcherHelper`, and it
      went to `Core` rather than `Business` because the adapter needs it too - it is the filter
      inside the enumeration. Checked against a hand built list of `SystemWindow` with nothing open,
      including "Notepad" matching Notepad++, an invalid regex, and a catastrophic pattern that
      gives up at its 100ms budget rather than hanging an execution.
- [x] **Closed the two library leaks.** `IInputService` was declared in SharpHook's `MouseButton`
      and `KeyCode`, so a step worker imported an input library to talk to its own port; it now
      speaks `KeyCodeEnum` and `CursorButtonTypeEnum`, which the recording side already used, and
      the mapping moved into the adapter as `SharpHookMap`. `ExecutionScreenshotReader` and
      `RecordingSummaryBuilder` drove `Mat` and `Cv2` directly, bypassing `IOpenCvService`; the port
      gained `GroupSimilar`, `FlattenErasedPixels` and `Downscale` instead.
- [x] **Internals became `internal`** - `NativeCursor`, `Direct3D11Interop`, `OcrLanguageCatalog`.
      Two could not follow: `ScreenMetrics`, because `App` enables DPI awareness at startup before a
      container exists, and `IWindowsGraphicsCaptureService`, because registration lives in `App` by
      house rule and `App` has to name the type.
- [x] **Fixed three folder and namespace lies** that the moves exposed: `Services/OpenCvService/`
      declared `Business.Services.MatchService`, and `AppWindowHelper` and `Direct3D11Helper` both
      declared `Business.Services.ScreenshotService` from `Business/Helpers/`.

Not in this phase, deliberately: `DiscordNotifier` and the Ollama client are external system
adapters rather than platform ones. They sit behind interfaces already and do not constrain the
target framework, so they stay in `Business`.

Linux is a later roadmap item, not a follow up. Under X11 the port is close to one for one; under
Wayland a client cannot enumerate or control another client's windows at all, and Wayland is the
default on Ubuntu, Fedora and RHEL 9. The ports make the question answerable on day one - implement
`IWindowService` and find out - without promising the answer is cheap.

### 4.6. Seams and enforcement

A review of the backend produced ten changes. They land before the parser rather than after it
because six of them are seams the parser and its tests will lean on, and a seam is cheap to cut
until something depends on it being missing.

The review arrived with three ideas from C rather than C#: the opaque pointer, the Law of Demeter
and MISRA C. The first two this codebase already follows without naming them - the `nint` window
handle is an opaque pointer and `IWindowService` documents it as one. MISRA's rules mostly do not
transfer, because no dynamic allocation and single exit exist for reasons C# does not have, but its
method does: define the subset, enforce it with a tool, record every deviation. That is item 5.

- [x] **Extract `AppUnderTestCloser` from `ExecutionEngine`.** `CloseAppUnderTestAsync` and
      `KillProcess` were 65 lines doing a different job from walking a flow, and they were the
      Demeter violation, the platform leak and the untestable part of the class all at once.
      `IWindowService` came off the engine's constructor; the context factory stayed, because
      loading the reachable steps still needs it.

      **Deleted two days later by phase 4.7**, which moved teardown into the flow itself. The
      extraction was still the right move - pulling it out is what made it obvious that the whole
      thing was a flow-level setting doing a step's job.
- [x] **`IProcessService` port.** `Process.GetProcessesByName` and `Kill` are now
      `Platform.Windows/SystemActions/ProcessService`. Process control had been living in the layer
      whose stated purpose is not to touch the machine, and the `net10.0` target did not catch it
      because `Process` is cross platform - which is the point: the architectural rule is stricter
      than the compiler's. It returns `bool` rather than logging, because no adapter in
      `Platform.Windows` takes an `ILogger` and this was not the place to start; the closer logs
      the false.
- [ ] **`CommandRunner` is a Windows adapter sitting in `Business`, and a port will not fix it.**
      Found while doing the item above. It is not only `new Process`: `BuildStartInfo` launches
      `cmd.exe` or `powershell.exe` and reads the console OEM code page, and every entry in
      `CommandPresetCatalog` is a Windows command - `taskkill`, `Get-Process`, `shutdown /s`,
      `Get-Clipboard`. Hiding the process behind a port would leave all of that behind. The real
      question is whether the runner moves to `Platform.Windows` whole and the preset catalog
      becomes per platform, which is a design decision rather than an extraction, and it belongs
      with the Linux work rather than here.
- [ ] **`IProcessService` has no caller.** Phase 4.7 removed the only one. Killing a process is a
      `Run KILL_PROCESS` step today, which goes through `CommandRunner` and `taskkill`. Keep the
      port for the `Close Window` step below, or delete it - but do not leave it unreferenced.
- [x] **`WindowQueryHelper.From(FlowArea)`.** Removed four `flow.AppUnderTestArea.X` reach throughs
      from the engine, and the identical block `AreaPointResolver` carried.

      **Deleted by phase 4.7 as well.** With the closer gone there is one caller left, and a helper
      with one caller is indirection rather than agreement. Inlined back into `AreaPointResolver`.
      Worth remembering as a rule: the case for a helper is two callers that must not drift, so it
      disappears when the second one does.
- [x] **`TimeProvider` instead of `DateTime.UtcNow`.** Eleven sites across eight files, and
      `Business` now holds no clock of its own. One registration, `AddSingleton(TimeProvider.System)`,
      and everything else takes it the way it takes any other dependency.

      The two that mattered were `SearchImageStepWorker` and `SearchTextStepWorker`, which compute
      a give-up time off the wall clock - so every test of a timeout used to cost the timeout.
      `FakeTimeProvider` moves that clock by hand, and fakes `Task.Delay` with it, which is what
      makes the debugger poll below testable as well.

      `ExecutionEngine` lost its `Stopwatch` too. `GetTimestamp` and `GetElapsedTime` are the
      provider's equivalent, so a step's `DurationMilliseconds` is now a number a test can decide
      rather than however long the machine happened to take.

      The two model initializers went too, which is what let the ban below apply to the whole
      solution. `BaseDbModel.CreatedOn` moved into a `TimestampInterceptor`, joining the
      `UpdatedOn` stamp that `AppDbContext` was already doing by hand - two halves of one concept
      that had been handled two different ways, one of them testable and one not. An interceptor
      rather than the existing `SaveChanges` override because the factory is **pooled**, and a
      pooled context may only have the one `DbContextOptions` constructor, so there is nowhere on
      it to put a clock.

      That moves `CreatedOn` from `new` to save: anything reading it in between now sees
      `0001-01-01`. Checked every read first - AutoMapper ignores it and the two query handlers
      read it back out of the database - so nothing does. Verified against a real SQLite database
      with the clock set to 2031: stamped on insert, not re-stamped on update, `UpdatedOn` null
      until modified.

      `RecordedInput` is not an entity and never reaches EF - its `CreatedOn` is the moment an
      input happened, read straight back to work out the gap between keystrokes. It is stamped in
      `InputRecordService.Publish`, the one place every recorded input passes through, so a
      seventh input type cannot forget to do it.
- [x] **Analyzers.** `backend/Directory.Build.props` decides which rules run - `EnableNETAnalyzers`,
      `AnalysisLevel=latest-recommended`, `EnforceCodeStyleInBuild`, and now
      `TreatWarningsAsErrors` with NU1901-1904 exempt, because a CVE published overnight against a
      transitive package should be news rather than a build that will not run.
      `backend/.editorconfig` decides what each rule says, and it is the only one of the two that
      can be scoped to a folder. Both are in the solution's `Solution Items` so they can be opened.

      **350 warnings on day one, 0 now.** No `.globalconfig` of pre-approved exceptions was needed,
      because roughly 250 of the 350 were three rules arguing with a deliberate convention:
      CA1707 wanted the underscores out of `KILL_PROCESS`, CA1711 wanted the `Enum` suffix off
      `FlowStepTypeEnum`, and CA1725 wanted MediatR's `cancellationToken` in place of the house
      `ct` in 95 handlers. Each is off with the reason written beside it, and CA1725 is off only
      under `Business/Ipc/Handlers/` - it caught six real ones elsewhere, five in `WindowService`
      where the adapter was saying `hWnd` while its own port says `handle`.

      The rest were fixed rather than silenced. Two were worth the exercise on their own: a
      `ValueTask` discarded in `InputRecordService`, which may be backed by an object that gets
      recycled under it, and `ExecutionEngine` never disposing the `CancellationTokenSource` it
      replaces on every execution. CA1305, promoted to a warning over `FlowScriptService` because
      a comma decimal separator there writes a file the parser cannot read, found two separate
      culture bugs in one line of the writer.

      A deviation is recorded three ways depending on how wide it is: a severity in
      `.editorconfig` for a rule, a path-scoped section for a folder, and a `[SuppressMessage]`
      with a `Justification` for the single site where forwarding a token to `Task.Run` would let
      a cancelled execution skip writing its own history.
- [x] **`BannedApiAnalyzers`.** The half that makes the architecture a build error rather than a
      convention. Two lists, and which project gets which is the rule:

      `backend/BannedSymbols.txt` goes to all five - `DateTime.UtcNow`, `DateTime.Now` and their
      `DateTimeOffset` twins. Nothing in this solution has a reason to read the wall clock, so that
      one is not about layering at all, it is about a timeout being a value a test can move.

      `backend/Rules/BannedSymbols.txt` goes to every project **except `Platform.Windows`** -
      `Process`, `DllImport`, `LibraryImport`. That exception is the whole statement: driving the
      machine is one project's job, and a solution-wide ban would have banned it in the project
      that exists to do it. Neither symbol is caught by the target framework, because `Process` is
      cross platform and `net10.0` compiles it happily - the architectural rule is stricter than
      the compiler's, and this is where the difference is written down.

      One deviation: `Business/Services/CommandService`, which a port would not fix. Recorded in
      `.editorconfig` beside the item above that has to decide it.

      **`P:` and not `M:`.** `DateTime.UtcNow` is a property, and `M:System.DateTime.get_UtcNow`
      silently matches nothing - the build goes green and the rule does not exist. Caught by
      dropping a file into `Core` that used a banned symbol and checking it failed; the `Process`
      ban fired and the clock ban did not. Worth remembering: a ban that matches nothing looks
      exactly like a ban nobody has broken.
- [ ] **Hold the execution task.** `_ = Task.Run(...)` in `StartAsync` is handed to nobody, so
      shutdown cannot await it and a test can only poll `IsRunning` in a sleep loop. Keep it and
      expose `Task Completion`. Three callers want it: shutdown, the tests, and phase 12's CLI
      runner. The `TODO.md` entry under Execution says `_ = WalkToEndAsync(ct)`, which is the shape
      before the `Task.Run` wrapper; it comes out of `TODO.md` when this lands.
- [ ] **Replace the 50ms poll in `DebugWaitAsync`.** A `SemaphoreSlim` released by `Continue`,
      `StepInto` and `StepOver` removes both the spin and the latency. The comment in the code
      already concedes the design.
- [x] **`WindowHandle` readonly record struct.** The opaque pointer, with the compiler enforcing
      what the comment used to ask for. `IWindowService` speaks `WindowHandle` across all seven
      methods; `WindowService` wraps at its own edge and unwraps to `IntPtr` on the first line of
      each body, so the P/Invokes are untouched. A monitor handle can no longer be passed where a
      window is wanted, and neither can arithmetic.

      `WindowHandle.None` and `IsValid` replace comparing against `IntPtr.Zero`, which read as a
      number and now reads as a question. Verified against live Win32: real handles, real bounds,
      every entry point still resolving.

      It also gives the Linux XID - 32 bits, not a pointer - one place to live rather than every
      call site. CA1725 caught the one slip on the way: naming the adapter parameter `window`
      while the port says `handle`.
- [x] **Synchronise `State` and `_debuggerSignalNextStep`, and leave `_cancellation` alone.**
      Two problems that look like one and are not.

      **The lock is for a race.** Checking whether an execution is going and claiming it are two
      steps, so two IPC messages arriving together could both get past the check and start a walk.
      Two walks, one mouse - the thing the class summary says must never happen. `lock` fuses the
      check and the claim, and that is the only thing it is for. Five lines, and the comment now
      says so, because `_lockObj` names the mechanism and not the reason.

      **`volatile` is for visibility, which is not a race at all.** One thread writes `State`, one
      reads it. The question is whether the pause gate ever sees the write: the JIT may keep a
      field the loop never assigns in a register, and then Continue never resumes the run. A lock
      would also work and would mean taking one twenty times a second on a parked walk.

      **CA1001 was satisfied and then unsatisfied, which is the part worth recording.** Disposing
      `_cancellation` on replacement looked obviously right, and made this item a live bug: `Stop`
      could read the source, `Reset` could dispose it, and `Cancel` would throw on the IPC thread.
      Guarding that cost a lock in `Stop`, a try/catch, a lock in `Dispose` and `IDisposable` on
      the class - 25 lines. Then the question nobody had asked: what does disposing it release?
      The source is never given a `CancelAfter`, and the only token linked to it is disposed by the
      `using` that made it, so the registration is already gone. Nothing. The disposal was removed,
      CA1001 is suppressed on the class with that reasoning, and the engine is 23 lines shorter.

      Owning a disposable field is not owning a resource, and a rule worth turning on is still a
      rule worth arguing with. The run's token is captured while the lock is held, so the
      background walk never reads shared state at all.
- [ ] **`<see cref="Helpers.WindowMatcher"/>` in `IWindowService` names a class that was renamed**
      to `WindowMatcherHelper`. While there: `FlowStepTreeNodeProjection`, `FlowStructureHasher` and
      `VariableTranslator` sit in `Core/Helpers/` without the suffix. If that is deliberate, because
      they own something and so are not helpers, then the folder name is the part that misleads.

Acceptance: a test project can be added afterwards without any further production change. That is
what the six seams are for, and it is checkable - `ExecutionFlowWalker` and the workers are already
there, so if the engine still cannot be driven from a test, one of these was done by halves.

Not in this phase: the tests themselves. The layering, the tooling and its traps are written up
under `## Tests` in `TODO.md`, to plan properly rather than bolt on.

### 4.7. Teardown is steps, and a verdict nobody gave

The flow-level close mode is gone - the column, the enum, the closer and the form field. What
replaces it is smaller and says more.

- [x] **`End Execution` holds children.** One entry in `TreeStepHelper.ContainerTypes` and the rest
      followed: `FlowStepTreeNodeProjection` feeds `Droppable` and `Leaf` to the tree, so the UI
      allows the drop; `TreeStepMoveHelper` and `CreateFlowStepsHandler` read the same rule;
      `FlowScriptWriter` already indents a container's children. Nothing in the frontend needed
      touching.
- [x] **The walk no longer stops at `End Execution`.** It ends when the stack empties, like every
      other flow. The walker clears the stack when it reaches one - the stack holds a sibling for
      every level walked down to get there, so skipping only this step's continuation would have
      carried on with an ancestor's - and pushes that step's own children instead. The cleanup
      written underneath runs, and the walk ends when it does.
- [x] **The verdict is latched** at the first `End Execution` reached, so cleanup below it is
      recorded like anything else but cannot change what the flow already said. A second
      `End Execution` under the first is an error, `END_EXECUTION_UNREACHABLE`, because it reads
      as a decision and is not one.
- [x] **`INCONCLUSIVE`.** Walking to the end with no `End Execution` anywhere used to report
      COMPLETED - which is how a flow whose every check failed came out green, the exact failure
      this model exists to stop. It is now its own status: not a pass, not a failure, nobody said.
      MSTest and NUnit both carry the same outcome; JUnit writes it as `skipped`, which is amber in
      every dashboard rather than green. No inference was added - the engine still does not count
      checks. It reports the structural fact that nothing declared a verdict.
- [x] **Every recorded flow is now amber**, because the recorder deliberately adds no
      `End Execution`. That is the honest reading and the point of the change, but it wants the
      validator warning and the recorder seeding landing near it - both under Execution in
      `TODO.md` - or it reads as a regression.
- [x] **The frontend status enum was missing `FAILED` entirely**, so a failed execution had been
      falling through to the `ERRORED` label since the day both existed. Both added, plus a
      `warning` pill severity for inconclusive.

An exception is the one path with no cleanup: no `End Execution` is in scope, so nothing was
authored to run. It ends the whole execution including the rest of the viewport matrix, which is
what the old close mode was protecting - there is no next pass to keep clean. What it needs instead
is a notification, which is in `TODO.md` rather than here.

Still open, and the reason this is not quite finished:

- [ ] **There is no `Close Window` step.** `AppCloseModeEnum.CLOSE_WINDOW` posted `WM_CLOSE`, which
      lets an application write its session, release its profile lock and remove its own temp
      files. The only way to close something from a step today is `Run KILL_PROCESS`, which is
      `taskkill /F` - a kill, not a close. Removing the close mode without adding the step loses
      the graceful option, so this is a gap the change opened rather than a nice to have. It wants
      a `WINDOW_CLOSE` type beside `WINDOW_FOCUS`, `WINDOW_RESIZE` and `WINDOW_RELOCATE`, and it
      would give `IProcessService` a caller again.

### 5. The parser

- [ ] Parse, validate, then replace, in one transaction. A typo leaves the existing flow untouched.
- [ ] Errors carry a line and column and say what was expected.
- [ ] Read `FlowScriptKeywords` and `Words` backwards rather than restating them, so a keyword can
      never mean one thing on write and another on read.
- [ ] **Round trip is the acceptance test**: export, import, export again, byte identical.

---

## Turning a recording into a test

### 6. The local model

Here rather than at the end, because the fix loop below is the feature the product turns on and it
cannot depend on an api key a customer may never add.

- [ ] A local vision model is always present; a cloud provider is something a customer adds on top.
      `AiProviderEnum` stops being one of three.
- [ ] **OmniParser** reads a screenshot and returns the interactive elements on it - type, label,
      position, state. That is what lets a flow find a button by what it says rather than by what it
      looks like, which is what phase 11 needs when text wraps at a smaller size.
- [ ] Structured output, not prose: `elements`, `screenState`, `notes`. Settled so it is not
      reopened at implementation time.
- [ ] The cloud payload is shown before it is sent, with `label` and `notes` highlighted as the two
      fields where screen text can appear.

Accepted limitation: a good vision model wants a GPU, and a tester's laptop may not have one.
Taking that cost for now rather than designing around it.

### 7. The recorder: the setup form and the gestures

- [ ] **A setup dialog before the first click is captured.** Section 1, the flow name, which must be
      unique. Section 2, what to test: application, browser or new tab, with a **Test** button that
      tries the opening there and then, plus the viewports, added one dialog at a time. Section 3,
      what to do when an execution ends.
- [ ] None of it is a step. It writes `Flow` fields that phase 3 already stores, so it stays
      editable afterwards and the recorder is not the only way to set it.
- [ ] **Ctrl + left click** asks what should be checked at that spot - must this exist, wait until
      it appears, wait until it goes. The position matters, which is why it is the left button.
- [ ] **Ctrl + right click** asks what should happen there instead of a click: run a command, open
      another application, or take the value from a csv column. Position does not matter for any of
      those, which is why it is the right button.
- [ ] **Pause and resume mid recording**, so a tester can type a wrong value on purpose and capture
      how the application rejects it. That is how failure paths get written.
- [ ] **Search mode from the wait.** A quick click becomes `FIND_BEST`; a click after a visible
      pause becomes `WAIT_UNTIL_FOUND` with a timeout derived from the recorded wait plus headroom,
      not a flat default that would make every failing branch on a slow viewport ten seconds slower
      forever. The step carries a code comment saying why: `# recorded after a 4.2s wait`.
- [ ] Polling is deliberately not as fast as possible - screenshot plus template match is real CPU,
      and a tight loop competes with the application being tested.

`RecordingSummaryBuilder` is already written against `IOpenCvService.GroupSimilar` and has no caller
yet. It is what turns "these twenty eight clicks were all the same icon" into something a model can
read without being shown twenty eight pictures.

### 8. Inputs, secrets and the CSV round trip

- [ ] A CSV template generated from the flow's columns, downloaded, filled in, uploaded back.
- [ ] **One execution per row.** `Execution.CsvRowIndex` already exists for it.
- [ ] `FlowCsvColumn.IsSecret` means the value is never stored in any file. A password in a
      repository is a leak.
- [ ] **Stored secrets encrypted at rest** under a master password: `Rfc2898DeriveBytes` to derive a
      key, `AesGcm` to encrypt and authenticate, a random data key wrapped by the derived key so
      changing the password is O(1) and needs no separate verifier. Both live in
      `System.Security.Cryptography` and behave identically on Windows and Linux, which DPAPI does
      not.
- [ ] **CI never decrypts anything.** A pipeline resolves from environment variables; a headless
      execution that can block on a password prompt is a broken CI story. Resolution order is
      `CI argument > environment variable > local secrets file > stored value`.
- [ ] A flow arriving on a new machine has no values, and the app says which inputs are unset before
      the execution starts rather than failing halfway through.

### 9. Happy-path validation and the fix loop

The feature the product turns on.

- [ ] **A freshly recorded flow may not run in CI.** A **Validate** button executes it and stops at
      the first `END_EXECUTION`. Pass marks it ready and unlocks viewports.
- [ ] On failure, the model is given the flow as a script, the application's documentation, the
      customer's own requirements, common issues and their solutions, and scripts of flows that
      already validate.
- [ ] The app applies the proposed fix and executes again, bounded at ten attempts, then asks the
      tester rather than editing forever.
- [ ] When the tester answers, re-execute, take fresh screenshots where needed, regenerate
      templates, and carry on from where validation stopped.

Groundwork already in: `FlowValidationService` with its rules, `FlowCheckHelper`, and the
`GetFlowChecks` tool so a model can ask what a flow verifies without walking the tree by hand.

---

## Making one recording cover every size

### 10. The viewport matrix

- [ ] The loop **above** the walk that produces one execution per viewport per csv row.
      `Execution.ViewportWidth`, `ViewportHeight` and `CsvRowIndex` already exist, which is what
      settles it: the matrix cannot be a step.
- [ ] Sizing targets the app under test window from phase 3, not "the screen".
- [ ] Each viewport passes happy-path validation of its own before it is worth running there.
- [ ] **Sequential.** One mouse, one keyboard, one screen. Three sizes triples wall clock in CI and
      nobody should be surprised by that later.

### 11. Scrolling for what moved below the fold

- [ ] A scroll loop that scans from the current position down and back to the top, rather than
      failing because a button is off screen at a smaller size.
- [ ] Finding an element by its label through OmniParser, which phase 6 already built, so a button
      reading "Add to cart" over two lines still matches.

---

## Shipping results

### 12. The CLI runner and JUnit XML

- [ ] The backend is already a .NET console application, so CI can execute it directly. A pipeline
      needs the execution engine, the flow, the data and somewhere to write results - not the
      editor, the recorder or the overlay.
- [ ] **JUnit XML**, because every CI system already renders it. `<failure>` means the product is
      broken; `<error>` means the harness is.

### 13. The execution bundle

- [ ] A folder: the JUnit XML, the execution steps as JSON, the flow script that ran, and the
      failure screenshots. CI uploads it as a build artifact, which every CI system already does,
      and the app imports it.
- [ ] Later, fetch the same bundle from the CI provider's API instead of the user downloading it. A
      central server that receives results directly is a product decision about becoming a service,
      and should not get decided by accident.

### 14. Git

- [ ] Point the app at a repository folder and a branch. Flows appear in the folder structure the
      repository has; adding a folder in the app adds one in the repository.
- [ ] **Switching branches hides flows, it never deletes them.** The working tree is the truth, the
      database is a cache plus execution history. This only holds together because identity is
      `PublicId`: the same flow on two branches is one family, so its executions accumulate rather
      than fragmenting.
- [ ] The execution records the branch and the commit, and stores the flow script as text. Git is
      the version history; what git cannot answer is "what exactly ran in execution 37".
- [ ] Script changes are visible in the app, including what a model changed when asked to fix
      something, so a tester can see the edit before trusting it.

### 15. Reporting

- [ ] Every check that can fail an execution is a thing worth counting.
- [ ] "Fifty at one viewport, fifty at another, four failures" is a sentence the product should be
      able to say - and so is which checks those four fell into, and which checks have never failed.
- [ ] That is the difference between "the login test is flaky" and "the login test fails at 390x844
      four times in fifty, always on the cart badge check".
