# Cloud Code Reference

## Table of Contents

- [Four Execution Models](#four-execution-models)
  - [Cloud Code Modules vs CloudBehaviours](#cloud-code-modules-vs-cloudbehaviours)
- [Where the details live](#where-the-details-live)

Accessed via `CloudCodeService.Instance` (`ICloudCodeService`, namespace `Unity.Services.CloudCode`). Assembly: `Unity.Services.CloudCode`.

Call `UnityServices.InitializeAsync()` from `com.unity.services.core` and sign in via `com.unity.services.authentication` before use.

---

## Four Execution Models

| Model | Language | Client Method | Deployed as |
|---|---|---|---|
| **Scripts** | JavaScript | `CallEndpointAsync` | `.js` files via Deployment Window |
| **Legacy modules** | C# (.NET 8, up to .NET 10) | generated bindings, or `CallModuleEndpointAsync` | `.ccmr` references to a .NET solution built outside Unity |
| **Cloud Code modules** | C# (.NET 8; .NET 10 on Unity 6.7+) | generated bindings only | `.ccmu` assets, authored natively in the Editor |
| **CloudBehaviours** (experimental) | C# (.NET 8; .NET 10 on Unity 6.7+) | generated bindings — `await behaviour.Method(args)` | `.ccmu` assets, authored natively in the Editor |

**Recommendation:** Prefer **C#** over JavaScript scripts for production — type safety, generated client
bindings, and access to NuGet packages. Do not write new scripts; maintain and call existing ones. Prefer authoring
natively in the Editor (`.ccmu`) over a `.ccmr` reference for new work: the module lives in the project,
compiles with it, and needs no separate solution. Between Cloud Code modules and CloudBehaviours, see
the breakdown below.

### Cloud Code Modules vs CloudBehaviours

Both are C# modules, and both can hold state per scope with `[StateScope]`. They differ in how the client
reaches them and in whether that state reaches the client on its own:

| | Cloud Code modules | CloudBehaviours |
|---|---|---|
| **Endpoint naming** | one function per `[CloudCodeFunction]` | one generated `[CloudCodeFunction]` per public method you define, named `Behaviour__Method` |
| **Scoped state** | `[StateScope]` persists public fields per player or session | the same, and public fields also synchronize to clients |
| **Client updates** | request/response; subscribe by hand for push messages | synchronized fields and push events arrive on the generated bindings |
| **Client code** | you write the call and deserialize the result | the bindings partial is generated from the cloud class |
| **Requires** | `com.unity.services.cloudcode` `3.0.0`+, Unity 6000.5+ | `com.unity.services.cloudcode` `3.1.0-exp.1`+, Unity 6000.5+ |

**Use CloudBehaviours when clients need to see the state** — shared match state, an inventory the client
displays, per-player views of a shared secret, or a server-owned timer whose deadline the client shows.
The bindings, the synchronization and the push plumbing are generated, so it is less code and less
error-prone than a module plus hand-written calls and subscriptions.

**Use Cloud Code modules when the client only needs the result of a call** — validating a purchase, awarding a
reward, calling another service, running a trigger on a schedule. Scoped state still works; simpler than
using CloudBehaviours if there is no need for state synchronization.

## Where the details live

| Reference | Covers |
| :-- | :-- |
| `references/cloud-code-modules.md` | Cloud Code modules (`.ccmu`), legacy modules (`.ccmr`) and scripts — creating a `.ccmu`, the `.ccmu` asset and how the deployed module name is derived, `ICloudCodeService`, subscriptions, triggers, module scope, generated bindings, deployment, error handling |
| `references/cloud-code-cloudbehaviours.md` | CloudBehaviours — the authoring model, synchronized state, per-viewer projections, timers, access control |

