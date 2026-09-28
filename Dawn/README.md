# Dawn runtime reference

See the [project README](../README.md) for installation, architecture, tests, and source-build instructions. Published executables are generated locally and excluded from Git.

## Search Retrieval

Dawn can optionally use free source-first lookup for factual questions, current information, definitions, people or character background, traits, and follow-up factual references like `her traits`. Search does not need an API key. The default provider chain is Wikipedia/Wikimedia first, Wikidata second, and Fandom/MediaWiki third for anime, games, fictional characters, and lore. Optional self-hosted SearXNG can be added after those sources.

Wikipedia lookup is staged: Dawn first tries an exact title lookup, then Wikipedia search, then fetches extracts for related pages and scores whether the requested entity and context are actually mentioned. Random fact requests use Wikipedia's random-page API and still attach source metadata. MediaWiki/Fandom lookup uses public `api.php` endpoints for candidate wiki domains derived from the topic and treats fan wiki evidence as lower authority. This lets Dawn use evidence from related pages when an entity does not have a standalone page, while avoiding similar-name guesses.

Retrieval answers are source-locked. Dawn stores the selected source title, URL, snippet/extract, confidence, matched entity, and matched work/title in the assistant message trace. If the final answer introduces a proper name or relationship that is not present in the selected evidence, Dawn falls back instead of guessing.

## Feedback Learning

Dawn has a local RL-lite feedback loop. Each Dawn message has `Good` and `Needs work` buttons plus an optional correction box. Feedback is saved locally as JSONL at `%APPDATA%\Dawn\feedback\feedback.jsonl`; it is not model training and it does not fine-tune Ollama.

The response policy tuner reads aggregate feedback by intent and response mode before a reply. It can lightly adjust style, such as avoiding therapy-mode language for casual slang if that pattern gets negative ratings. It cannot disable crisis safety, make Dawn claim to be human, bypass factual source-grounding, invent facts after weak retrieval, or create exact canned replies.

Feedback review commands:

- `/feedback patterns`
- `/feedback successes`
- `/feedback export bad`
- `/feedback export corrected`

Dawn reads search settings from these places, in this order:

1. `.env` or `appsettings.json` next to `Dawn.exe`
2. `.env`, `search.config.json`, or `appsettings.json` in `%APPDATA%\Dawn`
3. Windows machine, user, and process environment variables

Search is on by default. To turn it off for the double-click Windows app, create `%APPDATA%\Dawn\search.config.json`:

```json
{
  "SEARCH_ENABLED": false,
  "SEARCH_PROVIDER_CHAIN": "wikipedia,wikidata,mediawiki,searxng",
  "MAX_SEARCH_RESULTS": 3,
  "SEARCH_TIMEOUT_SECONDS": 8
}
```

You can also use PowerShell environment variables:

```powershell
$env:SEARCH_ENABLED="true"
$env:SEARCH_PROVIDER_CHAIN="wikipedia,wikidata,mediawiki,searxng"
$env:SEARXNG_BASE_URL="" # optional self-hosted instance, skipped when empty
$env:MAX_SEARCH_RESULTS="3" # 3 to 5
$env:SEARCH_TIMEOUT_SECONDS="8"
```

The sidebar debug card shows whether search is enabled, which provider chain is active, the last status code, and the last safe error. Use **Test Lookup** to run standalone checks for `Albert Einstein`, `Overwatch`, `Clannad visual novel`, and `Tomoyo Sakagami`. Provider-chain attempts are saved in `%APPDATA%\Dawn\debug\search_provider_chain.json`.

Search snippets are treated as untrusted. Dawn passes only title, URL, provider, and concise snippets into the prompt, never raw webpage text.

Dawn is not a licensed therapist or crisis service. If you are in immediate danger in the U.S., call 911. If you are in suicidal crisis or emotional distress in the U.S., call or text 988.

## Dawn Voice

Dawn uses a global voice policy in the main system prompt and per-turn response guidance. The goal is short, warm, friend-like replies: react first, explain second, avoid robotic AI disclaimers in normal chat, and stay honest when asked directly about being AI or human.

## File Commands

File access is command-only. Dawn will not manage files during normal AI chat. Use the file workspace field in the app, then type slash commands:

```text
/help files
/files [folder]
/read <file>
/write <file> <text>
/append <file> <text>
/mkdir <folder>
/copy <from> <to>
/move <from> <to>
/delete --confirm <file>
```

All file commands are locked inside the selected file workspace.

## Memories

Dawn saves direct self-identification such as `My name is Alex.` or `Call me Nadia.` as a structured name record. Other personal information still requires `remember this`, `save this`, or `/remember`. Names and other saved memories live separately from conversation messages in the local state file and survive New Chat and restarts. `/memories` lists them and `/forget-memory --confirm <number>` removes them. Clean voice-test mode deliberately excludes all personal context. There is no clickable memory page in the UI.

Use hidden chat commands or explicit phrases to control memories:

```text
/memories
/remember <something useful>
remember this <something useful>
save this <something useful>
/forget-memory --confirm <number>
```

The app saves your personal notes, recent chat history, and local memories in `%APPDATA%\Dawn\state.json`.

## History

The History button in the sidebar opens saved conversations. New Chat keeps the previous conversation in History, and older conversations can be searched and reopened. History is stored locally in `%APPDATA%\Dawn\state.json`.

## Safety And Style Checks

Example conversations for sadness, anxiety, anger, and crisis handling live in `Examples\safety_and_style_examples.md`.
Companion-style examples for casual chat, daily life, friend-like support, and creative fun live in `Examples\companion_style_examples.md`.
Slang and typo examples live in `Examples\slang_and_typos_examples.md`.

See [memory investigation and regression checks](../docs/memory-investigation.md) for the request trace, limits, and diagnostic fields.
