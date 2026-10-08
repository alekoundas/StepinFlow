# Flow format

The text form of a flow. One file per flow, sitting in the customer's repository beside the
template images it references.

It exists so a test can be reviewed in a pull request by someone who has never opened StepinFlow,
and so a model can read and write flows as easily as it reads code — which is the reason it is a
keyword language and not JSON. JSON has nowhere to put a comment, and intent is the one thing a
screenshot cannot carry.

The database stays the runtime store. This file is the interchange format: importing it replaces
every step of the flow, and the file wins.

---

## A complete flow

```
Flow:    Login and add to cart
Id:      8f14e45f-ea2b-4c3f-9f1a-77f0d2a3b111
Sizes:   1920x1080 1024x768 390x844

Areas:
  <[ Browser ]>       window process <[ chrome.exe ]> title contains <[ Swag Labs ]>   scales with dpi   at 120dpi
  <[ Cart badge ]>    inside <[ Browser ]>   ratio 0.88 0.00  size 0.12 0.10
  <[ Inventory ]>     inside <[ Browser ]>   ratio 0.00 0.15  size 1.00 0.85
  <[ Login form ]>    inside <[ Browser ]>   ratio 0.30 0.18  size 0.40 0.40

Inputs:
  <[ username ]>
  <[ password ]>      secret

Templates:
  <[ template-a7k2m.png ]>    click 48 16   captured 2304x1377 at 120dpi
  <[ template-c3x9w.png ]>    click 12 12   captured 276x162 at 120dpi
  <[ template-l8p4w.png ]>    click 150 20   captured 922x648 at 120dpi
  <[ template-f2r6t.png ]>    click 200 150   captured 2304x1620 at 120dpi
  <[ template-p5z1h.png ]>    click 150 18   captured 922x648 at 120dpi
  <[ template-u9d3n.png ]>    click 150 18   captured 922x648 at 120dpi

Steps:

## Start from a clean browser

# A fresh profile every time, so the second execution never inherits the first one's session.
Launch   <[ chrome.exe --user-data-dir={{temp}} --window-size={{width}},{{height}} https://www.saucedemo.com ]>

Wait For Image  <[ Login form appears ]>   template <[ template-f2r6t.png ]>   in <[ Browser ]>   timeout 15000ms
 Failure:
  End Execution  failed  <[ the site never loaded ]>

## Sign in

Find Image  <[ Find username field ]>   template <[ template-u9d3n.png ]> accuracy 0.85   in <[ Login form ]>
 Failure:
  End Execution  failed  <[ no username field on the login page ]>
 Success:
  Click  at <[ Find username field ]>
  Type   <[ {{username}} ]>

Find Image  <[ Find password field ]>   template <[ template-p5z1h.png ]> accuracy 0.85   in <[ Login form ]>
 Failure:
  End Execution  failed  <[ no password field on the login page ]>
 Success:
  Click  at <[ Find password field ]>
  Type   <[ {{password}} ]>

Find Image  <[ Find login button ]>   template <[ template-l8p4w.png ]>   in <[ Login form ]>
 Failure:
  End Execution  failed  <[ no login button ]>
 Success:
  Click  at <[ Find login button ]>

# The assertion: this is what makes the recording a test.
Wait For Text  <[ Products page loaded ]>   contains <[ Products ]>   in <[ Inventory ]>   timeout 10000ms
 Failure:
  Check Text   <[ Login error ]>   is not empty   in <[ Login form ]>
  Notify       <[ Login failed: {{Login error}} ]>
  End Execution  failed  <[ did not reach the products page ]>

## Add everything on the page to the cart

Find All Images  <[ Find add buttons ]>   template <[ template-a7k2m.png ]> accuracy 0.9   in <[ Inventory ]>
 Failure:
  End Execution  failed  <[ no products to add ]>
 Success:
  Loop  each match in <[ Find add buttons ]>
   Click  at match
   # Give the badge a moment to update before the next click.
   Wait For Image  <[ Badge updated ]>  template <[ template-c3x9w.png ]>  in <[ Cart badge ]>  timeout 3000ms
    Failure:
     End Execution  failed  <[ the cart did not update after adding an item ]>

## Check out

Sub Flow  <[ flows/checkout.sflw ]>
```

---

## Header

Everything the reader needs before the first step. Declared once, referenced by name.

### Flow and Id

```
Flow:  Login and add to cart
Id:    8f14e45f-ea2b-4c3f-9f1a-77f0d2a3b111
```

`Flow` is the display name. `Id` is the identity, and it is what matters: an integer is unique to
one machine's database, and a repository is cloned into many. Without it a fresh clone cannot tell
"a new version of the login flow" from "a second flow that happens to be called login".

It is generated once and travels with every copy and export of that flow from then on.

`Flow` is also a file name: the flow exports to `<Flow>.sflw` with its templates in a folder of the
same name, and a repository is cloned onto Windows, macOS and Linux. So the name keeps the
strictest of their rules - none of `< > : " / \ | ? *` or the control characters, no space or dot
at either end, and not a Windows device name such as `CON` or `NUL.txt` - or the line is
`FLOW_NAME_INVALID`. No two flows share a name, whatever its case, because Windows and macOS would
make them one file: importing a file whose name a flow with another `Id` already has is refused as
`FLOW_NAME_TAKEN`.

### Text is quoted with `<[` and `]>`

The quotes in this format are `<[` and `]>`. Every name and every piece of text - a message, a
command, a pattern, the text to type - is quoted, in the header and in the steps, whether or not it
contains a space. One rule reads back unambiguously; a rule about which names need quoting means a
parser has to guess where `Login form inside Browser` stops being a name.

```
Notify   <[ Login failed: "bad password" ]>
Run      <[ dir C:\temp\ ]>
```

Nothing between the quotes is special. Double quotes and backslashes are text like any other, so
nothing is ever escaped, and what is written is what is read. The price is that a quote ends at its
first `]>`, so quoted text cannot hold one. The forms refuse `<[` and `]>` as they are typed, so
whatever they save can always be quoted.

The spaces just inside the quotes are layout, not text. The printer writes one on each side, and the
reader trims whatever is there, so `<[This text]>` and `<[    This text ]>` are the same text.

Four keywords take the rest of their line as written, without quotes, because nothing can follow
them: `Flow:`, `Id:`, a `#` comment and a `##` section.

### Every word means something

Unquoted, a word is either a keyword or a number - `0.85`, `-10`, `800ms`, `120dpi`, `1920x1080`.
Anything else is an error.

A line is read in the order its parts are written here, to its end. A word nothing on that line
expects is an error, and so is one the line needs and does not have - a `Notify` with no message, a
`Click at` with no target. Either way the error names the line and the column, and what could have
stood there:

```
Unexpected "offset", expected "inside" or "on screen".
```

rather than a step quietly losing part of what was written.

### Sizes

```
Sizes:  1920x1080 1024x768 390x844
```

The viewports this flow is expected to pass at. The engine executes the whole flow once per size
and reports one result per size. `{{width}}` and `{{height}}` resolve to the current pass, which is
how the launch line above sizes the browser without a resize step. A flow with no sizes leaves the
line out.

Overridable from the command line, so CI can narrow or widen the matrix without editing the file.

### Areas

An area is a rectangle to look inside. Areas are the vocabulary of _where_, which is why steps say
`in <[ Inventory ]>` rather than carrying coordinates.

```
Areas:
  <[ Browser ]>     window process <[ chrome.exe ]> title contains <[ Swag Labs ]>   scales with dpi
  <[ Game ]>        inside <[ Browser ]>   ratio 0.10 0.20  size 0.80 0.70   scales with area
  <[ Header ]>      inside <[ Browser ]>   offset 0 0  size 1920 90   at 120dpi
  <[ Inventory ]>   inside <[ Browser ]>   ratio 0.00 0.15  size 1.00 0.85
  <[ Screen ]>      monitor primary
```

Roots first, then their children, each group alphabetical - so a child's `inside` always names
something already read, and two exports of one flow are the same bytes.

A root area is one of three things:

| form | what it is |
| --- | --- |
| `window process <[ chrome.exe ]> title contains <[ Swag ]>` | a window, by process and optionally title |
| `monitor primary`, `monitor <[ \\.\DISPLAY2 ]>` | a whole monitor; `primary` is the one that means the same thing on another PC |
| `on screen   offset 10 20  size 300 200` | fixed screen coordinates - right on the machine it was made on and nowhere else |

A child area is placed inside its parent, either by **ratio** (`ratio x y size width height`, each
0–1) or by fixed **offset and size** in pixels (`offset x y size width height`). Both say where the
area starts and how big it is, so both are written the same way.

Areas go one level deep: a child is inside a root, and an area inside a child is `AREA_TOO_DEEP`.

Ratios are what make one flow work at several sizes: a region defined as the bottom 85% of the
window is the bottom 85% at every width.

Two optional clauses end the line, in this order:

- **`scales with dpi`** or **`scales with area`** - what makes the things inside bigger or smaller
  on another screen. A browser or a native app keeps its contents' size when the window changes and
  only the monitor's DPI moves them; a game's contents fill the window, so they follow its size.
  Left out, a child takes its parent's and a root is `dpi`.
- **`at 120dpi`** - the DPI the area's `offset` and `size` were captured at, so inside a `dpi`
  parent they grow with the monitor. Left out, pixels stay as written.

### Points

A fixed position to click, for the cases where nothing is worth searching for.

```
Points:
  <[ Menu toggle ]>   inside <[ Browser ]>   ratio 0.95 0.05
  <[ Origin ]>        inside <[ Browser ]>   offset 12 12   at 120dpi
```

A point carries its own `at 120dpi`, the DPI its `offset` was captured at, for the same reason an
area does. `on screen` is a screen coordinate: right on this screen and nowhere else, which the
validator warns about.

### Inputs

The values a flow needs, declared by name only. Values live in a CSV beside the file — data is not
flow configuration, and a password in a repository is a leak.

```
Inputs:
  <[ username ]>
  <[ password ]>    secret
```

`secret` means the value is never written to any file and resolves from the environment.

What happens when a row leaves a value empty - error, fall back to the recorded default, or type
nothing - is undecided, and gets settled when csv binding is built rather than guessed at now.

---

## Steps

One step per line. Nesting is one space per level: a branch sits one space in from its check, and
the steps under it one space in from the branch. A line indented further than one level past the
line above it is an error.

### Names are references

Every step has a name, and **names are unique within a flow** — as are area, point and input names,
because they share one namespace. That single rule is what lets a step be referenced by name rather
than by position, so a later step reads `Click at <[ Find login button ]>` and an edit somewhere above
it changes nothing.

**A name is declared above its first use.** An area is inside one written above it, a point in an
area from the header, and a step only ever names one it passed on the way down. So a file reads top
to bottom in one pass: a name nothing above declares is an error on the line that uses it, and a
name declared twice is an error at the second.

The recorder names steps from what it saw — `Find login button`, not `Check 7` — and falls back
to a number only on a collision.

### Comments and sections

```
## Sign in
# A fresh profile every time, so the second execution never inherits the first one's session.
```

`##` marks a **section**, and a section carries a verdict: it fails if any step beneath it failed,
and passes otherwise. Sections do not nest and do not indent the steps under them.

That verdict is what a report is built from. A flow at one viewport is a test _suite_, and each
section in it is a _test case_:

```xml
<testsuite name="login [1920x1080]" tests="3" failures="1">
  <testcase name="Start from a clean browser"   time="2.1"/>
  <testcase name="Sign in"                      time="4.8"/>
  <testcase name="Add everything to the cart"   time="9.2">
    <failure message="the cart did not update after adding an item"/>
  </testcase>
</testsuite>
```

So a CI dashboard shows _"Sign in has failed 4 of the last 20 builds, only at 390×844"_ rather than
one flow flapping. Name sections after what a person would say they were doing, because those names
end up in front of everyone.

`#` is a **comment**, attached to the line below it, whatever that line is - a step, a `##` section
or a `Success:`/`Failure:` branch. This is where intent lives — the thing a screenshot can never
show, and the first thing a model reads when diagnosing a failure. A branch with nothing under it is
left out of the file, unless it has a comment saying why it is empty.

Only a step can carry a comment. One written above anything else - an area, a template, a header
line, or nothing at the end of the file - is an error at the comment, rather than a note that
quietly moves onto a step further down.

### Checks

A check takes its own screenshot, looks at it, decides, and produces a result. Checks are what turn
a recording into a test — a flow holding none of them proves nothing.

```
Find Image  <[ Find login button ]>   template <[ template-l8p4w.png ]> accuracy 0.85   in <[ Login form ]>
 Success:
  Click  at <[ Find login button ]>
 Failure:
  End Execution  failed  <[ no login button ]>
```

There are three things to check and four ways to look, and the keyword says both at once:

| Keyword               | Produces      | Use                                         |
| --------------------- | ------------- | ------------------------------------------- |
| `Find Image`          | one location  | the default                                 |
| `Find All Images`     | a list        | feeds `Loop each match`                     |
| `Wait For Image`      | one location  | polls until it appears                      |
| `Wait Until No Image` | nothing       | the spinner is gone; the banner has cleared |
| `Check Text`          | the text read | assert what the screen says                 |
| `Wait For Text`       | the text read | polls until it says it                      |
| `Wait Until No Text`  | nothing       | the error message has cleared               |
| `Check Value`         | nothing       | tests a value an earlier step produced      |

The waiting forms take `timeout 10000ms` and poll, taking a fresh screenshot each time until the
answer comes out right or the timeout expires. That replaces every recorded sleep, and recorded
sleeps are the largest single source of flakiness in any record-and-replay tool. `no timeout` waits
for ever. A duration is always written in milliseconds, so it has one spelling.

Making the mode part of the keyword means an impossible combination cannot be written down.
`Find All` reads a single screenshot and hands back every hit, which is meaningful for templates and
not for text, so there is no `Find All Texts` to mistype.

`Wait Until No …` is not "wait until gone": nothing verifies the thing was ever there, so it
succeeds immediately when the screen never matched at all.

The text forms narrow before they judge — `matches <[ total: (\d+) ]>` keeps the captured group, and the
condition is then tested against that. What is kept is what later steps read as `{{Name}}`, whether
the check passed or failed, because a failure that says what was actually on screen is worth far
more than one that only says it failed.

```
Check Text   <[ Read the total ]>        matches <[ total: (\d+) ]>   in <[ Cart badge ]>
Check Value  <[ Order is large ]>        <[ {{Read the total}} ]> > <[ 100 ]>
```

**A check's result must be read.** One that nothing branches on and nothing references was never
really made, and the validator rejects it before the file is saved. Both branches are optional;
ignoring the check entirely is not.

### Wait once, then branch freely

`Wait For` and `Find` answer different questions, and using the wrong one is what makes a suite slow
rather than wrong.

`Wait For` asks _"has this appeared yet?"_ — a question whose answer changes, so waiting is the
point. `Find` asks _"which state am I in?"_ — a question whose answer is already final. A phone
layout does not turn into a desktop layout after ten seconds, so a timeout there buys nothing and
costs its full length on every execution that takes the fallback.

So wait once, on something that is always present, then branch instantly:

```
Wait For Image  <[ Page loaded ]>   template <[ template-o4j7b.png ]>   in <[ Browser ]>   timeout 15000ms
 Failure:
  End Execution  failed  <[ the page never loaded ]>

Find Image  <[ Desktop nav present? ]>   template <[ template-n6v2e.png ]>   in <[ Browser ]>
 Failure:
  Click  at point <[ Hamburger menu ]>
 Success:
  Click  at <[ Desktop nav present? ]>
```

The anchor absorbs the patience once per page. Every layout question after it is free and still
safe, because the page is already known to have rendered. Three fallbacks on that page cost nothing
instead of three timeouts.

A step carrying both a populated `Failure:` branch and a long timeout is almost always a branch
point that was written as a wait. That is a warning, not an error - the flow still works, it is
just paying for patience it cannot use.

### Actions

```
Click       at <[ Find login button ]>          right double
Click       at point <[ Menu toggle ]>
Move        to <[ Find username field ]>
Type        <[ {{username}} ]>
Press       <[ Ctrl+C ]>
Scroll      down 3   in <[ Results panel ]>
Wait        800ms
```

`at` takes a name from the one namespace — a check's result, a point, or `match` inside a loop.
A click is left and single unless it says otherwise: the button (`right`, `middle`) comes first,
then what it does (`double`, `hold`, `release`). `Wait` is a fixed sleep and a last resort; prefer
a `Wait For` check.

### Loops

```
Loop  5 times
Loop  forever
Loop  each match in <[ Find add buttons ]>
```

One step, three sources. Inside `each match`, the keyword `match` refers to the current item, so
`Click at match` clicks each result in turn. This replaces the old behaviour where a find-all search
silently re-entered its own success branch — the repetition is now visible in the file, and steps
can run between passes.

### Going back

```
## Sign in
Click           at point <[ Sign in button ]>
Wait For Image  <[ Signed in ]>   template <[ template-v1y8s.png ]>   in <[ Browser ]>   timeout 5000ms
 Failure:
  Go Back  to <[ Sign in ]>
```

`Go Back` returns to a step the execution already passed on the way here, and carries on from there:
an earlier step beside it, the step it sits under, or one beside those, up to the top of the flow.
Never a step further down, never one in the other branch of a check, and never one inside a loop or
check that already finished - that block is gone back to as a whole. So the name a `Go Back` uses
is always written above it.

### Ending an execution

```
End Execution  failed   <[ did not reach the products page ]>
End Execution  passed
```

Stops the flow and stamps the verdict. Without it a flow ends when it runs out of steps, and the
verdict comes from whether every check passed.

Cleanup belongs above it, which is why it is a step and not a flag:

```
 Failure:
  Click    at point <[ Log out ]>
  Notify   <[ checkout failed ]>
  End Execution  failed  <[ could not complete the order ]>
```

### Sub-flows

```
Sub Flow  <[ flows/checkout.sflw ]>
```

### Cleanup under End Execution

`End Execution` decides the verdict and everything indented under it is what happens afterwards -
closing the application, a notification, a webhook. They are ordinary steps, so anything a flow can
do a teardown can do:

```
End Execution  failed  <[ did not reach the products page ]>
 Run     KILL_PROCESS  <[ chrome.exe ]>
 Notify  <[ login smoke failed at {{width}}x{{height}} ]>
```

The verdict is fixed the moment the step is reached. A cleanup step failing is recorded but changes
nothing, and a second `End Execution` underneath is rejected - the first one already decided.

A flow that reaches the end without an `End Execution` anywhere is **inconclusive**: not a pass and
not a failure, because nothing in it ever said. A flow whose every check failed looks exactly the
same from the outside, which is why walking to the end is not reported as success.

A path relative to the repository root, because names are only unique within a flow.

---

## Templates

`template <[ template-l8p4w.png ]>` names a file in a folder beside the flow:

```
flows/
  login.sflw
  login.csv            ← values, gitignored
  login/
    template-l8p4w.png
    template-u9d3n.png
```

A folder, not an archive: git can then show _which_ image changed, which is the whole point of
putting tests in a repository.

A template made in the app is named once, when it is created: `template-k3x9q.png`, five random
characters from `0-9a-z`, drawn again if the flow already has it. A file written by hand can use any
name a file can have, such as `login-button.png`. Either way the name is kept exactly as it is
through every import and export. Two names that differ only in case are one file on Windows and
macOS, so they are `TEMPLATE_DUPLICATE`.

**Not** a content hash, and not the database id: a hash changes whenever the image is edited and an
id whenever the flow is imported, and either way git would record a delete and an add rather than a
modification, which throws away the one thing this layout is for.

### On the step: how it is searched for

```
Find Image  <[ Find login ]>   template <[ template-k3x9q.png ]> accuracy 0.97 required  template <[ template-q7m5c.png ]> accuracy 0.9   match shape and brightness   in <[ Login form ]>
```

`accuracy` and `required` follow the template they belong to, so neither can be read as the
step's.

- **`accuracy`** - each template has its own: one variant of an icon can need a looser bar than
  another. Left out, it is the mode's default.
- **`required`** - with none marked, any one template found is enough: three variants of the same
  icon. Marked, those templates all have to be there. It is on the line because it turns an OR into
  an AND, and a reviewer should see that.
- **`match shape and brightness`** - the mode, for every template in the step. Written only when it
  is not the default, `match shape`.

| mode | compares | default accuracy |
| --- | --- | --- |
| `shape` | the pattern, with each picture's own brightness subtracted | 0.8 |
| `shape and brightness` | the pixels as they are - tells an enabled button from a disabled one | 0.95 |

### In the header: what the picture is

```
Templates:
  <[ template-k3x9q.png ]>    click 150 20   captured 922x648 at 120dpi
```

Facts about the picture rather than the search, once per file:

- **`click 150 20`** - where a match is clicked, x and y from the template's top left. It scales
  with the template.
- **`captured 922x648`** - the size of the area the template was captured in. What an area that
  `scales with area` measures against.
- **`at 120dpi`** - the DPI it was captured at. What an area that `scales with dpi` measures
  against.

Every template a step names has a line here, and every line has a `click`: a template always says
where it is clicked. A step naming a file with no line is `TEMPLATE_UNKNOWN`, and a line with no
`click` is a syntax error. The size and DPI are optional; one with neither is searched at the size
it was captured.

---

## What the parser guarantees

**Import is transactional.** Parse the whole file, then replace. Everything before the transaction
is pure, so a typo reports its line and leaves the existing flow untouched rather than
half-replaced.

**Every word is accounted for.** A word left over on a line, and a word a line needs but does not
have, are both errors with a line and a column - nothing is dropped and nothing is guessed. Every
such error is one code, `TOKEN_UNEXPECTED`, because its message already names the token and what
could have stood there. The other codes are kinds of problem rather than places in the grammar: a
line indented too far, a template described twice, a comment with no step below it, no `Flow:` line,
a flow name that cannot be a file name, a name nothing above declares, a name declared twice, an
area inside one that is already inside another.

**Names correlate history.** Execution history is keyed on step name, and an execution step keeps
the name it ran under while its foreign key is set null rather than cascaded — so a re-import keeps
the trend for every step whose name did not change. Renaming a step starts its history over —
accepted, because renames should be rare.

**The file wins.** A UI edit and a `git pull` cannot both be true. Importing overwrites.

**The round trip is the acceptance test.** Export, import, export again, byte identical - and the
imported areas, points and templates equal to the originals field by field, because identical bytes
cannot see a field the printer never prints. Verified two ways: purely, and through a real database
with template bytes written to disk and read back. A file written by hand is checked as well: its
template names come back out as they went in, extension included.

What the parser checks is structural — is that a keyword, is that a condition, does that name exist
above, does every word belong. The semantic validator runs on the saved flow, as it does after a
save from a form, so a flow it finds errors in is imported and shows them.

One thing it does not do yet: a `Sub Flow` step imports with no target, because resolving the path
means reading the `Id:` out of the file it names and deciding what a missing one does.

---

## Settled

- **`Launch` is one step.** Its target is an executable or a URL; anything else goes in `args`.
- **The text checks produce a value read as `{{Name}}`**, sharing the syntax with inputs. One
  substitution rule, and a step result and an input read the same because at the point of use they
  are the same thing.
- **There is no separate read step.** A read that nothing checks is a check that was never made, so
  reading and deciding are one step: `Check Text … is not empty` is the read that used to exist,
  and it can no longer silently hand on an empty string.
- **Sections carry a verdict**, and are the unit a report is built from.
