# Game Audio

> Craft reference for the `craft-game-floors` skill. Relative links in this file resolve to the skill directory. **Load:** during the feel pass, or whenever a game has zero or unrouted audio. Routing sources into *existing* mixer groups lives in `audio-setup-mixers`; import/memory settings in `optimize-audio`; everything else about the mixer is owned here. Confirm engine behavior against the linked documentation for the installed version before acting on it.

## The floor rule

**Zero audio reads as broken, not minimal.** The paved minimum — this skill's always-on floor, in every build it touches: pickup/confirm blip · milestone sting · ambient bed · UI click. Silence is never the default; "no audio assets" is not an excuse because placeholders can be synthesized.

## Synthesized placeholders (no assets, no downloads)

Generate PCM in code — either write WAVs from an editor script or build clips at runtime with `AudioClip.Create`. Recipes that sound intentional rather than like a test tone:

- **Pickup blip**: sine, 880→1320 Hz pitch envelope over 90 ms, fast decay. Vary pitch ±8% per play so repeats don't grate.
- **Milestone sting**: three ascending notes of a major triad (e.g. 523/659/784 Hz), 120 ms each with slight overlap, soft square or sine.
- **Ambient bed**: 4–8 s loop of low-passed noise (brown noise ≈ integrate white noise, clamp) at low volume — "room tone" — optionally a slow sine swell underneath.
- **UI click**: 20 ms filtered noise burst.

Always apply a 5 ms fade-in/out envelope to every clip — raw edges click.

## The mixer (owned here)

One AudioMixer: `Master → Music, SFX, Ambient`. Every AudioSource routes to a group — nothing on Master directly.

- **Creation is editor-UI work** in the [Audio Mixer window](https://docs.unity.com/en-us/engine/<VERSION>/manual/audio/mixer/overview.md) — there is no public scripting API for creating mixers or groups; the mechanics live under that manual section. Once the groups exist, `audio-setup-mixers` routes every source into the right one — that skill *routes only*: it does not create mixers or groups, set volumes, expose parameters, or configure ducking. Those are this file's recipes.
- **Exposed parameters.** Expose each group's Volume to script (mechanics on the Audio Mixer manual page above) and name the parameters **MusicVol** and **SFXVol** — the shell's settings sliders bind to these names. Set from script with [`mixer.SetFloat("MusicVol", dB)`](https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unityengine/audio/audiomixer/setfloat.md) — its call-timing constraint and its snapshot interaction are documented on that page; the ducking rule below exists because of that interaction.
- **Ducking — never through the parameter the sliders own.** Once a settings slider calls
  `SetFloat("MusicVol", …)`, snapshots stop controlling that parameter, so snapshot ducking of the
  same Music volume silently dies the first time the user touches the slider. Separate the two:
  expose MusicVol/SFXVol for the sliders on the groups' own Volume, and duck through a different
  mechanism — a Duck Volume effect on the Music group fed by a Send from SFX (sidechain; threshold
  high enough that blips don't pump the music), or a snapshot that lowers a *separate*
  duck-dedicated attenuation the sliders never touch. The sidechain route is the floor default
  because it coexists with the sliders by construction.

**Headless floor:** mixer *authoring* needs editor UI, so a fully headless session routes volumes through one static gain layer (Music/SFX floats every source multiplies in) and leaves mixer creation as the first human-session upgrade — the settings sliders bind either way.

## Placement

2D (no spatialization) for UI and milestone sounds; spatialized 3D (`spatialBlend = 1`, sensible min/max distance) for world pickups and ambient emitters. Force short 3D clips to mono (`optimize-audio`).

## Verify

Play-mode check via `unity-cli` eval, branched by implementation. **Mixer build:** trigger a pickup and a milestone, assert the sources played (`AudioSource.isPlaying` true during the clip, or `time` advanced), and assert the list of sources with a null `outputAudioMixerGroup` is empty — a null group means route that source. **Headless gain-layer build:** `outputAudioMixerGroup` is legitimately null everywhere; instead assert every source's volume multiplies through the gain layer (set the Music float to 0, assert the music source's effective volume followed; restore it). Either branch: a source that never plays means the clip was never generated — confirm the synth script ran and the clip's `length` is > 0. Caveat: a timed-out `eval` may still have executed — mutations are not idempotent-safe; put anything long through `run_script`.

## Expansion scope

Generated audio via asset services, adaptive/vertical music layers, footstep surface systems, audio LOD and voice management, loudness normalization to platform targets.
