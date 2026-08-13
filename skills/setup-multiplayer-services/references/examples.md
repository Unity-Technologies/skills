## Examples: user-facing language

These illustrate **User-facing questions and explanations** in [implementation-fit.md](implementation-fit.md). They are for prose to the user, not for code or file edits (those may use real API names).

### Clarifying questions

**Bad (SDK / product vocabulary):**

- "Do you want to use **Lobby** for the server list, or **Sessions** only?"
- "Should we call **`QuerySessionsAsync`** or **`MatchmakeSessionAsync`**?"
- "Do you need **Relay** or is **direct** fine?"

**Good (game / product terms):**

- "Should players **see a list of open games** and pick one, or **join with a code or invite** only?"
- "Should matchmaking be **automatic** (the game finds opponents for you) or **manual** (players choose a room)?"
- "When two players are on different home networks, is **mediated connectivity** (no open ports on a router) a requirement?"

### Explanations and plans

**Bad (splitting named backend products):**

- "We'll use **Lobby** for metadata, **Relay** for NAT traversal, and **Matchmaker** for ranked."
- "**Sessions** wraps **Lobby** so you don't need **Lobby** directly."

**Good (plain language, same ideas):**

- "We'll keep **room metadata** (map, rules) in one place, use **brokered connectivity** when direct links are unreliable, and **automatic pairing** for ranked."
- "The **main multiplayer package API** can own **room state and joins** so you don't add a second room system on top."

### When the user already named a product

If they wrote e.g. "we're on **Relay** already," you may **mirror their wording** in discussion; still avoid **extra** product enumeration they did not ask for.
