# Third-party notices

Dawn's MIT license applies to the original project source, not to third-party models, datasets, runtime components, or retrieved pages.

- **.NET/WPF:** installed through the Microsoft SDK/runtime; generated redistributables are excluded from Git. Consult the installed runtime's notices when distributing a packaged executable.
- **Ollama:** installed separately. See https://github.com/ollama/ollama for its license and notices.
- **Llama 3.2:** downloaded separately through Ollama. Its weights are governed by Meta's model license and use policy, not Dawn's MIT license. See https://ollama.com/library/llama3.2 and the linked model license.
- **Web retrieval:** Wikipedia, Wikidata, Fandom/MediaWiki, and other provider content retain the respective source's terms. Dawn does not vendor those pages.
- **Optional datasets:** see `docs/emotion_datasets.md`. The downloader is opt-in; downloaded files are excluded under `data/external/`. No external training corpus is shipped.
- **Repository fixtures:** `data/dawn_*_seed.json`, `data/dawn_behavior_examples.json`, and `Dawn/Examples/` contain synthetic examples, not exported user conversations. Fictional example names are not a default runtime profile.
