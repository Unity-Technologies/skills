# Sprite atlas documentation pages

Each Engine page lists its local file (under `<editor-install>/Editor/Data/Documentation/en/Manual/`, or `en/ScriptReference/` for Script Reference pages) and its online `.md` page. Replace `<VERSION>` with the project's Unity version (for example `6000.3`), or with the Addressables package major.minor for the Addressables pages.

## Engine Manual and Script Reference

- **Packing sprites into atlas textures** — local `sprite/atlas/atlas-landing.html`, online `https://docs.unity.com/en-us/engine/<VERSION>/manual/unity2d/sprite/atlas.md`: overview of sprite atlases, with links to every atlas page.
  - **Create a sprite atlas** — local `sprite/atlas/create-sprite-atlas.html`, online `https://docs.unity.com/en-us/engine/<VERSION>/manual/unity2d/sprite/atlas/create-sprite-atlas.md`: what an atlas holds and how its packable objects are gathered.
  - **Sprite Atlas Inspector window reference** — local `sprite/atlas/sprite-atlas-reference.html`, online `https://docs.unity.com/en-us/engine/<VERSION>/manual/unity2d/sprite/atlas/sprite-atlas-reference.md`: what each packing, texture and platform setting does.
  - **Create lower resolution versions of sprite atlases** — local `sprite/atlas/master-variant/master-variant-sprite-atlases.html`, online `https://docs.unity.com/en-us/engine/<VERSION>/manual/unity2d/sprite/atlas/master-variant-sprite-atlases.md`: master and variant atlases, and how a variant scales the master's texture.
  - **Load sprite atlases manually at runtime** — local `sprite/atlas/distribution/load-sprite-atlas-spriteatlasmanageratlasrequested.html`, online `https://docs.unity.com/en-us/engine/<VERSION>/manual/unity2d/sprite/atlas/load-sprite-atlas-spriteatlasmanageratlasrequested.md`: late binding with `SpriteAtlasManager.atlasRequested`.
  - **Upgrade Sprite Atlas V1 assets** — local `sprite/atlas/v2/sprite-atlas-v2.html`, online `https://docs.unity.com/en-us/engine/<VERSION>/manual/unity2d/sprite/atlas/sprite-atlas-v2.md`: Sprite Atlas V2 and migrating from V1.
- **Choose a GPU texture format by platform** — local `texture-choose-format-by-platform.html`, online `https://docs.unity.com/en-us/engine/<VERSION>/manual/materials-and-shaders/textures/textures-getting-started/texture-compression-formats/texture-choose-format-by-platform.md`: which compression formats each platform supports, for platform overrides.
- **SpritePackerMode** (Script Reference) — local `SpritePackerMode.html`, online `https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unityeditor/spritepackermode.md`: the values `EditorSettings.spritePackerMode` takes, including `Disabled`.
- **IPreprocessBuildWithReport** and **IPostprocessBuildWithReport** (Script Reference) — local `Build.IPreprocessBuildWithReport.html` and `Build.IPostprocessBuildWithReport.html`, online `https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unityeditor/build/ipreprocessbuildwithreport.md` and `https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unityeditor/build/ipostprocessbuildwithreport.md`: the build callbacks the generated scripts implement.

## Addressables package

Read these from `<project-folder>/Library/PackageCache/com.unity.addressables@*/`, or online under `https://docs.unity3d.com/Packages/com.unity.addressables@<VERSION>/manual/` if that folder has no .md files.

- **Addressables package** — `Documentation~/index.md` (online `index.html`): overview of the package, with links to every page.
  - **Addressables and sprite atlases** — `Documentation~/AddressablesAndSpriteAtlases.md` (online `AddressablesAndSpriteAtlases.html`): how atlases and their sprites load through Addressables.
  - **Create a group** — `Documentation~/groups-create.md` (online `groups-create.html`): creating the group that holds the atlas entries.
  - **Labels** — `Documentation~/Labels.md` (online `Labels.html`): tagging entries so atlases load by label.
  - **Start a build from a script** — `Documentation~/build-scripting-start-build.md` (online `build-scripting-start-build.html`): building Addressables content from the postprocess script.
  - **Build Addressables content with Player builds** — `Documentation~/build-player-builds.md` (online `build-player-builds.html`): building content as part of the player build.
  - **Load assets** — `Documentation~/load-assets.md` (online `load-assets.html`): loading an atlas by address in the late-binding loader.
  - **Release Addressable assets** — `Documentation~/UnloadingAddressableAssets.md` (online `UnloadingAddressableAssets.html`): releasing loaded atlases to free memory.
