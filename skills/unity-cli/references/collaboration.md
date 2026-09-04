# Collaboration — unity-cli command reference

Part of the **`unity-cli`** skill. See that skill's `SKILL.md` for CLI install, global flags,
environment variables, exit codes, and common workflows. All global flags (`--format json`,
`--non-interactive`, `--proxy`, …) apply to every command below. **`--yes` is not a global flag** —
it is bound per-command elsewhere in the CLI and no collaboration command accepts it, so passing it
here is an unknown-option usage error (exit 2). Use `--non-interactive` to skip confirmations.

---

`unity collaboration` (alias **`unity collab`** — both accepted everywhere; examples below use the
canonical name) manages Unity Collaboration resources: review **annotations** on project assets,
their **attachments** (files, sketches, spatial anchors), **Jira** integration, emoji
**reactions**, **thumbnails**, and per-thread read/notification state.

**In-Editor counterpart.** These commands operate on the same annotation data as the
[`com.unity.cloud.collaboration.tools`](https://packages.unity.com/com.unity.cloud.collaboration.tools)
package, which lets users view, create, and reply to annotations from inside the Unity Editor —
including the 3D pins and sketch overlays whose payloads are described under
[Data model](#data-model). Install it through the Package Manager in a cloud-linked project; it is
**experimental** (latest `0.2.0-exp.1`), needs **Unity 6000.0+**, and pulls in
`com.unity.cloud.collaboration` — the service SDK, a separate package id — as a dependency. The CLI
needs no package: it talks to the collaboration service directly, so it works with or without the
Editor open. Annotations created either way are visible to both.

### Shared behavior

**Project scoping — `--project-id` OR an inferred project.** The project-scoped commands
(`annotations`, `attachments`, `reactions`, `thumbnail`, `read`, `subscribe`, `unsubscribe`, and
`jira issues create/get/link/unlink/search/types`) take both `--project-id <id>` (Unity Cloud project
id — find one with `unity cloud project list`, see [auth-license-cloud.md](auth-license-cloud.md))
and `--project-path <path>`. Neither is required: resolution order is

1. explicit `--project-id`, else
2. the `UNITY_CLOUD_PROJECT` env var, else
3. `--project-path` → `UNITY_PROJECT_PATH` env var → the current directory, reading
   `ProjectSettings/PlayerSettings.asset` for the project's `cloudProjectId`, else
4. the stored default cloud project for your active organization
   (`unity cloud project set-default`, see [auth-license-cloud.md](auth-license-cloud.md)).

If none yields an id it fails with: `Could not determine the Unity Cloud project for '<path>'.
Pass --project-id explicitly, or --project-path to point at a project linked to Unity Cloud.` So
inside a cloud-linked Unity project you can drop the flag entirely, and with a default set you can
drop it outside one too. Note the link outranks the default: it is the more specific fact about the
directory you pointed the command at. Most of `jira` is scoped
differently — see [Jira](#jira).

**`--all` — auto-paginate.** `annotations list`, `annotations replies`, and `jira issues list` accept
`--all` to stream every page instead of one. It is **mutually exclusive with `--next` and
`--limit`** — passing either alongside it is an error.

**`--full` and `--resolve-users`** (table output helpers): `--full` prints annotation/reply text
untruncated (whitespace still collapsed to one line); `--resolve-users` replaces user ids with
display names. `--resolve-users` is on `annotations list`/`replies`/`get`/`export` and
`attachments list`; `--full` only on `annotations list`/`replies`.

**Delete confirmations.** `annotations delete`, `annotations delete-fields`, `attachments delete`,
`jira server delete`, and `jira project delete` prompt for confirmation (default **No**) only when
all three hold: output format is `human`, `--non-interactive` was not passed, and both stdin and
stdout are TTYs. In scripts/CI (piped output, `--format json`, or `--non-interactive`) they delete
immediately without prompting.

**`key=value` flags — typed vs string.** Two repeatable pair collectors look identical but behave
differently:

- `--metadata k=v` (typed): each value goes through `JSON.parse`, so `count=3` becomes the number
  `3`, `done=true` a boolean, `tags=["a","b"]` an array. Unparseable values stay strings. To force
  a numeric-looking string to stay a string, quote it as JSON: `--metadata 'k="2"'`.
- `--target-context k=v` (string-only): values are always kept as raw strings.

Both split on the first `=` only (values may contain `=`); repeating a key means last value wins;
a pair without `=` or with an empty key fails with `Invalid key=value pair: <pair>`.

**Raw-JSON flags.** `--camera`, `--local-space` / `--local`, `--time`, `--position`,
`--attachments`, and `annotations list --query` take a JSON value and fail with
`Invalid JSON for --<flag>` when it doesn't parse. Shapes:

| Flag | JSON shape |
|---|---|
| `--camera` | `{"position":{"x":0,"y":0,"z":0},"rotation":{"x":0,"y":0,"z":0},"fieldOfView":60,"target":{...},"projection":"...","verticalSize":1}` — position + rotation required, rest optional |
| `--local-space` (annotations) / `--local` (attachments spatial) | `{"parentId":"...","position":{"x":0,"y":0,"z":0},"cameraPosition":{"x":0,"y":0,"z":0}}` — same shape, different flag name per group |
| `--time` | `{"timeScale":1,"timeStamp":0}` |
| `--position` (attachments spatial) | `{"x":0,"y":0,"z":0}` |
| `--attachments` (annotations create) | JSON array of `{"type":"...", ...}` attachment objects |

---

### Data model

#### Root vs reply

Every annotation — root or reply — is the same type. The distinguishing field is
`rootAnnotationId`:

| Field | Root thread | Reply |
|---|---|---|
| `rootAnnotationId` | `null` | ID of the root annotation |
| `target` | asset or project path | more specific path, often includes `/files/<filename>` |
| `replyCount` | populated | `null` |
| `replyUserIds` | populated | `null` |
| `threadAttachmentsCount` | populated (thread-wide; see caveat below) | `null` |
| `hasDraftReply` | populated | `null` |
| `integrations` | `{}` or populated (Jira lives here) | `null` |
| `resolved` / `resolvedBy` | meaningful (thread-level op) | `null` |
| `camera` / `metadata` | minimal | rich — full viewer state snapshot |

Thread-level operations (resolve/unresolve, subscribe/unsubscribe, Jira linking, thumbnails) act on
the root. Replies capture a richer viewport snapshot (`camera`, `metadata` with `materialOverride`,
lighting, grid state) because they usually represent a specific view at time of writing. Both root
and reply can independently hold `attachments`, `reactions`, and `hasThumbnail`.

#### Target paths

`target` always starts with `<prefix>/projects/<projectId>/...`. The prefix sets the context:

| Prefix | Context | Example |
|---|---|---|
| `assets/projects/<id>/...` | Asset Manager asset | `assets/projects/<projectId>/assets/<assetId>` |
| `assets/projects/<id>/.../files/<name>` | Specific file within an AM asset | `assets/projects/<projectId>/assets/<assetId>/files/mesh.fbx` |
| `unity/projects/<id>/...` | Unity Editor | `unity/projects/<projectId>/assets/<assetId>` |

`**` as a trailing segment matches all descendants (`annotations count` target arg).

The two prefixes are **separate trees, and no glob spans both.** A project routinely holds
annotations under each, so any count or listing is scoped to whichever prefix you name — see the
`count` caveat under [Annotations](#annotations).

#### Mention syntax in `--text`

| Type | Syntax | Example |
|---|---|---|
| User | `:user[Display Name]{#userId}` | `:user[Alex Rivera]{#2475297437902}` |
| Asset | `:asset[Asset Name]{#assetId}` | `:asset[Unity Tower]{#68de9f5d476ac89c752cbf88}` |

#### Attachment payload shapes

`threadAttachmentsCount` on the root counts the **whole thread**, while `attachments list <id>`
returns only the attachments owned by that one annotation. A root reporting 5 can list just its own
single sketch, with the other four hanging off replies — that mismatch is expected, not a bug. To
reach them, list the replies (`annotations replies <id>`) and call `attachments list` per reply id,
or read the `attachments` field directly via `annotations list --include-fields attachments`.

**Don't index it.** `threadAttachmentsCount` is normally a number (or `null`), but a per-type object
(`{ "sketch": 4, "spatial-3d": 3, "file": 1 }`) also shows up in real payloads. Guard the type
before reading it rather than assuming either shape.

Every attachment object also carries its type under **two** keys, `type` and `Type`, with the same
value — the API emits both and the CLI passes responses through verbatim. Key off lowercase `type`;
that is what the formatters use.

**`sketch`** — 2D drawing overlay captured over a 3D viewport. `sketchData` is a JSON *string*
(stroke/arrow records with positions, colors, widths):
```json
{
  "type": "sketch",
  "attachmentId": "689f4236496f6d50dcbd6e20",
  "sketchData": "<JSON string>",
  "camera": { "position": {}, "rotation": {}, "fieldOfView": 60, "target": {}, "projection": "perspective" },
  "preview":     { "filePath": "..._preview.png", "fileSize": 42770, "contentType": "image/png", "status": "Uploaded" },
  "sketchImage": { "filePath": "..._sketch.png",  "fileSize": 42770, "contentType": "image/png", "status": "Uploaded" },
  "metadata": { "materialOverride": "default", "wireframe": -1 },
  "created": "2025-08-15T14:20:38.806Z",
  "createdBy": "2475297437902"
}
```

**`spatial-3d`** — numbered 3D pin on a mesh in world space. Multiple pins per annotation, each
with an incrementing `label`. `camera.target` points at the pin's `position`; no
`preview`/`sketchImage` (it's a point, not an image):
```json
{
  "type": "spatial-3d",
  "attachmentId": "6a1effe63e164caf9cea8aee",
  "label": "1",
  "position": { "x": -0.403, "y": 2.763, "z": 0.066 },
  "camera": { "position": {}, "rotation": {}, "fieldOfView": 60, "target": { "x": -0.403, "y": 2.763, "z": 0.066 } },
  "metadata": { "materialOverride": "default", "wireframe": -1 },
  "created": "2026-06-02T16:08:06.935Z",
  "createdBy": "2475297437902"
}
```

**`file`** — generic upload (image, document). The annotation's top-level `camera` is `null` for
these — not tied to a 3D viewport:
```json
{
  "type": "file",
  "attachmentId": "6a1effcfe9693f7f4d84ad13",
  "filePath": "qa_no_replies.jpg",
  "fileSize": 348842,
  "fileType": "image",
  "contentType": "image/jpeg",
  "status": "Uploaded",
  "metadata": {},
  "created": "2026-06-02T16:07:43.657Z",
  "createdBy": "2475297437902"
}
```

---

### Annotations

An annotation is a review comment anchored to a target path (e.g.
`unity/projects/<projectId>/assets/<assetId>`). A **reply** is an annotation whose
`rootAnnotationId` points at the thread root — `create --reply-to <id>` makes one, and
`replies <id>` lists a thread. Status lifecycle: `Draft` → `Sending` → `Active`.

Annotation objects returned by `get`/`list`/`replies` (`--format json`) carry: `annotationId`,
`messageType`, `target`, `targetContext`, `rootAnnotationId`, `status`, `text`, `created`/
`createdBy`, `updated`/`updatedBy`, `resolved`/`resolvedBy`, `metadata`, plus include-only fields
(below).

| Command | Args | Key options |
|---|---|---|
| `count` | `[target]` (glob `**` at end OK) — **defaults to `unity/projects/<id>/**` only**, see below | `--grouped` (per-target breakdown), `--offset <n>`, `--limit <n>` |
| `create` | `<target>` | `--text`, `--reply-to <id>`, `--status Active\|Draft`, `--metadata k=v`…, `--target-context k=v`…, `--camera`, `--local-space`, `--time`, `--attachments`, `--unresolve-root-annotation` |
| `delete` | `<annotationId>` | (confirmation — see Shared behavior) |
| `delete-fields` | `<annotationId> <field...>` | removes metadata fields; variadic; confirmation |
| `export` | — | `--target <path>` — **defaults to `assets/projects/<id>/**` only**, see below; `--out <file>` (else stdout), `--resolve-users`; the service returns `assetId` + `assetName` here that `list` does not — the CLI copies the response page verbatim, so treat those as service behavior |
| `get` | `<annotationId>` | `--fields a,b,c` or `--fields all` (table output only), `--resolve-users` |
| `list` | — | `--query <json>` (optional — defaults to root threads only), `--next <cursor>`, `--limit 1-100` (default 10), `--all`, `--sort Ascending\|Descending`, `--sort-field annotationId\|latestReply`, `--include-fields a,b`, `--fields a,b` or `--fields all` (table output only), `--full`, `--resolve-users` |
| `replies` | `<annotationId>` | `--next`, `--limit 1-100`, `--all`, `--sort`, `--status-filter All\|Active\|Sending\|Draft` (repeat flag), `--fields a,b` or `--fields all` (table output only), `--full`, `--resolve-users` |
| `resolve` / `unresolve` | `<annotationId>` | — (echoes only `annotationId`, see below) |
| `status` | `<annotationId> <Active\|Sending\|Draft>` | — |
| `update` | `<annotationId>` | `--text`, `--metadata k=v`…, `--camera`, `--local-space`, `--time` — at least one required |

```bash
# Create a thread on an asset, with typed metadata (count is a number, build stays a string)
unity collaboration annotations create "unity/projects/$PROJ/assets/$ASSET" \
  --project-id $PROJ --text "Texture seam visible here" \
  --metadata severity=2 --metadata 'build="2024.1"' \
  --camera '{"position":{"x":0,"y":1,"z":-5},"rotation":{"x":0,"y":0,"z":0}}'

# Reply to it
unity collaboration annotations create "unity/projects/$PROJ/assets/$ASSET" \
  --project-id $PROJ --reply-to $ANNOTATION_ID --text "Fixed in latest import"

# List root threads (default query) from inside a cloud-linked project — no --project-id needed
unity collaboration annotations list --include-fields replyCount,latestReply --format json

# Custom query — must be a JSON ARRAY of clauses; the default is
#   [{"type":"hasNot","field":"annotationParentId"}]   (root threads only)
unity collaboration annotations list --project-id $PROJ \
  --query '[{"type":"hasNot","field":"annotationParentId"}]' --all --format ndjson

# Export for offline analysis — ONE prefix tree per run; the bare command covers
# only assets/**, so an Editor-annotated project needs both invocations.
unity collaboration annotations export --project-id $PROJ \
  --target "assets/projects/$PROJ/**" --out annotations-assets.json
unity collaboration annotations export --project-id $PROJ \
  --target "unity/projects/$PROJ/**"  --out annotations-editor.json
```

**`export` is not a whole-project export.** With no `--target` the command defaults to
`assets/projects/<projectId>/**`, so annotations under the separate `unity/projects/<projectId>/**`
tree are silently absent from the archive — and nothing in the output says so. Always pass `--target`
explicitly, once per prefix, when completeness matters. (Note the default differs from
`annotations count`, which defaults to the `unity/**` tree instead.) The CLI's own `--target` help
text says "the whole project", which contradicts the actual default — trust the prefix above.

**`--query` shape.** A JSON *array* of clauses (a bare object is rejected:
`The --query value must be a JSON array of query clauses.`). Clause vocabulary is the
collaboration API's — e.g. `{"type":"hasNot","field":"annotationParentId"}`. Omitting `--query`
applies exactly that root-threads-only clause.

A supplied `--query` **replaces** that default rather than adding to it, so a lone filter clause
(e.g. `[{"type":"glob","field":"target","value":"assets/**"}]`) returns replies interleaved with
roots. Re-add `{"type":"hasNot","field":"annotationParentId"}` alongside your clause to keep
thread-roots-only results.

**Include-only fields.** `replyCount`, `replyUserIds`, `latestReply`, `attachments`,
`threadAttachmentsCount`, `replyLastReadTimestamp`, and `replyUnreadCount` come back null/absent
unless named in `--include-fields` on `list` (server omits them by default). If `replyCount` is
unexpectedly null, that's why.

**`delete-fields`** removes **`metadata` sub-keys**, not top-level annotation fields — the variadic
args are metadata key names (`delete-fields <id> severity build`). A name that isn't a metadata key
is a silent no-op (it is echoed back in `data.fields` and the command still exits 0), including
`metadata` itself: passing it does **not** clear the object.

**`count` with no target counts one prefix tree only.** It defaults to
`unity/projects/<projectId>/**`, so it reads like a project-wide total but omits everything under
`assets/projects/<projectId>/**` — in a project with annotations on both, the bare command can
report 16 while 34 more exist. Pass the target explicitly (once per prefix) when you want a real
total, and prefer `--grouped` to see which trees are populated.

**The mutators echo ids, not the annotation.** `resolve` and `unresolve` return just
`{ "annotationId": … }`; `status` adds `status`, and `delete-fields` adds the `fields` it was asked to
remove. None return a `resolved` timestamp or the updated object. A read-after-write therefore needs a follow-up `get`; an id-only response is success, not a
silent failure. Re-resolving an already-resolved thread is an error (`HTTP 409 … is already
resolved`), which is one way to confirm the first call landed.

---

### Attachments

Attachments hang off an annotation. Three kinds: **file** (uploaded blob), **sketch** (2D drawing
over a camera view), **spatial** (labeled 3D anchor) — payload shapes in
[Data model](#attachment-payload-shapes). All commands take `--project-id`.

| Command | Args | Key options |
|---|---|---|
| `list` | `<annotationId>` | `--resolve-users` |
| `delete` | `<annotationId> <attachmentId>` | (confirmation — see Shared behavior) |
| `download` | `<annotationId> <attachmentId>` | `--out <path>` (default: the attachment's original filename in CWD, falling back to `<attachmentId>` when it has no file path), `--force` (overwrite), `--width <px>` (resize image) |
| `upload` | `<annotationId> <file>` | `--name` (display name), `--content-type` (override inferred MIME) |
| `add file` | `<annotationId> <file>` | same options and **same handler** as `upload`; only the reported command label, the success message, and the JSON error code (`COLLAB_ATTACHMENTS_ADD_ERROR`) differ — use either |
| `add sketch` | `<annotationId>` | `--sketch-data <json>` **(required)**, `--camera <json>` **(required)**, `--time <json>`, `--preview <file>`, `--sketch-image <file>` |
| `add spatial` | `<annotationId>` | `--label` **(required)**, `--position <json>` **(required)**, `--camera <json>` **(required)**, `--time <json>`, `--local <json>` |
| `update [file]` | `<annotationId> <attachmentId>` | `--content-type`, `--metadata k=v`… — **at least one required**; `file` is the **default variant**: `update <ids…>` without a subcommand means `update file` |
| `update sketch` | `<annotationId> <attachmentId>` | `--sketch-data`, `--camera`, `--time`, `--metadata k=v`… — each individually optional, but **at least one required** |
| `update spatial` | `<annotationId> <attachmentId>` | `--label`, `--position`, `--camera`, `--time`, `--local`, `--metadata k=v`… — each individually optional, but **at least one required** |

```bash
# Attach a screenshot (upload and `add file` are interchangeable)
unity collaboration attachments upload $ANNOTATION_ID ./screenshot.png --project-id $PROJ

# Add a labeled 3D anchor
unity collaboration attachments add spatial $ANNOTATION_ID --project-id $PROJ \
  --label "Broken collider" --position '{"x":1.2,"y":0,"z":3.4}' \
  --camera '{"position":{"x":0,"y":2,"z":-4},"rotation":{"x":15,"y":0,"z":0}}'

# Download; refuses to overwrite an existing file unless --force
unity collaboration attachments download $ANNOTATION_ID $ATTACHMENT_ID --project-id $PROJ \
  --out ./shot.png --force
```

Notes:

- `--sketch-data` is passed through as a raw string, not parsed as JSON — only `--camera`/`--time`/
  `--position`/`--local` get JSON validation at the CLI layer.
- Options required on `add sketch`/`add spatial` become *individually* optional on the matching
  `update` variant — but every variant, `file` included, rejects a flagless invocation with a
  `NO_FIELDS` error. Pass at least one change flag.
- The spatial local-space flag is `--local` here, but `--local-space` on annotations (same JSON
  shape).

---

### Reactions, thumbnails, read state

All use the same optional project resolution as `annotations` — `--project-id` or `--project-path`,
else inferred from the current project (see [Shared behavior](#shared-behavior)).

| Command | Args | Key options |
|---|---|---|
| `reactions add` / `reactions remove` | `<annotationId> <emoji>` | emoji is a single Unicode emoji, e.g. `👍` |
| `thumbnail upload` | `<annotationId> <file>` | image file; MIME inferred from extension (jpg/png/gif/webp) |
| `thumbnail download` | `<annotationId>` | `--out <path>` (default `./thumbnail`), `--width <pixels>`; **no `--force`** — errors if the file exists ("Delete it first") |
| `read` | `<annotationId>` | `--timestamp <iso8601>` (default now) — marks the thread read up to that time (per-user read receipt) |
| `subscribe` / `unsubscribe` | `<annotationId>` | per-thread notification subscription for the current user |

```bash
unity collaboration reactions add $ANNOTATION_ID 👍 --project-id $PROJ
unity collaboration read $ANNOTATION_ID --project-id $PROJ   # mark thread read as of now
```

---

### Jira

Connects Collaboration annotations to Jira. Three layers, three id types — don't mix them up:

1. **Server config** (`serverConfigId`): a Jira server + credentials, scoped to a Unity
   **organization** (`--organization-id`).
2. **Project config** (`projectConfigId`, flag `--jira-project-config-id`): a Jira project
   (`--jira-project-id` — the Jira-side id) under a server config, linkable to Unity projects.
3. **Issues**: created from / linked to annotations, scoped by Unity `--project-id`. The resulting
   link lands in `annotation.integrations.jiraIssues[]` — see
   [Jira integration payload](#jira-integration-payload) below.

**Scoping — most of `jira` does not use the project resolver** (no `--project-path`, no inference):

| Group | Scoping |
|---|---|
| `jira server *` | `--organization-id`, required (rejected at parse time) |
| `jira project add` / `delete` / `update` | `--organization-id`, required (validated by the handler) |
| `jira project link` / `unlink` | Unity project id is a **positional** (`<unityProjectId> <projectConfigId>`); no `--organization-id` at all |
| `jira issues list` | `--organization-id`, required (rejected at parse time) |
| `jira configs` | exactly **one** of `--organization-id` or `--project-id`, **no `--project-path`** and no inference |
| `jira issues create/get/link/unlink/search/types` | `--project-id` / `--project-path`, or inferred — see [Shared behavior](#shared-behavior) |

**`--help` never tells you which options are required.** No collaboration option is annotated as
required in help output, on any command — so the tables in this file are the only place that
distinction is written down. What *does* differ is where a missing option is caught, and therefore
which exit code you get:

| Enforcement | Commands | Behavior when omitted |
|---|---|---|
| Parse time | `jira server add/delete/update/test/users/projects/permissions`, `jira issues create/get/search/types`, `jira issues list --organization-id`, `attachments add sketch` / `add spatial` required flags | usage error, **exit 2** |
| Handler | `jira project add/delete/update --organization-id`, `jira configs` | command failure (error envelope), not a usage error |

`jira project link` / `unlink` take positionals instead, so a missing id is always a parse-time
usage error.

#### `jira server` — server configurations

| Command | Args | Key options |
|---|---|---|
| `add` | — | `--organization-id`, `--url`, `--username`, `--key` (API token), `--name` — all required |
| `delete` | `<serverConfigId>` | `--organization-id` (required); confirmation |
| `update` | `<serverConfigId>` | `--organization-id` (required) + at least one of `--url`/`--username`/`--key`/`--name` |
| `test` | — | `--organization-id`, `--url`, `--username`, `--key` — all required; validates credentials **without persisting** |
| `users` | `<serverConfigId>` | `--organization-id` (required), `--query <text>` — search Jira users |
| `projects` | `<serverConfigId>` | `--organization-id` (required) — lists **Jira-side** projects on the server |
| `permissions` | `<serverConfigId>` | `--organization-id`, `--jira-project-id` — both required; checks required Jira permissions |

#### `jira project` — project configurations

| Command | Args | Key options |
|---|---|---|
| `add` | `<serverConfigId>` | `--organization-id`, `--jira-project-id`, `--default-reporter-id` — all required, though `--help` doesn't say so (fallback reporter when an annotation author has no Jira match) |
| `delete` | `<projectConfigId>` | `--organization-id` (required, not marked in `--help`); confirmation |
| `link` / `unlink` | `<unityProjectId> <projectConfigId>` | — (Unity project id is positional here, not a flag) |
| `update` | `<projectConfigId>` | `--organization-id` (required, not marked in `--help`), `--default-reporter-id`, `--linked-unity-project-id <id>` (repeatable — **replaces** the whole linked list), `--clear-linked-unity-projects` (mutually exclusive with the previous flag); at least one change flag required |

#### `jira issues`

| Command | Args | Key options |
|---|---|---|
| `create` | `<annotationId>` | `--jira-project-config-id`, `--summary`, `--type <issueTypeId>` — required; `--project-id`/`--project-path` optional (inferred); `--description`, `--assignee-user-id`, `--reporter-user-id`, `--parent-issue-id` (sub-task) |
| `get` | `<jiraIssueId>` | `--jira-project-config-id` required; `--project-id`/`--project-path` optional (inferred) |
| `link` / `unlink` | `<annotationId> <jiraIssueId>` | `--project-id`/`--project-path` optional (inferred); `link` also takes optional `--jira-project-config-id`. `unlink` does **not** delete the issue in Jira |
| `list` | — | `--organization-id` **(required, org-scoped — no `--project-id`/`--project-path` here)**, `--profile all\|active\|resolved\|unresolved\|draft\|sending` (repeat flag), `--next <token>`, `--limit 1-100` (default 10), `--all`, `--sort Ascending\|Descending` |
| `search` | — | `--jira-project-config-id` required; `--project-id`/`--project-path` optional (inferred); `--query <text>` (plain text, **not JQL**), `--include-subtasks` |
| `types` | — | `--jira-project-config-id` required; `--project-id`/`--project-path` optional (inferred); lists issue type ids for `create --type` |

#### `jira configs`

`unity collaboration jira configs` — single command. Pass **exactly one** of `--organization-id` (all
configs in the org) or `--project-id` (configs available to that Unity project).

```bash
# One-time setup: validate credentials, persist server, add a Jira project, link Unity project
unity collaboration jira server test --organization-id $ORG \
  --url https://jira.example.com --username bot@example.com --key $JIRA_TOKEN
unity collaboration jira server add --organization-id $ORG \
  --url https://jira.example.com --username bot@example.com --key $JIRA_TOKEN --name "Main Jira"
unity collaboration jira project add $SERVER_CONFIG_ID --organization-id $ORG \
  --jira-project-id 10042 --default-reporter-id $JIRA_ACCOUNT_ID
unity collaboration jira project link $PROJ $PROJECT_CONFIG_ID

# File an issue from an annotation (get valid type ids from `issues types` first)
unity collaboration jira issues create $ANNOTATION_ID --project-id $PROJ \
  --jira-project-config-id $PROJECT_CONFIG_ID --summary "Texture seam" --type 10001
```

#### Jira integration payload

Lives in `annotation.integrations.jiraIssues[]` on the root (`integrations` is `null` on replies).
Multiple issues can be linked to one thread.

```json
{
  "integrations": {
    "jiraIssues": [
      {
        "type": "Jira",
        "jiraIssueId": "18955",
        "jiraProjectConfigId": "6997204de238d85bc249625b",
        "jiraIssueKey": "PROJ-1",
        "jiraIssueUrl": "https://yourcompany.atlassian.net/browse/PROJ-1",
        "sourceAnnotationId": "698e04cb85335d04c814ea67",
        "createdBy": "2474131300352"
      }
    ]
  }
}
```

- `jiraIssueKey` — human-readable key (e.g. `PROJ-1`); use for display.
- `jiraIssueUrl` — direct link to the issue.
- `sourceAnnotationId` — the annotation the issue was created/linked from; may differ from the
  annotation carrying the integration when linked from a reply.

---
