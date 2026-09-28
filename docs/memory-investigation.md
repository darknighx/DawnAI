# Dawn memory investigation and focused fix

## Original message flow

1. WPF `SendCurrentMessageAsync` reads `InputBox`, calls `AddMessage`, then handles source follow-ups, safety, feedback, explicit memory commands, and file commands locally.
2. Ordinary input is classified by `ConversationContextResolver` and `RetrievalPlanner`. Lookup turns can return before reaching Llama if evidence fails.
3. `MemoryRelevanceFilter.Filter` selects saved memories. `DawnVoicePolicy.FilterHistoryForModel` removes legacy/contaminated assistant output. The app takes the last 14 messages, or 4 for restricted current-turn context. With history disabled, factual retrieval, or clean test mode it sends only the current user message.
4. `OllamaBridge.ChatAsync` checks local Ollama and the installed model, calls `BuildPromptMessagesForDebug`, and POSTs `model`, `stream:false`, `messages`, and sampling options to `http://127.0.0.1:11434/api/chat`.
5. The first message is a newly constructed system prompt on **every** request. Normal requests include personal notes and selected memories. Clean test mode and source-locked factual retrieval omit personal data intentionally. The response is polished, appended, and saved.

## Findings before editing

- **Storage:** `_messages`, `_conversations`, and `_memories` are WPF process state, serialized to `%APPDATA%/Dawn/state.json`. The file stores up to 80 conversations, each with 120 messages, plus the active 120 messages and up to 120 memories. Personal notes are a separate field.
- **Name source:** The welcome text, user label, and prompt instructions hardcoded Alex. This was not evidence of a stored user profile.
- **Loss point 1 — capture:** `My name is Alex.` was ordinary chat. Only `/remember`, `remember this`, and `save this` persisted facts. The older model/heuristic memory extraction method exists but is not called by the send flow.
- **Loss point 2 — retrieval:** Explicit saves were all `manual`. Identity relevance checked `identity|profile|name`, so an explicitly saved name could fail recall without matching query terms. Topic-specific branches could reject identity before the identity check.
- **Trimming:** 14/4/1-message request limits can remove the original introduction; saving 120 messages does not mean all 120 are sent. No token-aware budget or `num_ctx` override exists; extremely large messages can still exceed the model's configured context window. The observed ordinary name loss does not require a context-window overflow to occur.
- **Sessions:** `_activeConversationId` groups saved chats. New Chat creates a GUID and clears messages, but retains memories and notes. There is no separate web frontend/backend or server conversation ID to synchronize. Ollama receives stateless chat requests; Dawn must supply context every time.
- **Restart:** `LoadState` restores memory separately from history. The existing memory-mode version gate accepts version 2 and drops older-mode memories; this intentional compatibility policy is unchanged. Load/save errors are shown in the status UI.

## Change

Reuse the existing long-term store instead of adding a second database or duplicate profile. `UserMemory` stores the declared name in a `name` record, independently of chat messages and conversation IDs. Direct, standalone name declarations are saved immediately and acknowledged locally; other personal facts still need explicit save commands. Name corrections replace the previous record. Existing explicitly saved name declarations are migrated; identities are never inferred from assistant greetings or historical chats.

Normal model requests receive `User profile` with the stored name, or an explicit unknown-name instruction. The name is prioritized before memory ranking/caps and topic filtering. Questions about the user's identity/profile bypass public web lookup; broad profile recall also retrieves manual memories. Existing unrelated-topic filtering, factual source isolation, clean test mode, memory listing/deletion, and UI layout are preserved. The ordinary prompt and chat user label no longer assume Alex.

Memory is enabled for normal chat; there is no separate long-term-memory toggle. The existing clean voice-test mode deliberately excludes personal context and does not capture ordinary name declarations. Explicit memory commands still function as commands.

The local Windows account is the user identity boundary, as before. Multiple independent signed-in profiles are not supported by this single-user desktop application.

## Logging

Enable the existing **Debug Ollama logs** checkbox. `%APPDATA%/Dawn/debug/last_ollama_request.json` now contains:

- `conversationId`: Dawn's active chat GUID (not a server session).
- `conversationMessageCount`: actual user/assistant messages sent, excluding the system prompt.
- `userMemoryLoaded`: whether saved memory records were available in the app for this request.
- `memoryCategoriesInjected`: distinct categories actually included, empty for clean/factual modes.

Existing `messageCount`, full request payload, and `last_memory_relevance.json` remain available. The debug file is the most recent request, not an accumulating log. Local commands do not make an API request.

## Regression commands

```powershell
node -e "eval(require('fs').readFileSync('tests/dawnBehavior.test.cjs','utf8'))"
dotnet run --project tests/MemoryRegression/MemoryRegression.csproj
dotnet run --project tests/MemoryRegression/MemoryRegression.csproj -- --live
```

The C# regression uses real production memory selection, prompt building, and HTTP serialization with a mocked Ollama transport. It checks identity after 20 unrelated messages, a fresh conversation ID, and disk serialization followed by a new OS process. It also checks an alternate name, correction, forgetting, migration, topic changes, ranking limits, and clean/factual isolation. The optional live run submits synthetic data to the installed local Llama model at those three stages. Tests do not modify the user's AppData state or chats. The fresh-process test exercises deserialization and the production request path; it does not automate the WPF UI.

## Validation result

All 64 existing behavior checks passed. The C# mock regression passed, including a fresh child process. Live llama3.2 correctly returned the stored test name after history trimming, in a new conversation, and in the restarted process. Release self-contained Windows publishing succeeded. No WPF UI automation was performed.
