# Source control reads

Read-only reads that compare the last successful revision (`<good>`) with the first failed one (`<bad>`) in the project's local checkout, and that check whether a file exists at the built revision (`<rev>`, the build's `lastBuiltRevision`). Use the project's own source control. For Unity Version Control, use the commands under [Unity Version Control](#unity-version-control).

- Run them from the project's directory.
- Never run a command that changes the checkout, such as a fetch, a pull, an update or a branch switch, because the user decides what changes in their checkout.
- If the checkout lacks a revision a read needs, ask the user to bring it up to date, and skip the local read.
- Cap each read's output, because a wide range can print thousands of lines.
- Some reads stop to ask for a password, a sign-in or a certificate decision. Run them without prompts where the tool allows it. If one fails or waits for that reason, stop it and ask the user to run it once in their own terminal.
- If the checkout isn't on the target's branch (`scmBranch`), give the checks that depend on the branch to the user, because the checkout holds another branch's files.

## What to read

1. The revisions after `<good>` up to `<bad>`, with their messages, up to 50.
2. The files they changed, up to 40. `<path>` below is a file's path as this list prints it. It can be relative to the repository's root, which can sit above the project's directory, so read each file from there. For a renamed file, compare its old path with its new one.
3. The diff of each suspect file between `<good>` and `<bad>`, up to 200 lines.
4. Whether a file exists at `<rev>`, and whether it's ignored or was never added, because either keeps it out of the build. `<file>` is a path relative to the project's directory, such as `Assets/Scripts/Player.cs`. To find the file that defines a missing type, search the project's directory first, for example `grep -rlE --include='*.cs' '(class|struct|interface|enum) <Name>\b' Assets Packages | head -n 20`, then check each file it finds.
   - Check the checkout has `<rev>` first, because a missing revision fails the same way as a missing file.
   - A file whose content is only a pointer to large file storage needs its content on that storage's server, or the build gets the pointer.

## Unity Version Control

A `.plastic` folder in the project's directory or a folder above it means Unity Version Control, and `lastBuiltRevision` is a changeset number.

- Never pass a path to `cm diff`, because with one it opens the Diff window instead of printing. Compare a file's two versions with `cm cat` instead.
- Cap every `cm` command's output with `head -c`, never `head -n`. When the sign-in has expired, `cm` asks "Select your system" and, with no one to answer, repeats it forever on one line, which `head -n` lets through.
- `cm` keeps running after `head` stops reading, so run each `cm` command in the background, where you can stop it. If one prints that question, or runs for more than a minute, stop it. Then ask the user to sign in by running `cm repo list <server>` in their own terminal, with the server the question names. Signing in is the user's action.
- `--repositorypaths` makes `cm diff` print repository paths, such as `/Assets/A.cs`. `cm cat` reads them only with the `serverpath:` prefix.
- For a file `cm diff` marks `A` (added) or `D` (deleted), the `cm cat` of the side without it fails with "The specified revision was not found". That's expected: the file is new or gone. For the same reason, the last command below fails if `<file>` isn't in the project at `<rev>`.

```sh
cm wi . | head -c 1000
cm find changeset "where changesetid > <good> and changesetid <= <bad> and branch = '<scmBranch>'" --format="{changesetid} {comment}" --nototal | head -c 6000
cm diff cs:<good> cs:<bad> --repositorypaths | head -c 4000
cm cat "serverpath:<path>#cs:<good>" --file="<temp-dir>/good" | head -c 1000
cm cat "serverpath:<path>#cs:<bad>" --file="<temp-dir>/bad" | head -c 1000
diff "<temp-dir>/good" "<temp-dir>/bad" | head -n 200
cm cat "<file>#cs:<rev>" --file="<temp-dir>/exists" | head -c 1000
```
