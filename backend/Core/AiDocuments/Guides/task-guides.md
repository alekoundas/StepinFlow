# Task guides

> **These are stubs.** Each needs writing from a real flow that has actually been built and run.
> The headings mark what is worth covering; the content should come from doing it, not from
> describing it in the abstract.

## Automating a desktop application

Cover: focus and resize the window first, define areas inside it, capture templates at that size,
and why that order makes the flow portable.

## Waiting for something to load

Cover: `WAIT_UNTIL_FOUND` versus a fixed `WAIT`, choosing a timeout, and what to put in the Failure
branch.

## Reading a number off the screen and acting on it

Cover: a single `SEARCH_TEXT` with a Keep only expression and a numeric condition - the read and
the comparison are one step now - and why a non-numeric read fails rather than taking the false
branch. Then when a second `CHECK_VALUE` is still worth adding: asking another question of the same
read without going back to the screen.

## Retrying something that sometimes fails

Cover: a `LOOP` around a search, breaking out on success, and how the execution view distinguishes a
handled retry from a real failure.

## Working through a list of items

Cover: `FIND_ALL`, that every hit comes from one screenshot, and what that means when the screen
changes as you work through them.

## Reusing part of a flow

Cover: Extract to sub-flow, what gets copied versus moved, and why references are checked first.

## Repeating a step somewhere else in the flow

Select the step in the tree and press **Copy**. The copy takes everything under the step, and a bar
over the tree names it. **Paste here** beside any + puts a copy at the end of that branch; the copy
stays, so it can go in several places. A pasted step is named apart from the one it copies ("Find
button 2"), since the script refers to steps by name.
It links to the same template images as the step it copies, so fixing a picture once fixes both;
**Edit a copy for this step** gives one of them its own. A step inside the copy that read another
step inside it reads the copy's. One that read a step outside it still reads that step, and validation marks it
when the new place cannot reach it.

A copy goes only into the flow it was copied from. Another flow has its own areas, points and
inputs, so to reuse steps across flows, extract them to a sub-flow instead.

## Steps the app names

A cursor, wait, keyboard, window or system action step has no name to type. The app names it from
its own settings each time it is saved: "Double right click", "Wait 2s", "Type {{email}}", "Focus
chrome", "Monitor off". A second one is numbered ("Click 2", "Wait 2s (2)"). Where a Move or a Drag
goes is shown beside its name in the tree rather than in it, so renaming a search or a saved
location never leaves a step with a stale name. Changing a step's settings renames it, which starts
its execution history over.

## Saying why a step is there

Every step's form, Success and Failure included, ends with a **Comment**, written above the step
in the script as `#` lines. The form shows that line as it will print. It is the
first thing read when a failure is explained, so say what you expect at that point and what it
means if it fails, rather than what the step does, which its line already says.

## Finding a flow or a step

**Search flows and steps**, at the top of Flows and Sub-flows, or Ctrl K there, finds a flow by
its name or description and a step by its name, on the branch checked out. Every word typed has to
be there, in any order. A flow found opens in the editor; a step found opens its flow with the step
picked in the tree. The chips over the list narrow it to automations, QA tests, flows with issues,
or flows with changes not committed.

## Sharing a flow as a file

A flow's **Export**, beside Execute at the top of the editor, writes it as a `.sflw` script with its templates in a
folder of the same name beside it. The folder starts as the app's export folder; Browse picks
another, and a script of the same name there is replaced, with any image in its folder that the
flow no longer uses removed.

**Import**, in the menu, reads a `.sflw` file and says what importing it would do before anything
is saved: a new flow, when no flow has the file's `Id`; the flow with that `Id` replaced, when one
has - its executions are kept, and the page shows it beside the file; or nothing, when the file does
not read cleanly or another flow already has its name. Its errors and warnings are marked at their
lines in the script. A template the file names that is not a PNG in the folder beside it is imported
without a picture, and a secret input has no value in the file: it is asked for when the flow
executes.

## Keeping a flow's history and seeing what changed

Every flow is also kept as its `.sflw` file, with its template images beside it, in a git
repository the app makes the first time it starts. Saving a flow writes its files there within a
second; nothing has to be exported. The first launch commits every flow as it is, so each edit
after that shows as a change.

Committing is never automatic. The **Script** tab marks each line changed since the last commit in
its margin: green added, blue changed, a red tick where lines above were removed. The top of the
editor shows the branch and how many of this flow's files changed since the last commit; the count
opens the **Script** tab's **Changes**, which shows
the script against the last commit, unchanged lines folded, and each changed template image before
and after. **Commit** lists every changed file - this flow's ticked, other flows' not - with a
message filled in from what changed, and records exactly the ticked files. **History** on the same
tab lists the commits that changed the flow, newest first, each shown against the one before it.
The Flows page marks a flow with changes not committed yet.

Each run records the commit it executed at, shown in the run's header and in the list of runs, and
marked "uncommitted" when the flow or a sub-flow it calls had changes not committed yet. A run like
that also keeps the script it executed, since no commit holds it.

**Settings › Repository** shows the folder, with Open folder, and the name and email commits are
made as; left empty, they come from the computer's git configuration. The folder belongs to the
app: a file changed there by another program is replaced the next time that flow is saved.

**Discard changes**, in **Changes**, puts the flow back as the last commit has it, after asking,
since what changed since is lost. **Restore this version**, on a commit in **History**, brings the
flow back to how it was after that commit, as a change not committed yet - commit it, or discard it
to return. Both take back a rename too, and keep the flow's executions.

The branch at the top of the editor opens the **branches**. **New branch** starts one from the last
commit and switches to it; nothing changes, and changes not committed yet come along. Picking
another branch switches to it once everything is committed or discarded and no flow is executing:
only the flows that differ between the two are touched, each brought to that branch's version, and a
flow the branch does not have is hidden, never deleted - its executions are kept, and it comes back
on any branch that has it. A switch is all or nothing: a flow that cannot be imported leaves every
flow as it was. Two branches can each have a flow of the same name.

**Settings › Repository › Remote** connects a server such as GitHub, GitLab or Azure DevOps: its
HTTPS address and a token made on the provider's site, allowed to read and write the repository's
contents. **Connect** sends an empty remote this history; a remote with history is pulled instead,
after saying what arrives and what goes. **Push** sends the branch checked out to the remote's
branch of the same name; it is refused while the remote has commits the branch does not - pull
first. **Pull** takes the remote's version and replaces what is here: it shows the commits that
arrive and the ones never pushed that would go, keeps those on a branch named `backup/` and the
date and time, then brings every flow across as a branch switch does. Branches only the remote has
are in the branch menu. There is no merging: when this computer and the remote both changed a
branch, a pull keeps the remote's version, and what was here is on the backup branch.

## Being told when a flow fails

Cover: a `NOTIFY` step in a Failure branch, reporting on the parent step, and what a `SEARCH_IMAGE`
failure attaches.
