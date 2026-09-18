# Search result scheduling regression tests (#798)

## Scope of this patch

Search submission and textbox updates remain immediate. Only application of asynchronous count/page results to the WPF-bound collection is queued at `DispatcherPriority.Background`, below `Input`. There is no debounce interval, timer, minimum query length, result cap, or IPC worker-thread rewrite.

The dispatcher checks cancellation before running a queued operation. The collection also checks its captured query token **inside** the callback, so replacement/disposal between query completion and UI application cannot publish stale results. The explicitly synchronous scrollbar-drag path is unchanged.

### Evidence and limits

- The pristine 3.4.0 source built successfully before modification.
- The real SearchBox pair retained 1,000 programmatic edits, so a simple binding feedback loop was not reproduced.
- An isolated native-parent/WPF-child probe intermittently lost injected letters when results refreshed. The same behavior occurred with a fake search backend and no focus change. This falsifies the claim that moving Everything IPC alone is sufficient.
- Unhosted and search-as-you-type-disabled controls passed. Blocking the UI with a test-only sleep, without refreshing results, also retained input.
- Patched native-hosted tests passed with both a fixture and the real Everything 1.4.1 backend. However, a later old-behavior control also passed. The integration symptom is timing-dependent; these results do **not** prove every instance of #798 is fixed or establish an exact Win32/TSF failure mechanism.
- The deterministic suite below proves the scheduling/cancellation contract. Its legacy immediate-dispatch control fails because a completed result is published before pending input.

This is a focused input-priority mitigation to test in the affected toolbar, not a claim of a conclusively identified Windows keyboard-routing root cause. IME/dead-key interaction, actual Explorer-hosted Deskband, ARM64, and long-running use still need manual validation.

## Deterministic tests (no windows, no input injection)

From the repository root, with a Windows .NET SDK:

```text
dotnet run --project tests/EverythingToolbar.Search.Tests -p:Platform=x64 -p:SignAssembly=false
```

This builds only the App/Core projects and the production WPF result dispatcher, without C++/COM hosting or Explorer build hooks. Ten tests cover pending-input priority, UI-thread ownership, dispatcher cancellation, superseded/disposed count and page updates, current-page notifications, synchronous scrollbar behavior, and the native probe's fail-closed foreground guard.

The negative control is expected to exit nonzero:

```text
dotnet run --project tests/EverythingToolbar.Search.Tests -p:Platform=x64 -p:SignAssembly=false -- --legacy-control
```

It deliberately executes result callbacks inline, emulating the old application behavior. This checks that the input-priority assertion can actually fail; it is not an automated reproduction of missing physical keys.

## Native input integration probe (explicit opt-in)

Build the complete x64 solution with Visual Studio MSBuild first (native C++ projects and COM-reference resolution require it). Disable signing for a local build. The production project normally has process-stop build hooks; avoid running these against a loaded development Deskband. For this task the launcher pre/post-build events were suppressed with `-p:PreBuildEvent= -p:PostBuildEvent=` and the Deskband target reported it was not loaded.

The probe references the **completed** Debug build by default; it does not invoke the production build hooks. With `-c Release` it references the Release build. `ProbeBinaries` can override the reference directory to compare a separately built baseline.

```text
dotnet run --project tests/EverythingToolbar.InputProbe -p:Platform=x64 -p:SignAssembly=false
```

Without `--send-input`, this prints usage and never opens a window or injects input.

For an interactive session, stop using the keyboard and mouse during each short run:

```text
dotnet run --project tests/EverythingToolbar.InputProbe -p:Platform=x64 -p:SignAssembly=false -- --send-input --fake
dotnet run --project tests/EverythingToolbar.InputProbe -p:Platform=x64 -p:SignAssembly=false -- --send-input
```

The fake backend uses explicit test fixtures, not filesystem results. The second command queries the running default Everything instance. The probe uses real application search controls and popup, with a separate-thread native parent instead of Explorer. It compares the exact injected string against both visible text and the search model at three intervals, reporting key/text-event counts and refreshes. It has no persisted configuration, no global hotkeys, and no installed-toolbar or registry changes. Foreground/focus loss aborts the run rather than refocusing repeatedly.

Additional controls:

- `--unhosted`: leave the editor in an ordinary WPF window.
- `--manual`: disable search-as-you-type in the in-memory settings.

Do not treat `PostMessage(WM_CHAR)` or assigning TextBox.Text as equivalent to physical-input routing. Do not count aborted/focus-interrupted trials as reproductions. Burst injection is a stress test, not proof of normal human-keyboard/IME behavior.
