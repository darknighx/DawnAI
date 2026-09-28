# Emotion Dataset Notes for Dawn

These notes are for future training/evaluation of Dawn's local emotion/tone detector. They are not a dump of user conversations, and Dawn should not store sensitive mental-health details unless Alex explicitly asks it to remember them.

## Current Local Seed Data

- `data/dawn_emotion_tone_seed.json`
- Purpose: Dawn-specific examples for slang, greetings, tone feedback, loneliness, anger, sadness, anxiety, excitement, joking, identity questions, and crisis routing.
- Usage: safe to keep in-repo because examples are synthetic test cases, not private user conversations.

## Public/Open Datasets Reviewed

### Google GoEmotions

- Source: https://github.com/google-research/google-research/tree/master/goemotions
- Publication page: https://research.google/pubs/goemotions-a-dataset-of-fine-grained-emotions/
- What it provides: 58k English Reddit comments labeled for 27 emotion categories plus Neutral.
- Useful labels for Dawn: excitement, anger, annoyance, fear, grief, nervousness, sadness, loneliness-adjacent signals through sadness/grief, neutral.
- Download notes: the repo README lists Google Cloud CSV URLs for the raw files and includes train/dev/test TSV files.
- License notes: the parent Google Research repository contains the Apache License 2.0. Because this dataset includes Reddit-derived text and metadata, review the repository README/model card before using it beyond local development.
- Current decision: do not vendor the full dataset into Dawn. Keep the script as an optional downloader and use it only after reviewing terms.

### DAIR.AI / CARER Emotion Dataset

- Hugging Face dataset card: https://huggingface.co/datasets/dair-ai/emotion
- GitHub repo: https://github.com/dair-ai/emotion_dataset
- What it provides: English messages with six labels: sadness, joy, love, anger, fear, surprise.
- Useful labels for Dawn: sadness, anger, fear/anxiety, joy/excitement.
- License notes: Hugging Face lists the license field as `other`, and the dataset card/GitHub README say it should be used for educational and research purposes only.
- Current decision: do not download or ship this dataset by default for the app. Use only for research/evaluation if that usage is acceptable.

## Adding More Examples

Add Dawn-specific examples to `data/dawn_emotion_tone_seed.json` using the same schema:

```json
{
  "id": "seed-021",
  "user_message": "example text",
  "primaryEmotion": "neutral",
  "secondaryEmotion": "optional nuance",
  "intent": "general_chat",
  "intensity": 0,
  "confidence": 0.5,
  "responseMode": "normal_chat",
  "safetyFlag": "none",
  "notes": "Why this example matters."
}
```

Keep examples synthetic or explicitly allowed. Do not paste private chats into the dataset.
