---
name: route-audio-to-mixer-groups
description: Routes Audio Sources into existing Audio Mixer Groups, classifying each source by what it plays. Use when the user asks about cleaning up mixer assignments, routing audio through a mixer, or which group a sound belongs in.
---
# Route Audio to Mixer Groups

## Important

- Use the unity cli (`unity command --format json`) and the `unity-cli` and `unity-pipeline` skills for each step.
- Before making changes, establish a baseline of the current behavior so you can tell whether a later result is actually caused by your change.
- Create a restore point you can roll back to if your changes fail.
- Do only what's asked. Don't change unrelated assets or files.
- Avoid long explanations.
- Never leave verification code in the user's script. Never build logging or detection they didn't ask for. See [Final step](#final-step).

## Full topic map

Before you go forward with a workflow or diagnosis:

1. Open and read the [documentation page(s)](#full-topic-map) that cover the specific subject related to what you intend to do or fix and check if you've missed anything.
2. Name the file(s) you read in your response.

This is because your training knowledge about Unity might be out of date, incorrect, or for the wrong Unity version.

If the Editor install folder has a `Data/Documentation` folder (eg `...Unity/Hub/Editor/6000.6.0b2/Editor/Data/Documentation`), read the html files from `<editor-install>/Editor/Data/Documentation/en/Manual` and `<editor-install>/Editor/Data/Documentation/en/ScriptReference`, using the local file names below.

Otherwise read the online pages listed below under `https://docs.unity.com/en-us/engine/<VERSION>/`, as markdown.

Prefer fetching the page directly (for example `WebFetch`) over searching (for example `WebSearch`) — faster and lands on the exact reference. Only fetch what you need, and only search if you don't have the exact URL. Replace `<VERSION>` with the following before fetching:

- **docs.unity3d.com**: The Unity version, eg `6000.3`
- **Engine on docs.unity.com**: The Unity version, eg `6000.3`

Find the Unity version in `ProjectSettings/ProjectVersion.txt`, and use only its first two parts (`6000.3.2f1` becomes `6000.3`).

Manual pages: local file in `en/Manual`, then online page under `https://docs.unity.com/en-us/engine/<VERSION>/manual/`:

- `AudioMixerOverview.html` — `audio/mixer/overview.md`: what an Audio Mixer and its groups do.
- `AudioMixerSpecifics.html` — `audio/mixer/specifics.md`: creating a mixer and adding groups in the Audio Mixer window.
- `class-AudioSource.html` — `audio/reference/class-audio-source.md`: Audio Source properties, including Output.

Script Reference pages: local file in `en/ScriptReference`, then online page under `https://docs.unity.com/en-us/engine/<VERSION>/script-reference/`:

- `Audio.AudioMixer.FindMatchingGroups.html` — `unityengine/audio/audiomixer/findmatchinggroups.md`: listing a mixer's groups.
- `AudioSource-outputAudioMixerGroup.html` — `unityengine/audiosource/outputaudiomixergroup.md`: reading and setting an Audio Source's group.
- `AssetDatabase.FindAssets.html` — `unityeditor/assetdatabase/findassets.md`: finding mixer assets, and its overloads.

Routing an Audio Source to a mixer group is a scene edit that only a running Editor can
make, so this skill needs a live Editor it can execute C# in. Step 0 establishes that
before anything else.

**Automate the routing; hand group creation to the user.** Inspect mixers and route Audio
Sources into groups yourself, because that is all public Unity API. Don't create a mixer or
a group yourself, because that has no public API; it exists only on types Unity does not
commit to keeping stable. Inventory what exists, propose the routing, ask the user to add
any missing group in the Audio Mixer window, then do all the routing yourself.

That is a deliberate limit, not a gap to work around. Do not reach for reflection to
create groups, and do not hand-edit a `.mixer` file — mixer structure is not safely
authorable blind.

### API reference

The API is `UnityEngine.Audio.AudioMixer`, `UnityEngine.AudioSource` and `UnityEditor.AssetDatabase`, part of the Unity Engine. It is versioned by Unity version, so check the project's Unity version before relying on a specific class or method.

1. Use the `unity-cli` skill to check the project's Unity version.
2. If the Editor install folder has a `Data/Documentation` folder (e.g. `<editor-install>/Editor/Data/Documentation`), grep the html files in the `en/ScriptReference` folder inside, and skip the remaining steps.
3. Otherwise, fetch and cache `https://docs.unity3d.com/<VERSION>/Documentation/ScriptReference/xrefmap.yml` (replace `<VERSION>` with the Unity version, e.g. `6000.3`). This file lists every public API member's fully qualified name as a `uid` (e.g. `uid: 'UnityEngine.Audio.AudioMixer.FindMatchingGroups(System.String)'`).
4. Grep the yml file locally for a class or method instead of refetching per lookup. The file is large (about 6 MB), so don't read the whole file.
5. Convert the `uid` to a markdown page: remove the parameters in brackets, lowercase it, replace `.` with `/`, and add `.md`. Then fetch `https://docs.unity.com/en-us/engine/<VERSION>/script-reference/<converted uid>`. For example, `UnityEngine.Audio.AudioMixer.FindMatchingGroups(System.String)` => `https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/audio/audiomixer/findmatchinggroups.md`. All overloads are on the same page.
   - Constructors use `ctor` (e.g. `unityengine/vector3/ctor.md`), and indexers use `item` (e.g. `unityengine/vector3/item.md`).
   - For operators, fetch the class page (e.g. `unityengine/vector3.md`) and follow its link.
   - Links inside the pages start with `/engine/`. Add `https://docs.unity.com/en-us` before them.
   - If the page doesn't exist for that Unity version, use the `href` in the `xrefmap.yml` entry instead (e.g. `href: 'Audio.AudioMixer.FindMatchingGroups.html'` => `https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Audio.AudioMixer.FindMatchingGroups.html`).

Always use the docs for the project's Unity version rather than relying on training knowledge, since APIs are frequently marked obsolete between versions.

## Step 0: Confirm you can run C# in the Editor

Every C# step below runs inside a live Editor through the Unity CLI. **The `unity-cli` skill
owns getting you there** — installing the CLI, confirming a connected Editor, adding the
project's `com.unity.pipeline` package, telling a genuinely absent Editor apart from one
stuck in Safe Mode, and discovering the Editor's command catalog. Follow it first; don't
re-derive any of it here.

Two things it can't know for you:

- **You need `script eval` in particular**, not just a reachable Editor. Confirm it appears in the
  catalog. Its presence depends on the Pipeline package version, not on the CLI, so a
  healthy install can still lack it — if it's missing, say so and stop.
- **Do not fall back to editing `.mixer` files by hand.** Mixer routing is not safely
  authorable blind, so an unreachable Editor is a stop, not a cue to improvise.

Once `script eval` is available, that is how each C# step below runs.

Run C# through the connected Editor with the `script eval` command. Discover its parameter shape
from `unity command --format json` rather than assuming one — the inline form is
`unity command script eval --code '<snippet>'`, and some Pipeline versions also register
`script eval_file` for running a snippet from a file. **Check the catalog before reaching for
`script eval_file`; it is frequently absent.** `script eval`'s default timeout depends on the Pipeline version, so read it from the command's schema (`unity command --detail full --format json`) and pass `--timeout <seconds>` for a slow call.

### Passing C# to `script eval`

`script eval` compiles a **statement block, not a file**, so write no `using` directives and fully
qualify every type. If `script eval` returns one of these compile errors, apply the fix and re-run:

| Error | Cause | Fix |
|---|---|---|
| `CS0210` | A `using` directive, which the compiler reads as a resource-disposal statement | Remove every `using` directive |
| `CS0246` / `CS0103` | A bare type name, such as `AssetDatabase` or `AudioMixer` | Fully qualify it, for example `UnityEditor.AssetDatabase` |
| `CS0104` | A bare `Object`, which is ambiguous with `object` | Write `UnityEngine.Object` |

Where a snippet below is written as a file — with usings, for readability, or because it is
meant to be saved into the project — qualify the types before passing it to `script eval`.

## Step 1: Pre-flight
If the user hasn't explicitly asked for Audio Mixers, confirm that they want to proceed with setting them up, because routing changes the output of every Audio Source in the scene. Otherwise go straight to the inventory.

Then inventory what already exists with the mixer-inventory snippet in
[references/api.md](references/api.md), run through the Editor as described in Step 0. That gives
you every mixer in the project and the group names in each.

It returns a flat list of groups, not the parent/child tree. That is enough to route into, and it
is all the public API exposes. If the hierarchy matters for the conversation, ask the user to look
at the Audio Mixer window and describe it — don't reach for the non-public tree API to find out.

**If the project has no mixer at all,** say so and stop rather than improvising one: creating a
mixer has no public API. Ask the user to create one (Window → Audio → Audio Mixer, then the **+**
next to Mixers), and pick up from here once it exists.

## Step 2: Find scene references
1. List every Audio Source, its Audio Clip and its current group with the read-routing snippet in
   [references/api.md](references/api.md).
2. Classify each source by its Audio Clip asset name into a sound category, preferring one that
   already exists as a group, because the user's existing mix structure is the one to keep.
   Examples for Audio Clip asset names:
   - "FootStep4_Sound" -> Foley
   - "Dialogue_Female_Scene4" -> Vox/Voice/Dialogue
   - "GunShot" -> SFX
   - "Menu_Theme_Variation" -> Music
3. If the clip name isn't descriptive, or the source has no Audio Clip (for example a script
   supplies the audio at runtime), classify it by its GameObject name, then by the MonoBehaviours on
   the same GameObject, because those are the next most reliable signs of what it plays.
4. If none of those make the category clear, propose an Uncategorized group for that source rather
   than guessing, because a wrongly routed sound is harder for the user to spot than an unsorted one.

## Step 3: Agree the group list, and get any missing groups created
Present the classification from Step 2 as a proposed routing — each Audio Source and the group you
intend to send it to — and revise it with the user.

**WAIT for the user to respond before proceeding.**

Prefer an existing group when it genuinely covers the category, even if you'd have named it
differently. But **don't collapse categories that a mixing engineer would keep apart** — Foley is a
subset of SFX, not another word for it, so a gunshot does not belong in a `Foley` group just because
one exists. When the existing groups only partly cover your categories, say which ones fit and which
need a new group, and let the user decide.

For categories with no matching group, you cannot create the group — there is no public API for it.
Hand it over precisely, naming the mixer and the exact group names, as shown at the end of
[references/api.md](references/api.md). Then **re-run the inventory snippet to confirm the groups
exist and check their spelling** before routing. Don't assume the user did it, and don't assume
they spelled it the way you asked.

## Step 4: Route the Audio Sources
With the group list settled and confirmed present, assign each Audio Source's output group using the
routing snippet in [references/api.md](references/api.md). It is public API throughout, and it wraps
the whole pass in a single undo step so the user can back all of it out at once.

**Key the mapping on the identifier you classified by.** Step 2 reads the clip asset name first and
only falls back to the GameObject name, so the mapping accepts either — the two are different
identifiers and keying on the wrong one drops sources.

Three things to report rather than assume:

- **Any `NO SUCH GROUP` entries the snippet returns.** That means a group you expected is not in the
  mixer — usually a spelling difference. Resolve it with the user, don't silently skip the source.
- **Any `NOT IN THE MAPPING` entries.** Those are Audio Sources your classification missed. Reporting
  a successful routing while sources were quietly left unrouted is the worst outcome here, because it
  reads as success.
- **The scene was modified, not the mixer asset.** Routing lives on the Audio Source, so it only
  persists once the scene is saved. Tell the user, and save only with their agreement.

**Volume, effects, and re-parenting are out of scope.** Those live on non-public API. If the user
asks for them, say the routing is done and point them at the Audio Mixer window for the mix itself.

## Final step

Do the following checks:

1. Re-run the read-routing snippet in [references/api.md](references/api.md) and check every
   routed Audio Source now reports its agreed group. If one doesn't, report it rather than re-running
   the routing, because a mismatch usually means a misspelled group or two GameObjects sharing a name.
2. Check the project has zero console errors. Use `console status` for counts.
3. Stop there unless the user asked you to prove it works or you suspect a specific fault.
4. If you do enter Play mode, do it once only to chase a fault you already suspect. Read the world
   through `script eval` rather than adding logging. Take no screenshots, because they take too much time.

If a check fails, go back and reread the [docs pages](#full-topic-map) to find out what you missed.

## Final report

Report to the user, in order:

1. **Routed**: each Audio Source and the group you routed it to.
2. **Not routed**: any `NOT IN THE MAPPING` sources, and why.
3. **Missing groups**: any `NO SUCH GROUP` entries, and the exact group names the user needs to create.
4. **Save the scene**: whether the scene still needs saving for the routing to persist.
5. **Side effects**: anything else you changed, and the undo step ("Route Audio Sources to mixer
   groups") that reverts the whole routing pass.

## References
See [references/api.md](references/api.md)