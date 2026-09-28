# Dawn

A local Windows AI companion built with C#, WPF, and Llama through Ollama. Dawn combines conversational context, persistent user memory, rule-based intent detection, and optional source-backed retrieval in a desktop interface.

This university AI portfolio project explores the application engineering around an LLM: deciding what context to send, preserving user identity across conversations, keeping retrieved evidence separate from personal memory, and testing those boundaries.

> **Runtime status:** the existing app targets .NET 6, which reached end of support on November 12, 2024 ([Microsoft notice](https://devblogs.microsoft.com/dotnet/dotnet-6-end-of-support/)). This release preserves the tested implementation. It is an educational prototype; migrating to a supported .NET LTS version is the first maintenance priority.

## Features

- Local chat with an Ollama-hosted model; `llama3.2` is the default.
- Recent conversation context, saved chat history, and searchable conversations.
- Persistent name/profile memory with explicit saving and deletion commands.
- Tone and intent routing, typo normalization, and conversational style controls.
- Optional Wikipedia, Wikidata, and MediaWiki/Fandom retrieval; optional self-hosted SearXNG.
- Evidence checks and source traces for factual answers.
- Local response ratings and lightweight style-policy tuning (not model training).
- Explicit file commands restricted to a selected workspace.
- Optional local request diagnostics, synthetic behavior fixtures, and executable regression tests.

## Technologies

| Component | Technology |
| --- | --- |
| Desktop interface and runtime | C#, WPF, .NET 6, Windows |
| Language model | Ollama HTTP API, Llama 3.2 by default |
| Persistence | Local JSON state and JSONL feedback |
| Retrieval | HttpClient and public source APIs |
| Behavior reference code | TypeScript, maintained separately from the WPF runtime |
| Tests and tooling | C# console regression harness, Node.js built-ins, PowerShell |

There is no Python backend, hosted database, npm runtime dependency, or separate web frontend. The TypeScript files are reference implementations and are not executed by the desktop app.

## Architecture

```text
WPF input
  -> local commands and safety checks
  -> text/intent analysis + conversation binding
  -> optional retrieval + evidence validation
  -> relevant saved memory + recent messages
  -> system prompt + conversation messages
  -> POST http://127.0.0.1:11434/api/chat
  -> reply validation/polishing -> UI -> local saved state
```

The desktop app owns the conversation ID and sends a complete context payload on each request. Ollama is not expected to remember earlier requests. The system prompt is rebuilt every time. Explicit memory/file commands and some safety/source responses are handled locally without calling the model.

### Short-term conversation memory

Normal requests use up to 14 recent messages after filtering legacy or unsuitable assistant output. Some focused turns use 4; disabling history, factual retrieval mode, or clean voice-test mode can reduce the request to the current message. The saved history retains up to 80 conversations and 120 messages per conversation. Saved history is not the same as the model's active context window.

### Persistent user memory

`StoredState.Memories` is separate from the message lists and survives New Chat and application restarts. A direct declaration such as `My name is Alex.` stores a structured `name` record. Ordinary requests include that name under `User profile`, independently of history trimming. Corrections replace the previous name.

Other facts require `remember this`, `save this`, or `/remember`. A relevance filter selects useful facts rather than injecting every saved item. `/memories` lists records; `/forget-memory --confirm <number>` removes one. Normal chat enables memory; clean voice-test mode intentionally excludes it. Source-locked factual requests also exclude personal context to avoid contaminating public-source answers.

This is deterministic local memory selection, not embeddings, a vector database, automatic lifelong learning, or fine-tuning. The Windows account is the user boundary; multi-user authentication is not implemented.

## Installation

Use Windows with PowerShell. Install:

1. [Git for Windows](https://git-scm.com/downloads/win).
2. [.NET 6 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0), version 6.0.413 or a later 6.0.4xx patch. `global.json` pins the tested SDK feature band. See the runtime support warning above.
3. [Ollama for Windows](https://ollama.com/download/windows).
4. [Node.js](https://nodejs.org/) only if running the behavior-fixture tests. Use a supported Node release; the scripts have no external npm dependencies.

Clone the repository:

```powershell
git clone https://github.com/darknighx/DawnAI.git
cd DawnAI
ollama pull llama3.2
ollama list
dotnet restore Dawn/Dawn.csproj
dotnet build Dawn/Dawn.csproj --no-restore
```

Keep Ollama running. If its desktop service is not already running, use `ollama serve` in a separate terminal. The optional `SETUP LOCAL AI.bat` helper can install Ollama using winget and download the default model.

**No API key is required.** This implementation uses the local Ollama endpoint, not a hosted Llama provider. Adding a cloud-provider key to `.env` does not enable a cloud backend.

## Environment setup

Configuration is optional; default local chat works without `.env`.

```powershell
Copy-Item .env.example .env
```

The example contains only safe sample settings and an empty optional endpoint. Edit the private `.env` if needed. Real `.env` files are ignored by Git.

| Setting | Example | Purpose |
| --- | --- | --- |
| `SEARCH_ENABLED` | `true` | Permit lookup when the UI search option is also enabled |
| `SEARCH_PROVIDER_CHAIN` | `wikipedia,wikidata,mediawiki,searxng` | Ordered providers |
| `MAX_SEARCH_RESULTS` | `3` | Clamped to 3–5 |
| `SEARCH_TIMEOUT_SECONDS` | `8` | Clamped to 2–20 seconds |
| `SEARXNG_BASE_URL` | empty | Optional instance URL; blank skips SearXNG |

The loader reads files next to the executable, then the working directory, then `%APPDATA%\Dawn`; later values override earlier values. Machine, user, and process environment variables are applied afterward, with process values last. Run from the repository root so the root `.env` is found. For a packaged build, place `.env` next to its executable. The model name is configured in the UI; the Ollama URL is currently fixed to localhost.

## Run

```powershell
dotnet run --project Dawn/Dawn.csproj
```

To create your own self-contained Windows build:

```powershell
dotnet publish Dawn/Dawn.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o artifacts/Dawn
.\artifacts\Dawn\Dawn.exe
```

Keep the entire published folder together, including native runtime libraries. Build outputs and locally installed binaries are not part of the source repository.

## Example usage

```text
My name is Alex.
/remember I prefer concise explanations.
Tell me a short story about a lost robot.
Who am I?
/memories
```

Start a new chat, or restart Dawn, and ask `Who am I?` again. The stored name remains available. Use `/forget-memory --confirm 1` only after checking the displayed memory number. Names in fixtures and examples are fictional test identities.

## Tests

From the repository root:

```powershell
npm test
npm run test:memory
npm run test:startup
```

No `npm install` is needed: Node tests use only built-in modules. C# projects use framework libraries and a local project reference; there are no third-party NuGet package dependencies. `NuGet.Config` explicitly selects the public feed for restore.

Or run the build and all offline checks together:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/check.ps1
```

- The Node suite checks synthetic fixture schemas and source-policy invariants; it is not a model-quality benchmark.
- The C# regression exercises memory selection, actual HTTP payload serialization with a mock transport, conversation changes, and persistence in a fresh OS process.
- The startup smoke test initializes the real WPF window and resources without showing it or loading private user data. It does not test interactive rendering.
- Optional live integration: `dotnet run --project tests/MemoryRegression/MemoryRegression.csproj -- --live`. This requires local Ollama and may be slower or nondeterministic.

## Privacy and diagnostics

Runtime data lives outside the checkout:

- `%APPDATA%\Dawn\state.json`: chat history, notes, model settings, and saved memories.
- `%APPDATA%\Dawn\feedback\`: ratings, corrections, and exports.
- `%APPDATA%\Dawn\debug\`: request payloads and retrieval diagnostics when enabled.

These files contain private text and are not encrypted by Dawn. Do not attach them to public issues. `.gitignore` also excludes common copies of these files inside the checkout. **Debug Ollama logs** includes full prompts as well as `conversationId`, `conversationMessageCount`, `userMemoryLoaded`, and `memoryCategoriesInjected`.

Inference runs locally. Optional web retrieval sends search queries to external providers, so disabling web search is necessary when you do not want that network activity. Feedback changes local style guidance; it does not retrain Llama.

## Project structure

```text
Dawn/                        WPF application and C# runtime
  Assets/                    App icon assets
  Examples/                  Synthetic conversational examples
  Knowledge/                 Companion/support reference material
  UserMemory.cs              Structured name capture and migration
src/dawn/                    TypeScript behavior reference modules
data/                       Synthetic JSON regression fixtures
docs/                       Memory design, dataset notes, release audit
scripts/                    Offline checks and optional dataset downloader
tests/                      Node tests and C# regression/startup harness
.env.example                 Safe optional search configuration
.gitignore                   Build, credentials, and private-data exclusions
global.json                  Tested .NET SDK selection
NuGet.Config                 Public restore source
package.json                 Dependency-free test commands
LICENSE                      MIT license for original project code
THIRD_PARTY_NOTICES.md        External model/data licensing boundaries
```

## Known limitations

- File-workspace checks validate paths but are not a hardened sandbox against filesystem junctions or symbolic links. Use a trusted workspace.
- Windows/WPF only; the current .NET 6 runtime is out of support.
- Rule-based name capture handles direct declarations; it is not a general identity extraction model.
- Single local user, unencrypted state, and best-effort JSON writes; no database transactions or synchronization.
- Context is limited by message count, not a tokenizer-aware budget. Very long inputs can exceed the model context window.
- Generated answers can be wrong. Retrieval coverage depends on public endpoints and evidence quality.
- Tone and safety heuristics are not clinically validated. Dawn is not a medical professional or crisis service.
- A large portion of the orchestration remains in `MainWindow.xaml.cs`; the TypeScript reference layer can drift from runtime behavior.
- Optional external datasets and model weights are not distributed here.

## Future improvements

1. Migrate and validate against a supported .NET LTS release.
2. Extract orchestration and persistence services from the window code.
3. Add atomic state writes, profile controls, and optional encryption.
4. Add token-budget-aware context selection and broader retrieval tests.
5. Improve accessibility, installer packaging, and offline model evaluation.

## License

Original project code is provided under the [MIT License](LICENSE), a permissive license allowing reuse, modification, and redistribution with the license notice retained. Model weights, optional external datasets, and retrieved content retain their own terms; see [third-party notices](THIRD_PARTY_NOTICES.md).
