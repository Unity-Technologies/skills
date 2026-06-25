# Deployment Reference

## Table of Contents

- [Overview](#overview)
- [Supported Service Integrations](#supported-service-integrations)
- [Workflow](#workflow)
- [Deployment Definitions (.ddef)](#deployment-definitions-ddef)
  - [JSON Schema](#json-schema)
  - [Behavior](#behavior)
- [Programmatic API](#programmatic-api)
  - [Entry Point: Deployments.Instance](#entry-point-deploymentsinstance)
  - [IDeploymentWindow](#ideploymentwindow)
  - [DeploymentResult and DeploymentStatus](#deploymentresult-and-deploymentstatus)
  - [SeverityLevel](#severitylevel)
  - [DeploymentProvider (Abstract)](#deploymentprovider-abstract)
  - [IDeploymentItem](#ideploymentitem)
  - [Code Template -- Register a Custom Provider](#code-template--register-a-custom-provider)
  - [Code Template -- Trigger Deployment Programmatically](#code-template--trigger-deployment-programmatically)

## Overview

Editor-only package providing the **Deployment Window** (Services > Deployment). Deploys cloud resources for multiple UGS services from one place, without leaving the Unity Editor.

- **Package:** `com.unity.services.deployment`
- This package is Editor-only. Runtime code does not reference it.

## Supported Service Integrations

Each service package registers its own file types with the Deployment Window. Install the relevant service package to enable its file types.

| Service | Package | File Type(s) | Min Version |
|---|---|---|---|
| Cloud Code Scripts | `com.unity.services.cloudcode` | `.js` | 2.1.0 |
| Cloud Code C# Modules | `com.unity.services.cloudcode` | `.ccmr` | 2.5.0 |
| Remote Config | `com.unity.remote-config` | `.rc` | 3.2.0 |
| Economy | `com.unity.services.economy` | `.ecc`, `.eci`, `.ecv`, `.ecr` | 3.2.1 |
| Leaderboards | `com.unity.services.leaderboards` | `.lb` | 2.0.0 |
| Game Server Hosting | `com.unity.services.multiplayer` | `.gsh` | 1.1.0 |
| Matchmaker | `com.unity.services.multiplayer` | `.mmq` | 1.0 |
| Game Overrides | `com.unity.services.tooling` | `.ugo` | 1.3.0 |
| Access Control | `com.unity.services.tooling` | `.ac` | 1.0 |
| Deployment Definitions | `com.unity.services.deployment` | `.ddef` | -- |

## Workflow

1. Add `com.unity.services.deployment` via Package Manager.
2. Open **Services > Deployment**.
3. Select the target environment in the dropdown.
4. Choose files to deploy (or select all).
5. Click **Deploy**.

## Deployment Definitions (.ddef)

A Deployment Definition scopes the Deployment Window to a subset of files. Useful for large projects with multiple environments or teams.

**File extension:** `.ddef`

**Create via:** Right-click in Project window > Create > Unity Gaming Services > Deployment Definition

### JSON Schema

```json
{
  "name": "GameplayServices",
  "excludePaths": [
    "Assets/CloudCode/Experimental/**",
    "Assets/Config/Archive/**"
  ]
}
```

| Field | Type | Description |
|---|---|---|
| `name` | `string` | Display name shown in the Deployment Window |
| `excludePaths` | `string[]` | Glob patterns for files/folders to skip |

### Behavior

- **Without a `.ddef`:** the Deployment Window discovers and deploys all UGS config files under `Assets/`.
- **With a `.ddef` selected:** only files within the definition's scope (minus excluded paths) are shown and deployed.
- Multiple `.ddef` files can exist in one project; select which one to use in the Deployment Window dropdown.

## Programmatic API

The separate **`com.unity.services.deployment.api`** package (v1.1) provides programmatic access to the Deployment Window. All types in the main `com.unity.services.deployment` package are `internal` -- there is no public C# API from the main package.

- **Namespace:** `Unity.Services.DeploymentApi.Editor`

### Entry Point: Deployments.Instance

| Member | Type | Description |
|---|---|---|
| `Instance` | `Deployments` | Static singleton |
| `DeploymentProviders` | `ObservableCollection<DeploymentProvider>` | All registered service providers |
| `DeploymentWindow` | `IDeploymentWindow` | Programmatic window control |
| `EnvironmentProvider` | `IEnvironmentProvider` | Current environment |
| `ProjectIdProvider` | `IProjectIdentifierProvider` | Current project ID |

### IDeploymentWindow

| Member | Description |
|---|---|
| `Deploy(items, token)` | Returns `Task<DeploymentResult<IDeploymentItem>>` -- deploy selected items |
| `Deploy(filePaths, token)` | Extension: deploy items by file path (same return type) |
| `GetAllDeploymentItems(includeDeploymentDefinitions)` | Extension: get all items across all providers |
| `GetFromFiles(filePaths)` | Get items matching given file paths |
| `GetDeploymentDefinitions()` | Get available `.ddef` items |
| `OpenWindow()` | Opens the Deployment Window (`EditorWindow`) |
| `GetChecked()` / `GetSelected()` | Get checked/selected items (window must be open) |
| `Check(items)` / `ClearChecked()` | Programmatically check items |
| `Select(items)` / `ClearSelection()` | Programmatically select items |
| `DeploymentStarting` | `event Action<IReadOnlyList<IDeploymentItem>>` -- before deployment |
| `DeploymentEnded` | `event Action<IReadOnlyList<IDeploymentItem>>` -- after deployment |
| `GetCurrentDeployment()` | Returns the active `DeploymentScope`, or null |

### DeploymentResult and DeploymentStatus

**DeploymentResult\<T\>**

| Member | Type | Description |
|---|---|---|
| `Deployed` | `List<T>` | Items that were successfully deployed |

**DeploymentStatus** has static factory methods for common states:

| Factory | Description |
|---|---|
| `Empty` / `UpToDate` / `ModifiedLocally` / `FailedToDeploy` | Static readonly instances |
| `GetDeployed(details)` / `GetDeploying(details)` | Deployment in-progress/complete |
| `GetFailedToDeploy(details)` / `GetFailedToFetch(details)` | Failure states |
| `GetFailedToLoad(e, path)` / `GetFailedToRead(e, path)` | File I/O failures |
| `GetFetched(details)` / `GetFetching(details)` | Fetch from remote states |
| `GetPartialDeploy(details)` / `GetPartialFetch(details)` | Partial completion |

### SeverityLevel

| Value | Meaning |
|---|---|
| `None` (0) | No status |
| `Info` (1) | Informational |
| `Warning` (2) | Warning |
| `Error` (3) | Error |
| `Success` (4) | Success |

### DeploymentProvider (Abstract)

Subclass this to expose your own deployable assets to the Deployment Window.

| Member | Description |
|---|---|
| `Service` | Display name for the service (abstract) |
| `DeployCommand` | Required deploy command (abstract) |
| `DeploymentItems` | `ObservableCollection<IDeploymentItem>` -- add/remove to populate the window |
| `Commands` | Additional context menu commands |
| `OpenCommand` | Double-click handler (optional) |
| `ValidateCommand` | Validation command (optional) |
| `SyncItemsWithRemoteCommand` | Sync-with-remote command (optional) |

### IDeploymentItem

| Member | Description |
|---|---|
| `Name` | File name with extension |
| `Path` | Full asset path |
| `Progress` | Deploy progress 0-100 |
| `Status` | `DeploymentStatus` (message + severity) |
| `States` | `ObservableCollection<AssetState>` -- local asset validation states |

### Code Template -- Register a Custom Provider

```csharp
using UnityEditor;
using Unity.Services.DeploymentApi.Editor;

class MyServiceDeploymentProvider : DeploymentProvider
{
    public override string Service => "MyService";
    public override Command DeployCommand { get; } = new MyDeployCommand();
}

[InitializeOnLoadMethod]
static void RegisterProvider()
{
    Deployments.Instance.DeploymentProviders.Add(new MyServiceDeploymentProvider());
}
```

> **Deploy commands that write to UGS services** must use `IAdminClient` from the
> `com.unity.services.apis` package (`com.unity.services.apis`). Authenticate with a service
> account via `adminClient.SetServiceAccount(keyId, keySecret)`, then call the appropriate
> admin API (e.g. `adminClient.CloudSaveData.SetCustomItem` for Cloud Save game data).
> See [apis.md](apis.md) for the full `IAdminClient` interface and code templates.

### Code Template -- Trigger Deployment Programmatically

```csharp
using Unity.Services.DeploymentApi.Editor;

// Deploy all items
var allItems = Deployments.Instance.DeploymentWindow.GetAllDeploymentItems();
var result = await Deployments.Instance.DeploymentWindow.Deploy(allItems);

// Deploy by file paths
await Deployments.Instance.DeploymentWindow.Deploy(new[] { "Assets/MyConfig.rc" });

// Listen for deployment events
Deployments.Instance.DeploymentWindow.DeploymentStarting += items =>
{
    Debug.Log($"Deploying {items.Count} items...");
};
Deployments.Instance.DeploymentWindow.DeploymentEnded += items =>
{
    Debug.Log($"Deployment complete: {items.Count} items");
};
```
