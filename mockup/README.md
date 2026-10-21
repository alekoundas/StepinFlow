# The mockup

The screens StepinFlow is being built towards, one file per board. The live canvas, where the
boards are laid out and can be clicked through, is the Design artifact
[StepinFlow recorder redesign](https://claude.ai/artifact/KD5kCxkafbq8PV5HB8JrCi); these files are
a copy of it, kept beside the code so a change can point at the board it follows.

They are `.dc.html` Design Component files: plain HTML with `{{ }}` holes and a small logic class
at the bottom. They read as they are, but they render only inside the canvas, which supplies the
`support.js` runtime each one loads. `canvas.json` is the canvas's index: where each board sits and
its title.

The canvas is the source. When it changes, copy its files here again rather than editing these.

| Board | File |
| --- | --- |
| 1 · Flows | `Main.dc.html` |
| 2 · New flow | `NewFlow.dc.html` |
| 3 · Create with AI: describe it, pick the app | `AiSetup.dc.html` |
| 4 · Recording: the floating bar | `RecordingHud.dc.html` |
| 4b · After the stop: getting the review ready | `AfterStop.dc.html` |
| 5 · Review the recording | `RecordingReview.dc.html` |
| 6 · Generating | `Generating.dc.html` |
| 7 · Check what was generated | `GeneratedReview.dc.html` |
| 8 · Lands in the editor | `Editor.dc.html` |
| Fallback · step through the recording by hand | `ManualWizard.dc.html` |
| Foundations · tokens and parts | `Foundations.dc.html` |
| Home · what executed, what is waiting | `Home.dc.html` |
| Import a flow · read the file before it lands | `Import.dc.html` |
| Step forms · every type, each with its code comment | `StepForms.dc.html` |
| Editor · copy and paste a step | `EditorCopyPaste.dc.html` |
