# Public release audit

## Scope and findings

The source, configuration, scripts, synthetic fixture files, documentation, and project inventory were reviewed for release. Pattern scans covered text files including hidden and generated text, looking for known credential formats, assigned secrets, credential-bearing URLs, email addresses, and absolute user paths. Binary build artifacts were inventoried and excluded rather than treated as auditable source. Pattern scanning is not a proof that arbitrary secrets cannot exist.

- No API keys, access tokens, passwords, database credentials, private keys, or real `.env` files were found in the project text. No key rotation is indicated by these findings.
- The runtime uses local Ollama and public lookup APIs, not an authenticated cloud Llama endpoint. No secrets needed migration. The existing search configuration loader already supports environment variables and private configuration files.
- Personal-name references in code, fixture examples, and prose were anonymized. Actual runtime prompt instructions now address the user generically; synthetic test names do not supply a default profile.
- A machine-specific Windows shortcut and generated files contained or could expose local paths. Shortcuts, executables, debug symbols, runtime libraries, build metadata, caches, and temporary directories are excluded.
- No exported personal chat history or saved-memory file was found in the project source. Real user state is stored under the Windows account's AppData directory, outside this repository, and was not copied or modified. Ignore rules also cover runtime state, feedback, debug output, databases, and common private export filenames inside the checkout.
- The three checked-in JSON datasets contain synthetic fixtures (160 behavior examples, 23 emotion/tone examples, and 10 text-normalization examples), not chat exports. Optional external datasets remain excluded.
- Portable runtime paths use Windows special-folder APIs. The fixed loopback Ollama endpoint is intentional; it is not a developer-specific filesystem path.

## Changes

- Added the root README, MIT license, third-party notices, `.gitignore`, `.gitattributes`, `.env.example`, `global.json`, and `NuGet.Config`.
- Kept the existing folder architecture. Updated the nested runtime guide to point to source-build instructions rather than assume committed executables.
- Cleaned `package.json`; all Node checks remain dependency-free. No Python dependency file or fabricated cloud API key configuration was added.
- Added one offline check script and a WPF constructor/XAML startup smoke mode to the existing C# regression harness.
- Made the optional Ollama setup helper stop on failed installation/model download and describe launching from source.
- Kept all local generated files in place but excluded them from Git. Nothing was pushed, no remote was configured, and no first commit was created.

## Public file list and exclusions

`docs/public-files.txt` lists the exact intended first-commit paths, including itself. Stage using that manifest rather than force-adding the entire working directory. Regenerate/review it if files change before publication.

Excluded categories: `bin/`, `obj/`, `.tmp/`, `Dawn-DoubleClick/`, `artifacts/`, executable/runtime/debug files, machine shortcuts, node_modules, virtual environments, IDE caches, logs, private configuration/credentials, runtime state and memory exports, feedback and debug traces, local databases, downloaded datasets, and model weights. `.env.example` is explicitly retained while real `.env` files are ignored.

## Validation

The application build, all 64 Node behavior checks, C# memory/request/persistence regressions, and the new WPF startup smoke test passed. Startup validation constructs the window without showing it, loading private state, or making a network request. The C# regression uses a mock Ollama transport; live model testing remains optional.

All 19 private/build ignore probes passed; `.env.example` remained public. Exactly 43 intended files matched a temporary staging index, and `git diff --cached --check` passed. A source-only copy independently restored, built, and passed the same behavior, memory, and startup checks. A final scan of the public manifest found no credential patterns, personal-name references, email addresses, or absolute user paths. The temporary Git metadata used for this check is inside ignored `.tmp/`; the actual project is not initialized as a repository.

## Remaining limitations

This is a portfolio source release, not a claim of production security. .NET 6 is out of support and should be migrated in a separate validated change. State is unencrypted, writes are best-effort, and local file-path restrictions are not a hardened filesystem sandbox. Public providers and LLM output can be unreliable. These constraints are explained in the README.

## License choice

MIT was selected for the original project: it is concise and permits learning, reuse, modification, and redistribution with attribution/license notice retention. It does not relicense model weights, external datasets, or third-party runtime components. See `THIRD_PARTY_NOTICES.md`.
