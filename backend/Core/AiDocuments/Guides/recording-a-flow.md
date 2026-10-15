# Recording a flow

## What the recorder does

The recorder watches what you do once, lets you review it, and writes it into a flow. You are not
asked a question per action: each kind of action becomes steps by a rule, and you correct the
result in Review and afterwards in the editor.

A flow is created before recording starts: its name, what it should do, and the window it works in,
which becomes its main area. Everything recorded is kept relative to that window.

## What is and is not recorded

Clicks, drags, scrolls and typing are recorded. A press and release become one click; a burst of
typing becomes one entry; a long gap becomes a pause.

Cursor movement on its own is not recorded. Recording every movement would produce thousands of
events in a couple of minutes and bury the ones that matter.

When you press a mouse button, StepinFlow keeps the window you picked as it was just before - the
screen you were looking at when you decided to click, rather than whatever the click then changed.

## The recording bar

Record now minimises StepinFlow and opens a small bar at the top of the screen. Arrange your windows,
then press Start on the bar. While recording, the bar shows whether it is recording or paused, a
clock, the last few actions, and Pause and Stop. Closing the bar before Start records nothing and
brings StepinFlow back. Nothing you do on the bar is recorded, and it never appears in a
screenshot. Pause and Stop also have keys, F9 and F10 by default, which work whatever window is in
front and are never recorded; they are under Recording keys in Settings. Typed text shows in the
bar as a count, "Typed 15 characters", never as the text.

## What each click hit

When the recording stops, StepinFlow looks at the screenshot from before each click and finds the
element under it — the button, field or icon — and reads the text on it. It takes about a second
per click. Then it groups the clicks that hit the same picture, such as the same download icon on
several rows. The recording page shows how far each of those has got.

A box around the click stands in for the element when the model had nothing to say: no element was
under the click, the element was bigger than the area it looked at, or the model file is not
installed.

## Review

Every action is listed with a checkbox to keep it or leave it out. Select one to see the window at
that moment, where you clicked, and the rectangle the template will be cut from; drag its corners
to change it. Each action says what it becomes:

- **A click**: wait for it, then click (the default); click this exact spot without looking for
  anything; or leave it out. A click outside the window you picked starts left out.
- **Typed text**: an input, named from the field you clicked before typing, with what you typed as
  its default; or the text as written. A field that reads as a password makes the input secret,
  which is never written to a file.
- **A key, a scroll**: kept, or left out.

Beside it: the text read under the click, how long you waited before it, and which other clicks hit
the same picture. "What it should do" can be edited here too.

## The plain draft

"Plain draft, no AI" writes the reviewed recording into the flow and opens it in the editor:

- A click becomes a search that waits for the element, then a move onto it and a click. The wait is
  three times how long you waited, never under 10 seconds nor over 60, with a comment saying what
  you waited.
- Everything after a search, up to the next search, goes in that search's Success branch. Its
  Failure branch ends the execution, failed, saying what was not found.
- A search is named from the text on the element, such as "Find Invoices", or "Find icon".
- Clicks that hit the same picture share one template.
- Typed text types its input, such as {{email}}. A pause is not a step; it sizes the next wait.

The draft is written the way an imported script is, so a draft that does not read cleanly leaves the
flow as it was and says on which line.

## Generate flow

"Generate flow" gives the plain draft, as a script, with what the flow should do, to the model chosen
in Settings, and the model shapes it: it names the steps after the task, turns clicks on the same
picture into one search for every match, and says in each failure what was not found. It is off
until an AI provider and a model are chosen.

The answer is read the way an imported file is. When it does not read cleanly, or changes something
it may not - the flow's name and id, a template, an input other than by renaming it - it is sent back
with the lines that were wrong, three times at most. Nothing is saved until an answer reads cleanly.
When none does, or the model cannot be reached, the plain draft is saved instead, and the page says
why.

What goes to the model: what the flow should do, the draft as a script, and which clicks hit the same
picture. The text read under each click goes too, unless the provider is a cloud one and "Send text
read from the screen" is off; then the model names the steps from what the flow should do. What you
typed never goes: the model sees only the input's name, such as {{email}}. The screenshots and
templates stay on this machine. "Show the exact request" shows what was sent.

The page shows each stage as it happens, and once it is done, what the model says to check. "Open in
the editor" opens the flow.

## Recording into a flow you already have

Not available yet: the editor's **Record steps here** button is disabled until a model's draft can
go into a flow that already exists. Record a new flow instead, and call it from the existing one as
a sub-flow.
