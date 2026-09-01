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

## AI

- [ ] **Decide on an orchestration framework at feature 3.** Asking questions about flows is the
      first feature needing a tool-calling loop, which is the first point where a framework would
      do real work rather than wrap one HTTP call. Until then the loop is ~40 lines and no
      framework earns its place.
- [ ] **Encrypt the stored API key.** It sits in plaintext in AppSettings. Windows DPAPI
      (ProtectedData.Protect) is about ten lines and needs no key management.
- [ ] **Streaming answers.** Explain is one request/response today, so a slow local model shows a
      spinner for 20+ seconds. Needs a broadcast type and partial-message plumbing.
- [ ] **Anthropic as a native provider.** Only OpenAI-compatible endpoints work today, which
      covers OpenAI, Ollama and gateways like OpenRouter, but not Anthropic directly.
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
