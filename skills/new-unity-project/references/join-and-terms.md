# Step 3: join the create, reconcile, accept terms — new-unity-project reference

### Contents

- Read the create result and reconcile it with the manifest
- Install modules when the Editor was already present
- Accept the Editor Software Terms

Wait for the background create and read its JSON result: `data.path` is the project,
`data.version` the Editor, `data.editorInstalled` whether this run installed it.

**Paved: reconcile the result against the manifest**, narrating divergence — the cloud-link
soft-fail included (`cloudLinked: false`, warnings, project intact; Step 6 keys off it). A 403 on
the link is often the saved default org refusing new projects — retry
`unity projects link cloud <path> --cloud-org <an org the user owns>` before concluding
no-identity. A result carrying a `steps` field reconciles step-by-step from it.

`--module` only applies when the run installs the Editor: on `data.editorInstalled: false`, add
the platform modules with `unity install-modules -e <version> -m <platform-modules> --accept-eula`
(skips what's already there). If create failed, surface the error (`unity-cli` troubleshooting)
and stop — a failed install creates no project, and nothing downstream works without an Editor.

**Once create succeeded, accept the Editor Software Terms before any open, and only after the
user's yes.** If the terms question is still unanswered, wait for the answer before running
anything. If the user declined, don't run `unity terms accept`: stop before the first open and say
the Editor won't start past its terms dialog. On a yes, run
`unity terms accept <data.version> --architecture <data.architecture> --yes` with no Editor running
(a running Editor can overwrite the answer). It reports `alreadyAccepted: true` and writes nothing
when they are already recorded. Do the same for any Editor the run moves to later, such as the
6000.5.3+ update in Step 7. If `unity status --until-ready` after an open reports
no instance while the Editor process is running, check `unity terms status <version> --architecture <architecture>`: a
`not-accepted` Editor is sitting on that dialog, which only the user can click.

