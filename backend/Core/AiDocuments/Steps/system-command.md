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

## Launching an app

`LAUNCH_APP` starts something and carries on without waiting for it to close. It names the area it
**opens**: when that app is already open the launch is skipped, and otherwise the step waits for its
window - 30 seconds, or the step's own timeout - so the next step finds it loaded.

What starts is the area's **Started by**, unless the step has a program and arguments of its own,
which then start instead: a fresh browser profile and a window size for a QA test, say. A launch
with no area is a program, a file or a URL, started and left.

There is no "only when it is missing" option, because that is always so.

```
Launch   opens <[ Firestone ]>
Launch   <[ chrome.exe --user-data-dir={{temp}} --window-size={{width}},{{height}} https://www.saucedemo.com ]>   opens <[ Browser ]>
```

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
