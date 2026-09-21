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
Sizes:   1920x1080, 1024x768, 390x844

Areas:
  "Browser"       window process "chrome.exe" title contains "Swag Labs"
  "Cart badge"    inside "Browser"   ratio 0.88 0.00  0.12 0.10
  "Inventory"     inside "Browser"   ratio 0.00 0.15  1.00 0.85
  "Login form"    inside "Browser"   ratio 0.30 0.18  0.40 0.40

Inputs:
  "username"
  "password"      secret

Steps:

## Start from a clean browser

# A fresh profile every time, so the second execution never inherits the first one's session.
Launch   "chrome.exe --user-data-dir={{temp}} --window-size={{width}},{{height}} https://www.saucedemo.com"

Wait For Image  "Login form appears"   template "login-form.png"   in "Browser"   timeout 15s
  Failure:
    End Execution  failed  "the site never loaded"

## Sign in

Find Image  "Find username field"   template "username-field.png"   in "Login form"   accuracy 0.85
  Failure:
    End Execution  failed  "no username field on the login page"
  Success:
    Click  at "Find username field"
    Type   "{{username}}"

Find Image  "Find password field"   template "password-field.png"   in "Login form"   accuracy 0.85
  Failure:
    End Execution  failed  "no password field on the login page"
  Success:
    Click  at "Find password field"
    Type   "{{password}}"

Find Image  "Find login button"   template "login-button.png"   in "Login form"
  Failure:
    End Execution  failed  "no login button"
  Success:
    Click  at "Find login button"

# The assertion: this is what makes the recording a test.
Wait For Text  "Products page loaded"   contains "Products"   in "Inventory"   timeout 10s
  Failure:
    Check Text   "Login error"   is not empty   in "Login form"
    Notify       "Login failed: {{Login error}}"
    End Execution  failed  "did not reach the products page"

## Add everything on the page to the cart

Find All Images  "Find add buttons"   template "add-to-cart.png"   in "Inventory"   accuracy 0.90
  Failure:
    End Execution  failed  "no products to add"
  Success:
    Loop  each match in "Find add buttons"
      Click  at match
      # Give the badge a moment to update before the next click.
      Wait For Image  "Badge updated"  template "cart-badge.png"  in "Cart badge"  timeout 3s
        Failure:
          End Execution  failed  "the cart did not update after adding an item"

## Check out

Sub Flow  "flows/checkout.sflw"
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

### Names are always quoted

Every name is written in quotes, in the header and in the steps, whether or not it contains a
space. One rule reads back unambiguously; a rule about which names need quoting means a parser has
to guess where `Login form inside Browser` stops being a name.

### Sizes

```
Sizes:  1920x1080, 1024x768, 390x844
```

The viewports this flow is expected to pass at. The engine executes the whole flow once per size
and reports one result per size. `{{width}}` and `{{height}}` resolve to the current pass, which is
how the launch line above sizes the browser without a resize step.

Overridable from the command line, so CI can narrow or widen the matrix without editing the file.

### Areas

An area is a rectangle to look inside. Areas are the vocabulary of _where_, which is why steps say
`in "Inventory"` rather than carrying coordinates.

```
Areas:
  "Browser"     window process "chrome.exe" title contains "Swag Labs"
  "Header"      inside "Browser"   offset 0 0  size 1920 90
  "Inventory"   inside "Browser"   ratio 0.00 0.15  1.00 0.85
```

Roots first, then their children, each group alphabetical - so a child's `inside` always names
something already read, and two exports of one flow are the same bytes.

A root area binds to a window by process and title. A child area is placed inside its parent,
either by **ratio** (`x y width height`, each 0–1) or by fixed **offset and size** in pixels.

Ratios are what make one flow work at several sizes: a region defined as the bottom 85% of the
window is the bottom 85% at every width.

### Points

A fixed position to click, for the cases where nothing is worth searching for.

```
Points:
  "Menu toggle"   inside "Browser"   ratio 0.95 0.05
  "Origin"        inside "Browser"   offset 12 12
```

### Inputs

The values a flow needs, declared by name only. Values live in a CSV beside the file — data is not
flow configuration, and a password in a repository is a leak.

```
Inputs:
  "username"
  "password"    secret
```

`secret` means the value is never written to any file and resolves from the environment.

What happens when a row leaves a value empty - error, fall back to the recorded default, or type
nothing - is undecided, and gets settled when csv binding is built rather than guessed at now.

---

## Steps

One step per line. Long steps wrap with continuation lines indented under the first.

### Names are references

Every step has a name, and **names are unique within a flow** — as are area, point and input names,
because they share one namespace. That single rule is what lets a step be referenced by name rather
than by position, so a later step reads `Click at "Find login button"` and an edit somewhere above
it changes nothing.

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

`#` is a **comment**, attached to the step below it. This is where intent lives — the thing a
screenshot can never show, and the first thing a model reads when diagnosing a failure.

### Checks

A check takes its own screenshot, looks at it, decides, and produces a result. Checks are what turn
a recording into a test — a flow holding none of them proves nothing.

```
Find Image  "Find login button"   template "login-button.png"
            in "Login form"   accuracy 0.85
  Success:
    Click  at "Find login button"
  Failure:
    End Execution  failed  "no login button"
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

The waiting forms take `timeout 10s` and poll, taking a fresh screenshot each time until the answer
comes out right or the timeout expires. That replaces every recorded sleep, and recorded sleeps are
the largest single source of flakiness in any record-and-replay tool. A timeout of `0` waits for
ever.

Making the mode part of the keyword means an impossible combination cannot be written down.
`Find All` reads a single screenshot and hands back every hit, which is meaningful for templates and
not for text, so there is no `Find All Texts` to mistype.

`Wait Until No …` is not "wait until gone": nothing verifies the thing was ever there, so it
succeeds immediately when the screen never matched at all.

The text forms narrow before they judge — `matches "total: (\d+)"` keeps the captured group, and the
condition is then tested against that. What is kept is what later steps read as `{{Name}}`, whether
the check passed or failed, because a failure that says what was actually on screen is worth far
more than one that only says it failed.

```
Check Text   "Read the total"        matches "total: (\d+)"   in "Cart badge"
Check Value  "Order is large"        "{{Read the total}}" > 100
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
Wait For Image  "Page loaded"   template "logo.png"   in "Browser"   timeout 15s
  Failure:
    End Execution  failed  "the page never loaded"

Find Image  "Desktop nav present?"   template "nav-bar.png"   in "Browser"
  Failure:
    Click  at point "Hamburger menu"
  Success:
    Click  at "Desktop nav present?"
```

The anchor absorbs the patience once per page. Every layout question after it is free and still
safe, because the page is already known to have rendered. Three fallbacks on that page cost nothing
instead of three timeouts.

A step carrying both a populated `Failure:` branch and a long timeout is almost always a branch
point that was written as a wait. That is a warning, not an error - the flow still works, it is
just paying for patience it cannot use.

### Actions

```
Click       at "Find login button"          left double
Click       at point "Menu toggle"
Move        to "Find username field"
Type        "{{username}}"
Press       Ctrl+C
Scroll      down 3   in "Results panel"
Wait        800ms
```

`at` takes a name from the one namespace — a check's result, a point, or `match` inside a loop.
`Wait` is a fixed sleep and a last resort; prefer a `Wait For` check.

### Loops

```
Loop  5 times
Loop  forever
Loop  each match in "Find add buttons"
```

One step, three sources. Inside `each match`, the keyword `match` refers to the current item, so
`Click at match` clicks each result in turn. This replaces the old behaviour where a find-all search
silently re-entered its own success branch — the repetition is now visible in the file, and steps
can run between passes.

### Ending an execution

```
End Execution  failed   "did not reach the products page"
End Execution  passed
```

Stops the flow and stamps the verdict. Without it a flow ends when it runs out of steps, and the
verdict comes from whether every check passed.

Cleanup belongs above it, which is why it is a step and not a flag:

```
  Failure:
    Click    at point "Log out"
    Notify   "checkout failed"
    End Execution  failed  "could not complete the order"
```

### Sub-flows

```
Sub Flow  "flows/checkout.sflw"
```

### Cleanup under End Execution

`End Execution` decides the verdict and everything indented under it is what happens afterwards -
closing the application, a notification, a webhook. They are ordinary steps, so anything a flow can
do a teardown can do:

```
End Execution  failed  "did not reach the products page"
  Run     KILL_PROCESS  "chrome.exe"
  Notify  "login smoke failed at {{width}}x{{height}}"
```

The verdict is fixed the moment the step is reached. A cleanup step failing is recorded but changes
nothing, and a second `End Execution` underneath is rejected - the first one already decided.

A flow that reaches the end without an `End Execution` anywhere is **inconclusive**: not a pass and
not a failure, because nothing in it ever said. A flow whose every check failed looks exactly the
same from the outside, which is why walking to the end is not reported as success.

A path relative to the repository root, because names are only unique within a flow.

---

## Templates

`template "login-button.png"` names a file in a folder beside the flow:

```
flows/
  login.sflw
  login.csv            ← values, gitignored
  login/
    login-button.png
    username-field.png
```

A folder, not an archive: git can then show _which_ image changed, which is the whole point of
putting tests in a repository.

The file is named after the template, with whitespace hyphenated and a number appended if two
templates in one flow want the same name. **Not** a content hash: a hash changes whenever the
image is edited, so git would record a delete and an add rather than a modification, which
throws away the one thing this layout is for.

Each template carries its click offset and the window size it was captured at. Those are properties
of the image, so they live beside it rather than cluttering the step line.

---

## What the parser has to guarantee

**Import is transactional.** Parse the whole file, validate it, then replace. A typo must leave the
existing flow untouched, never half-replaced.

**Names correlate history.** Execution history is keyed on step name, so a re-import keeps the
trend for every step whose name did not change. Renaming a step starts its history over — accepted,
because renames should be rare.

**The file wins.** A UI edit and a `git pull` cannot both be true. Importing overwrites.

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
