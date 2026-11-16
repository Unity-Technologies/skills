# Implementation fit: clarify multiplayer requirements

Links in this file resolve to the skill directory's `references/` folder, the folder this file is in. Project and package paths in code spans are relative to the user's project or the package root.

Ground advice in the choices that fit *this* application before recommending architecture or APIs, and map answers to [entrypoints.md](entrypoints.md) only once requirements are clear. Topology and authority decisions live in the architecture reference; this file covers how to obtain requirements and how to phrase questions. Phrasing samples: [examples.md](examples.md).

**How to obtain requirements (in order):**

1. **Conversation and task context.** Use stated goals (e.g. "2-player co-op", "ranked 5v5", "mobile", "host leaves often").
2. **Project state.** Inspect the workspace (`Packages/manifest.json` for multiplayer packages, existing networking scripts, platform settings) and align with what is already chosen.
3. **Ask the user.** If a dimension below would **change** the recommendation and is not inferable from (1) or (2), ask a **short, targeted** question in the same reply as the recommendation, with your assumption stated, instead of blocking on the answer. Follow the language rules below.

## User-facing questions and explanations

- **Clarifying questions** stay **high level**: player count, how people **find or join** a game, who **runs** the match (one player's machine vs a dedicated machine), fairness and latency expectations, disconnect/reconnect and "host leaves" behavior, target platforms, and which **Unity networking stack** the project already uses **only if** the user brought it up or it is visible in the project. Phrase questions in **game / product** terms.
- **Do not** name or ask about Unity **product or service** implementations in questions: avoid the terms **Sessions**, **Lobby**, **Matchmaker**, **Relay**, and avoid **API or type names** (`ISession`, `QuerySessionsAsync`). The user should not need SDK vocabulary to answer.
- In **explanations, summaries, and plans** addressed to the user (outside code blocks and file edits), **do not mention** Lobby, Matchmaker, Relay, or Multiplayer **Sessions** as named products, and do not contrast or enumerate those underlying services, unless the **user** asked about them by name or asked for that level of detail. Use plain language ("list of open games", "automatic pairing", "brokered connectivity when direct player-to-player links are unreliable", "the main Unity multiplayer package API").
- **Code, API references, and file contents** use the exact types, namespaces, and methods.

## Dimensions to consider

- **Player count and scale.** Players per match; many small matches vs few large ones. Topology choice: architecture reference.
- **Competitive stakes.** Tolerance for host-based authority vs need for strict authority and fairness.
- **Discovery and how matches form.** Join codes or invites vs **browsing a list** of open games vs **automatic pairing**; visibility and filterable game metadata. *Internal mapping:* query/list flows, quick-join filters, ticket/queue flows; session properties and indexes.
- **Connection model and resilience.** **NAT / home networks** (mediated connectivity vs published listen addresses), **reconnect** after disconnects, and whether the match must survive the host leaving. *Internal mapping:* relay vs direct network options, `ReconnectAsync`, host migration hooks.
- **Platform constraints.** Targets (e.g. **mobile** dropouts and backgrounding, **console** networking expectations) that affect match lifetime, reconnect UX, and viable connection patterns.
- **Responsiveness.** Whether a player's own actions must react instantly even on a laggy connection, and whether hits must register fairly at high ping. Ask it as "Should your own character react instantly even on a laggy connection, or is a short delay fine?" *Internal mapping:* instant means prediction and lag compensation, so Unity Netcode; a short delay is fine means NGO ([choosing-netcode.md](choosing-netcode.md)).
- **Team skills and codebase stack.** An existing project extends the stack already in `manifest.json` rather than adding a parallel net model. A new project picks its stack by the requirements in [choosing-netcode.md](choosing-netcode.md); team familiarity with GameObjects or ECS breaks a tie.
