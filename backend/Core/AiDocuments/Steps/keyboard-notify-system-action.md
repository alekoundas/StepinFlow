# Keyboard Input, Notify and System Action

## Keyboard Input

`KEYBOARD_INPUT` types text or presses a key combination. The type is either `TEXT` or
`COMBINATION`.

It types wherever the keyboard focus already is. If focus matters, put a `WINDOW_FOCUS` step or a
click before it.

`KEYBOARD_INPUT` is a leaf and has no branches.

### Press, Hold and Release

A `COMBINATION` step does one of three things with its keys (`KeyboardKeyActionType`):

| | what it does |
|---|---|
| `PRESS` | presses the keys and lets them go, the way a shortcut arrives. What an empty value means. |
| `HOLD` | puts the keys down and leaves them down for the steps that follow. |
| `RELEASE` | lets them back up. |

Hold and Release are two halves of one gesture, the way a click's hold and release are. A
Ctrl+click is three steps: hold `Ctrl`, click, release `Ctrl` - the click stays an ordinary click.
A hold may name a modifier on its own (`Ctrl`, `Ctrl+Shift`), which a press may not.

A hold with no release of the same keys below it is the `KEYS_NOT_RELEASED` warning. Whatever is
still held when an execution ends, however it ends, is let go - so a flow that fails between the
two does not leave Ctrl down on the machine.

The recorder writes the same thing from what it saw: a modifier held over a click or a scroll is
recorded as a hold before it and a release after it. A modifier held only while typing is already
in the text, and one held for a shortcut is already in the shortcut, so neither becomes a step.

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
about the execution; it should not be able to end it.

### Reporting another step's failure

A `NOTIFY` step can report on a step above it. Tick the option and pick the step, and the message
says which step failed and why.

When the reported step is an image search, the templates it was looking for are attached to the
message — so the notification shows what it could not find, rather than only saying that it could
not find it.

The step being reported on has to be one this `NOTIFY` sits below on a failure path. If it stops
being reachable, validation reports `FAILED_STEP_UNREACHABLE`.
