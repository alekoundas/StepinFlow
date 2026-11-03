# System Command step

## What System Command does

`SYSTEM_COMMAND` runs a command and branches on whether it ran. It has **Success** and **Failure**
branches, and its output is available to a `CHECK_VALUE` step below it.

The shell is either `CMD` or PowerShell 5.1.

## Presets

Rather than typing a command, pick a preset and fill in its one parameter:

| Preset | Parameter |
|---|---|
| `KILL_PROCESS` | Process name |
| `IS_PROCESS_RUNNING` | Process name |
| `READ_CLIPBOARD` | — |
| `WRITE_CLIPBOARD` | Text to write |
| `CHECK_INTERNET` | Host to reach |
| `SHUTDOWN_IN` | Delay |
| `CANCEL_SHUTDOWN` | — |
| `CUSTOM` | The whole command |

The step stores the preset and its parameter, never the finished command text. Improving a preset
later therefore fixes every flow that uses it.

Every preset returns a meaningful exit code. Some standard tools do not — `tasklist` and `ping`
report success even when the answer is no — so those presets run through PowerShell instead.

`KILL_PROCESS` ends every program of that name at once, nothing saved. To close one window and let
its application save first, use `Close Window` instead.

## Launching an app

`LAUNCH_APP` starts something and carries on without waiting for it to close. It names the area it
**opens**: when that app is already open the launch is skipped, and otherwise the step waits for its
window - 30 seconds, or the step's own timeout - so the next step finds it loaded.

What starts is the area's **Started by**, and the step's **Arguments** go after it: a fresh browser
profile and a window size for a QA test, say. An area started by a link, such as a Steam game's,
takes no arguments (`LAUNCH_ARGUMENTS_ON_LINK`); a Steam game's are set in Steam. A launch with no
area has a **Program and arguments** of its own instead: a script, a file or a URL, started and left.

There is no "only when it is missing" option, because that is always so.

A browser tab is open already when it is in front of a window of its browser, and brought to the
front when it is behind another. When no tab has its address, the browser the area is started by
opens at the address, in a new window.

```
Launch   <[ Start the game ]>   opens <[ Firestone ]>
Launch   <[ Open the shop ]>    opens <[ Browser ]>   with <[ --user-data-dir={{temp}} --window-size={{width}},{{height}} https://www.saucedemo.com ]>
Launch   <[ Open the notes ]>   <[ notepad.exe notes.txt ]>
```

The step's name comes first, because a `CHECK_VALUE` or typed text reads what a command printed by
it, `{{Read it}}`.

## What success means

Success means the command **ran** and its exit code was one of the accepted ones. It does not mean
the output said what you wanted.

Checking the content is a separate job: put a `CHECK_VALUE` step in the Success branch and point it
at this step.

`IS_PROCESS_RUNNING` failing means the process is not running. `IS_PROCESS_RUNNING` on the Failure
branch is how you test for absence.

## Which part of the output becomes the result

The result source can be standard output, standard error, both, or the exit code. That is the value
a `CHECK_VALUE` step below reads.

**Keep only** applies a regular expression to it first, keeping the first capture group.

## Testing a command

The **Test** button runs the command for real and shows the resolved command text, the exit code,
how long it took, and both output streams.

It runs for real, so a destructive preset asks for confirmation first.
