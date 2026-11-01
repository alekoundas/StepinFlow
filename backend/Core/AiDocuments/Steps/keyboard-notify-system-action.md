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
A hold may name a modifier on its own (`Ctrl`, `Ctrl+Shift`), and so may a press: `Win` on its own
opens the Start menu.

A hold with no release of the same keys below it is the `KEYS_NOT_RELEASED` warning. Whatever is
still held when an execution ends, however it ends, is let go - so a flow that fails between the
two does not leave Ctrl down on the machine.

The recorder writes the same thing from what it saw: every Ctrl, Alt or Win is a hold where it went
down and a release where it came up, with the keys between pressed on their own - Ctrl+C is hold
`Ctrl`, press `C`, release `Ctrl`. A Shift held only while typing is already in the text's capitals,
so it is not a step.

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

### Including another step's result

A `NOTIFY` step can include the result of a step it sits under, on either side: **Include the
result of**. The message then names that step and says how it went, above the message itself:

| Step | What the message says |
|---|---|
| `SEARCH_IMAGE` | the template found, its accuracy and the accuracy it needed; when nothing was found, the closest one |
| `SEARCH_TEXT` | the text it read, or why it did not match |
| `CHECK_VALUE` | the value and the condition |
| `SYSTEM_COMMAND` | the exit code, and what it printed |
| anything else | whether it succeeded, and the engine's reason |

It is read from what this execution recorded, not from how the step is set up, so it says what
actually came back. Only a step it sits under has a result to read: a result is kept for the steps
under its step and dropped once the flow moves past it. If the notify stops being under it,
validation reports `REPORTED_STEP_UNREACHABLE`.

The script line does not carry the pick. A `{{Name}}` in the message is another way to put a text
search's read or a command's output into it.
