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
- [x] **`AppCloseModeEnum`** - `LEAVE`, `CLOSE_WINDOW`, `KILL_PROCESS`.
- [x] **`ExecutionEngine.CloseAppUnderTestAsync`** runs on the way out whatever the verdict, unless
      the execution was stopped by hand, with `CancellationToken.None` so a cancelled execution
      still cleans up. Teardown cannot be steps at the bottom of the tree, because `END_EXECUTION`
      stops the walk where it stands - a failed pass would leave the application open and the next
      viewport would start against a stale window.
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

Still open:

- [ ] Templates to the folder beside the file, named by content hash. The writer already takes the
      file names; nothing writes the files yet.
- [ ] The CSV template beside it, plus a `.gitignore` entry for the secrets file.
- [ ] An export handler and a button. The writer is a pure function today with no caller.

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
