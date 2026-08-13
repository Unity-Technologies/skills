## Workflow prerequisites (packages and cloud setup)

**Agent-only sanity check** before recommending a path: match the user's **intent** to **dependencies** and **live services / deployment** (see **`llms.txt`** install, init, deployment, and tutorial pages for authoritative steps). Package IDs can vary slightly by Unity/editor version—verify in Package Manager or docs when implementing.

**Which workflow applies** must be **inferred** when possible from **conversation context** and **project state** (e.g. `Packages/manifest.json`, server vs client build targets, existing multiplayer scripts, deployment assets). If more than one row in the table could fit and the choice **changes** prerequisites or APIs, **ask the user** with **high-level** questions (see **User-facing questions and explanations** in [implementation-fit.md](implementation-fit.md)), not by naming product rows from this table. This aligns with **Implementation fit** in [implementation-fit.md](implementation-fit.md): infer first, then clarify if ambiguous.

Unity Gaming Services **initialization** and **authentication** are required for all workflows.

| Workflow (what the product is doing) | Typically required |
|--------------------------------------|-------------------|
| Rooms / join-in-progress **without** starting the session **gameplay network** (metadata, codes, lists, properties only) | `com.unity.services.multiplayer`. **No** Netcode gameplay package required unless they add a custom **`INetworkHandler`** or later start **`StartRelayNetworkAsync`** / **`StartDirectNetworkAsync`**. |
| **Gameplay** simulation synced over the session **Network** (host/client or server roles with Relay or direct transport) | **Exactly one** gameplay stack: **NGO** — `com.unity.netcode.gameobjects` **or** **NFE** — `com.unity.netcode.entities` (Netcode for Entities). Integrate transport with session network APIs per Unity's session + Netcode guides; do not assume both stacks. |
| **Quick join** (filter-based auto pick / create) | Session **type** and **indexed** properties for filters; **`QuickJoinOptions`** in code. |
| **Ticket matchmaking** into a **player-hosted** match | A deployed **Matchmaker queue (MMQ)** (name matches **`MatchmakerOptions`**), Matchmaker **environment** / dashboard setup, and authenticated players. |
| **Ticket matchmaking** with a **dedicated game server (DGS)** | **MMQ** configured for the **DGS / server allocation** flow, a **server build** and **hosting** setup (e.g. Game Server Hosting / Multiplay—see **`llms.txt`** hosting and deployment topics), server process using the **server** session role where applicable, and often **`WithBackfillingConfiguration`** + **`StartBackfillingAsync`** / **`StopBackfillingAsync`** when refilling player slots on an existing allocation. |
| **Editor wiring** with **`MultiplayerSession`** / **`SessionConnector`** | Same multiplayer package (components assembly **`Unity.Services.Multiplayer.Components`**); still subject to the Netcode row above if gameplay networking is used. |
| **Deploying** Matchmaker queues or Multiplayer assets from the Editor | Unity **Deployment** window / deployment docs under **`llms.txt`** (queue, environment, multiplayer config) so cloud resources exist before code calls into them. |
