# Sign-in and license details — new-unity-project reference

### Contents

- Rules for signing in and activating
- Declining

- Never tell the user to sign in or activate a license themselves, and never hand them commands to
  run. You run them, after they say yes.
- On yes, sign in only where the choice needs an account: Personal and an existing subscription
  activate the signed-in user's licenses, and a cloud-linked project needs one too. Run
  `unity auth login` then (it opens the browser; the user only completes the sign-in page, so tell
  them that page is coming), in the background or with its output unfiltered, and pass on the
  sign-in link it prints in case the browser doesn't open: a pipe through `tail` holds the link
  back until sign-in ends. Then `unity license activate --personal --accept-eula` for Personal.
  Serial and floating activation need no account: `unity license activate --floating`, or the
  serial through `UNITY_LICENSE_SERIAL` and a bare `unity license activate`, so it stays off argv.
- Declining stops the run before the create only when nothing can license the Editor. A user who
  declines sign-in but has, or activates, a serial or floating license still gets a local project:
  create with `--no-cloud` and say the cloud link and player accounts are off.

