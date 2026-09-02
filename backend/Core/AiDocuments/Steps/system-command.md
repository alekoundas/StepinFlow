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
