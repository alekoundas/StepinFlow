
# StepinFlow help

StepinFlow builds and runs GUI automation. A **flow** is a list of **steps** that click, type,
search the screen for an image, read text off it, run commands, and call other flows.

## Where to start

- **Concepts** — [Flows](concepts/flows.md), [Areas](concepts/areas.md), [Points](concepts/points.md),
  [Steps and branches](concepts/steps-and-branches.md), [How execution works](concepts/how-execution-works.md)
- **Step reference** — one page per family, in [steps/](steps/)
- **Guides** — [Recording a flow](guides/recording-a-flow.md), [Debugging a run](guides/debugging-a-run.md)
- **Reference** — [Validation messages](reference/validation-messages.md), [Settings](reference/settings.md)
- **When something goes wrong** — [Troubleshooting](troubleshooting.md)

## What StepinFlow is not

It does not read a program's internals. It works from what is on screen: pixels it matches against
saved images, text it reads with OCR, and windows it finds by process name and title. Anything it
cannot see, it cannot act on.
