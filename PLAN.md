# Build order

What gets built, and why in this order. `PROJECT.md` says what the product does and how it is put
together; `TODO.md` is everything deferred that is not part of this plan.

Ordering is by dependency first and value second. Where both allowed a choice, the thing that makes
an earlier phase honest wins over the thing that makes a later one possible.

Only open work is listed. A finished item is deleted rather than ticked: what it built is described
in `PROJECT.md`, and why it was built that way is in the git history of this file.

---

## Landed

Everything here was finished by 2026-10-09.

| phase | what it gave | where it is described |
| --- | --- | --- |
| 0 | One chokepoint for sending screen data to a model; screenshots gated, typed text redacted | `PROJECT.md` §10 |
| 1 | Unique names across steps, areas, points and inputs; `NAME_DUPLICATE` | §9 |
| 2 | `{{variables}}` from inputs, the viewport and earlier steps | §8 |
| 3 | `FlowViewport`, and an app under test on the flow that 5.13 replaced with the main area | §5, §11 |
| 4 | The script writer and exporter | §7 |
| 4.5 | `Platform.Windows` split out; `Business` on plain `net10.0` | §2 |
| 4.6 | `TimeProvider`, analyzers as errors, banned APIs, `WindowHandle` | §2, §14 |
| 4.7 | Teardown as steps under `End Execution`; `INCONCLUSIVE` | §8 |
| 5, 5.5 | Parser, binder and a transactional importer, shaped like a compiler | §7 |
| 5.6 | Feature folders, the `Transport` project, MediatR replaced by a switch | §2, §4 |
| 5.7 | Portable search: `ScalesWith`, DPI on every captured pixel, two match modes | §8 |
| 5.9 | `Go Back` returns only to a step it passed; validator, form lookup and moves agree | §9 |
| 5.10 | The script reads straight into linked rows; one model for scanner, printer, import and export | §7 |
| 5.11 | Every write to a flow goes through its data service; a flow with errors is refused at Run | §9, §14 |
| 5.12 | Every step is a line: Click and Scroll act where the cursor is, Find All's Success once per hit, `nowhere` | §7 |
| 5.13 | The header says what a flow is for and where it works: `Description:`, a `main` area, input defaults; an import returns its warnings and takes template images from memory | §3, §7, §11 |
| Tests 0-4 | 365 tests over Core, Business, DataAccess and the architecture | §15 |

**Next:** the recording update - phase 7 for automation flows, with phase 6's detection model
inside it. Settled 2026-10-08: a recording and a description become a flow with no
question per action, and QA tests come after. The machine-only test bucket and the two engine seams
layer 5 is waiting on follow it. The open decision on how long a step's result lives (`TODO.md`,
Execution) still wants making before layer 5, because the engine tests would pin whichever answer is
live.

---

## Loose ends in finished phases

### The script

- [ ] **The CSV template beside the script**, plus a `.gitignore` entry for the secrets file.
- [ ] **Buttons.** `FlowScript.export` and `FlowScript.import` are reachable over IPC; nothing in the UI calls
      them yet. They share phase 7's script view, and an import lists its errors and warnings (5.13).
- [ ] **The description's 5000 characters are the form's limit only.** A script can carry a longer
      one, imports it, and the flow form then refuses to save until it is cut. The data service
      should hold the same limit, so a form and a file meet the same rule.
- [ ] **`Sub Flow` imports with no target.** The path is parsed and carried as far as the importer
      (`FlowScriptSchema.SubFlowPaths`), which then writes the step with no sub-flow - resolving it
      means reading the `Id:` out of the file it names and matching that, and deciding what a missing
      file does. It lands in the importer, which has the files and the database.

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
- [ ] **`IProcessService` has no caller.** Teardown as steps removed the only one. `Close Window`
      above would give it one, and so would the launch port in `TODO.md` (Execution) - but do not
      leave it unreferenced.

### Portable search

- [ ] **Click through the 5.7 forms in the running app.** Verified by the build, the type check,
      the round trip and fakes, never by hand: capture a template with and without an area, and an
      application area's "Contents scale with".
- [ ] **`Thumbnail`.** Half built - the tree renders it, the projection reads it, and the only
      writer is commented out.

---

## Structure, again

### 5.8. A stage marker is a label

A stage marker divides a flow into parts, and everything that reads a flow afterwards wants to know
which part a step was in: the CI report says "failed in Checkout", the assistant is told it is "the
part of the journey it tests", and a reviewer reads it as a heading.

**Renamed 2026-10-07: `MARKER` is `STAGE_MARKER`**, `MarkerParser` is `StageMarkerParser`, and the
form says "Stage Marker". Rejected names: `SECTION`, because a section is already a header block -
`Areas:`, `Steps:` - to `ScriptLineParser.IsSection` and `SectionParser`: one word, two meanings,
inside one parser; and `CHECKPOINT`, because it promises resumable state this holds none of, and sits
one letter from `CHECK_VALUE`, `FlowCheck` and `GetFlowChecks`, where a check is specifically a step
that can fail the test.

**It stays a label.** A named divider in the tree, `## Sign in` in the script, nothing at execution
time. Which stage a step is in comes from position: the last stage marker above the top-level step
it sits under (`FlowCheckListHelper.StageMarkerOf`), given to the model as `FlowCheck.StageName`.

Rejected 2026-10-07, after it was built and reverted: **making it a container.** The tree would
have shown what a stage holds, at the price of a root-only rule, a printer that flattens and a
parser that re-parents to keep the file flat, two new validation codes, a refused drag and drop, and
a `Go Back` that could no longer reach into an earlier stage - and a container at the root rules out
sub-stages, which is what the reporting wants. Also rejected: **stamping the stage onto
`ExecutionStep`.** The model is given the script, where the markers are, and every execution step
carries its `FlowStepId`, so the stage is already there. The one case a stamp covered, a flow edited
after the execution, is answered by storing the script with the execution (`PROJECT.md`, "what
exactly ran in execution 37").

- [ ] **Sub-stages.** A stage holds stages - "Checkout › Payment" - and the CI report groups by
      them. As a label that is heading levels, the way markdown does it: `##` a stage, `###` a stage
      inside it, the file still flat. Open: where the level lives (a column on the step, or a step
      type per level), the rule for which stage a step is in (the nearest marker above at each
      level), how deep levels go, and how a sub-stage appears in the report - most CI dashboards
      flatten nested test suites, so probably as a test case named "Checkout › Payment".

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
- [ ] **It is also what decides where a template is cut from a recording**, settled 2026-09-29: an
      element's rectangle *is* the crop. Two things travel with the crop or it is not portable - the
      area it was cut inside, and that area's size and DPI, which is what phase 5.7 exists for. A
      model handing back bare pixels would undo it.
- [ ] **Its detection model lands inside phase 7**, settled 2026-10-09: after the recorder's session
      work, because it reads a screenshot of the whole main area, which the session does not take
      yet, and before the draft, which cuts its templates from it. The caption model comes later -
      Windows OCR already names anything with text. Behind one seam,
      `Business/Ai/Vision/ElementLocator`, with a box around the click as the fallback when the model
      file is missing, said out loud when it is, as the docs index should be (`TODO.md`, AI).
- [ ] **ONNX, and the licence before it ships.** OmniParser is a Python project, so it comes in as
      ONNX models on the ONNX Runtime the docs search already ships. Its detection weights are AGPL,
      inherited from YOLO, and its caption weights MIT. GPL-3.0 allows combining with AGPL-3.0, but
      it goes on the licence audit in `TODO.md`, read from the LICENSE file in each weight folder of
      Microsoft's own release.
- [ ] Structured output, not prose: `elements`, `screenState`, `notes`. Settled so it is not
      reopened at implementation time.
- [ ] The cloud payload is shown before it is sent, with `label` and `notes` highlighted as the two
      fields where screen text can appear.

Accepted limitation: a good vision model wants a GPU, and a tester's laptop may not have one.
Taking that cost for now rather than designing around it.

### 7. The recorder: a recording and a description become a flow

Today a recording is turned into steps by a wizard that asks what each recorded action was for.
Settled 2026-10-08, with the details on 2026-10-09: a recording and the tester's description become
a flow with no question per action. The draft is built without a model and is a flow on its own. A
model, when there is one, is given that draft **as a script** with the description and shapes it,
through the same scanner and importer a file goes through. Every result lands in the editor and
never executes on its own - a model that could execute what it wrote would turn a prompt injection
in OCR'd screen text into a moving mouse. Automation flows first; a QA test is the same path with
more added, at the end of this phase.

```
Setup -> Record -> Review -> Draft ---------------------------> Import -> Check -> Editor
                              \-> Model -> Scanner --(clean)--/
                                    ^---- diagnostics, at most 3 attempts
```

`Business/Recording` becomes a pipeline whose folders are its stages, the way `Business/FlowScript`
is:

```
Business/Recording/
  Session/      RecordingSessionService   start, pause, resume, stop; the main area; the screenshot buffer
  Actions/      RecordingActionBuilder    events into actions (exists, moves here)
  Evidence/     ClickEvidenceBuilder      per click: place in the main area, the element's rectangle,
                                          the text on it, the clicks that hit the same picture
  Draft/        RecordingDraftBuilder     actions and evidence into a FlowScriptSchema
  Generation/   FlowGenerationService     draft, script, model, scanner, import
Business/Ai/ScriptRevision/
                ScriptRevisionService     a script and an instruction in, a script that reads cleanly out
Business/Ai/Vision/
                ElementLocator            phase 6's detection model, with a fallback box
```

In this order:

- [ ] **Setup before the first click.** A unique name, the description (5.13), and the main area,
      picked from the windows open now with the one in front listed first, plus a **Test** button
      that finds it and brings it forward. A "this is a QA test" switch, off. The flow and its main
      area are saved in one write, so the name is reserved; Discard deletes it while it has no steps.
- [ ] **The session knows its main area.** At each press it keeps the area's bounds and DPI, and
      the window under the cursor, read in the hook handler itself. That window sorts the click:
      inside the main area, another window of the same application such as a Save As dialog, or
      anything else, which starts left out. Its title is only what Review shows. Today the session
      reads the foreground window's title later, on the drain side, which is wrong whenever the click
      changes the window in front.
- [ ] **The screenshot comes from just before the press.** While recording, the session captures the
      main area's rectangle a few times a second and keeps the last two or three; a press takes the
      newest one captured before it. Today the capture happens after the press reached the
      application, so a template can show the pressed button or the menu the press opened; the hover
      look stays either way, since the cursor was already there. The rectangle rather than the window
      by its handle, because menus, dropdowns and dialogs are often windows of their own.
- [ ] **Nothing done on StepinFlow's own windows is recorded.** Electron sends its process id once,
      when it connects. A press whose window belongs to that process is dropped before anything sees
      it, keys are dropped while one of its windows is in front, and the bar's hotkeys always. Today
      only the last click, Stop, is trimmed.
- [ ] **Pause and resume, hotkeys, a folded live list, and the recording bar.** Hotkeys are
      matched at runtime (`TODO.md`, Hotkeys), with two settings of the bar's own. The live list
      shows actions as they fold - "Typed 15 characters", "Clicked “Invoices”" - rather than every
      key-down and key-up. The bar is an Electron window of its own: frameless, always on top, and
      `setContentProtection(true)`, so it never appears in a screenshot.
- [ ] **Phase 6's detection model**, behind `ElementLocator`, gives each click's element rectangle,
      and Windows OCR reads the text on it. Read text is screen data, so a cloud model gets it only
      through `CanSendScreenDataAsync`.
- [ ] **Review.** Every action with the screen at that moment, the element's rectangle to adjust,
      and what it becomes: kept or left out, typed text as an input. The clicks that hit the same
      picture are grouped - what `RecordingSummaryBuilder` already measures against
      `IOpenCvService.GroupSimilar`, with no caller until now.
- [ ] **The draft, without a model.** `RecordingDraftBuilder` turns actions and evidence into a
      `FlowScriptSchema` - linked rows, the shape a script reads into - with one builder per kind of
      action, as there is a parser per kind of line. A click is three lines: a wait for the element,
      `Move to` it, `Click`. Every wait is `WAIT_UNTIL_FOUND`, never `FIND_BEST`, with a timeout of
      `max(10s, observed × 3)` capped at 60s, always written, and a comment saying why:
      `# recorded after a 4.2s wait` (the reasoning is in `PROJECT.md` §6). A pause is not
      a step. Everything after a search, up to the next search, sits in that search's Success branch.
      Names come from the text on the element, through `FlowNameHelper.MakeUnique`. Every typed run
      is an input with its default (5.13). Templates are the element rectangles, cut inside the main
      area with its size and DPI.
- [ ] **The plain draft ships first**, imported straight into the editor: with no model at all, a
      recording becomes a flow with no questions.
- [ ] **The model shapes the script.** `ScriptRevisionService` is phase 9's loop, built once: it
      sends the script and the instruction, reads the answer with `Scanner.Read`, and sends the
      diagnostics back, three attempts at most, after which the plain draft is saved and the last
      diagnostics shown. The answer is structured - `{ script, notes[], inputRenames }` - and needs
      no tool calling, so any model that returns JSON works. Guard rails sit in code rather than the
      prompt: `Flow:` and `Id:` match the draft, every template the answer names is one the draft
      cut, and inputs are renamed only through the map, so their values and secrets follow.
- [ ] **The Check page.** The script with what the model changed marked, the tree, the model's
      notes, the import's errors and warnings (5.13), and "ask for changes", which sends the script
      back with what the tester wrote.
- [ ] **The screens, and the app around them.** A new shell, built with PrimeReact components and
      PrimeFlex classes first, CSS only where they cannot get there (settled 2026-10-09).
      `features/recording/` replaces `features/wizard/`, and its script view is the one the export
      and import buttons use.
- [ ] **Tests that guard the work come with it**, settled 2026-10-09: `RecordingActionBuilder`
      before the session is rewritten around it, every draft printing and reading back with no
      diagnostics, an import reporting warnings. Everything else is tested once the update is done.
- [ ] Polling is deliberately not as fast as possible - screenshot plus template match is real CPU,
      and a tight loop competes with the application being tested.

Open, each with a recommendation:

- **Keep the per-action wizard?** Retire it: Review, the plain draft and the editor cover what it
  does, and it builds steps in TypeScript, which the draft now does in `Business`.
- **Is "QA test" stored on the flow?** A `Flow.Kind`, automation or test: an inconclusive automation
  is a strange verdict, the CI runner should take tests only, and screen sizes belong to tests.
- **A generated automation's waits** end the execution, failed, with a reason, when they time out.
  An empty Failure branch carries on and clicks blind. With `Flow.Kind`, the end of an automation is
  COMPLETED rather than INCONCLUSIVE.
- **Opening the application** is a `Launch` step the setup writes first, rather than a field on the
  main area. How a launch works on every system is in `TODO.md` (Execution).
- **Typed text** is always an input, named from the field's label, and secret when the label reads
  as a password.
- **The pressed screenshots** go to a temp folder per session, deleted when the tester leaves the
  Check page. Whether a recording outlives the app is in `TODO.md` (Recording).
- **The model's answer** is a whole script; questions back to the tester wait for phase 9.
- **A cloud model without screen content** gets no read text, so it names steps from the description
  and the order alone. Allowed, and the setup page says so.
- **Window steps name an area** rather than repeating a process and title the main area already
  holds.

**QA tests, after the automation path.** The setup's switch turns them on.

- [ ] The screen sizes, and what happens when an execution ends - which becomes steps under
      `End Execution`, because teardown is steps.
- [ ] The model adds stage markers, and `End Execution` checks for what the description says the
      flow is meant to prove.
- [ ] **Ctrl + left click** asks what should be checked at that spot - must this exist, wait until
      it appears, wait until it goes. The position matters, which is why it is the left button.
- [ ] **Ctrl + right click** asks what should happen there instead of a click: run a command, open
      another application, or take the value from a csv column. Position does not matter for any of
      those, which is why it is the right button.
- [ ] **Pause to provoke a failure**: a tester pauses, types a wrong value on purpose, resumes, and
      captures how the application rejects it. That is how failure paths get written.

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

**The script is what the model reads and writes, settled 2026-09-29.** Not database rows. Three
reasons, and the third is the one that decides it: the script shows the structure, where rows make
the model rebuild the tree out of `ParentFlowStepId` and `OrderNumber`; a script line names only what
matters, where a step row is forty columns with most of them empty, and a tool result is re-sent on
every round of the tool loop; and a fix handed back as a script goes through `FlowScriptImporter`,
which parses, validates and replaces in one transaction and refuses a bad file outright. The model's
output uses the same door as a human's, with the same safety, rather than the app applying a change
field by field.

`DbQueryTools` keeps the other half - what *happened*. Scores, outcomes, durations, the closest
template and "which flows use curl" are history and cross-flow questions, none of which the script
holds. Definition from the script, history from the rows.

- [ ] **A freshly recorded flow may not run in CI.** A **Validate** button executes it and stops at
      the first `END_EXECUTION`. Pass marks it ready and unlocks viewports.
- [ ] On failure, the model is given the flow as a script - carrying the flow's own `Description:`
      and each step's `#` comment, which is where the tester's intent lives - a text log of the
      execution, the application's documentation, the customer's own requirements, common issues and
      their solutions, and scripts of flows that already validate.
- [ ] **The execution log is generated from the rows, not stored instead of them.** Phase 15 wants
      "which checks have never failed", and that is a query. The log is a projection for the model.
      It leaves markers out: they execute nothing, so a line saying so is noise.
- [ ] The app applies the proposed fix and executes again, bounded at ten attempts, then asks the
      tester rather than editing forever.
- [ ] **A script the importer refuses goes back to the model with the diagnostics.** `Diagnostic`
      carries a code, a line, a column and a message saying what was *expected*, so the retry says
      which line is wrong and what belonged there rather than "that did not work". Both kinds go
      back: a parse error means the script will not read, a validation error means it reads and the
      flow is wrong. The import is transactional, so a refused attempt leaves the flow exactly as it
      was and the retries cannot corrupt anything.
- [ ] **A question the model cannot answer ends its turn rather than blocking.**
      `UseFunctionInvocation` runs the tool loop inside one request, so a tool that waits on a person
      would hang it. The model returns a structured "I need to know X", the app shows it, and the
      tester's answer goes back as the next message. A schema and a UI state, not new machinery.
- [ ] When the tester answers, re-execute, take fresh screenshots where needed, regenerate
      templates, and carry on from where validation stopped.

Groundwork already in: `FlowValidationService` with its rules, `FlowCheckListHelper`, and the
`GetFlowChecks` tool so a model can ask what a flow verifies without walking the tree by hand. Phase
7 builds the loop itself: `ScriptRevisionService` sends a script, reads the answer back and returns
the diagnostics.

---

## Making one recording cover every size

### 10. The viewport matrix

- [ ] The loop **above** the walk that produces one execution per viewport per csv row.
      `Execution.ViewportWidth`, `ViewportHeight` and `CsvRowIndex` already exist, which is what
      settles it: the matrix cannot be a step.
- [ ] Sizing targets the main area (5.13), not "the screen".
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
- [ ] **Refuse a flow with validation errors, as `StartExecutionHandler` does.** The engine runs
      whatever it is given, so the check is the entry point's job, and the CLI is a second entry
      point. A second caller is the cue to move the check out of the handler into `Business`.
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
