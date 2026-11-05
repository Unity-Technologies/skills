# Source control commands

Read-only commands that compare the last successful revision (`<good>`) with the first failed one (`<bad>`) in the project's local checkout. `<path>` is a file's path as the command that lists the changed files prints it.

- Run them from the project's directory.
- Never run a command that changes the checkout, such as `fetch`, `pull`, `checkout`, `update`, `switch`, `sync` or `submit`, because the user decides what changes in their checkout.
- If the checkout lacks either revision, ask the user to bring it up to date, and skip the local read.

## Which source control

- **Git:** `lastBuiltRevision` is a 40-character hexadecimal hash. This covers GitHub, GitLab and Bitbucket connections too.
- **Unity Version Control, Perforce or SVN:** `lastBuiltRevision` is a plain number: a changeset, a changelist or a revision.
- **Telling those three apart:** a `.plastic` folder means Unity Version Control, and a `.svn` folder means SVN. For Perforce, `p4 info` shows a client root that contains the project's directory.

## Git

- `--name-status` prints paths from the repository's root, so `:/` makes `git diff` read `<path>` from there too.
- `core.quotePath=false` prints a name with non-ASCII characters as it is, not as quoted octal escapes.
- A renamed file prints as `R`, a similarity score, its old path and its new path. To see what changed in it, pass both: `":/<old path>" ":/<new path>"`.

```sh
git rev-parse --is-inside-work-tree
git cat-file -e "<good>^{commit}" && git cat-file -e "<bad>^{commit}"
git log --oneline <good>..<bad> | head -n 50
git -c core.quotePath=false diff --name-status <good> <bad> | head -n 40
git diff <good> <bad> -- ":/<path>" | head -n 200
```

## Unity Version Control

- Never pass a path to `cm diff`, because with one it opens the Diff window instead of printing. Compare a file's two versions with `cm cat` instead.
- Cap every `cm` command's output with `head -c`, never `head -n`. When the sign-in has expired, `cm` asks "Select your system" and, with no one to answer, repeats it forever on one line, which `head -n` lets through.
- `cm` keeps running after `head` stops reading, so run each `cm` command in the background, where you can stop it. If one prints that question, or runs for more than a minute, stop it. Then ask the user to sign in by running `cm repo list <server>` in their own terminal, with the server the question names. Signing in is the user's action.
- `--repositorypaths` makes `cm diff` print repository paths, such as `/Assets/A.cs`. `cm cat` reads them only with the `serverpath:` prefix.
- For a file `cm diff` marks `A` (added) or `D` (deleted), the `cm cat` of the side without it fails with "The specified revision was not found". That's expected: the file is new or gone.

```sh
cm wi . | head -c 1000
cm find changeset "where changesetid > <good> and changesetid <= <bad> and branch = '<scmBranch>'" --format="{changesetid} {comment}" --nototal | head -c 6000
cm diff cs:<good> cs:<bad> --repositorypaths | head -c 4000
cm cat "serverpath:<path>#cs:<good>" --file="<temp-dir>/good" | head -c 1000
cm cat "serverpath:<path>#cs:<bad>" --file="<temp-dir>/bad" | head -c 1000
diff "<temp-dir>/good" "<temp-dir>/bad" | head -n 200
```

## Perforce

`<good+1>` is the number after `<good>`, so the list leaves out the last successful changelist. `...` is every file under the current directory. `diff2 -q` prints depot paths such as `//depot/Project/Assets/A.cs#2`; `<path>` is that path without the `#` and the number after it.

```sh
p4 info
p4 changes -m 50 -L ...@<good+1>,@<bad>
p4 diff2 -q ...@<good> ...@<bad> | head -n 40
p4 diff2 -du "<path>@<good>" "<path>@<bad>" | head -n 200
```

## SVN

`--non-interactive` makes a read that needs a password or a certificate decision fail instead of waiting for an answer. If one fails that way, ask the user to run it once in their own terminal.

```sh
svn info
svn log --non-interactive -r <bad>:<good+1> --limit 50
svn diff --non-interactive --summarize -r <good>:<bad> | head -n 40
svn diff --non-interactive -r <good>:<bad> "<path>" | head -n 200
```
