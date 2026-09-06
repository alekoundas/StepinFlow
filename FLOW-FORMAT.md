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
Sizes:   1920x1080, 1024x768, 390x844

Areas:
  Browser         window process "chrome.exe" title contains "Swag Labs"
  Login form      inside Browser    ratio 0.30 0.18  0.40 0.40
  Inventory       inside Browser    ratio 0.00 0.15  1.00 0.85
  Cart badge      inside Browser    ratio 0.88 0.00  0.12 0.10

Inputs:
  username
  password        secret

Steps:

## Start from a clean browser

# A fresh profile each run, so run two never inherits run one's session.
Launch   "chrome.exe"  args "--user-data-dir={{temp}} --window-size={{width}},{{height}} https://www.saucedemo.com"

Condition Image  "Wait for the login form"   template "login-form.png"   in Browser
                 wait until found   timeout 15s
  Failure:
    End Run  failed  "the site never loaded"

## Sign in

Capture Screen   "login page"   in Browser

Condition Image  "Find username field"   template "username-field.png"
                 in "login page"   find best   accuracy 0.85
  Failure:
    End Run  failed  "no username field on the login page"
  Success:
    Click  at "Find username field"
    Type   {{username}}

Condition Image  "Find password field"   template "password-field.png"
                 in "login page"   find best   accuracy 0.85
  Failure:
    End Run  failed  "no password field on the login page"
  Success:
    Click  at "Find password field"
    Type   {{password}}

Condition Image  "Find login button"   template "login-button.png"   in "login page"
  Failure:
    End Run  failed  "no login button"
  Success:
    Click  at "Find login button"

# The assertion: this is what makes the recording a test.
Condition Text   "Products page loaded"   contains "Products"   in Inventory
                 wait until found   timeout 10s
  Failure:
    Read Text    "Login error"   in "Login form"
    Notify       "Login failed: {{Login error}}"
    End Run      failed  "did not reach the products page"

## Add everything on the page to the cart

Capture Screen   "inventory"   in Inventory

Condition Image  "Find add buttons"   template "add-to-cart.png"
                 in "inventory"   find all   accuracy 0.90
  Failure:
    End Run  failed  "no products to add"
  Success:
    Loop  each match in "Find add buttons"
      Click  at match
      # Give the badge a moment to update before the next click.
      Condition Image  "Badge updated"  template "cart-badge.png"  in "Cart badge"
                       wait until found  timeout 3s
        Failure:
          End Run  failed  "the cart did not update after adding an item"

## Check out

Run Flow  "flows/checkout.flow"
```

---

## Header

Everything the reader needs before the first step. Declared once, referenced by name.

### Flow

```
Flow:  Login and add to cart
```

The display name. The file name is the identity.

### Sizes

```
Sizes:  1920x1080, 1024x768, 390x844
```

The viewports this flow is expected to pass at. The runner executes the whole flow once per size
and reports one result per size. `{{width}}` and `{{height}}` resolve to the current pass, which is
how the launch line above sizes the browser without a resize step.

Overridable from the command line, so CI can narrow or widen the matrix without editing the file.

### Areas

An area is a rectangle to look inside. Areas are the vocabulary of *where*, which is why steps say
`in Inventory` rather than carrying coordinates.

```
Areas:
  Browser       window process "chrome.exe" title contains "Swag Labs"
  Inventory     inside Browser   ratio 0.00 0.15  1.00 0.85
  Header        inside Browser   offset 0 0  size 1920 90
```

A root area binds to a window by process and title. A child area is placed inside its parent,
either by **ratio** (`x y width height`, each 0–1) or by fixed **offset and size** in pixels.

Ratios are what make one flow work at several sizes: a region defined as the bottom 85% of the
window is the bottom 85% at every width.

### Points

A fixed position to click, for the cases where nothing is worth searching for.

```
Points:
  Menu toggle   inside Browser   ratio 0.95 0.05
  Origin        inside Browser   offset 12 12
```

### Inputs

The values a flow needs, declared by name only. Values live in a CSV beside the file — data is not
flow configuration, and a password in a repository is a leak.

```
Inputs:
  username
  password      secret
  order id      optional
```

`secret` means the value is never written to any file and resolves from the environment.
`optional` means a run may leave it empty.

---

## Steps

One step per line. Long steps wrap with continuation lines indented under the first.

### Names are references

Every step has a name, and **names are unique within a flow** — as are area, point and input names,
because they share one namespace. That single rule is what lets a step be referenced by name rather
than by position, so a later step reads `Click at "Find login button"` and an edit somewhere above
it changes nothing.

The recorder names steps from what it saw — `Find login button`, not `Condition 7` — and falls back
to a number only on a collision.

### Comments and sections

```
## Sign in
# A fresh profile each run, so run two never inherits run one's session.
```

`##` marks a **section**, and a section carries a verdict: it fails if any step beneath it failed,
and passes otherwise. Sections do not nest and do not indent the steps under them.

That verdict is what a report is built from. A flow at one viewport is a test *suite*, and each
section in it is a *test case*:

```xml
<testsuite name="login [1920x1080]" tests="3" failures="1">
  <testcase name="Start from a clean browser"   time="2.1"/>
  <testcase name="Sign in"                      time="4.8"/>
  <testcase name="Add everything to the cart"   time="9.2">
    <failure message="the cart did not update after adding an item"/>
  </testcase>
</testsuite>
```

So a CI dashboard shows *"Sign in has failed 4 of the last 20 builds, only at 390×844"* rather than
one flow flapping. Name sections after what a person would say they were doing, because those names
end up in front of everyone.

`#` is a **comment**, attached to the step below it. This is where intent lives — the thing a
screenshot can never show, and the first thing a model reads when diagnosing a failure.

### Capture

```
Capture Screen  "login page"   in Browser
```

Takes a frame at the moment the engine reaches it, and names it. Conditions search inside a named
capture, so several checks can be made against the same instant rather than three different ones.

### Conditions

A condition searches, decides, and produces a location. It is the only step that branches.

```
Condition Image  "Find login button"   template "login-button.png"
                 in "login page"   find best   accuracy 0.85
  Success:
    Click  at "Find login button"
  Failure:
    End Run  failed  "no login button"
```

Modes, one per condition:

| Mode | Produces | Use |
|---|---|---|
| `find best` | one location | the default |
| `find all` | a list | feeds `Loop each match` |
| `wait until found  timeout 10s` | one location | polls, capturing its own frames |
| `wait until not found  timeout 5s` | nothing | the spinner is gone; the banner has cleared |

`wait until found` needs no preceding `Capture Screen` — it takes its own, repeatedly, until it
finds the template or the timeout expires. That replaces every recorded sleep, and recorded sleeps
are the largest single source of flakiness in any record-and-replay tool.

Other condition kinds:

```
Condition Text   "Products page loaded"  contains "Products"  in Inventory
Condition Value  "Order is large"        "{{total}}" > 100
```

**A condition's result must be read.** A condition nobody branches on and nothing references is a
check that was never made, and the validator rejects it before the file is saved. Both branches are
optional; ignoring the condition entirely is not.

### Actions

```
Click       at "Find login button"          left double
Click       at point "Menu toggle"
Move        to "Find username field"
Type        {{username}}
Press       Ctrl+C
Scroll      down 3   in "Results panel"
Wait        800ms
```

`at` takes a name from the one namespace — a condition result, a point, or `match` inside a loop.
`Wait` is a fixed sleep and a last resort; prefer a condition in `wait until found` mode.

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

### Ending a run

```
End Run  failed   "did not reach the products page"
End Run  passed
```

Stops the flow and stamps the verdict. Without it a flow ends when it runs out of steps, and the
verdict comes from whether every condition that was checked passed.

Cleanup belongs above it, which is why it is a step and not a flag:

```
  Failure:
    Click    at point "Log out"
    Notify   "checkout failed"
    End Run  failed  "could not complete the order"
```

### Sub-flows

```
Run Flow  "flows/checkout.flow"
```

A path relative to the repository root, because names are only unique within a flow.

---

## Templates

`template "login-button.png"` names a file in a folder beside the flow:

```
flows/
  login.flow
  login.csv            ← values, gitignored
  login/
    login-button.png
    username-field.png
```

A folder, not an archive: git can then show *which* image changed, which is the whole point of
putting tests in a repository.

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
- **`Read Text` produces a value read as `{{Name}}`**, sharing the syntax with inputs. One
  substitution rule, and a step result and an input read the same because at the point of use they
  are the same thing.
- **Sections carry a verdict**, and are the unit a report is built from.
