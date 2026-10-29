# Docs workflow: find, cite and match Unity documentation

Part of the **`unity-cli`** skill. See that skill's `SKILL.md` for CLI install, global flags, environment
variables and exit codes. The full flag list for `unity docs` and `unity docs toc` is in
[diagnostics-maintenance.md](diagnostics-maintenance.md#docs--version-matched-unity-documentation); this file
is the order an agent follows, with 2D examples.

**Why use it:** the pages `unity docs` resolves match the Editor version of the project you are in. An API
remembered from another Unity version is a common source of wrong code, 2D physics and Tilemap included.

Relative links in this file resolve to the skill directory's `references/` folder, so `../SKILL.md` is the skill's main page.

## Contents

- The flow
- Version flags
- The page index cache
- Rules for agents

## The flow

1. **Find the page with `docs toc`.** It lists real pages, so you never guess a name.
2. **Resolve the address with `docs --url`.** It prints the docs.unity.com address and opens nothing.
3. **Cite or read it.** Put the address in your answer or review comment, or print the page's Markdown with
   `docs --content`. Without `--url` or `--content`, `unity docs` opens the page in the default browser
   (only useful when a person is watching).
4. **Cache the docs with `docs cache add`** before working offline, or when you will read many pages.
5. **Pin the version** whenever the project is not the one you are standing in (see Version flags).

For a package's manual (`com.unity.*`), use `docs --package` instead of steps 1 and 2.

### 1. Find a page: `docs toc`

```bash
unity docs toc --format json                             # scripting API reference, this project's version
unity docs toc --manual --format json                    # the manual
unity docs toc --editor-version 6000.0 --format ndjson   # one page per line, for a version branch
```

Each entry has a `path` (`unityengine/rigidbody2d/addforce`) and an absolute `url`. Under `--format json`,
`data` is `{version, requestedVersion, section, entries}`. `--format ndjson` writes one entry per line. The
default human and `--format tsv` output show the path and URL as two columns. The index carries no page
titles, only paths, so search the entries for the class or member name you want.

Only the Engine scripting API and the manual have an index. `--cli`, `--hub` and `--cloud` publish none, so
`toc` doesn't take them.

### 2. Resolve the address: `docs --url`

```bash
unity docs --url Rigidbody2D                  # the class page
unity docs --url Rigidbody2D.AddForce         # a member page
unity docs --url Tilemap.SetTile              # another 2D member
unity docs --url UnityEngine.SpriteRenderer   # a qualified name matches exactly
unity docs --url --manual "2D physics"        # the manual is a search, so this returns a search address
```

A bare class name or `Class.member` matches the page whose path ends with it (preferring `UnityEngine`, then
`UnityEditor`). A name two pages tie on goes to search rather than a guess. The lookup never fails the
command: offline, a qualified name is built directly when a version is known (a project or
`--editor-version`), and anything else goes to search.

Add `--format json` to see how the address was found:

```bash
unity docs --url Rigidbody2D.AddForce --format json
```

`data` carries `url`, `opened` (false under `--url`), `version` (the docs version in the URL, `null` for a search or a home page),
`requestedVersion` (the one you asked for) and `resolvedBy`: `index` (an exact page from the index),
`direct` (built without the index) or `search` (a search page). When `resolvedBy` is `search`, the address is
a list of results, not the page. For a scripting API page, run `docs toc` and pick a path. The manual has
no name lookup, so `docs --url --manual` always returns search results: for a manual page, cite the `url` of
the `docs toc --manual` entry you picked.

### 3. Read a page: `docs --content`

```bash
unity docs --content Rigidbody2D.AddForce                      # the page's Markdown on stdout
unity docs --content Rigidbody2D.AddForce --format json        # wrapped with url, version and source
```

The scripting API resolves by name as `--url` does. The manual has no name lookup, so `--content --manual`
fails; read a cached manual page from its file instead (below).

### Package manuals: `docs --package`

```bash
unity docs --package com.unity.2d.sprite --url                   # the manual for the version the project uses
unity docs --package com.unity.inputsystem --url --format json   # adds package, packageVersion and resolvedBy
```

Package manuals live on docs.unity3d.com, not docs.unity.com. The version is the one in the project's
`Packages/packages-lock.json`, a package built into the Editor uses its one manual version, and anything else
(outside a project too) uses the newest release on the Unity registry. A name the registry doesn't list fails
with `DOCS_PACKAGE_UNKNOWN`, and a registry that can't be reached with `NETWORK_UNREACHABLE`. `--package`
takes no topic and can't be combined with another documentation set, `--search` or `--editor-version`.

### 4. Work offline: `docs cache`

```bash
unity docs cache add                                     # the manual, this project's version
unity docs cache add --api --editor-version 6000.0       # the scripting API too (about 96,000 pages)
unity docs cache --format json                      # what is cached, per version
```

`add` prints the folder it fills: one Markdown file per page, `<section>/<path>.md` under the version, with
the section's listing in `<section>.txt` beside it. After that:

- `docs --content` reads a cached page without a request (`source: cache` in json), and caches every page it
  fetches online too.
- `docs toc` and the name lookup behind `docs --url` and `--content` use the cached listing when
  docs.unity.com can't be reached.
- Manual pages are read from their files: `docs toc --manual` path `get-started/first-time-user/2dor3d` is
  `manual/get-started/first-time-user/2dor3d.md` under the version's folder.

A failed `add` (`DOCS_CACHE_INCOMPLETE`) resumes where it stopped when run again. Cached pages don't
expire: run `add` again to refresh them, `docs cache clear --yes` to delete them. `unity cache clean`
deletes them with the other downloads.

### Other documentation sets

`--cli`, `--hub` and `--cloud` look a topic up in the Unity CLI, Unity Hub or Unity Cloud documentation. Topic
names are lowercase and hyphenated (`use-unity-cli`, `project-create`, `developer-data/collection-settings`).
These sets are not versioned by Editor, so `--editor-version` with one of them is a usage failure, and so is
passing two sets at once.

```bash
unity docs --url --cli use-unity-cli
unity docs --url --hub project-create
```

## Version flags

The documentation version is, in order:

1. `--editor-version <version>`: a full version (`6000.0.26f1`) or a version branch (`6000.0`). An
   unparseable value is a usage failure, not a silent fallback.
2. The current project's `ProjectSettings/ProjectVersion.txt`. No Editor has to be installed.
3. The newest version docs.unity.com hosts.

A version the site doesn't host resolves to the nearest hosted version below it (or the oldest hosted one),
with a one-line notice on stderr. Read `version` against `requestedVersion` in the JSON when the difference
matters.

Pin the version in two cases: you are not inside the project (a new 2D project you have yet to create, or a
question about another Editor), or the project upgrades soon and you are checking an API against the target
version.

```bash
unity docs --url Rigidbody2D.AddForce --editor-version 6000.0
unity docs toc --editor-version 2022.3 --format ndjson
```

## The page index cache

`docs toc` and the index lookup behind `docs --url` download the page index once a day and read it from a
local cache after that, so repeat calls in a session are fast. With no connection and no fresh cache:

- `docs toc` fails with `NETWORK_UNREACHABLE` (exit 7). A server error is `DOCS_TOC_FETCH_FAILED` and an
  index that can't be read is `DOCS_TOC_INVALID`.
- `docs --url` does not fail. It builds a qualified name directly when a version is known (a project or
  `--editor-version`) and sends anything else to search.

This daily cache holds the page index only. For page content offline, and an index that never expires, use
`docs cache add` (step 4): with a cached version, `docs toc` lists it offline instead of failing.

## Rules for agents

- Resolve the address first and cite it. Don't quote an API from memory when a page for the project's
  version exists.
- Prefer `--format json` and read `resolvedBy`, so you can tell a real page from a search.
- Don't open a browser from an unattended run: use `--url`.
- Only use documentation addresses the CLI printed: `docs.unity.com` for the Engine, CLI, Hub and Cloud
  docs, and `docs.unity3d.com/Packages/` for package manuals from `docs --package`.
