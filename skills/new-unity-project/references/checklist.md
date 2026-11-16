# Checklist — new-unity-project reference

Tick every line before the Final report.

- [ ] Delegated skills checked before Step 1 (incl. `craft-game-floors`): a missing one offered, or named
      in the report
- [ ] Mode decided once; in paved, the manifest printed before the create launches
- [ ] Brief captured; platforms + monetization recorded
- [ ] Background create with `--install-editor` + modules, URP template (`--template 3d`/`2d`)
      unless Built-in was requested; `--bare` only on the user's ask; result reconciled
- [ ] Source control attached (Step 4), or local git + Unity `.gitignore`
- [ ] Packages via `package add` (or the C# script pre-open); `manifest.json` verified; no
      render-pipeline package atop the template
- [ ] Identity confirmed from the result or wired in Step 6 (stated cost on a failed link)
- [ ] `craft-game-floors` loaded (or its absence named); `unity-pipeline` read once the Editor was ready
- [ ] Baseline applied: Volume + camera post on, quality tier, UI stack (no `OnGUI`), URP shaders,
      settings read back with `script eval` (skipped on `--bare`; on 6000.5.0–6000.5.2, Editor updated first)
- [ ] `.meta` files exist; first commit made; `Library/` excluded
- [ ] At least one playable loop closed (floor 7, or the user's stated shape), the done gate run on
      the build, and the build committed (skipped on `--bare`)
- [ ] Publish asked once after the Step 9 commit, never assumed; on a yes, the Unity Play link in
      the report (the gap named on CLI versions without `publish`)
- [ ] Forward baton offered and written only on a yes (none on `--bare`); handoff done if applicable
