# Identity: set it up with the telemetry

Workflow A step 5 and the identity rows of the decisions reference point here.

On a cloud-linked project with no sign-in of its own, install
Unity Authentication and kick off services init plus anonymous sign-in at bootstrap; where
the game already signs in, add neither (a second `SignInAnonymouslyAsync` throws). The
install is the one greenfield item that adds a **service dependency**, so it is never
bundled silently: the shown plan carries it as its own line — *adds dependency:
`com.unity.services.authentication` (anonymous sign-in; player-keyed telemetry)* — and the
report lists it. (The retrofit route offers it as a question.) Services Core
initialization itself is not optional: every collector holds its deliveries until
`UnityServices.InitializeAsync()` has completed, so the bootstrap kicks it off whether or
not Authentication is installed, unless the game already initializes Core: then that call
and its options stay as they are and none is added, since the first call's options win
(the api-surface reference, Envelope calls). Never block
`Configure()...Apply()` or `EnterSession()` on it: where the package
carries an Authentication context provider (check `Runtime/ContextProviders/`), the
provider samples per envelope, so telemetry starts immediately and the player id
attaches from the moment sign-in lands (an offline first run just carries no player id).
An anonymous id is effectively install-scoped until the player links an account — the
value of wiring it now is that identity upgrades in place when they do. A project that
is not cloud-linked skips this, and the report says what that means: with no cloud
project id the runtime's collection policy is Off, so the game sends nothing at all, the
Log collector included, and verification needs a linked project reachable from the
Editor. The developer-provided id (`WithUser`) stays a separate, developer-chosen
identity per Constraint 7.
