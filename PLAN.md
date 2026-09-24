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

Four pieces, split the way the writer and exporter are - pure first, database last, so the part
that has to be right can be tested without one.

`ScriptTokenizer` turns text into lines of words. The writer's one rule is what makes it small:
every name is quoted, always, even when it would read fine without, so a quoted run is a single
word whatever is inside it and nothing else needs escaping. Indent is counted in levels of two
rather than spaces, because a parser that accepts three-space indentation accepts a file no
exporter could produce.

`FlowScriptReader` turns lines into a document where every reference is still a name. An
unreadable line is recorded and skipped rather than thrown, so one typo reports one error instead
of hiding the nine below it.

`FlowScriptResolver` turns the names into ids. Its own step because it needs the whole file - a
cursor step can aim at a check written below it - and because needing no database is what lets the
round trip be tested on its own.

`FlowScriptImporter` writes the rows.

- [x] **Parse, validate, then replace, in one transaction.** Everything before the transaction is
      pure, so a file with a typo reports the line and leaves the flow exactly as it was. Proved
      rather than asserted: the probe imports a script with `Find Image` misspelled, gets a refusal
      naming line 25, and then exports the flow again and compares it to what it was before.
- [x] **Errors carry a line and column** and say what was expected rather than what was found -
      "Expected \"5 times\", \"forever\", or \"each match in\" a search" rather than "unexpected token".
- [x] **`FlowScriptKeywords` and `Words` read backwards.** The reverse tables live in the same two
      files as the forward ones, so a keyword cannot mean one thing on write and another on read.
      Longest match first, which is what makes `Move Window` win over `Move` and
      `Wait Until No Image` over `Wait`. A quoted word is never a keyword, so a step named "Click"
      is a name.
- [x] **Round trip is the acceptance test**: export, import, export again, byte identical. Passing
      twice - once purely, on a flow built in memory that uses most of the grammar, and once
      through a real SQLite database with template bytes written to disk and read back.

      **It earned its keep on the first run.** `Scroll` was writing `in match`: the writer used the
      point-target fragment for its `in` clause, which falls through to "match" when a scroll names
      neither a point nor a step, while the format means an area. A line no parser could read,
      found the moment something tried to read one. That is the argument for the round trip being
      the acceptance test rather than a set of examples.

Still open, and honestly rather than quietly:

- [ ] **`Sub Flow` imports with no target.** The path is parsed and carried as far as the importer,
      which then writes the step with a null `SubFlowId` - resolving it means reading the `Id:` out
      of the file it names and matching that, and deciding what a missing file does.
- [ ] **`FlowValidationService` does not run on import.** What the reader and resolver check is
      structural: is that a keyword, is that a condition, does that name exist. The semantic rules -
      a check nothing branches on, a variable nothing defines - are a service away and should run
      before the replace rather than after the next save.

---

### 5.5. The script, shaped like a compiler

Sixteen files flat in one folder, which was the complaint that started the whole refactor
conversation. The pipeline underneath was already right - the parser never resolved a name and the
binder never touched text, which is the separation a compiler is built around and the reason the
round trip could be tested without a database. The names and the folders hid it.

- [x] **Out of `Services/` and into `Business/FlowScript/`.** The first feature to move; the rest
      are in `TODO.md`. `Services/` was a level claiming everything below it was a service, and
      `FlowScriptService/Syntax/Parser.cs` is three words of ceremony claiming a parser is one.
- [x] **The pipeline, named after what it is.**

      | was | is |
      | --- | --- |
      | `ScriptTokenizer` | `Syntax/Lexer` |
      | `FlowScriptReader` + `.Steps` | `Syntax/Parser` + `Syntax/StepParser` |
      | `FlowScriptKeywords` + `Words` | `Syntax/SyntaxFacts` |
      | `FlowScriptDocument`, `ParsedStep` | `Syntax/FlowSyntax`, `StepSyntax` |
      | `FlowScriptResolver` | `Binding/Binder` |
      | `FlowScriptSource` | `Binding/BoundFlow` |
      | `FlowScriptWriter` | `Text/Printer` |
      | `FlowScriptError` | `Diagnostics/Diagnostic` |

- [x] **`StepParser` is a class, not the other half of a `partial`.** The dot in `Parser.Steps.cs`
      was the symptom; the partial was the thing. `Parser` owns the shape of the document -
      sections, indentation, what is a parent of what - and `StepParser` owns the grammar of one
      line. 475 lines and the largest switch in the codebase, now readable and testable against a
      single line of text with no document around it. The helpers they shared moved to where they
      belong: numbers and durations to `SyntaxFacts`, a token's column to `ScriptLine`.
- [x] **`Words` merged into `SyntaxFacts`.** One table, read in both directions, in one file. Two
      files is how a keyword comes to mean one thing on write and another on read.
- [x] **Diagnostics carry a code and a severity.** `FLOW-FORMAT.md` already promised a warning -
      "a long timeout on a branch point is a warning, not an error" - and the importer could not
      express one, because every diagnostic was fatal and anonymous. `IsValid` now means no
      errors rather than no diagnostics, and the code travels out through the DTO so the UI can
      branch instead of matching on message text.

      No span yet. A length would let an editor underline the word rather than point at the line,
      and there is no script editor to do that with; worth adding with the editor, not before.

**Not done: splitting `StepSyntax` from `FlowStep`.** It was first on the list of recommendations
and it came off on closer reading. The argument was that it would delete the id mapping in the
importer, and that was wrong - inserting into a database with generated keys means mapping
synthetic ids to real ones however the model is shaped. What is left of the case is layering: a
syntax node holding an EF entity is not what a compiler would do. True, and the price is
duplicating forty fields and a mapper. Not obviously worth it at this size, so it is a decision to
take deliberately rather than something to slip into a rename.

The parser only fills what the text says; the binder fills everything structural. That was worth
tidying on its own and it was nearly true already.

## Structure

### 5.6. Feature folders and the transport boundary

`Business` carries two parallel trees describing the same features. `Services/` has eight folders,
`Ipc/Handlers/` has twelve, and `Helpers/` belongs to neither. The two trees already agree with
each other, so this is merging them rather than inventing a structure.

#### The helpers

- [x] **`Business/Helpers/` is one feature wearing a generic name.** All three files serve a single
      handler area - `FlowNameLookupHelper` and `FlowStepTemplateSyncHelper` are used by
      `Handlers/FlowStep` and nothing else, `TreeStepMoveHelper` by `Handlers/Flow` and
      `Handlers/FlowStep`. It is not a shared-helper folder and should not survive.

- [x] **The rule: one consumer and it lives with its consumer; two or more features and it is
      shared vocabulary that stays in `Core/Helpers`.** Mechanical, and defensible out loud, which
      matters more than where any single file lands.

      | moves into its feature | stays in `Core/Helpers`, and why |
      | --- | --- |
      | `FlowStructureHasher` → Execution | `TreeStepHelper` - 6 consumers across 5 features |
      | `KeyCombinationHelper` → Execution | `ConditionEvaluatorHelper` - Execution, Notification, FlowStep |
      | `ConditionHelper` → Validation | `FlowNameHelper` - FlowScript, Ipc, Validation |
      | `FlowStepTreeNodeProjection` → Flows | `VariableTranslator`, `TextExtractHelper` - 2 features each |

      Two cannot move whatever their consumer count: `WindowMatcherHelper` is named in
      `Core/Ports/IWindowService` and implemented against in `Platform.Windows`, so it is port
      vocabulary, and `PathHelper` is used by `DataAccess` as well.

- [x] **Pure functions stay static; only what holds a dependency gets injected.** Four of the six
      helpers under discussion are pure - `TreeStepMoveHelper`, `FlowStructureHasher`,
      `FlowNameHelper`, `FlowStepTreeNodeProjection` - and a pure static function is the cheapest
      thing in the repository to test: no fake, no fixture, no container. Wrapping them in one
      injected orchestrator would make every consumer depend on all of them and turn a test that
      needs nothing into a test that needs a six-member fake. It would also be a class whose only
      description is "the flow-step things", which is a misc folder that learned to be injected.

      The two that take an `AppDbContext` - `FlowNameLookupHelper.TakenAsync` and
      `FlowStepTemplateSyncHelper.Sync` - are queries wearing a helper's name. Passing the context
      in as a parameter is honest and testable, so this is a rename and a move rather than a
      redesign.

- [x] **A `Helpers/` subfolder only past three files.** `Helpers` names what a class *is*, which is
      the same mistake as `Services/` at a smaller scale. The better precedent is already in this
      codebase: `Workers/`, `Rules/`, `Providers/`, `Syntax/`, `Binding/` all name a role. So
      `Ai/Helpers` with five files keeps its folder, `FlowValidationService/Helpers` with one does
      not, and a feature with two helpers leaves them flat beside the service.

#### The duplication, extracted

- [x] **`ImageSearcher` and `TextSearcher`, called by both the worker and the handler.**
      `TestImageSearchHandler` (158 lines) and `SearchImageStepWorker` (206) inject the same
      `IScreenshotService` and `IOpenCvService`, both call `CaptureRaw`, both loop
      `step.FlowStepTemplates`, and both build a `TemplateMatchRequest` with the same fields down
      to an identical `MaxMatches = SearchMode == FIND_ALL ? step.MaxMatches : 1`. The search is
      shared; only the reporting differs - the worker records to the cache and returns an
      `ExecutionStep`, the handler builds per-match DTOs for the editor.

      **This is already solved once in the codebase.** `TestRunCommandHandler` is 27 lines because
      `ICommandRunner` exists and `SystemCommandStepWorker` calls the same one. The three handlers
      prefixed `Test*` are exactly the three that shadow a worker, and one of them is already
      right. Apply it to the other two.

      This is the only part of 5.6 that is a refactor rather than a move, so it is the only part
      that wants a test written first.

      **Done, and it grew.** `Business/Searching/` owns the whole look at the screen: the guards,
      the capture, the template loop, the click points and the best score. The worker dropped
      `IScreenshotService` entirely; neither caller names `IOpenCvService` any more.
      `SearchTemplate.From` and `SearchSettings.From` exist once for the row and once for the
      form's dto, side by side, because the worker holds entities and the handler holds unsaved
      form state and neither could call the other's code.

      It found that **the two guards disagreed** - the worker refused on `bounds.Width <= 0`, the
      handler on `haystack.IsEmpty`, with different messages. Both now run, in the searcher.

      The loops differ on purpose and the difference is a parameter, `stopAtFirstHit`: the engine
      stops once anything matches, the editor never does, because a report that skipped the
      templates after the first hit would say they were not there.

      The boundary: the searcher knows the screen, OpenCV and templates, never an execution step,
      the cache, a dto or the poll loop.

      **`TextSearcher` was not built, because there was nothing to extract.** The text pair shares
      capture, OCR, extract and evaluate, and three of those four were already behind
      abstractions - the handler even carried a comment saying it follows the worker's order on
      purpose. The one real difference is that only an execution can translate `{{variables}}`.
      A class calling four things in a row so that two callers call one thing is ceremony.

      **Not verified end to end:** the development database has no flow areas, so nothing reached
      the capture or the match loop. It rests on being a move. Phase 5.7 changes the maths in
      exactly this code, which is why it starts with a test.

#### How thin a handler should be

- [ ] **As thin as the second caller makes it, and not one line thinner.** A handler whose body is
      `return await _service.DoThing(request)` is a wasted file - a renamed method and an
      indirection. Thin is a consequence, not a rule. The test is: who else needs this? One caller
      and it stays in the handler, because extracting is speculation. Two or more callers, or a
      caller that is not a handler at all, and it is feature logic that moves out.

      CRUD stays: nothing but the editor creates a flow step. Search moves: the engine does it too.

#### The transport boundary

- [x] **`Core` referenced MediatR and protobuf-net, and PROJECT.md said it referenced nothing.**
      Section 3's table describes `Core` as "Models, DTOs, enums, ports, pure logic" with no
      dependencies. `Core.csproj` disagrees, because `Core/Models/Ipc` holds thirteen files of
      MediatR `IRequest` messages and the protobuf contracts. That is the transport living in the
      domain, and it is a bigger crack than the handler question it came from.

      **`Core.csproj` now has no `PackageReference` at all.**

- [x] **One `Transport` project for every transport.** Not a new layer - a layer that already
      existed spread across three projects:

      | today | lines |
      | --- | --- |
      | `App/Ipc/` - the two pipes, the dispatcher, the broadcast service | 538 |
      | `Business/Ipc/Handlers/` - twelve feature folders | 4,722 |
      | `Core/Models/Ipc/` - the messages and the protobuf contracts, and why `Core` references MediatR and protobuf-net | 13 files |

      One concern, three projects, and the only reason it is spread that way is that nothing ever
      gave it a home. Collecting it is the change; where the CLI later sits inside it is a folder
      decision, not an architectural one.

      **It is an adapter, the same shape as `Platform.Windows`.** `IIpcBroadcastService` is already
      a port in `Core/Ports` implemented by `App/Ipc/BroadcastService`, so `Transport` referencing
      `Business` and implementing a port `Business` consumes is a pattern this codebase already
      runs.

      **And it keeps a boundary that merging into `App` would lose.** `App` is the only project
      that references `Platform.Windows`. Handlers live in `Business` today, so a handler
      *cannot* call `WindowService` directly - the compiler stops it. Move them into `App` and that
      stops being true. A `Transport` project that does not reference `Platform.Windows` holds the
      line that `Business` holds now.

      `Core/Models/Dtos` stays where it is - both sides use it and it is plain records.

      Done in four steps, each building green on its own: the empty project; `App/Ipc/` in;
      `Business/Ipc/Handlers/` in; `Core/Models/Ipc/` in, splitting into `Transport/Messages/`
      and `Transport/Ipc/Protobuf/`. **The order was forced** - the messages implement
      `IRequest<>` and the handlers referenced them, so moving the messages before the handlers
      would not compile.

      One thing blocked it and was dead code: `DiscordNotifier` carried
      `using ProtoBuf.WellKnownTypes;`, unused, which alone would have kept protobuf-net on
      `Business`. Nothing in the build reported it, which is what led to IDE0005 below.

- [x] **Drop MediatR for a hand-rolled dispatcher.** The licence forced the question - MediatR
      14.2.0 ships a Lucky Penny Software `LICENSE.md` offering RPL-1.5 or a paid commercial
      licence, and this repository is GPL-3.0-or-later and ships as an installer, so neither arm
      sits comfortably. See `TODO.md` under Licences, and AutoMapper with it.

      But the licence is only what raised it. **MediatR is barely being used.** No
      `IPipelineBehavior`, no `INotification`, no `IStreamRequest` anywhere, and the dispatch is
      already hand-written: `IpcDispatcher` is a 99-arm switch on a string, and every arm names its
      command type at compile time -

      ```
      "Flow.create" => await _mediator.Send(new CreateFlowCommand(Deserialize<FlowDto>(payload)), ct),
      ```

      so the only work left for the library is resolving `IRequestHandler<CreateFlowCommand, ...>`
      out of the container. That is one `GetRequiredService` call behind a generic method.

- [x] **The switch stayed, and that took three tries to work out.** The plan here said to
      replace it with a route table, and that was wrong. Three shapes were built:

      | | |
      | --- | --- |
      | `IIpcHandler<TPayload, TResult>` + `[IpcRoute]` on the class | two generic interfaces force `MakeGenericMethod` and a closure per route - 125 lines, most of it getting back to a typed call |
      | `[IpcRoute]` on the method, no interface | much smaller: `JsonSerializer.Deserialize(payload, type, options)` is non generic, and `await (dynamic)` handles `Task<ResultDto<T>>` for any T |
      | **the switch, kept** | 97 arms, `Handler<T>()` and `Payload<T>(request)` doing the repeated work |

      **The switch was never the problem - MediatR was.** The noise in the old arm was
      `_mediator.Send(new GetFlowQuery(...), ct)`: a wrapper type per route, built so a library
      could match on it. 103 of those records existed, **none with more than one field and 29 with
      none at all**. Strip the wrapper and the arm reads as the route it serves:

      ```
      "Flow.get" => await Handler<GetFlowHandler>().HandleAsync(Payload<int>(request), ct),
      ```

      What the switch gives that neither reflective version did: a duplicate action is CS0152, a
      handler whose signature changes breaks the arm that calls it, and F12 reaches the handler.
      The startup check it was supposed to need is the compiler. Registration is 96 explicit
      `AddTransient<T>()` lines in `Program.cs` - the other half of the protocol, and the price of
      no reflection anywhere in `Transport`.

      A route string nobody wrote is a 404 on the first click in development, which is where a
      name mismatch belongs. An enum instead of strings is a later thought; it moves the mismatch
      to `Enum.TryParse` rather than removing it, unless the frontend is generated from it.

- [x] **The deviation deleted itself.** `.editorconfig` had CA1725 off under the handlers because
      MediatR declares `Handle(TRequest request, CancellationToken cancellationToken)` and the
      house spells a token `ct`. Our own signature spells it `ct`, so there is nothing to deviate
      from - 194 suppressed diagnostics that stopped needing suppression. It found one on the way
      out: `StartExecutionHandler` took `CancellationToken _`, which had been invisible inside the
      suppression.

- [x] **One handler, one file.** Six files held 25 classes between them - `AiHandlers.cs` alone
      held seven. 98 files, 98 classes now. Four expression-bodied members became blocks.

- [x] **IDE0005 turned on, and it is not free.** A dead `using` is how a project keeps a package
      it stopped using, which is exactly what `DiscordNotifier` did with protobuf-net. The rule
      only reports at build when `GenerateDocumentationFile` is set, because a `using` can be
      needed by an XML `cref` alone and the compiler will not guess without binding doc comments.
      That flag brings CS1591 with it - 4,116 hits, "missing XML comment on a public member" -
      so it is in `NoWarn`.

      24 dead usings across all five projects, each a change that did not finish:
      `System.Diagnostics` in `ExecutionEngine` from before `TimeProvider`, `System.Globalization`
      in `StepParser` from before the number helpers moved, and
      `using System.Runtime.Intrinsics.Arm;` in `IInputRecordService`, which is autocomplete.

      Two things worth knowing. IDE0005 reports a **run** of consecutive dead usings as one
      diagnostic at the first line, so one pass does not finish the job. And EF migrations are
      exempted by path - EF writes their usings from a template, so `migrations add` would fail
      the build.

      It also brought CS1573 and CS1574, four real ones, two of them stale names this refactor
      had left behind: a cref to `FlowSyntax.Errors` after it became `Diagnostics`, and one to
      `Printer` after it moved to `Business.FlowScript.Text`.

#### The target

```
backend/Business/
  Executions/       engine, walker, cache, history, StepWorkerFactory
                    FlowStructureHasher, KeyCombinationHelper       (from Core/Helpers)
                    Workers/        IStepWorker + 13 workers
  Searching/        ImageSearcher, TextSearcher                     (new, extracted)
  FlowScript/       Syntax/ Binding/ Text/ Diagnostics/ + importer, exporter   (done)
  Flows/            TreeStepMoveHelper, FlowStepTreeNodeProjection
                    FlowStepTemplateSync, FlowNameLookup            (from Business/Helpers)
  Validation/       FlowValidationService, ConditionHelper, Rules/
  Recording/        session service, action builder, summary builder
  Notification/     DiscordNotifier, DiscordSendQueue, NotifyMessageBuilder
  Command/          CommandRunner, CommandPresetCatalog
  AreaPoint/        AreaPointResolver
  AppSettings/      AppSettingService
  Ai/               Providers/ Tools/ Helpers/ + the three services

backend/Transport/  new project: Messages/ Protobuf/ Ipc/Handlers/ (12 folders, thin)
                    Cli/ arrives in phase 12 as a folder beside Ipc/
```

Gone: `Services/`, `Business/Helpers/`, and three files out of `Core/Helpers`.

#### Order

- [x] 4. **Split the `Transport` project out of `App`, `Business` and `Core`**, and drop MediatR
      in the same pass - every handler file was being touched anyway, and `IRequestHandler` was
      the thing being replaced. **Done first**, out of the order below, because the licence made
      it the question that mattered.
- [x] 1. **Extract the searcher.** Image only - see above for why not text.
- [x] 2. **Move the helpers** by the one-consumer rule.

      Done 2026-09-25, with `git mv` so history follows. The consumers were counted again first -
      the handlers had moved to `Transport` since the table was written - and the table held.
      - `Business/Helpers/` is gone, into a new `Business/Flows/`: `FlowNameLookup` and
        `FlowStepTemplateSync` (the `Helper` suffix dropped - they are queries), `TreeStepMoveHelper`,
        and `FlowStepTreeNodeProjection` out of `Core/Helpers`.
      - `FlowStructureHasher` and `KeyCombinationHelper` beside the execution engine,
        `ConditionHelper` beside the validation service - in today's `Services/` folders, which
        step 3 renames.
      - `FlowValidationService/Helpers/` held one file, `FlowCheckHelper`, so the folder went.
      - `Core/Helpers` keeps the seven with two or more features or a port behind them.
      - Along the way: `ConditionHelper`'s arrow members, and a `var` and a multi-line ternary in
        `ExtractSubFlowHandler`.

      Verified by the build, IDE0005 included, and both round-trip probes.
- [x] 3. **Flatten `Services/`** into feature folders - `git mv` and namespaces, no logic touched,
      one commit per feature as `TODO.md` already says.

      Done 2026-09-25. `Business/Services/` is gone: `Ai`, `AppSettings`, `AreaPoint`, `Command`,
      `Executions`, `Notification`, `Recording` and `Validation` sit beside `FlowScript`,
      `Searching` and `Flows`. **Two names are plural** because the singular is an entity:
      namespace `Business.Execution` would hide the `Execution` class from all code under
      `Business.*`, and `Business.AppSetting` the `AppSetting` class - the same reason `Flows` is
      plural. The one path-based rule that named a folder, the `RS0030` exemption for
      `CommandRunner` in `.editorconfig`, moved with it. Done as one change rather than a commit
      per feature, because the consumers' `using` lines span features; each folder is still a
      separate `git mv`, so the renames show as renames.

      Verified by the build, IDE0005 and the banned-API analyzer included, and both round-trip
      probes.

Steps 2 and 3 are verified by the compiler. **Phase 5.7 went first and is done** (2026-09-25);
these two are next.

## Portable search

### 5.7. A flow that survives another PC

**Done 2026-09-24, ahead of the rest of 5.6**, apart from the test that was to come first,
deferred by decision. The changes were verified by the build, the round-trip probes, scratch
programs against fakes and in-memory databases, and the running app where a step says so. The
forms are the one part not yet clicked through.

The point of a flow is that it runs somewhere else tomorrow: another monitor, another DPI, a
browser window that is not maximised. Three things stop that today, and the third one makes the
other two pointless.

#### What is wrong

**One formula for two kinds of application.** `ScaleRatio = areaNow / authoredArea` is right for a
game whose picture grows with its window and wrong for a browser, which reflows: make the window
narrower and the login button stays the same size and moves. Area ratio shrinks the template
anyway and nothing matches. It looked right because in the demo case - maximised, 1080p at 100%
to 4K at 200% - the area ratio and the DPI ratio are both 2.0.

**Pixels without a DPI, in three places.** Templates, `ABSOLUTE_PX` child areas
(`parentBounds.X + area.LocationX`) and `ABSOLUTE_PX` points are all raw physical pixels. A 200px
sidebar defined at 100% is 20% too narrow at 125%. `FlowStepTemplate.AuthoredMonitorDpi` exists
for exactly this, and **nothing has ever written it** - the capture sets width and height and
nothing else. The backend fetches per-monitor DPI for the capture overlay and the frontend never
reads it.

**The flow script drops everything that makes a template portable** - see below.

#### Decided

- [x] **`FlowArea.ScalesWith`: `DPI` or `AREA`**, written `scales with dpi` and
      `scales with area` on the area line. One setting per area, inherited by every template,
      child area and point inside it, because everything in an area obeys the same physics - the
      browser tab does not grow its contents, the game does.

      | `ScalesWith` | apps | ratio |
      | --- | --- | --- |
      | `DPI` | browser, native apps, the OS | `dpiNow / authoredDpi` - the window's size does not matter |
      | `AREA` | games | `min(widthNow / authoredWidth, heightNow / authoredHeight)` |

      **Defaults:** a browser tab and a monitor scale with DPI. An application asks, because a
      native app and a game look identical from outside. A `CUSTOM` child inherits its parent's
      and can override - which is the game inside a browser tab.

      `AREA` uses the **smaller** of the two ratios, because a game whose window
      changes shape keeps its proportions and adds bars - letterboxing - rather than stretching
      its art. When the shape is unchanged the two ratios are equal. One uniform number, so
      `TemplateMatchResult.Scale` stays single. **The first draft had a non-uniform `STRETCH`
      here, with `ScaleX` and `ScaleY`; stretching is the rare case and it came out.**

      `AREA` must not also apply DPI: the area is measured in device pixels, so a higher DPI
      already shows in its width. Both would square the correction.

      Deferred: `RATIO` child areas and points inside a letterboxed area are fractions of the
      window, not of the picture between the bars. That only bites when the window changes shape,
      and template search does not need it - it scans the whole area.

- [x] **`FlowArea.AuthoredDpi`**, so `ABSOLUTE_PX` child areas and points inside a `DPI` area
      scale with the monitor. The template keeps a DPI of its own, renamed
      `AuthoredMonitorDpi` -> `AuthoredDpi`, because it may be captured on a different day and
      monitor than its area was defined on: two moments, two DPIs. **And it gets written** - the
      overlay already has it.

      **Changed in the forms step, 2026-09-24: a point carries its own DPI too** -
      `FlowPoint.AuthoredDpi`. The same two-moments argument: a point is captured when it is
      captured, and an application or monitor area has no pixel numbers of its own, so nothing
      ever stamped a DPI on it for its points to borrow. Every set of pixel numbers now carries the
      DPI it was captured at - a template, a child area's offset and size, a point.

      **The DPI now is the monitor holding the largest part of the area** - the rule Windows itself
      uses to give a window its DPI. `FindMonitorContaining` returns null for an area spanning two
      today, so it needs that rule rather than a null.

- [x] **Nesting stays.** The setting answers how big; a nested area answers where. A game inside a
      browser tab has no window handle of its own, so a fraction of the tab is the only portable
      way to say where it is - and it is exactly the case where the two areas need different
      settings: the tab is `DPI`, the game inside it is `AREA`.

- [x] **The sweep goes: `AllowMultiScale`, `ScaleTolerance`, `ScaleSweep`, `MultiScaleSteps`.**
      It has never run - `allowMultiScale` defaults to false and no form control, script keyword
      or code path sets it. Deleting it changes no behaviour. What it might have covered has a
      computed answer, or is not this phase's problem:

      | case | answer |
      | --- | --- |
      | browser against game | the area's `ScalesWith` |
      | the window changes shape | the smaller of the two ratios |
      | an in-app UI scale slider | not measurable, so it looks different, so it is another template - several templates with none required is already an OR |
      | browser zoom | not this phase - a step to set it, in `TODO.md` |

      The sweep also cost more than CPU: nine attempts at one threshold is nine chances at a false
      positive, it took the first scale that matched rather than the best, and it tried larger
      before smaller. One computed attempt fails legibly - "0.62 at 1.25" says the ratio was wrong.

- [x] **An impossible ratio is an error, not an empty result.** A template scaled larger than the
      screenshot returns an empty outcome today - indistinguishable from "not on screen".

- [x] **`AuthoredFrameWidth/Height` -> `AuthoredFlowAreaWidth/Height`**, and the rule that makes
      the name true: **no capture until the step has an area.** Today the capture falls back to
      the size of the crop itself, so a 50px template can be recorded as its own "area" and scaled
      16x against an 800px one. Touches the entity, both dtos, the capture form, the template list
      label, the sync helper, `SearchTemplate`, `ImageSearcher`, `DbQueryTools` and the AI docs;
      the dtos are the JSON contract, so frontend and backend ship together.

- [x] **A warning for anything positioned in screen coordinates.** A `CUSTOM` area with no parent,
      and a `FlowPoint` with no area - the same problem, found while checking the first. Both
      resolve as absolute screen coordinates and cannot survive another screen layout. A
      validator warning, like the other portability rules.

- [x] **`MonitorUniqueId` -> `MonitorDeviceName`, plus a primary monitor option.** It holds the GDI
      name - `\\.\DISPLAY1` - which renumbers when monitors are plugged and unplugged and may not
      exist on another PC. "Primary" is the portable choice and should be the default.

- [x] **Drop `AuthoredMonitorId`.** Written empty, read by nothing; it was for a warning that was
      never built.

- [x] **The AI stops describing a feature nobody can reach.** `search-image.md` describes the
      sweep and area-ratio scaling, and `DbQueryTools.TemplateSummary` exposes `AllowMultiScale`,
      so the assistant can tell someone to turn on something with no control. Rewrite both with
      the change.

- [ ] Not in this phase: `Thumbnail`. Half built - the tree renders it, the projection reads it, and
      the only writer is commented out - but it is not a portability problem.

#### Match modes

- [x] **Two modes, not six.** Keep `CCoeffNormed` and `SqDiffNormed`. Drop `CCorrNormed` - bright
      flat regions score high against anything, and the code already records it scoring 0.95
      against blank grey - and the three unnormalised forms, which only answer "where is the best
      spot" and cannot take a threshold. Measured against a real 70x71 template: SqDiff 27-32
      million, CCorr 67-74 million, CCoeff +-1 million, so SqDiff never passes and the other two
      always do.

- [x] **Named by intent, and SCREAMING_CASE like every other enum here: `SHAPE` for
      `CCoeffNormed`, `SHAPE_AND_BRIGHTNESS` for `SqDiffNormed`.** The name says the second is the
      stricter of the two, and that is only true because of its 0.95 default below - at 0.80 it is
      looser about shape, not stricter. The name and the default depend on each other. Written
      `match shape and brightness` on a step, and only when it is not `SHAPE`.

      `SHAPE_PLUS_GRADIENT` was considered and rejected: in image processing a gradient is how fast
      brightness changes from pixel to pixel - edges and outlines - which is what `SHAPE` already
      responds to. What the second mode adds is brightness itself.

- [x] **The mode belongs to the step, for every template in it.** `FlowStepTemplate.TemplateMatchMode`
      is dropped.

- [x] **Accuracy belongs to each template.** One variant of an icon can need a looser bar than
      another. The backend always supported it - `FlowStepTemplate.Accuracy` as an override,
      `template.Accuracy ?? settings.Accuracy` in the searcher - and the frontend never set it,
      so every template fell back to the step's one slider. **The first draft of this phase
      dropped per-template accuracy; that was reversed on 2026-09-24.**

      Done in the UI: a slider on each template in the list, and the step-wide slider removed. A
      template saved before this starts on the step's value, which is what it was already being
      matched at, so nothing shifts until someone moves it.

      **Done in the templates step:** `FlowStep.Accuracy` is gone and `FlowStepTemplate.Accuracy`
      is required. Its readers, each now per template:
      - the execution detail's "0.79 against your 0.80" line - `ExecutionStep.BestTemplateId`
        now records which template came closest, and the meter reads that template's accuracy as
        it stands now and names it;
      - the failure text in `SearchImageStepWorker.Detail` and `NotifyMessageBuilder`;
      - `FlowStepFieldCatalog` and `DbQueryTools`, which tell the AI about it;
      - the script, where `accuracy` moves from the step line to the template.

- [x] **Each mode has its own default accuracy: 0.80 for `SHAPE`, 0.95 for
      `SHAPE_AND_BRIGHTNESS`.** A new template starts on it, and changing the step's mode puts
      every template back on the new mode's default rather than carrying a number over. One number cannot mean "82% close" in both, because they are
      different instruments. A UI crop is mostly background and background always agrees, so
      `SHAPE_AND_BRIGHTNESS` scores high whatever the foreground does; `SHAPE` subtracts the background
      first. Measured with the app's OpenCV build, its grayscale conversion and its score
      formula:

      | | `SHAPE` | `SHAPE_AND_BRIGHTNESS` |
      | --- | --- | --- |
      | the right template | 1.000 | 1.000 |
      | letter A, screen blank white | 0.000 | **0.893** |
      | letter A, only B on screen | 0.302 | **0.823** |
      | enabled button, disabled one on screen | **0.954** | 0.752 |

      At 0.80 `SHAPE_AND_BRIGHTNESS` finds an "A" on an empty screen; at 0.95 it rejects all three
      wrong cases. And `SHAPE` clicks a disabled button, which is the reason the second mode
      exists at all.

#### Known limits, recorded rather than fixed

- **Text does not survive a DPI change.** Captured at 100%, searched at 125%: an icon scores 0.93
  in `SHAPE`, a word scores 0.67. A new DPI *re-renders* text rather than scaling it - hinting
  keeps strokes whole pixels, so 125% text is not a 125% copy. No ratio fixes that, and it does not
  matter which side is resized: shrinking the screenshot instead of enlarging the template scored
  the same, 0.930 against 0.931. **Capture icons with Find Image; read words with Search Text.**

- **A single-colour template matches everywhere.** `SHAPE` scores it 1.000 at every one of 28,809
  positions in the probe - OpenCV defines a template with no variance as a perfect match - so the
  step succeeds and clicks the first position in the area. Not acted on: capturing a sensible
  template is the author's and the recorder's job. If it ever bites, this is why. A blank
  *screen* is fine: `SHAPE` scores 0.000 there, which is correctly "not found".

- **Colour is never compared.** Both modes match in grayscale. State changes usually change
  lightness, which `SHAPE_AND_BRIGHTNESS` sees. Where they do not - the same lightness in a different
  hue, like rarity borders in a game - nothing tells them apart. In `TODO.md`.

#### Found while planning: the script throws away what makes a template portable

This is the one to read. Export writes the PNG and its file name, and nothing else about the
template. Import (`FlowScriptImporter`) builds each template with a name, an order, the image and
`IsRequired = true` - and every other field falls to its default:

| lost on the way through | effect **today**, before any of 5.7 |
| --- | --- |
| `ClickOffsetX/Y` | the capture form centres it; import makes it 0,0 - **every imported flow clicks the top left corner of every button** |
| `IsRequired` | forced to true - **a step with three variant templates and none required, an OR, becomes an AND** that needs all three on screen at once |
| step `TemplateMatchMode` | not in the grammar at all - resets to `CCoeffNormed` |
| authored size and DPI | gone - so the scaling key does not survive the one route a flow has to another PC |

**The round trip cannot see any of it.** It compares script to script, and the printer never
prints these fields, so export, import, export produces the same bytes from different rows.

- [x] **Decided: a `Templates:` section in the header**, beside `Areas:` and `Inputs:` - declared
      once, referenced by name from a step, parsed and printed by machinery that already handles
      header sections. The `.sflw` then holds every fact and the PNGs hold nothing but pixels. A
      sidecar per image would be a second source of truth, one a rename orphans and a reviewer
      never sees.

      ```
      Templates:
        "login-form.png"    click 120,40   captured 800x600 at 120dpi
      ```

      - Facts about the picture go in the header: the click point, and the area size and DPI it
        was captured at.
      - Decisions about the search go on the step: `required` on the template clause, because it
        turns an OR into an AND and a reviewer should see that; `match ...` when the mode is not
        the default.
      - The area line carries `scales with ...` and its DPI, or they are lost the same way.

- [x] **The round trip compares rows, not only bytes.** Export, import, export being byte
      identical is exactly what hid this. The imported templates and areas are compared field by
      field with the originals, ids and timestamps aside.

#### Settled 2026-09-24

Every question this phase raised has an answer above: how template facts travel (the header),
what an area's size follows and the default per type (`ScalesWith`), letterboxing (the smaller
ratio), which monitor's DPI (the largest part), the modes and their names, accuracy per mode, per
template mode (dropped) and accuracy (kept, per template), browser zoom and colour (`TODO.md`), and the single-colour
template (a known limit).

#### Order

- [ ] **A test around the searcher first** - deferred by decision. A `FakeOpenCvService` or a
      fixed `RawImage`, and a test that imports a script and inspects the template rows.
- [x] **Match modes**: `SHAPE` and `SHAPE_AND_BRIGHTNESS`, the other four gone, the form's
      dropdown of raw enum names replaced by a two-option control with a description per mode.
- [x] **Accuracy per template in the UI**, the per-mode defaults, and the reset on a mode change.
- [x] **Templates**: drop `TemplateMatchMode`, `AllowMultiScale`, `ScaleTolerance`,
      `AuthoredMonitorId`; rename `AuthoredFrame*` to `AuthoredFlowArea*` and
      `AuthoredMonitorDpi` to `AuthoredDpi`; take the sweep out of `OpenCvService` and
      `TemplateMatchRequest`; drop `FlowStep.Accuracy`, make the template's required, and fix its
      readers listed above. One migration.

      Done 2026-09-24. `accuracy` now follows its template in the script -
      `template "a.png" accuracy 0.85` - and is the first template fact the round trip guards:
      the printer prints it, so an importer that dropped it would change the bytes. Written before
      any template, it is a diagnostic - `ACCURACY_WITHOUT_TEMPLATE` then, `CLAUSE_WITHOUT_TEMPLATE`
      since the script step gave `required` the same rule. The AI docs caught up in their own
      step, below.
- [x] **Areas**: `ScalesWith`, `AuthoredDpi`, `MonitorDeviceName` with empty meaning the primary
      monitor. One migration.

      Done 2026-09-24. `ScalesWith` is nullable - null inherits - and neither it nor `AuthoredDpi`
      is written or read yet: the forms and the resolver steps do that. "Empty is primary" is
      written where it is used - the resolver and the monitor capture - after a shared helper for
      it was tried and dropped. The monitor picker lists **Primary monitor** first with an empty value,
      which makes it the default for a new area. Verified against the running app: an empty name
      resolved to the primary monitor, and an unplugged one failed naming the device.
- [x] **The resolver and the searcher maths**: the effective `ScalesWith`, the largest-part
      monitor for the DPI, `ABSOLUTE_PX` children and points scaled by DPI, one uniform template
      ratio, an error for an impossible ratio.

      Done 2026-09-24. `AreaResolution` now carries the area's effective `ScalesWith` and the DPI
      of the monitor holding most of it, so the searcher gets both from the resolution it already
      had. A child's pixels follow its **parent's** physics - it sits in the parent - while its own
      setting governs what is inside it: a game canvas in a browser tab moves with the tab's DPI
      and scales its templates with its own size.

      Verified through the running app with a generated template and nothing read off the screen,
      on a 120 DPI monitor:

      | case | result |
      | --- | --- |
      | DPI area, template authored at 1 dpi | "scaled by 120.00 it is 4800x2400" - the impossible-ratio error, reporting the DPI |
      | DPI area, authored at 120 | ratio 1, searched |
      | DPI area, authored area size set | ignored |
      | AREA area, authored at 10x10 | scale 216 = min(3840/10, 2160/10) - the smaller ratio |
      | AREA area, authored at 1 dpi | ignored |
      | `ABSOLUTE_PX` child 100x50 at (10,20), authored at 60 dpi | 200x100 at (20,40) |
      | the same, no DPI recorded / parent AREA | 100x50 at (10,20), unscaled |
      | `ABSOLUTE_PX` point (100,40), area authored at 60 dpi | (200,80) |

      **Until the forms step writes DPI, nothing scales.** Every existing template and area has
      `AuthoredDpi` 0, which leaves it as it is - correct on the machine it was made on, and what
      the forms step is for. Before this step a template scaled by the area's width, which was
      wrong for every browser.
- [x] **The script carries every fact**: `Templates:` in the header, `required`, `accuracy` and
      `match` on the step, `scales with` and the DPI on the area line, and the row-comparing
      round trip. Fixes the live bug on its own.

      Done 2026-09-24. Every row of the loss table above now survives:

      ```
      Areas:
        "Browser"     window process "chrome.exe"   scales with dpi   at 120dpi
        "Game"        inside "Browser"   ratio 0.10 0.20  0.80 0.70   scales with area
        "Screen"      monitor primary

      Templates:
        "login.png"           click 150,20   captured 922x648 at 120dpi

      Steps:
      Find Image  "Find login"   template "login.png" accuracy 0.97 required   match shape and brightness   in "Browser"
      ```

      - The header's facts are joined to the step's templates by file name in the **binder**, the
        stage that already turns names into things. The parser reads text; the binder resolves.
      - A template with no header line, or none with a `click`, is **centred on its picture** by
        the importer, read from the png's IHDR and rounded half up as the capture form rounds. A
        hand-written flow then clicks the middle of a button rather than its corner.
      - **No `accuracy` takes the mode's default**, 0.8 or 0.95 - applied once the whole line is
        read, because `match` may come after the templates it governs.
      - Written only when not the default: `match`, `required`, `scales with`, the DPI, the
        captured size. Accuracy and click are always written.
      - **Found on the way: the area line could only say "window" or "inside".** A monitor area
        exported as `window process ""` and came back as an application with no process; a root
        `CUSTOM` area did the same. Now `monitor primary`, `monitor "\\.\DISPLAY2"` and
        `on screen   offset x y  size w h`. Still not in the grammar: `BROWSER_TAB` (exported as its
        window, and the resolver does not support it yet) and `UseClientArea = false`.
      - New diagnostics: `CLAUSE_WITHOUT_TEMPLATE` (was `ACCURACY_WITHOUT_TEMPLATE`, now also
        `required`), `MATCH_MODE_UNKNOWN`, `AREA_ARGUMENT_UNKNOWN`, `TEMPLATE_MALFORMED`,
        `TEMPLATE_DUPLICATE`.

      **Verified** by both probes. `ScriptRoundTrip` prints a flow using every new clause, reads it
      back byte identical, and checks the defaults on a hand-written script: no accuracy under
      `match shape and brightness` is 0.95, `required` binds to its own template, a header line
      without a click leaves it to the importer. `ScriptRoundTripDatabase` now loads the rows before
      and after the import and compares every scalar field of every area, point and template, ids
      and timestamps aside, and imports a hand-written script to check a 41x20 png is clicked at
      21,10. It also prints, without failing, which step fields did not survive: only the cursor
      button, null on one side and `LEFT_BUTTON` on the other - see `TODO.md`.
- [x] **The forms**: capture needs an area, DPI written, `ScalesWith` defaulted or asked by area
      type, the screen-coordinate warning.

      Done 2026-09-24.
      - **Capturing a template needs the step's area**, resolved on screen now: no area, an area
        not saved yet, or one not on screen, and the form says which instead of opening the
        overlay. The drag is confined to the area, because a template from outside it can never
        be found. The template records the area's size and DPI - the preview now returns the DPI
        of the monitor holding most of it, which is the DPI the searcher compares against.
      - **Every capture of pixels writes the DPI beside them**: a child area's offset and size from
        its parent's preview, a point's offset from its area's. Typed numbers keep whatever DPI
        they had.
      - **`FlowPoint.AuthoredDpi`**, one migration - see the decision above. The script writes it
        on the point line: `"Menu"  inside "Browser"   offset 40 8   at 120dpi`.
      - **"Contents scale with"** on every area: *Screen DPI* or *Area size*, plus *Same as "…"*
        for a region inside another. Null is stored for "the default" and shown as what it means -
        the parent's for a region inside one, DPI otherwise. An **application shows nothing picked
        and will not save until someone says which**, because a native app and a game look the
        same from outside.
      - **`SCREEN_COORDINATES`**, a warning on each step that uses a region with no parent, a
        region inside one, a point measured from nothing, or a point inside such a region. Its own
        rule class, `FlowPortabilityValidator`, so the validator now takes the flow's areas and
        points - id, name and what each sits in.
      - Found on the way: **`ExtractSubFlowHandler` copied an area with half its fields** - no
        ratio width or height, no window match, no monitor. A region extracted into a sub-flow
        came out with no size. It copies every field now.

      Verified: backend build, frontend type check and lint, both round-trip probes with a point
      at 120dpi, and a scratch program against fakes - no database, no screen - for the resolver
      (a point at 60 dpi on a 120 dpi monitor moves from 100,40 to 200,80 whatever its area's DPI)
      and the warning (four steps flagged, the region inside a monitor not). **Not verified in
      the running app**: the forms themselves need clicking through - capture a template with and
      without an area, and an application area's "Contents scale with".
- [x] **The AI docs and `DbQueryTools`.**

      Done 2026-09-24. What the assistant is told now matches what the app does:
      - **`search-image.md`**: accuracy per template with per-mode defaults, the two match modes
        and when each is right, scaling decided by the area's `ScalesWith` with one computed
        attempt and no sweep, the impossible-ratio error, templates with nothing recorded, and the
        known limits - text across a DPI change, a single-colour template, colour. It also states
        the `IsRequired` gap plainly: only Test now honours it (`TODO.md`).
      - **`areas.md`** gains what an area's contents scale with and screen coordinates; **`points.md`**
        the point's own DPI and the warning; **`validation-messages.md`** `SCREEN_COORDINATES`;
        **`troubleshooting.md`** the wrong `ScalesWith` as the likely cause of a far-off score on
        another machine, and `SHAPE_AND_BRIGHTNESS` for a template found in the wrong state.
      - **A score is read against the template that produced it.** The prompts said "that step's
        accuracy", which no longer exists. `SearchImageStepWorker`'s failure message now names the
        closest template - `no template matched, closest play hover at 0.86 - play at 0.80,
        play hover at 0.90` - so the execution page, Explain and the tools all carry it, and
        `GetRunSteps` returns `ClosestTemplate` and `ClosestTemplateAccuracy`. Read against the
        wrong template, 0.86 would pass a 0.80 bar it never had to clear.
      - **`DbQueryTools` says what portability turns on.** An area comes back with its parent, its
        effective `ScalesWith` - `DPI (from Browser)` for an unset child, `DPI (default)` otherwise
        - its DPI, and `primary` for the primary monitor; a point with the area it is measured
        from, its mode and DPI; a template with its click point. Before, a point said only X and Y,
        so the model could not tell an anchored point from a screen coordinate.
      - Vocabulary: "frames" became screenshots in `how-execution-works.md` and
        `troubleshooting.md`, and "run" execution where it named the thing.

      Verified by a scratch program against an in-memory database: the four areas, two points,
      two templates and one execution step came back as above, and every edited document still
      splits into chunks under the embedding model's 2000-character cap.

- [x] **Required templates in an execution.** Found while making the script carry `required`:
      Test now honoured it and an execution did not - any template found was a success.

      Done 2026-09-25. The rule is in `ImageSearcher`, once, and Test now takes its verdict from
      there instead of keeping a copy. None required: any one is enough and the first hit ends the
      search. Some required: every required one is looked for before anything is decided, a missing
      one leaves no hits whatever else matched, and the failure names it - `required login button
      not found`. The waits follow: `WAIT_UNTIL_FOUND` until all required are there,
      `WAIT_UNTIL_NOT_FOUND` until one is gone. Verified against a fake matcher in five cases.

A migration per schema step rather than one at the end, so the app starts after each step:
`Program.cs` runs `Migrate()`, and a model without its migration does not. Existing data is not
preserved - no conversion SQL; the database is recreated.

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

---

## Tests

There is no test project yet. The order below is what makes one cheap, and the first two layers
need no production change at all. Do it after phase 5.6, so nothing is written against a namespace
that is about to move.

### The shape

- [ ] **Coverage per project, not one number.** `Core` near total, because it is pure decisions and
      has no excuse. `Business` decision code high - walker, printer, parser, binder, validators,
      searchers. `Business` orchestration moderate and by integration test. `Platform.Windows` near
      zero **on purpose**, because it is the part that touches the machine. That table is the
      architecture diagram, and being able to say why a number is what it is beats reporting a high
      one.

      A blanket 100% target buys tests for property getters and catches nothing. The number worth
      putting in the readme is a **mutation score over the walker and the script**, because that is
      a claim about whether the tests detect defects rather than about which lines ran. For
      reference: Google publishes no org-wide gate and treats 60% as acceptable, 75% commendable,
      90% exemplary; most enterprises that gate at all gate 70-80% **on changed code**; mature
      teams use a ratchet - coverage may not decrease - rather than a threshold. 100% is a
      safety-critical standard, and there it means MC/DC coverage, not line coverage.

### The tooling, settled

| | | |
| --- | --- | --- |
| runner | **xUnit v3** | each test project is a real executable rather than a dll in a shared runner - which matters here, with OpenCvSharp, SharpHook, ONNX Runtime and Tesseract all carrying native bits |
| assertions | **Shouldly** or **AwesomeAssertions** | FluentAssertions v8 moved to a paid licence for commercial use; AwesomeAssertions is the community fork of v7 |
| port fakes | **hand-written** | a `FakeInputService` recording tuples shows a reader what the ports bought; `Received()` shows them a mocking library. NSubstitute for anything incidental |
| database | **SQLite `:memory:`** | not `UseInMemoryDatabase` - it is not relational, enforces no foreign key, and would leave the `DeleteBehavior` cycle-breaking unverified |
| clock | `Microsoft.Extensions.TimeProvider.Testing` | `FakeTimeProvider`, already proven in `probes/TimestampInterceptor` |
| snapshots | **Verify** | for the script |
| architecture | **ArchUnitNET** | NetArchTest is semi-dormant |
| coverage | coverlet collector + ReportGenerator | and **Fine Code Coverage**, the free VS extension - worth trying early, see below |
| mutation | **Stryker.NET** | occasionally, scoped to one project |

- [ ] **`backend/Tests/`**, capital T to match its neighbours - this repository has no `src/`, and
      `App`, `Business`, `Core` are all capitalised. Solution folder `/Tests/`. Decide the case now
      and never change it: `core.ignorecase` is true on Windows, so a later rename produces a repo
      that is one case locally and the other on GitHub.

- [ ] **Test projects inherit `backend/Directory.Build.props`** - analyzers, `TreatWarningsAsErrors`
      and both `BannedSymbols.txt` files. That is mostly wanted: a test reaching for
      `DateTime.UtcNow` or `Task.Result` should fail like anything else. A few rules will need
      relaxing, and the lever is a second `Directory.Build.props` in `backend/Tests/` that imports
      the one above it and then overrides a short, deliberate list - better than `.editorconfig`
      sections for MSBuild-level settings, and it keeps the relaxations in one visible file.

- [ ] **The database, per test, not shared.** A single seeded database serving every test is the
      classic trap: tests that pass alone and fail together, a seed file that becomes a god object
      nobody dares delete a row from, and failures where the first question is whose change broke
      it. Seeding what a test needs *inside the test* is just the arrange step and is fine.

      The one trick that catches everybody: with SQLite `:memory:` the database lives inside the
      connection, and EF opens and closes connections as it pleases. Open one, hold it for the
      test, and pass **that connection object** to `UseSqlite` rather than a connection string, or
      the second query reports no such table.

      `Database.Migrate()` rather than `EnsureCreated()`, so the fifteen real migrations run and a
      broken one fails a test instead of a user. If the migration cost ever shows up across a few
      hundred tests, the answer is a template database copied per test, not a shared mutable one.
      Measure before bothering.

- [ ] **Real PNGs in `Tests/Assets/`.** Template matching cannot be meaningfully faked, and a
      read-only fixture directory has none of the problems a shared database has.

### Layer 0 - `Core/Helpers` and the four pure ones

- [ ] Pure static functions, no fakes, no fixtures, nothing to arrange but an argument.
      `ConditionEvaluatorHelper`, `VariableTranslator`, `KeyCombinationHelper`, `FlowNameHelper`,
      `TreeStepHelper`, `FlowStructureHasher`, `WindowMatcherHelper`, `TextExtractHelper`, and the
      150 lines of drag-and-drop rules in `TreeStepMoveHelper`. An afternoon, and it gets the
      solution wired, `dotnet test` green and the `Directory.Build.props` question settled before
      anything harder starts.

### Layer 1 - architecture tests

- [ ] Highest value per line in the whole suite **for this repository specifically**, because
      separation of concerns is the thesis. `Business` does not reference `Platform.Windows`;
      nothing outside `Platform.Windows` names OpenCvSharp or SharpHook; `Core` depends on nothing
      but the framework; `Business` does not reference MediatR.

      **Two of those fail today** - `Core` carries MediatR and protobuf-net - which is the point.
      They turn PROJECT.md section 2 from a claim into a build failure, and they are what stops
      phase 5.6's boundary from quietly eroding afterwards.

### Layer 2 - the workers

- [ ] Testable with no production change, because of the ports. The entry toll is a
      `FakeExecutionCache` (twelve members - let it throw `NotImplementedException` on the ones no
      test needs yet) and a step builder; every worker after the first costs five lines.

      Cheapest first: `CheckValueStepWorker` has no constructor dependencies and needs two cache
      members. Then `PassThrough` and `EndExecution` - the second is where the verdict latches.
      Then the one-port workers, then `Cursor` and `Window`, then the searchers once 5.6 has pulled
      them out. `NotifyStepWorker` is the odd one: it takes an `IDbContextFactory` directly, so it
      is an integration test rather than a unit test. Twelve workers talk to ports and one talks to
      the database - that is the suite telling you something about the design before a line is
      written.

- [ ] **`WaitStepWorker` will not cooperate, and that is a finding.** Twenty-three lines, no
      constructor, and two things a test cannot work with: `await Task.Delay(ms, ct)` means a test
      of a five-second wait takes five seconds, and a `private static readonly Random` means "picks
      a value between min and max" is not assertable. `TimeProvider.Delay` and `Random.Shared` fix
      both, one line each. The second is worth doing regardless - a shared `Random` instance is
      documented as not thread-safe and corrupts silently rather than throwing, which does not bite
      on a single-threaded walk today and is not a property to rely on.

### Layer 3 - the flow script

- [ ] **The round trip, promoted from `probes/`.** Export, import, export again, byte identical,
      in both forms - pure, and through a real database with template bytes written to disk and
      read back. It found the `Scroll ... in match` writer bug on its first run.

- [ ] **Snapshots beside it, with Verify.** The round trip proves `A == B`; it cannot prove either
      is right. If the printer wrote a line the parser read back the same wrong way, the round trip
      stays green. A `.verified.txt` is a real flow script committed in the repository that a human
      read once and approved - simultaneously the fixture and the clearest documentation the format
      will ever have, because the build fails when it drifts. Table tests over `SyntaxFacts` in
      both directions belong here too.

### Layer 4 - `ExecutionFlowWalker`

- [ ] 400 lines of pure decision - no database, no screen, no mouse. The most intricate code in the
      repository and the cheapest to test, with nothing to refactor first. Build a tree in memory,
      feed results, assert the sequence of step names, and the assertion reads like the flow it
      describes. Loop pass counting, the `_maxSubFlowDepth` cap, `TakeMatchRepeats` handing out a
      FIND_ALL search's second and third hit, `_depthByStepId` dropping results as the walk leaves
      a subtree.

- [ ] **Property-based testing, optional, and worth a conversation before it is started.** The
      walker is the one place in the repository where random garbage is a legitimate input, because
      the walker executes nothing - it takes a step and *a result handed to it* and returns the
      next step. So a generator produces a structurally valid tree and a random sequence of
      outcomes, and neither has to mean anything. A generated flow could never be executable, and
      never needs to be: whether a check can read a step that ran is `ExecutionCacheService`'s
      problem, and the walker never asks.

      The properties are all structural - the walk terminates, the stack ends empty, every returned
      step exists in `StepsById`, no loop runs more passes than its count. CsCheck then shrinks a
      failing 40-node tree to the smallest one that still fails, which is the feature; the random
      generation is only how it gets there. This is also why the technique fits nowhere else -
      generating a flow that *means* something is hard, so the engine will never be tested this
      way.

### Layer 5 - `ExecutionEngine`

- [ ] Phase 4.6 cleared most of what was in the way: `TimeProvider` is injected so a timeout is a
      value a test moves rather than a wait it sits through, and process killing went behind a port
      so a test cannot kill the browser. Two remain, both open in 4.6:

      1. The background task is unobservable. `_ = Task.Run(...)` in `StartAsync` leaves a test
         polling `IsRunning` in a sleep loop. Hold it and expose `Task Completion` - phase 12's CLI
         runner needs it anyway.
      2. `DebugWaitAsync` polls two fields on a 50ms `Task.Delay`, so a pause-and-step-over test
         pays 50ms per decision. A `SemaphoreSlim` released by Continue / StepInto / StepOver
         removes both the spin and the latency.

      Then: SQLite, fake ports, a recording broadcast, and assert the event sequence and the
      `Execution` row. A second `StartAsync` refusing rather than queueing, `Stop()` landing as
      STOPPED and not ERRORED, a worker throwing leaving `errorStepId` on the right step, a
      breakpoint inside a stepped-over subtree parking there anyway, and a walk that reaches the
      end with no `End Execution` recording INCONCLUSIVE.

### The other three probes

| probe | becomes |
| --- | --- |
| script round trip, pure | layer 3 |
| script round trip, database | layer 3 |
| timestamp interceptor | a `DataAccess` test - `CreatedOn` on insert, `UpdatedOn` on modify, neither re-stamped |
| P/Invoke entry points | **stays a probe, or becomes a traited test** |

- [ ] **`PInvokeEntryPoints` needs a live desktop session** - it reads the foreground window - so it
      can never run in headless CI. Give it `[Trait("Category", "Desktop")]` and filter it out of
      the default run **from day one**, rather than discovering it when CI first goes red. Real
      OpenCV against the files in `Tests/Assets/` belongs in the same bucket: `Platform.Windows`
      near zero on purpose does not mean zero, it means the handful that need a real machine are
      quarantined and labelled.

### Running it

- [ ] **Coverage is a property of a run, not of a test.** It instruments the production assemblies
      and records which lines executed, so running one test correctly reports almost everything
      uncovered. There is no per-method coverage button anywhere.

- [ ] **Try Fine Code Coverage early.** Visual Studio's built-in coverage is Enterprise only -
      Community and Professional have none, and Test Explorer's Run All gives pass/fail and nothing
      else. Fine Code Coverage is the free extension that fills the gap: it hooks the test run, so
      Run All does produce coverage, into its own window and the editor margin. Rider has it built
      in, every edition.

- [ ] **Script the CLI pair anyway**, because it is also what CI runs, and put it beside the
      existing npm scripts:

      ```
      dotnet test --collect:"XPlat Code Coverage"
      reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coveragereport -reporttypes:Html
      ```

      Use `coverlet.collector`, not `coverlet.msbuild` - the older msbuild integration interacts
      badly with multi-targeting.

- [ ] **Stryker occasionally, scoped, and never in the build.** It runs the suite once per mutant,
      so it is minutes to hours. Point it at the walker and `Business/FlowScript/` once their tests
      exist, read the surviving mutants as a to-do list - each one is a sentence saying nothing
      checks this line - fix what it finds, and put the number in the readme. Then leave it alone
      for a quarter.

### Not now

- [ ] **Vitest and Testing Library** for the frontend. The `zod` schemas and the pure TS helpers
      are testable with Vitest alone if a cheap win is ever wanted.
- [ ] **No Playwright.** This product *is* a UI automation tool; driving it with another automation
      harness is the wrong shape. The real end-to-end test for StepinFlow is a flow script that
      tests StepinFlow, which belongs with phase 12.
