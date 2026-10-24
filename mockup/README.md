# The mockup

The screens StepinFlow is being built towards. The live canvas, where the boards are laid out and
can be clicked through, is the Design artifact
[StepinFlow recorder redesign](https://claude.ai/artifact/KD5kCxkafbq8PV5HB8JrCi); this folder is a
copy of it, kept beside the code so a change can point at the board it follows.

**[mockup.png](mockup.png)** is every board in one picture, in canvas order. Open it to look; it
is a screenshot, so nothing in it can be clicked.

The `.dc.html` files are the boards' source: Design Component files, plain HTML with `{{ }}` holes
and a small logic class at the bottom. They do not render on their own. Each loads the canvas's
runtime as `support.js`, which is not in this folder, so a browser shows the bare `{{ }}`
template. `canvas.json` is the canvas's index: where each board sits and its title.

The canvas is the source. When it changes, copy its files here again and retake the picture rather
than editing these.

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
| 8a · Editor: Areas, the windows and regions steps look in | `EditorAreas.dc.html`, which shows `Editor.dc.html` with `tab="areas"` |
| 8b · Editor: Locations, one to fix after its area changed | `EditorLocations.dc.html`, with `tab="locations"` and `issue="true"` |
| 8c · Editor: Inputs, the values steps type in | `EditorInputs.dc.html`, with `tab="inputs"` |
| 8d · Editor: Templates, the pictures steps share | `EditorTemplates.dc.html`, with `tab="templates"` |
| 8e · Editor: Script, what Export writes | `EditorScript.dc.html`, with `tab="script"` |
| 8f · Editor: Add template, capture now or reuse one | `EditorTemplateAdd.dc.html`, with `dialog="add"` |
| 8g · Editor: Reuse a template, picked by its picture | `EditorTemplateReuse.dc.html`, with `dialog="reuse"` |
| 8h · Editor: Editing a template two steps share | `EditorTemplateShared.dc.html`, with `dialog="shared"` |
| 8i · Editor: a smaller tree, and a step with three templates | `EditorTreeSize.dc.html`, with `step="inv"` and `size="80"` |
| Fallback · step through the recording by hand | `ManualWizard.dc.html` |
| Foundations · tokens and parts | `Foundations.dc.html` |
| Home · what executed, what is waiting | `Home.dc.html` |
| Import a flow · read the file before it lands | `Import.dc.html` |
| Editor · copy and paste a step | `EditorCopyPaste.dc.html` |
| Step forms · checks, input, control and end | `StepForms.dc.html` |
| Step forms · system steps and branches | `StepFormsSystem.dc.html`, which shows `StepForms.dc.html` with `part="system"` |
