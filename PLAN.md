# Build order

What gets built, and why in this order. `PROJECT.md` says what the product does and how it is put
together; `TODO.md` is everything deferred that is not part of this plan.

Ordering is by dependency first and value second. Where both allowed a choice, the thing that makes
an earlier phase honest wins over the thing that makes a later one possible.

Only open work is listed. A finished item is deleted rather than ticked: what it built is described
in `PROJECT.md`, and why it was built that way is in the git history of this file.

---

## Landed

Everything here is finished: phases 0 to 5.14 by 2026-10-09, Git by 2026-10-25.

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
| 5.14 | Keys pressed, held or released: a Ctrl+click is hold, click, release; whatever is still held is let go as an execution ends; `KEYS_NOT_RELEASED` | §7, §8, §9 |
| 14 | Git: the flows kept as files in a repository of the app's own and written on every save; commits, discard and restore, branches that hide a flow rather than delete it, a remote to push to and pull from; `Platform.Shared` | §2, §11 |
| Tests 0-4 | 365 tests over Core, Business, DataAccess and the architecture | §15 |
| Tests 5 | The engine end to end on SQLite, by 2026-10-26: what it broadcasts, the `Execution` row, how it ends, one execution at a time, the debugger | §15 |

**Next:** phase 7's QA tests, the rest of it - automation flows are in. First the screen sizes and
what happens when an execution ends, asked in the setup. The machine-only test bucket after.

---

## Loose ends in finished phases

### The script

- [ ] **The CSV template beside the script**, plus a `.gitignore` entry for the secrets file.
- [ ] **The description's 5000 characters are the form's limit only.** A script can carry a longer
      one, imports it, and the flow form then refuses to save until it is cut. The data service
      should hold the same limit, so a form and a file meet the same rule.

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

### Portable search

- [ ] **Click through the 5.7 forms in the running app.** Verified by the build, the type check,
      the round trip and fakes, never by hand: capture a template with and without an area, and an
      application area's "Contents scale with".

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
- [ ] **The caption model**, OmniParser's Florence-2 (MIT), for elements with no text on them - an
      icon. The detector is in (`PROJECT.md` §6, §10); Windows OCR names anything with text.
- [ ] Structured output, not prose: `elements`, `screenState`, `notes`. Settled so it is not
      reopened at implementation time.
- [ ] The cloud payload is shown before it is sent, with `label` and `notes` highlighted as the two
      fields where screen text can appear.

Accepted limitation: a good vision model wants a GPU, and a tester's laptop may not have one.
Taking that cost for now rather than designing around it.

### 7. The recorder: a recording and a description become a flow

A recording was turned into steps by a wizard that asked what each recorded action was for; Review
and the plain draft replaced it on 2026-10-14. Settled 2026-10-08, with the details on 2026-10-09: a recording and the tester's description become
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

`Business/Recording` is a pipeline whose folders are its stages, the way `Business/FlowScript` is.
`Setup/`, `Session/`, `ActionBuilder/`, `Elements/`, `Draft/` and `Generation/` are built, with
Review, the plain draft, the model's draft and Check (`PROJECT.md` §6):

```
Business/Recording/
  Setup/        RecordingSetupService     the flow, its main area and a Launch step, before the first click
  Session/      RecordingSessionService   start, pause, resume, stop; the bar's hotkeys; the live list;
                                          RecordingSessionCache holds what it remembers
  ActionBuilder/ RecordingActionBuilder   events into actions, one builder per kind
  Elements/     ClickedElementFinder      per click: place in the main area, the element's rectangle,
                                          the text on it
                ClickedElementGrouper     the clicks that hit the same picture
  Draft/        RecordingDraftBuilder     the reviewed recording into a FlowScriptSchema, one builder
                                          per kind of action; RecordingDraft holds what they share
  Generation/   FlowGenerationService     draft, script, scanner, import; the model's draft too
                FlowGenerationSessionService  the model's draft and Check's revisions in the
                                              background, by broadcast
Business/Ai/ScriptRevision/
                ScriptRevisionService     a script and an instruction in, a script that reads cleanly out
Business/Ai/Vision/
                ElementLocator            phase 6's detection model, with a fallback box
```

**QA tests, after the automation path.** The setup's switch turns them on. Settled 2026-10-25: a
QA test differs from an automation only in being generated by the model, with stage markers and
`End Execution` steps added where they belong; everything else is the same. An automation that
reaches its end is COMPLETED and a QA test that never said is INCONCLUSIVE (`PROJECT.md` §8).
Settled 2026-10-28, in this order; the first needs nothing else decided. Nesting the draft by page
was talked about and left in `TODO.md`, Recording. Since 2026-10-28 a QA test is generated by the
model only, told to add stages and a pass and refused without them (`PROJECT.md` §6). The model
cannot cut a template, so a check it adds is a Check Text or reuses a picture the recording cut -
which is why the checks marked while recording, below, are where a QA test's checks really come
from.

- [ ] **The screen sizes, and what happens when an execution ends**, asked in the setup. The sizes
      become `FlowViewport` rows; what happens at the end becomes steps under `End Execution`,
      because teardown is steps. The only teardown step today is a kill-process command, so the
      `Close Window` loose end above decides whether this ships with kill only.
- [ ] **Checks and substitutes marked while recording.** **Ctrl + left click** marks a check at
      that spot - must this exist, wait until it appears, wait until it goes; the position matters,
      which is why it is the left button. **Ctrl + right click** marks something done there instead
      of a click - execute a command, open another application, or type a value from an input;
      position does not matter for any of those, which is why it is the right button.
      - Both are app settings, Ctrl + click by default and rebindable like the recording bar's
        hotkeys. A real Ctrl+click - multi-select, open in a new tab - is recorded after rebinding
        them. Every hotkey today is a key combination, so this is a new kind of binding: the
        settings picker captures a modifier with a mouse button, and the recorder holds the Ctrl
        back until it sees whether a click follows, as `RecordingHotkeyReader` holds back the Ctrl
        of Ctrl+F9.
      - Nothing is asked while recording, so nothing appears over the application being recorded.
        Review asks what the check is, and what is done instead of the click.
- [ ] **Image or text, per click and per check, in Review.** Image keeps the crop editor. Text shows
      what was read there, editable, and the tester drags a rectangle on the screenshot with the
      same editor; it becomes a `FlowArea` under the main area, named from the text through
      `FlowNameHelper.MakeUnique`. A text search succeeds with its area's centre as its point, so
      for a click the rectangle sits on the button, where for a check it can be loose. Text is read
      with Windows OCR, so this is Windows only until the OCR item in `TODO.md` is decided.
- [ ] **A deliberate wrong value**, replacing "pause to provoke a failure". A QA test should prove
      the application rejects bad input, not only that the happy path works. The tester types the
      wrong value, marks the error message with Ctrl + left click, and in Review marks that typed
      text as a deliberate wrong value. It stays literal text on the line - not an input, and never
      a secret, which keeps no value and would lose it - and the model is told it was on purpose and
      gives it a stage of its own, "Rejects a wrong password", with its check.
- [ ] **A check whose failure can never fail the execution is an info note, QA tests only.** A
      check step whose Failure path reaches no `End Execution` - not in its own Failure branch, not
      in any check below it - changes nothing when it fails, and a suite of those is green and
      proves nothing. It is named at save and at export with a new `INFO` severity beside `ERROR`
      and `WARNING`, which never blocks an execution, as a warning does not. Quiet rather than a
      warning because the draft already ends every Failure branch with `End Execution` failed, so it
      fires only on one somebody removed, or on a flow written by hand.

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
7 built the loop itself: `ScriptRevisionService` sends a script, reads the answer back, and sends
what was refused back to the model (`PROJECT.md` §6).

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

Built 2026-10-24 to 2026-10-25 (`PROJECT.md` §11). Left:

- [ ] If SSH or the browser sign-in becomes necessary, push and pull alone move to the `git`
      program, behind the same port.
- [ ] Later: pointing the app at an existing repository, flows in a folder of it, and folders in the
      app becoming folders in the repository.

### 15. Reporting

- [ ] Every check that can fail an execution is a thing worth counting.
- [ ] "Fifty at one viewport, fifty at another, four failures" is a sentence the product should be
      able to say - and so is which checks those four fell into, and which checks have never failed.
- [ ] That is the difference between "the login test is flaky" and "the login test fails at 390x844
      four times in fifty, always on the cart badge check".

---

## Tests

Layers 0 to 5 are done - see `PROJECT.md` §15 for what exists, how it is wired and what it found.
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
