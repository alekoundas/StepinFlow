# Build order

What gets built, and why in this order. `PROJECT.md` says what the product does and how it is put
together; `TODO.md` is everything deferred that is not part of this plan.

Ordering is by dependency first and value second. Where both allowed a choice, the thing that makes
an earlier phase honest wins over the thing that makes a later one possible.

Only open work is listed. A finished item is deleted rather than ticked: what it built is described
in `PROJECT.md`, and why it was built that way is in the git history of this file.

---

## Landed

Everything here was finished by 2026-09-25.

| phase | what it gave | where it is described |
| --- | --- | --- |
| 0 | One chokepoint for sending screen data to a model; screenshots gated, typed text redacted | `PROJECT.md` §10 |
| 1 | Unique names across steps, areas, points and inputs; `NAME_DUPLICATE` | §9 |
| 2 | `{{variables}}` from inputs, the viewport and earlier steps | §8 |
| 3 | The app under test as the flow's root area; `FlowViewport` | §5, §11 |
| 4 | The script writer and exporter | §7 |
| 4.5 | `Platform.Windows` split out; `Business` on plain `net10.0` | §2 |
| 4.6 | `TimeProvider`, analyzers as errors, banned APIs, `WindowHandle` | §2, §14 |
| 4.7 | Teardown as steps under `End Execution`; `INCONCLUSIVE` | §8 |
| 5, 5.5 | Parser, binder and a transactional importer, shaped like a compiler | §7 |
| 5.6 | Feature folders, the `Transport` project, MediatR replaced by a switch | §2, §4 |
| 5.7 | Portable search: `ScalesWith`, DPI on every captured pixel, two match modes | §8 |
| Tests 0-4 | 345 tests over Core, Business, DataAccess and the architecture | §15 |

**Next:** the machine-only test bucket, then the two engine seams that layer 5 is waiting on, then
phase 6. The open decision on how long a step's result lives (`TODO.md`, Execution) wants making
before layer 5, because the engine tests would pin whichever answer is live.

---

## Loose ends in finished phases

### The script

- [ ] **The CSV template beside the script**, plus a `.gitignore` entry for the secrets file.
- [ ] **Buttons.** `Flow.export` and `Flow.import` are reachable over IPC; nothing in the UI calls
      them yet.
- [ ] **`Sub Flow` imports with no target.** The path is parsed and carried as far as the importer,
      which then writes the step with a null `SubFlowId` - resolving it means reading the `Id:` out
      of the file it names and matching that, and deciding what a missing file does. It lands in
      `Binding/Binder.cs`.
- [ ] **`FlowValidationService` does not run on import.** What the parser and binder check is
      structural: is that a keyword, is that a condition, does that name exist. The semantic rules -
      a check nothing branches on, a variable nothing defines - are a service away and should run
      before the replace rather than after the next save.
- [ ] **Decide whether `StepSyntax` stays a `FlowStep`.** A syntax node holding an EF entity is not
      what a compiler would do. The price of splitting is forty duplicated fields and a mapper, and
      it would not remove the id mapping in the importer - inserting with generated keys needs that
      whatever the model looks like. Not obviously worth it at this size; a decision to take
      deliberately rather than slip into a rename.

### The engine

- [ ] **Hold the execution task.** `_ = Task.Run(...)` in `ExecutionEngine.StartAsync` is handed to
      nobody, so shutdown cannot await it - the walk unwinds while the host tears down, and the
      final history flush can fail against a disposed context factory - and a test can only poll
      `IsRunning` in a sleep loop. Keep it and expose `Task Completion`. Three callers want it:
      shutdown, the tests, and phase 12's CLI runner.
- [ ] **Replace the 50ms poll in `DebugWaitAsync`.** A `SemaphoreSlim` released by `Continue`,
      `StepInto` and `StepOver` removes both the spin and the latency. The comment in the code
      already concedes the design.

### Commands and windows

- [ ] **`CommandRunner` is a Windows adapter sitting in `Business`, and a port will not fix it.**
      It is not only `new Process`: `BuildStartInfo` launches `cmd.exe` or `powershell.exe` and
      reads the console OEM code page, and every entry in `CommandPresetCatalog` is a Windows
      command - `taskkill`, `Get-Process`, `shutdown /s`, `Get-Clipboard`. Hiding the process behind
      a port would leave all of that behind. The real question is whether the runner moves to
      `Platform.Windows` whole and the preset catalogue becomes per platform, which is a design
      decision rather than an extraction, and it belongs with the Linux work. Until then it is the
      one recorded `RS0030` deviation in `.editorconfig`.
- [ ] **There is no `Close Window` step.** The flow-level close mode posted `WM_CLOSE`, which lets
      an application write its session, release its profile lock and remove its own temp files. The
      only way to close something from a step today is `Run KILL_PROCESS`, which is `taskkill /F` -
      a kill, not a close. Removing the close mode without adding the step lost the graceful option,
      so this is a gap rather than a nice to have. It wants a `WINDOW_CLOSE` type beside
      `WINDOW_FOCUS`, `WINDOW_RESIZE` and `WINDOW_RELOCATE`.
- [ ] **`IProcessService` has no caller.** Teardown as steps removed the only one. Keep the port for
      `Close Window` above, or delete it - but do not leave it unreferenced.

### Portable search

- [ ] **Click through the 5.7 forms in the running app.** Verified by the build, the type check,
      the round trip and fakes, never by hand: capture a template with and without an area, and an
      application area's "Contents scale with".
- [ ] **`Thumbnail`.** Half built - the tree renders it, the projection reads it, and the only
      writer is commented out.

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

Today a recording is taken with a hotkey and turned into steps afterwards by a wizard that asks
what each recorded action was for. This phase moves the questions to where the answers are known.

- [ ] **A setup dialog before the first click is captured.** Section 1, the flow name, which must be
      unique. Section 2, what to test: application, browser or new tab, with a **Test** button that
      tries the opening there and then, plus the viewports, added one dialog at a time. Section 3,
      what to do when an execution ends - which becomes steps under `End Execution`, because
      teardown is steps.
- [ ] None of it is a step. Sections 1 and 2 write `Flow` fields that phase 3 already stores, so
      they stay editable afterwards and the recorder is not the only way to set them.
- [ ] **Ctrl + left click** asks what should be checked at that spot - must this exist, wait until
      it appears, wait until it goes. The position matters, which is why it is the left button.
- [ ] **Ctrl + right click** asks what should happen there instead of a click: run a command, open
      another application, or take the value from a csv column. Position does not matter for any of
      those, which is why it is the right button.
- [ ] **Pause and resume mid recording**, so a tester can type a wrong value on purpose and capture
      how the application rejects it. That is how failure paths get written.
- [ ] **Search mode and timeout from the recording.** Every recorded check waits -
      `WAIT_UNTIL_FOUND`, never `FIND_BEST` - with a timeout of `max(10s, observed × 3)` capped at
      60s, and the step carries a code comment saying why: `# recorded after a 4.2s wait`. The
      reasoning, and why a quick click is not evidence of anything, is under Recording in
      `TODO.md`.
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
      editor, the recorder or the overlay. It arrives as `Transport/Cli/`, a folder beside `Ipc/`.
- [ ] **JUnit XML**, because every CI system already renders it. `<failure>` means the product is
      broken; `<error>` means the harness is.
- [ ] **CI for the repository itself** - build, the analyzers, and the backend tests with the
      desktop-only ones filtered out.

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

Layers 0 to 4 are done - see `PROJECT.md` §15 for what exists, how it is wired and what it found.
What is left, in order:

### The machine-only bucket

- [ ] **Real PNGs in `Tests/Assets/`, against the real OpenCV.** Template matching cannot be
      meaningfully faked, and a read-only fixture directory has none of the problems a shared
      database has. The match-mode measurements in `PROJECT.md` §8 - the letter A on a blank
      screen, the disabled button - become the first cases, so the table is a test rather than a
      claim.
- [ ] **`probes/PInvokeEntryPoints` becomes a traited test** in `Platform.Windows.Tests`, and
      `probes/` is deleted. It needs a live desktop session - it reads the foreground window - so it
      can never run in headless CI. `[Trait("Category", "Desktop")]`, filtered out of the default
      run **from day one**, rather than discovered when CI first goes red. `Platform.Windows` near
      zero on purpose does not mean zero; it means the handful that need a real machine are
      quarantined and labelled.

### Layer 5 - `ExecutionEngine`

- [ ] Waits on the two engine items above: the held task and the semaphore. Then: SQLite, fake
      ports, a recording broadcast, and assert the event sequence and the `Execution` row. A second
      `StartAsync` refusing rather than queueing, `Stop()` landing as STOPPED and not ERRORED, a
      worker throwing leaving `errorStepId` on the right step, a breakpoint inside a stepped-over
      subtree parking there anyway, and a walk that reaches the end with no `End Execution`
      recording INCONCLUSIVE.
- [ ] **`SystemCommandStepWorker`**, the one worker without a test - see `TODO.md`, it wants the
      `IMapper` out first.

### Optional

- [ ] **Property-based tests for the walker, and worth a conversation before they are started.**
      The walker is the one place in the repository where random garbage is a legitimate input,
      because it executes nothing - it takes a step and *a result handed to it* and returns the next
      step. So a generator produces a structurally valid tree and a random sequence of outcomes, and
      neither has to mean anything. The properties are all structural - the walk terminates, the
      stack ends empty, every returned step exists in `StepsById`, no loop runs more passes than its
      count. CsCheck then shrinks a failing 40-node tree to the smallest one that still fails, which
      is the feature. Check its licence first.
- [ ] **Stryker occasionally, scoped, and never in the build.** It runs the suite once per mutant,
      so it is minutes to hours. Point it at the walker and `Business/FlowScript/`, read the
      surviving mutants as a to-do list - each one is a sentence saying nothing checks this line -
      fix what it finds, and put the number in the readme. Then leave it alone for a quarter.
- [ ] **Try Fine Code Coverage.** Visual Studio's built-in coverage is Enterprise only, and Test
      Explorer's Run All gives pass and fail and nothing else. Fine Code Coverage is the free
      extension that hooks the test run, so Run All produces coverage into the editor margin. Rider
      has it built in, every edition.

### Not now

- [ ] **Vitest and Testing Library** for the frontend. The `zod` schemas and the pure TS helpers
      are testable with Vitest alone if a cheap win is ever wanted.
- [ ] **No Playwright.** This product *is* a UI automation tool; driving it with another automation
      harness is the wrong shape. The real end-to-end test for StepinFlow is a flow script that
      tests StepinFlow, which belongs with phase 12.
