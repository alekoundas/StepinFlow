# Probes

Console programs that each prove one thing about the backend by doing it for real, rather than
by asserting it. Each returns `0` when everything passed and the number of failures otherwise.

They are **not** the test suite - that is `backend/Tests/`. Each probe moves into it and is
deleted, as the two script round trips already have (`Business.Tests/FlowScript/`). The two left
are next: the timestamp interceptor into `DataAccess.Tests`, and the P/Invoke check into
`Platform.Windows.Tests` behind a desktop-only trait.

They sit outside `backend/` on purpose: `backend/Directory.Build.props` turns on the analyzers,
`TreatWarningsAsErrors` and the banned-symbol lists, and a probe is throwaway code that should not
be held to the rules the product is held to.

Run one with:

```bash
dotnet run --project probes/TimestampInterceptor
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

