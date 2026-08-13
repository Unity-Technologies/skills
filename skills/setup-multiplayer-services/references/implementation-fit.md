## Implementation fit: clarify multiplayer requirements

Before recommending architecture or APIs internally, the agent **must** ground advice in the right product choices for *this* application. Map answers to [entrypoints.md](entrypoints.md) and the **Priority: Multiplayer Sessions first** section in [SKILL.md](../SKILL.md) only **after** requirements are clear (from context, project, or user). For concrete phrasing samples, see [examples.md](examples.md).

**How to obtain requirements (in order):**

1. **Conversation and task context** — Use stated goals (e.g. "2-player co-op", "ranked 5v5", "mobile", "host leaves often").
2. **Project state when available** — Inspect the workspace (e.g. `Packages/manifest.json` for Netcode / multiplayer packages, existing multiplayer or networking scripts, platform settings) and align recommendations with what is already chosen.
3. **Ask the user** — If a dimension below would **change** the recommended approach and is not inferable from (1) or (2), ask a **short, targeted** question instead of assuming. Follow **User-facing questions and explanations** below.

### User-facing questions and explanations

- **Clarifying questions** must stay **high level**: player count, how people **find or join** a game, who **runs** the match (e.g. one player's machine vs a dedicated machine), fairness/latency expectations, disconnect/reconnect and "host leaves" behavior, target platforms, and which **Unity networking stack** the project already uses **only if** the user has brought it up or it is visible in the project. Phrase questions in **game / product** terms.
- **Do not** name or ask about Unity **product or service** implementations in questions: e.g. avoid the terms **Sessions**, **Lobby**, **Matchmaker**, **Relay**, and avoid prompting with **API or type names** (`ISession`, `QuerySessionsAsync`, etc.). The user should not need SDK vocabulary to answer.
- In **explanations, summaries, and plans** addressed to the user (outside of code blocks and file edits), **do not mention** Lobby, Matchmaker, Relay, or Multiplayer **Sessions** as named products, and **do not** contrast or enumerate those **underlying services**—unless the **user explicitly** asked about them by name or asked for that level of SDK/architecture detail. Use plain language (e.g. "list of open games", "automatic pairing", "brokered connectivity when direct player-to-player links are unreliable", "the main Unity multiplayer package API") when a concept must be described.
- **Code, API references, and file contents** may use the exact types, namespaces, and methods from this skill and from `llms.txt` as needed for a correct implementation.

### Dimensions to consider

- **Player count and topology** — Players per match and rough scale (many small matches vs few large ones). Whether the simulation can run on a **host client** (often with mediated connectivity) or needs a **dedicated server** / server-authoritative hosting story. *Internal mapping:* relay vs direct listen/publish, host vs dedicated server roles, capacity limits on the multiplayer entrypoint APIs.

- **Casual vs competitive** — Tolerance for **host-based authority** and latency variance vs need for **stricter authority, consistency, and fairness** (often favoring dedicated servers and careful netcode choices). Informs how strongly to push dedicated hosting, tick/interpolation choices, and cheat-sensitive design (without duplicating full anti-cheat guidance here).

- **Discovery and how matches form** — Join codes or invites vs **browsing a list** of open games vs **automatic pairing**; visibility and filterable game metadata. *Internal mapping:* query/list flows, quick-join-style filters, ticket/queue flows; session properties and indexes as needed.

- **Connection model and resilience** — **NAT / home networks** (need for mediated connectivity vs published listen addresses), **reconnect** after disconnects, and **moving the host** without ending the match. *Internal mapping:* relay vs direct network options, `ReconnectAsync`, host migration hooks, network start/stop.

- **Platform constraints** — Targets (e.g. **mobile** dropouts and backgrounding, **console** networking and certification expectations) that affect match lifetime, reconnect UX, and viable connection patterns.

- **Team skills and codebase stack** — **Netcode for GameObjects** vs **Netcode for Entities** (or other networking) must match packages and patterns already in the project; prefer extending the stack in `manifest.json` and existing code rather than introducing a parallel net model without an explicit user request.
