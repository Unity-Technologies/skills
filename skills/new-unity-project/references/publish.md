# Publish to Unity Play (Step 10) — new-unity-project reference

### Contents

- The human gate
- Confirm the CLI offers publishing
- Build WebGL
- Upload and read the result

The flow's last leg is a link someone can play, because a project that exists only on this machine
hands the user nothing to show. **Publishing is a human gate** (the Mode contract): after Step 9's
commit, ask once whether to upload the project to Unity Play, and wait — the upload is
public-facing and runs on the user's account, so it is never assumed, in paved mode included. Say
plainly what a yes uploads at this point — describe the scene as it actually stands: on the paved
path that is Step 9's playable loop as its done gate scored it, on a `--bare` run the raw
template (not yet a game, so that listing is only a pipeline receipt). On a yes, in order:

1. **Confirm the CLI offers it**: run `unity --help` and look for a publishing command group.
   Most installed CLIs have none yet — then say so, point the user at the WebGL Publisher package
   as the manual route, with `<VERSION>` the package version, eg `4.2` (check it if unsure; the site has no `.md` variant)
   (https://docs.unity3d.com/Packages/com.unity.connect.share@<VERSION>/manual/index.html), record the
   gap in the report, and never drive the Editor UI to publish on the user's behalf.
2. **A WebGL build is the input.** WebGL has no built-in command-line build, so it takes a build
   profile: `unity build --create-profile WebGL` once, then `unity build --profile "Assets/Settings/Build
   Profiles/WebGL.asset" --output-path <dir>` (the `webgl` module must be installed — Step 2's
   modules, else `unity install-modules`).
3. **Upload through that publishing group's own command** — read its `--help` first; it takes the
   build directory, zips and uploads it on the signed-in account, and asks its own confirmation,
   which is the same human gate the user just answered, so pass the confirmation flag its help
   names. Read the result with `--format json`: `url` is the listing and `urlKind` says what kind —
   `listing-edit` is the author's editing page, not a playable link, so hand it to the user as
   "finish the listing here" and present a playable URL only once they have. The command's errors
   are self-contained (sign-in, a non-WebGL folder, the size limit) and name the fix: run the
   named fix and retry only when the failure clearly came before the upload was accepted — after
   acceptance, every invocation uploads again and creates a second listing, so report the accepted
   upload for inspection instead of retrying. A timeout or dropped connection is neither: the
   upload may have been accepted anyway, so do not retry — report the outcome as uncertain and ask
   the user to check their Unity Play listings before any second attempt.

A declined publish is a one-line note in the report with the route to run later, never a nag.

