# TODO

Deferred work, to pick up near the end of the app. Add here rather than in conversation, so nothing
is lost between sessions.

## Execution

- [ ] **Clear the execution cache when the walk ends.** `ForgetFrom` only runs inside `Pop()`, and
      when the stack empties `Pop()` returns `null` without calling it — so the last path's values
      sit in the cache until the next run resets it. Clear on end of walk, and log if anything was
      still there, so "a finished flow leaves no cached values" is a property you find out about
      when it breaks rather than one you hope for.
- [ ] **Keep-last-X-runs retention.** Nothing prunes `Executions` / `ExecutionSteps` today. Needs a
      setting and a sweep, or the history grows without bound.
- [ ] **Hold the walk Task so shutdown can await it.** `_ = WalkToEndAsync(ct)` discards it, so on
      app shutdown the run unwinds while the host tears down and the final history flush can fail
      against a disposed context factory. Caught and logged, but the last batch is lost.
- [ ] **Startup check that every `FlowStepTypeEnum` has a worker** or is on an explicit structural
      list (SUCCESS, FAILURE, LOOP, GO_TO, SUB_FLOW). Today an unmapped type silently falls through
      to `PassThroughStepWorker`, so a new step type with a forgotten registration runs as a no-op
      that reports success.
- [ ] **Poll interval jitter** for `WAIT_UNTIL_FOUND` searches.
- [ ] **Downscale matching** for template search.
- [ ] **Dry-run mode** that logs resolved coordinates instead of clicking.
- [ ] **Details dialog for an image search's screenshot.** Panel 3 shows the frame but nothing on
      it. Wants a button opening `FlowStepImageSearchTestDialogComponent`, which already draws
      boxes, click points and per-template scores, plus one addition: the best candidate that
      *failed* the accuracy threshold, drawn differently from a real hit, so "0.79 against your
      0.80" is visible. FIND_ALL shows every successful position.
      The match data must be recorded **during the run**, not recomputed on open: the matcher works
      on raw BGRA and the saved screenshot is JPEG q60, so re-matching returns scores the run never
      computed - and the step's accuracy or templates may have been edited since. Recording is
      nearly free (`IOpenCvService.Match` already returns boxes and scores, the worker discards
      them); the only new work is a second pass at threshold 0, `MaxMatches: 1`, for the best
      rejected candidate, run only when nothing matched and the screenshot is being kept.
      Undecided: where it goes - a JSON sidecar beside the .jpg, a column on `ExecutionStep`, or an
      `ExecutionStepMatch` table. Whatever it is, extract the per-template loop
      `TestImageSearchHandler` already has so the two paths cannot drift again.
- [ ] **Remove the `Success` / `Failure` static factories from `ExecutionStep`.** They build an
      entity, which reads as though an execution step is something a worker mints rather than a row
      the engine fills in and the history writes. Workers should set `Outcome`, `Location` and
      `Message` directly, or the shape should move to a type that is not the EF entity.

## AI

- [x] **Orchestration framework: no.** Settled at feature 3. `Microsoft.Extensions.AI` already
      ships `UseFunctionInvocation()`, which is the ask / call tool / feed back / ask again loop as
      middleware, with `MaximumIterationsPerRequest` as the guard. Nothing left for a framework.
- [ ] **See what actually goes to the model.** `.UseOpenTelemetry(configure: x => x.EnableSensitiveData = true)`
      in the same builder chain as `UseFunctionInvocation`, exported to the standalone Aspire
      Dashboard (`docker run mcr.microsoft.com/dotnet/aspire-dashboard`). Gives a trace per request
      showing the messages sent, the tool schemas, every tool call and its result, timing and token
      counts - the Postman equivalent for this. `.UseLogging()` is the zero-infrastructure version
      if that is too much. Sensitive data is on, so keep it to development.
- [ ] **Make the model ask better questions, and give it less to read.** Feature 3 works but is
      naive at both ends. In rough order of return:
      *Asking* - few-shot examples of question to tool call in the system prompt (biggest win for a
      small local model, no extra round trip); entity grounding, ie put the actual flow names in the
      system prompt so it matches real ones; synonym and token normalisation, because `SearchSteps`
      is `LIKE '%text%'` and "Google Chrome" misses `chrome.exe`; conversational condensation, to
      collapse "and what about that one?" plus history into a standalone question.
      *Answering* - return a count with the top N ("showing 50 of 320") so it narrows rather than
      guesses; offset and paging so it can ask for more; more narrowing parameters so it filters
      server side; aggregate tools such as `CountStepsByType(flowId)`, which answers "what does this
      flow mostly do" in 18 rows instead of 500. Aggregates are the biggest lever, because a tool
      result is re-sent on every later round of the loop.
- [ ] **Say it out loud when the docs index is unavailable.** If the embedding model is missing or
      ONNX will not load, help answers fall back to whatever the tools can reach and the assistant
      is quietly worse at questions about the app itself. The AI settings panel should say so, and
      it is worth deciding whether that also deserves a banner across the top of the app - a
      degraded feature nobody is told about is the failure mode this codebase keeps rejecting.
      This matters more than it would otherwise, because the model ships as `Content` - a loose
      file beside the exe rather than sealed into the assembly - so a user can simply delete it.
      Check the file on startup and report it, rather than finding out when somebody asks a
      question and gets a worse answer for no stated reason.
- [ ] **Encrypt the stored API key.** It sits in plaintext in AppSettings. Windows DPAPI
      (ProtectedData.Protect) is about ten lines and needs no key management.
- [ ] **Streaming answers.** Explain is one request/response today, so a slow local model shows a
      spinner for 20+ seconds. Needs a broadcast type and partial-message plumbing.
- [ ] **Anthropic as a native provider.** Only OpenAI-compatible endpoints work today, which
      covers OpenAI, Ollama and gateways like OpenRouter, but not Anthropic directly.
- [ ] **Redact what tool results send to a cloud provider.** The screen-text rule has exactly one
      chokepoint today, `ExecutionPromptHelper`. Tool calling returns rows from everywhere, and the
      exposure is wider than OCR text: `FlowStep.KeyboardInputText` holds, in plaintext, whatever a
      flow types - a password typed into a login form is in there. `RunCommand`, `ConditionText` and
      `NotifyMessage` are the same shape. `AppSetting` holds the OpenAI key and `DiscordBot` holds
      webhook urls, which is why those two columns are left unselected rather than redacted.
      Wants one pass every tool result goes through, keyed on provider exactly as the existing rule
      is, so a cloud model gets `(hidden)` where a local one gets the value.
- [ ] **No way to clear a stored API key.** A secret reads back as empty, so emptying the box
      compares equal to what is saved and nothing is written. Switching provider is the only way
      to stop using a key, and the key stays in the table. Wants an explicit Clear next to the
      field rather than a rule about when an empty value counts as an edit.

## Notify

- [ ] **Read the failed step's result.** `NotifyMessageBuilder` builds every line from saved
      configuration, so it says what a step was set up to do rather than what came back. The engine
      now keeps that on the step itself — `ExecutionStep.Message` and `Value` — which would say
      considerably more.
- [ ] **Distinguish an unresolved area from a failed condition** in the message. Both currently read
      as a plain failure.

## Hotkeys

- [ ] **Hotkey-to-command matching.** The capture and the settings exist; nothing yet maps a stored
      combination to Continue / Step Into / Step Over / Pause / Stop at runtime.

## Frontend

- [ ] **Flow edit / view / clone routes are broken.** `FlowFormPage` reads a `formMode` route param
      that no route declares, and `const flow = null` means it never loads the flow it is editing.

## Codebase sweep

- [ ] **Target-typed `new()`.** Pre-existing uses were left in files not authored during the
      execution-engine work. House style is the full `new TypeName()`.
