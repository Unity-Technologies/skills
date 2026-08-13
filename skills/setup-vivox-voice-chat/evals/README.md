# Vivox Voice & Text Chat Skill Eval Suite

Evaluation suite for the `setup-vivox-voice-chat` skill, powered by [Promptfoo](https://www.promptfoo.dev/). Validates that the skill routes the model to the correct Vivox v16 APIs (no v4/legacy hallucinations) across init, channel join, and messaging.

## Prerequisites

- **Node.js** v18 or later
- A **LiteLLM API key** (from https://uai-litellm.internal.unity.com)

## Setup

### 1. Install Promptfoo

```bash
# Option A: install globally
npm install -g promptfoo

# Option B: use npx (no install needed)
npx promptfoo@latest eval
```

### 2. Configure your API key

```bash
cd evals/
cp .env.example .env
```

Open `.env` and set your personal LiteLLM key:

```
OPENAI_API_KEY=your-litellm-api-key-here
OPENAI_BASE_URL=https://uai-litellm.internal.unity.com
```

> **Important:** Never commit your `.env` file. It is already in `.gitignore`.

## Running the evals

All commands should be run from the `evals/` directory.

Use `-j 10` to run up to 10 eval requests concurrently.

### Run the full suite

```bash
promptfoo eval -j 10
```

### Run a specific test file

```bash
promptfoo eval --tests tests/init-and-login.yaml -j 10
promptfoo eval --tests tests/voice-channels.yaml -j 10
promptfoo eval --tests tests/text-chat.yaml -j 10
```

## Viewing results

### Terminal output

Results are printed to the terminal with pass/fail per assertion.

### Interactive web UI

```bash
promptfoo view
```

Opens a local UI (usually `http://localhost:15500`) for browsing results, filtering, and comparing runs.

## Assertions used

| Type | What it checks |
|---|---|
| `icontains` | Response contains a substring (case-insensitive), e.g. an exact Vivox API name |
| `not-icontains` | Response does NOT contain a substring (used to catch v4 legacy names like `Client.Instance`) |
| `llm-rubric` | An LLM judges whether the response meets a semantic requirement (e.g. correct init order) |

## Adding new tests

1. Create a new YAML file in `tests/`:

```yaml
- description: "Short description of what is being tested"
  vars:
    user_message: "The user request to test"
    reference_content: "file://../references/your-reference.md"  # optional
  assert:
    - type: icontains
      value: "VivoxService.Instance.JoinGroupChannelAsync"
    - type: not-icontains
      value: "SendDirectedTextMessageAsync"
    - type: llm-rubric
      value: |
        Describe the semantic requirement the response must meet.
```

2. Add the file to `promptfooconfig.yaml` under `tests:`.

3. Run it: `promptfoo eval --tests tests/your-new-test.yaml`.
