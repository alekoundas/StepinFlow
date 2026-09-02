# Keyboard Input, Notify and System Action

## Keyboard Input

`KEYBOARD_INPUT` types text or presses a key combination. The type is either `TEXT` or
`COMBINATION`.

It types wherever the keyboard focus already is. If focus matters, put a `WINDOW_FOCUS` step or a
click before it.

`KEYBOARD_INPUT` is a leaf and has no branches.

**The text is stored as plain text.** A flow that types a password holds that password in the
database in readable form.

## System Action

`SYSTEM_ACTION` performs one machine action: `LOCK_WORKSTATION`, `SLEEP_PC`, `MONITOR_OFF` or
`MONITOR_ON`.

It is always a leaf: no output, no branches, no timeout. These actions do not report a meaningful
result, so there is nothing to branch on.

## Notify

`NOTIFY` posts a message to Discord through a webhook.

Bots are configured in Settings: a name, the webhook URL, the bot name to post as, an avatar, and a
rate limit in seconds. The webhook URL is the credential, so it is never written to a log.

Sending happens off the flow's thread, and a send that fails never stops a flow. A notification is
about the run; it should not be able to end it.

### Reporting another step's failure

A `NOTIFY` step can report on a step above it. Tick the option and pick the step, and the message
says which step failed and why.

When the reported step is an image search, the templates it was looking for are attached to the
message — so the notification shows what it could not find, rather than only saying that it could
not find it.

The step being reported on has to be one this `NOTIFY` sits below on a failure path. If it stops
being reachable, validation reports `FAILED_STEP_UNREACHABLE`.
