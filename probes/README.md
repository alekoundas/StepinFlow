# Probes

Console programs that each prove one thing about the backend by doing it for real, rather than
by asserting it. Each returns `0` when everything passed and the number of failures otherwise.

They are **not** the test suite - that is `backend/Tests/`. Each probe moves into it and is
deleted, as three already have - the script round trips into `Business.Tests/FlowScript/`, the
timestamp interceptor into `DataAccess.Tests`. The one left is the P/Invoke check, which needs a
desktop, and goes into `Platform.Windows.Tests` behind a desktop-only trait.

They sit outside `backend/` on purpose: `backend/Directory.Build.props` turns on the analyzers,
`TreatWarningsAsErrors` and the banned-symbol lists, and a probe is throwaway code that should not
be held to the rules the product is held to.

Run one with:

```bash
dotnet run --project probes/PInvokeEntryPoints
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

