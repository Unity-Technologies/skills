# Version control — unity-cli command reference

Part of the **`unity-cli`** skill. See that skill's `SKILL.md` for CLI install, global flags,
environment variables, exit codes, and common workflows. All global flags (`--format json`,
`--non-interactive`, `--yes`, `--proxy`, …) apply to every command below.

`unity vcs` covers **GitHub, GitLab, self-hosted git, and Unity Version Control (UVCS)**. It is
not Unity Collaboration — that is `unity collaboration` / `unity collab`, documented in
[collaboration.md](collaboration.md).

**What this group is for.** Generic git porcelain is not its job; you already have `git`. Every
verb here exists because it knows something about Unity that a generic tool cannot: that a scene
is YAML ordered by serialization rather than structure, that a missing `.meta` breaks references,
that switching branches with the Editor running invites a reimport storm, that a changed GUID
silently breaks every prefab pointing at it. When a verb below looks like a git command with a
Unity coat of paint, the Unity part is the point.

---

## Map

| Verb | What it answers |
|---|---|
| `vcs setup` | Get this project into version control and push a first commit |
| `vcs status` | What changed, grouped by what it means to Unity |
| `vcs sync` | Pull safely, with the Editor and LFS accounted for |
| `vcs switch <branch>` | Change branch safely, reporting the reimport it causes |
| `vcs doctor` | Are this repository's Unity settings, ignores, LFS patterns and pinning right |
| `vcs providers` | Which provider can this machine reach, as whom |
| `vcs merge-setup` | Make scene and prefab merges work at all |
| `vcs conflicts` | Which conflicts exist, and which can merge automatically |
| `vcs explain <path>` | What did each branch actually change in this conflicted asset |
| `vcs resolve` | Resolve a conflicted asset, by merge or by taking a side |
| `vcs diff <path>` | What changed inside a scene or prefab, by object name |
| `vcs blame <path>` | **Who** last changed each object in a scene or prefab |
| `vcs summarize` | What did this branch change, ready to paste into a PR |
| `vcs affected` | Which assets, assemblies and tests does a change affect |
| `vcs hooks` | Run the Unity integrity checks at commit time |
| `vcs git …` | Git-only verbs: `migrate-lfs`, `worktree add` / `remove` |
| `vcs uvcs …` | UVCS reads that join `cm` data to your project |

Read the group's own help first when you are unsure — `unity vcs --help` names the providers in
its first line, and every verb takes `--help`.

---

## Getting set up

### vcs setup

One command, zero required flags. It detects everything inferable — whether you are in a Unity
project, whether a repository or remote already exists, which providers you are signed in to —
and asks only what it cannot work out. If a git repository with a remote is already there, it
**adopts** it rather than failing.

```bash
# The whole happy path, from inside the project
unity vcs setup

# Non-interactive, naming the provider and the repository
unity vcs setup --vcs github --git-namespace my-org --git-repo my-game --git-visibility private

# UVCS, which needs no token: it uses your Unity sign-in
unity vcs setup --vcs uvcs --cloud-org my-org --vcs-region <region>

# See what UVCS setup would create, without creating it
unity vcs setup --vcs uvcs --dry-run

# Reuse a saved set of choices, or save this run's
unity vcs setup --preset team-default
unity vcs setup --vcs github --git-namespace my-org --save-preset team-default
```

`--vcs` takes `github`, `gitlab`, `uvcs`, or a **self-hosted host name**. Anything else fails
with `VCS_PROVIDER_UNSUPPORTED` — note the bare word `git` is not a valid value.

**Credentials, in the order they are tried.** You will usually not need to supply one:

1. `--git-token-stdin` (prefer this in CI — an argv token is visible in the process table) or
   `--git-token <pat>`
2. The provider's environment variable
3. Your git credential helper / Git Credential Manager, which may itself open a browser
4. A signed-in provider CLI (`gh` / `glab` / `tea`) — the CLI can offer to run its login for you
5. A guided personal-access-token prompt that names the scopes needed and links the right page

**LFS.** `--git-lfs` initializes Git LFS before the first commit, for new repositories only. If
`git-lfs` is missing the CLI offers to install it through your platform's package manager
(brew / apt / dnf / winget / pacman / zypper / apk); it never vendors its own copy, because git
resolves `lfs` on `PATH` and a private copy would leave a repository only this binary could
satisfy. Set `UNITY_INSTALL_MISSING_TOOLS=1` to accept that offer non-interactively.

### vcs providers

Which provider this machine can reach, as whom, and what each host allows. Spawns the provider
CLIs and makes an authenticated request per host, so it is the **opt-in, network-touching**
counterpart to the offline VCS section of `unity doctor`.

```bash
unity vcs providers
unity vcs providers --host github.example.com --host gitlab.example.internal   # repeatable
unity vcs providers --json
```

Reports binary presence and version, per-host auth state, credential-helper entries, and each
host's capability tier. It deliberately does **not** report how a repository would get created:
that depends on whether a token resolves, and resolving one can prompt — a read-only report must
not ask for a credential. It also does not probe `cm`; that belongs to `unity plugin list`, and
two diagnostics disagreeing about a path is worse than one.

### vcs merge-setup

Scene and prefab merges do not work in a fresh Unity repository. Every editor install ships
`UnityYAMLMerge`, and this wires it up: the `.gitattributes` entries, the merge-driver config,
and a test merge that proves the tool actually runs.

```bash
unity vcs merge-setup                          # set it up, using the editor the project requires
unity vcs merge-setup --check                  # report only; exits 4 when work remains
unity vcs merge-setup --editor-version 6000.0.30f1
unity vcs merge-setup --skip-verify            # skip the proving test merge
```

`--check` **exits 4 when work remains**, which is what makes it usable as a CI gate.

### vcs doctor

The repository-side hygiene audit: git settings, ignore rules, LFS patterns, and package
pinning that a Unity project needs under version control.

```bash
unity vcs doctor
unity vcs doctor --fix                    # repair everything repairable; idempotent
unity vcs doctor --check META,LFS         # run only these checks
```

`--fix` is idempotent by contract — running it twice changes nothing the second time.

Distinct from `unity projects verify`, which is detection-only and never runs git. Different
subjects: `projects verify` checks the asset tree's integrity, `vcs doctor` checks the
repository's Unity configuration.

### vcs hooks

Installs managed `pre-commit`, `post-checkout` and `post-merge` hooks so the Unity integrity
checks run without anyone remembering to.

```bash
unity vcs hooks install      # install or upgrade
unity vcs hooks status       # which managed hooks are installed, and at which version
unity vcs hooks uninstall    # remove the managed hooks, leaving the rest of the file alone
```

Uninstall edits only the managed block, so hand-written hook content survives.

---

## Day to day

### vcs status

What changed, grouped by what it means to Unity rather than by path, and with **meta-file
pairing problems called out** — an asset added without its `.meta`, or a `.meta` left behind by
a move, is the single most common "works on my machine" bug.

```bash
unity vcs status
unity vcs status --json
```

On a UVCS workspace it additionally joins in lock state, which is a join no passthrough to `cm`
can do.

### vcs sync

Pull safely.

```bash
unity vcs sync
unity vcs sync --rebase          # replay local commits on top of incoming ones
unity vcs sync --allow-dirty     # pull over uncommitted changes to tracked files
unity vcs sync --force           # pull even while an Editor holds the project
unity vcs sync --verify          # check the project afterwards without asking
```

**It refuses while an Editor holds the project**, and that refusal is the feature: pulling under
a live Editor invites a reimport storm or a corrupted `Library`. It pulls LFS objects too, then
reports the reimport the pull will cause.

### vcs switch

```bash
unity vcs switch main
unity vcs switch feature/lighting --dry-run      # every check, plus the reimport scope, no change
unity vcs switch main --allow-dirty              # carry uncommitted changes across
unity vcs switch main --discard-changes          # discard uncommitted changes to tracked files
unity vcs switch main --close-editor             # ask the Editor to quit, then switch
unity vcs switch main --force -y
```

Same Editor gate as `sync`, and `--dry-run` reports the reimport the switch would cause — worth
running before a switch between branches that differ in assets.

---

## Reading a change

These four are read-only and never write the working tree or the index.

### vcs diff

A semantic diff of one scene or prefab, **by GameObject and component name rather than by
`fileID`**. A raw `git diff` of a scene is unreadable: everything is a numeric id and the file is
ordered by serialization, so a one-object edit shows up as hunks scattered through thousands of
lines with nothing naming what they belong to.

```bash
unity vcs diff Assets/Scenes/Level.unity                       # HEAD vs your working tree
unity vcs diff Assets/Scenes/Level.unity --from main --to HEAD
unity vcs diff Assets/Prefabs/Player.prefab --json
```

`--from` defaults to `HEAD`; `--to` defaults to **your working tree**, which is why it is not
defaulted to `HEAD` — absent and `HEAD` are different comparisons.

Reads like `Changed Transform on Player at Level/Actors/Player: position {x: 0} → {x: 5}`.

**Identity is reported with a confidence.** Matching is three tiers — `fileID` plus class tag,
then class plus name plus hierarchy path, then content similarity — and every row carries which
tier matched it. Treat anything below `exact` as a hypothesis: a diff that confidently reports a
rename as a delete plus an add is worse than one that admits it is unsure.

**Prefab overrides are flagged, not attributed.** An override lives in the *referencing* asset,
inside `PrefabInstance.m_Modification`, and resolving it means opening the source prefab. So the
change is reported against the instance with a `prefabInstance` flag and a note that attribution
stops at this asset — never as "the Player's mass changed".

### vcs blame

Who last changed each object. `git blame` on a serialized scene answers "who last touched line
4,812", which is not a question anybody has; this answers "who last changed the Player's
Rigidbody".

```bash
unity vcs blame Assets/Scenes/Level.unity

# Just one object -- and, when it names a GameObject, its components too
unity vcs blame Assets/Scenes/Level.unity --object Player
unity vcs blame Assets/Scenes/Level.unity --object Level/Actors/Player

# Just one serialized field
unity vcs blame Assets/Scenes/Level.unity --object Player --field m_Mass

# As of a tag or release branch rather than HEAD
unity vcs blame Assets/Scenes/Level.unity --at v1.4.0

# Bound the history walk
unity vcs blame Assets/Scenes/Level.unity --max-revisions 50

unity vcs blame Assets/Scenes/Level.unity --json
```

One row per object: what it is, its hierarchy path, whether the commit **changed** or **created**
it, the commit, author, date, the identity confidence, and which fields moved.

- `--object` matches an object's own name, its hierarchy path, **or its owning GameObject's
  name** — which is what makes `--object Player` return the Player and every component on it.
- `--field` narrows each row to the newest commit that changed that field, and drops objects
  that do not carry it.
- `--at` blames as of another revision, so you can ask what a release branch shipped.
- `--max-revisions` bounds the walk (default 200). It rations **time**, not memory: peak memory
  tracks the scene's object count and is flat in revision count, while each revision costs
  roughly a fifth of a second.

**Three honest answers to expect, rather than a confident wrong one:**

- `confidence` below `exact` means the object's identity was *matched* across a revision, not
  established — a re-serialization that renumbers `fileID`s lands here. The count is also
  reported as a warning.
- `attribution: unattributed` (`unknown` in the table) means the walk hit its bound with that
  object still unchanged. Its real answer is older; raise `--max-revisions`. In `--json` this is
  `walkBounded: true`, and a consumer acting on the output should branch on it.
- `historyCrossedNonYaml` means history reaches a revision where the asset was
  binary-serialized, so the walk stopped rather than attributing every object to the commit that
  switched Asset Serialization Mode.

A binary-serialized or non-Unity asset is reported as such, never as an empty result.

### vcs summarize

What a branch changed, ready to paste into a pull request: counts by Unity category, plus the
risks worth a reviewer's attention — GUID changes that may break references, assets moved
without their `.meta`, binaries that cannot be reviewed as text, and package-manifest changes.

```bash
unity vcs summarize --since main
unity vcs summarize --since origin/dev --json
```

`--since` is **required** and takes a **revision**; the summary is measured from its merge base
with `HEAD`. This is the one verb in the group that is *not* gated on a Unity project root — the
range is a git range, so it works from a monorepo's root, one level above the project, which is
where PR bodies actually get written.

### vcs affected

Which assets, assemblies and tests a change affects, by walking the GUID reference graph:
changed asset → prefabs referencing its GUID → scenes → asmdefs → affected tests. A CI primitive
no generic tool can build.

```bash
unity vcs affected                      # uncommitted work
unity vcs affected --since main
unity vcs affected --since main --json
```

`--since` takes a revision and uses its **merge base** with `HEAD`; it defaults to `HEAD`, which
reports uncommitted work.

**The report is a lower bound, and every format says so.** Addressables groups, resource-folder
lookups, load-by-name code and binary-serialized assets are real dependencies no static reader
finds. `--json` carries `lowerBound` and a count of `holes`. A consumer that *skips* work on
this is trading correctness for time — see `unity test --affected` in
[build-run-test.md](build-run-test.md), which refuses rather than guessing when a diff carries
non-code changes.

---

## Conflicts

The three verbs in the order a merge presents them: list, understand, resolve.

### vcs conflicts

```bash
unity vcs conflicts
unity vcs conflicts --json
```

Every unresolved conflict, classified by Unity type, **with whether each one can merge
automatically** — which is the column that tells you what needs a human.

### vcs explain

```bash
unity vcs explain Assets/Scenes/Level.unity
```

What each branch changed in a conflicted asset, in plain language, by object and component name.
Requires the path; there is no repository-wide form, because the output is per object inside one
file.

### vcs resolve

```bash
unity vcs resolve Assets/Scenes/Level.unity            # merge with UnityYAMLMerge (the default)
unity vcs resolve Assets/Scenes/Level.unity --ours     # take our side, discarding theirs
unity vcs resolve Assets/Scenes/Level.unity --theirs
unity vcs resolve --all                                # every auto-resolvable conflict
unity vcs resolve Assets/Scenes/Level.unity --editor-version 6000.0.30f1
```

`--merge` is the default and uses `UnityYAMLMerge`, so `vcs merge-setup` should have run first.
`--all` replaces the path operand, which is why the path is optional.

---

## Git-only verbs

`unity vcs git` holds the things UVCS has no equivalent of.

### vcs git migrate-lfs

Finds binaries already committed to history and prints the LFS migration command for them.

```bash
unity vcs git migrate-lfs
unity vcs git migrate-lfs --min-size 500KB
unity vcs git migrate-lfs --since "6 months ago"
```

⚠ **`--since` here takes a git DATE, not a revision** — unlike `vcs summarize --since` and
`vcs affected --since`, which take revisions. Same flag name, three leaves, two meanings.

Two things it deliberately does:

- **The recommendation is narrower than the report.** A size floor over a long history finds
  generated bundles, sourcemaps and docs as readily as textures, and telling a team to put its
  source into LFS stops it diffing and merging. So the include list is restricted to the asset
  types the shipped `.gitattributes` template already routes through LFS, plus what the
  repository already tracks. Everything else is reported and named as *not* offered.
- **A bounded scan still prints an unbounded migration.** `--since` narrows the diagnosis, but
  the printed `git lfs migrate import` carries `--everything`, because a rewrite that skips a
  branch leaves every blob on it reachable and the repository does not shrink.

It prints the command rather than running it: this rewrites history.

### vcs git worktree

A git worktree with the right editor version, a seeded `Library`, and a Hub registry row — so a
second branch is usable in minutes instead of a full reimport.

```bash
unity vcs git worktree add feature/lighting
unity vcs git worktree add feature/lighting --into ../lighting --seed full
unity vcs git worktree add feature/lighting --install-editor
unity vcs git worktree add feature/lighting --dry-run
unity vcs git worktree remove ../lighting
unity vcs git worktree remove ../lighting --discard-changes --force
```

`--seed` takes `cache`, `full`, or `none`. The default deliberately **withholds
`PackageCache/`**: it is three quarters of a real `Library`'s bytes, and copying it finishes
*later* than withholding it, because UPM refills it from its own global store during the import
either way.

`remove` requires the path, so a bare run can never delete the checkout you are standing in.

---

## UVCS

`unity vcs uvcs` holds the reads that **join `cm` data to your project** — the things a raw
passthrough cannot do. Everything else goes straight to `cm`.

```bash
# Wrapped reads: these join cm's data to your working tree
unity vcs uvcs locks                          # who holds a lock, AND which locks cover files you changed
unity vcs uvcs changesets --limit 50          # recent changesets in a stable envelope (default 25, max 1000)

# Code review
unity vcs uvcs review list --status pending
unity vcs uvcs review comments --review 42
unity vcs uvcs review comments --branch /main/feature --all-activity
unity vcs uvcs review reply --review 42 --comment 7 --body "Fixed in cs:118"
unity vcs uvcs review resolve --review 42 --comment 7 --changeset 118

# Everything else: the whole command line forwards verbatim to cm
unity uvcs lock list
unity uvcs shelve -c "wip: lighting pass"
unity uvcs partial update /Assets/Levels
```

**`unity uvcs` and `unity vcs uvcs` are different commands, and the difference matters.**
`unity uvcs <args>` is an opaque passthrough: the whole tail goes to `cm` in `cm`'s own
vocabulary, with `cm`'s own flags and output. `unity vcs uvcs <verb>` is Unity-aware wrapping
with a stable output envelope. Prefer the wrapped verbs when something *parses* the output;
prefer the passthrough when a human reads it, or when you need a `cm` verb we do not wrap.

Mutations in `cm`'s vocabulary — shelves, partial checkout, lock acquire and release — are
deliberately *not* wrapped: `cm` versions that vocabulary, and a wrapper would pin a paraphrase
of it at build time and rot silently.

`review reply` and `review resolve` both require `--review` and `--comment`; `resolve`
additionally requires `--changeset`, the changeset the comment was applied in.

---

## Machine output

Every read verb here supports `--format json` (and `--ndjson`) with the standard envelope:
`success`, `command`, `data`, `errors`, `warnings`. The `data` payload carries raw values —
unsanitized names, paths and field names exactly as the file holds them — because that is what a
consumer matches against the asset. Human and `tsv` output are sanitized for terminal display
instead.

`tsv` is the **default whenever stdout is redirected**, not `human`. So a piped run gets tabular
output, and the advisories (identity confidence, walk bounds, lower-bound caveats) go to
**stderr** rather than being dropped — which is exactly where a script author needs them.

Fields worth branching on rather than ignoring:

| Field | Verb | Why |
|---|---|---|
| `confidence` | `diff`, `blame` | Below `exact`, identity was matched rather than established |
| `walkBounded` | `blame` | The answer may be older than reported |
| `historyCrossedNonYaml` | `blame` | The walk stopped at a binary-serialized revision |
| `prefabInstance` | `diff` | Attribution stops at this asset |
| `lowerBound`, `holes` | `affected` | Real dependencies exist that no static reader finds |
| `outcome` | `diff`, `blame` | `binary-serialized` / `not-a-serialized-asset` are answers, not failures |

---

## Common traps

- **`--since` is not one flag.** A revision on `vcs summarize` and `vcs affected`; a git **date**
  on `vcs git migrate-lfs`.
- **`--vcs git` is not a value.** `--vcs` takes `github`, `gitlab`, `uvcs`, or a self-hosted host
  name.
- **`vcs` is not `collab`, and `unity vcs uvcs` is not `unity uvcs`.** Three adjacent
  namespaces; each one's help says what it is not.
- **A binary-serialized scene defeats every semantic verb here.** `diff`, `blame`, `explain` and
  `resolve` all need text YAML. Set Asset Serialization Mode to **Force Text** in Editor
  settings; the commands say so when they hit it.
- **`sync` and `switch` refuse while an Editor holds the project.** That is the safety feature,
  not an obstacle — reach for `--close-editor` before `--force`.
- **`merge-setup --check` exits 4 when work remains**, so a `set -e` script fails there by
  design.
- **`migrate-lfs` rewrites history.** It prints the command rather than running it. Read the
  warning.
