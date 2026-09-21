# Probes

Four console programs that each prove one thing about the backend by doing it for real, rather
than by asserting it. Each returns `0` when everything passed and the number of failures
otherwise, so any of them works as a CI step as it stands.

They are **not** the test suite. There isn't one yet — the plan for it is the Tests section at the
end of `PLAN.md`, and these are the first things that should move into it. They live here because
each was written to answer a question during a change, each answered it, and each found something
that a green build had not.

They sit outside `backend/` on purpose: `backend/Directory.Build.props` turns on the analyzers,
`TreatWarningsAsErrors` and the banned-symbol lists, and a probe is throwaway code that should not
be held to the rules the product is held to.

Run one with:

```bash
dotnet run --project probes/ScriptRoundTripDatabase
```

---

## PInvokeEntryPoints

**Every converted `LibraryImport` still resolves against live Win32.**

`LibraryImport` generates `ExactSpelling = true` and `DllImport` does not. `user32.dll` exports
`PostMessageA` and `PostMessageW` but no plain `PostMessage`, so a naive conversion compiles,
passes review, and throws `EntryPointNotFoundException` the first time a flow closes a window.
This calls all twenty converted imports for real — real monitors, real window handles, real bounds.
The five that move or close windows are resolved with `Marshal.Prelink` instead of being invoked.

Needs a desktop session; it reads the foreground window.

## TimestampInterceptor

**`CreatedOn` and `UpdatedOn` are stamped on the way to the database, and nothing else is.**

Moving `CreatedOn` off a property initializer and into a `SaveChanges` interceptor changed *when*
it is set, from `new` to save. If the interceptor were not wired up, every row would silently get
`0001-01-01`. Runs against SQLite in memory with the clock set to 2031, so a pass cannot be a
coincidence: stamped on insert, survives a round trip, `UpdatedOn` null until modified, and
`CreatedOn` not re-stamped on update.

## ScriptRoundTrip

**Write, read, write again, byte identical** — over a flow built in memory that uses most of the
grammar: both search kinds, all four search modes, every placement form, branches, a loop, a
section, a comment, and cleanup under `End Execution`.

No database and no files, so it is the fast one to run while changing the parser.

This is the probe that caught `Scroll` being written as `in match`: the writer used the
point-target fragment for its `in` clause, which falls through to `"match"` when a scroll names
neither a point nor a step, while the format means an area. A line no parser could read, found the
moment something tried to read one.

## ScriptRoundTripDatabase

**The same round trip through a real database**, which is the acceptance test `PLAN.md` phase 5
asks for. Seeds a flow, exports it to a temp folder with its template bytes, imports it back over
itself, exports again and compares.

Also checks the two things the pure version cannot:

- a script with `Find Image` misspelled is refused, with the line number
- the flow is **unchanged** afterwards, which is the transactional guarantee
