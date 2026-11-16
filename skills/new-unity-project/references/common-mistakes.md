# Common mistakes — new-unity-project reference

- **Blocking on the create** instead of backgrounding it: with `--install-editor` it waits for the
  whole Editor install.
- **Acting before the manifest prints** (paved), or burying an assumption outside it — if it isn't
  in the manifest, it isn't assumed.
- **Wiring auth twice** — if create's result shows identity, Step 6's wiring is a skip.
- **Hand-editing `manifest.json`** instead of the package commands or the skill's script.
- **Doing a required sub-skill's job from memory** when it isn't loaded, without telling the user
  or offering the install.
- **Committing `Library/`/`Temp/`/`obj/`/`Build/`**, or scripts without their `.meta` files.
- **Missing Editor modules** — a mobile target needs `android`/`ios`; WebGL needs `webgl`.
- **Picking `com.unity.template.2d` / `.3d` by name** — Built-in templates; use `--template 3d`/`2d`.
- **Skipping the visual baseline** — flat template defaults, `OnGUI` HUDs, and
  `Shader.Find("Standard")` under URP.
- **Stopping at the baseline** — a project that renders but doesn't play leaves the done gate
  nothing to score. The opposite slip: piling on content before the first loop closes.
- **Publishing on an assumption, or not asking at all** — the link is the flow's last leg, and
  it's a human gate in both directions: never uploaded without a yes, never silently dropped.
