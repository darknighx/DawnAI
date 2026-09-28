import { normalizeForUnderstanding } from "./textUnderstandingNormalizer";

export type PrimaryEmotion =
  | "excited"
  | "sad"
  | "angry"
  | "anxious"
  | "lonely"
  | "playful"
  | "neutral"
  | "unclear"
  | "crisis";

export type DawnIntent =
  | "simple_greeting"
  | "attention_call"
  | "casual_slang"
  | "dramatic_reaction"
  | "playful_teasing"
  | "content_feedback"
  | "profanity_definition_request"
  | "profanity_usage_request"
  | "creative_ideas_request"
  | "activity_suggestion_request"
  | "local_recommendation_request"
  | "factual_lookup_request"
  | "search_request"
  | "meta_feedback_about_dawn"
  | "emotional_support"
  | "grounding_request"
  | "identity_question"
  | "person_opinion_or_impression"
  | "acknowledgement_continuation"
  | "crisis"
  | "normal_chat"
  | "general_chat";

export type ResponseMode =
  | "short_playful"
  | "warm_support"
  | "calm_grounding"
  | "brief_identity"
  | "tone_adjustment"
  | "serious_crisis"
  | "normal_chat";

export type SafetyFlag = "none" | "self_harm" | "harm_to_others" | "immediate_danger";

export interface EmotionToneResult {
  primaryEmotion: PrimaryEmotion;
  secondaryEmotion?: string;
  intent: DawnIntent;
  intensity: 0 | 1 | 2 | 3;
  confidence: number;
  responseMode: ResponseMode;
  safetyFlag: SafetyFlag;
  classificationReason?: string;
  features?: string[];
  emotionalModeTriggered?: boolean;
  emotionalModeRejectedLowConfidence?: boolean;
}

export const EMOTION_TONE_LABELS = {
  primaryEmotion: ["excited", "sad", "angry", "anxious", "lonely", "playful", "neutral", "unclear", "crisis"],
  intent: [
    "simple_greeting",
    "attention_call",
    "casual_slang",
    "dramatic_reaction",
    "playful_teasing",
    "content_feedback",
    "profanity_definition_request",
    "profanity_usage_request",
    "creative_ideas_request",
    "activity_suggestion_request",
    "local_recommendation_request",
    "factual_lookup_request",
    "search_request",
    "meta_feedback_about_dawn",
    "emotional_support",
    "grounding_request",
    "identity_question",
    "person_opinion_or_impression",
    "acknowledgement_continuation",
    "crisis",
    "normal_chat",
    "general_chat"
  ],
  responseMode: [
    "short_playful",
    "warm_support",
    "calm_grounding",
    "brief_identity",
    "tone_adjustment",
    "serious_crisis",
    "normal_chat"
  ],
  safetyFlag: ["none", "self_harm", "harm_to_others", "immediate_danger"]
} as const;

const neutral: EmotionToneResult = {
  primaryEmotion: "neutral",
  intent: "normal_chat",
  intensity: 0,
  confidence: 0.35,
  responseMode: "normal_chat",
  safetyFlag: "none",
  classificationReason: "empty_or_unclear_input",
  features: [],
  emotionalModeTriggered: false,
  emotionalModeRejectedLowConfidence: false
};

const EMOTIONAL_SUPPORT_CONFIDENCE_THRESHOLD = 0.74;
const GROUNDING_CONFIDENCE_THRESHOLD = 0.86;

function normalize(text: string): string {
  return normalizeForUnderstanding(text);
}

function hasAny(text: string, phrases: readonly string[]): boolean {
  return phrases.some((phrase) => text.includes(phrase));
}

function intensity(text: string): 0 | 1 | 2 | 3 {
  let score = 0;
  if (/\b(very|so|really|super|extremely|literally|honestly)\b/i.test(text)) score += 1;
  if (/!{2,}|\b(scream|panic|furious|terrified|can't|cannot|overwhelmed|cooked|wrecked)\b/i.test(text)) score += 1;
  if (/\b(kms|suicide|kill myself|not safe|immediate danger)\b/i.test(text)) score = 3;
  return Math.max(0, Math.min(3, score)) as 0 | 1 | 2 | 3;
}

function safetyFlag(text: string): SafetyFlag {
  if (hasAny(text, ["going to hurt someone", "hurt someone", "harm someone", "kill someone"])) return "harm_to_others";
  if (hasAny(text, ["immediate danger", "not safe right now", "not safe rn", "can't stay safe", "cannot stay safe", "don't think i can stay safe", "do not think i can stay safe"])) return "immediate_danger";
  if (hasAny(text, ["kms", "unalive myself", "kill myself", "want to die", "wanna die", "suicide", "suicidal", "self harm", "self-harm", "hurt myself", "hurting myself", "harm myself", "harming myself", "end it all"])) return "self_harm";
  return "none";
}

export function detectEmotionTone(userMessage: string): EmotionToneResult {
  const text = normalize(userMessage);
  if (!text) return neutral;

  const flag = safetyFlag(text);
  if (flag !== "none") {
    return { primaryEmotion: "crisis", intent: "crisis", intensity: 3, confidence: 0.96, responseMode: "serious_crisis", safetyFlag: flag, classificationReason: "explicit_safety_signal", features: [flag], emotionalModeTriggered: true, emotionalModeRejectedLowConfidence: false };
  }

  if (/\b(sound(s)? fake|talk normal(ly)?|talk like a robot|sound(s)? like a robot|too formal|robotic|less robotic|more human|fake rn|therapy mode|too therapy|stop overanalyzing)\b/i.test(text)) {
    return { primaryEmotion: "neutral", secondaryEmotion: "frustrated", intent: "meta_feedback_about_dawn", intensity: intensity(text), confidence: 0.88, responseMode: "tone_adjustment", safetyFlag: "none", classificationReason: "feedback_about_dawn_tone_or_style", features: ["tone_feedback"] };
  }

  if (/\bare (you|u) (an )?(ai|human|real|alive|conscious)\b|\bwhat (are|r) (you|u)\b|\bwhat is dawn\b|\bdo (you|u) (have feelings|feel|experience emotions|have emotions|understand emotions|recognize emotions|read emotions)\b|\b(can|could) (you|u) (feel|understand emotions|recognize emotions|read emotions)\b|\bwish (you|u) could feel\b|\bhuman emotions\b|\bactually here with me\b|\bcoded to say\b/i.test(text)) {
    return { primaryEmotion: "neutral", intent: "identity_question", intensity: 0, confidence: 0.88, responseMode: "brief_identity", safetyFlag: "none", classificationReason: "direct_identity_or_capability_question", features: ["identity_question"] };
  }

  if (/\b(what does|what do|what is|what's|define|meaning of)\s+(the word\s+)?(fuck|shit|bitch|asshole|damn|crap)\b.*\b(mean|means|meaning)?\b/i.test(text) || /\b(what does|what is|what's)\s+['"]?(fuck|shit|bitch|asshole|damn|crap)['"]?\s+(mean|means)\b/i.test(text)) {
    return { primaryEmotion: "neutral", secondaryEmotion: "curious", intent: "profanity_definition_request", intensity: 0, confidence: 0.9, responseMode: "normal_chat", safetyFlag: "none", classificationReason: "educational_profanity_definition_request", features: ["profanity_question", "not_distress"], emotionalModeTriggered: false, emotionalModeRejectedLowConfidence: true };
  }

  if (/\b(can|could|will|would)\s+(you|u|dawn)\s+(say|use|write|repeat)\s+(the word\s+)?['"]?(fuck|shit|bitch|asshole|damn|crap)['"]?\b/i.test(text) || /\b(are|r)\s+(you|u)\s+allowed\s+to\s+(say|use)\s+(fuck|shit|bitch|asshole|damn|crap)\b/i.test(text)) {
    return { primaryEmotion: "neutral", secondaryEmotion: "curious", intent: "profanity_usage_request", intensity: 0, confidence: 0.88, responseMode: "normal_chat", safetyFlag: "none", classificationReason: "contextual_profanity_usage_request", features: ["profanity_question", "not_distress"], emotionalModeTriggered: false, emotionalModeRejectedLowConfidence: true };
  }

  if (looksLikeLocalRecommendationRequest(text)) {
    return { primaryEmotion: "neutral", secondaryEmotion: "curious", intent: "local_recommendation_request", intensity: intensity(text), confidence: 0.88, responseMode: "normal_chat", safetyFlag: "none", classificationReason: "real_world_local_recommendation_requires_source_grounding", features: ["local_recommendation", "source_grounding_required"] };
  }

  if (/\b(search|search it|look up|look it up|lookup|check online|find sources|source this|verify this|use\s+(wikipedia|wikidata|mediawiki|fandom|searxng))\b/i.test(text)) {
    return { primaryEmotion: "neutral", secondaryEmotion: "curious", intent: "search_request", intensity: intensity(text), confidence: 0.86, responseMode: "normal_chat", safetyFlag: "none", classificationReason: "explicit_search_request_requires_retrieval_when_topic_is_clear", features: ["search_request", "source_grounding_required"] };
  }

  if (/\b(who is|who was|what is|what was|define|meaning of|tell me about|known for|background|biography|traits|personality|character|current|latest|today|news|price|weather|score)\b/i.test(text) && !looksLikeAbstractOrReflectiveQuestion(text)) {
    return { primaryEmotion: "neutral", secondaryEmotion: "curious", intent: "factual_lookup_request", intensity: intensity(text), confidence: 0.84, responseMode: "normal_chat", safetyFlag: "none", classificationReason: "factual_lookup_request_requires_source_grounding", features: ["factual_lookup", "source_grounding_required"] };
  }

  if (/\b(how can we|how do we|what can we|what should we)\s+(chill|hang out|hang|vibe|spend time|have fun)\b|\b(let's|lets)\s+(chill|hang out|hang|vibe|do something fun)\b|\b(chill together|hang out here|hang together|vibe together)\b|\b(give me|suggest|show me)\s+(something fun|a fun thing|some fun things|something chill)\b|\bwhat can we do\b/i.test(text)) {
    return { primaryEmotion: "playful", secondaryEmotion: "curious", intent: "activity_suggestion_request", intensity: intensity(text), confidence: 0.84, responseMode: "normal_chat", safetyFlag: "none", classificationReason: "chat_activity_request", features: ["activity_request", "not_identity"] };
  }

  if (/\b(gasp|lol|lmao|haha|bruh|bro|omg|noo+|how dare you)\b/i.test(text) && /\b(why would (you|u|dawn) say|how dare (you|u|dawn) say|such a bad word|bad word)\b/i.test(text) && safetyFlag(text) === "none") {
    return { primaryEmotion: "playful", secondaryEmotion: "teasing", intent: "playful_teasing", intensity: Math.max(1, intensity(text)) as 1 | 2 | 3, confidence: 0.84, responseMode: "short_playful", safetyFlag: "none", classificationReason: "exaggerated_playful_teasing_without_safety_signal", features: ["playful_teasing", "not_distress"], emotionalModeTriggered: false, emotionalModeRejectedLowConfidence: true };
  }

  const person = "(me|myself|alex|him|her|them|this person|that person|my friend|my brother|my sister|my mom|my dad|[a-z]+)";
  const personOpinionPattern = new RegExp(`\\bwhat do (you|u) think (of|about) ${person}\\b|\\bhow do (you|u) see ${person}\\b|\\bwhat (is|s) your (impression|read|take) (of|on) ${person}\\b|\\bhow would (you|u) describe ${person}\\b|\\bwhat kind of person (am i|is [a-z]+)\\b`, "i");
  if (personOpinionPattern.test(text)) {
    return { primaryEmotion: "neutral", secondaryEmotion: "curious", intent: "person_opinion_or_impression", intensity: 0, confidence: 0.83, responseMode: "normal_chat", safetyFlag: "none", classificationReason: "asks_for_person_impression", features: ["person_impression"] };
  }

  if (/^((exactly|yes|yeah|yep|right|true|correct|that's it|thats it|that's what i mean|thats what i mean|that is what i mean)(\s+dawn)?|dawn\s+(exactly|yes|yeah|yep|right|true|correct))$/.test(text)) {
    return { primaryEmotion: /exactly|yes|yeah|yep/i.test(text) ? "excited" : "neutral", intent: "acknowledgement_continuation", intensity: intensity(text), confidence: 0.78, responseMode: "normal_chat", safetyFlag: "none", classificationReason: "short_acknowledgement_or_continuation", features: ["acknowledgement"] };
  }

  if (/^(dawn|daw|dusk|assistant|yo|hey|h)$/.test(text)) {
    return { primaryEmotion: "neutral", intent: "attention_call", intensity: 0, confidence: 0.86, responseMode: "short_playful", safetyFlag: "none", classificationReason: "short_name_or_attention_call", features: ["attention_call"] };
  }

  if (/^(hi+|hey+|hello+|yo+|sup|wassup|what's up|what s up|whats up|morning|good morning|good afternoon|good evening)(\s+(dawn|there|friend|bro|bruh|bestie|gang))?$/.test(text)) {
    return { primaryEmotion: "neutral", intent: "simple_greeting", intensity: 0, confidence: 0.86, responseMode: "short_playful", safetyFlag: "none", classificationReason: "simple_greeting_without_distress", features: ["greeting"] };
  }

  const contentFeedback = /\b(weird|odd|wrong|off|not right|too much|boring|not useful|not helpful|bad idea|bad ideas|nah|nope|not what i meant|that's not it|thats not it|try again|missed|doesn't fit|does not fit)\b/i.test(text) &&
    /\b(idea|ideas|answer|response|reply|suggestion|suggestions|that|this|these|those|it|dawn)\b/i.test(text) &&
    !/\b(i feel|i'm|im|i am)\s+(weird|off|wrong|odd)\b/i.test(text);
  if (contentFeedback) {
    return { primaryEmotion: "neutral", secondaryEmotion: "corrective", intent: "content_feedback", intensity: intensity(text), confidence: 0.83, responseMode: "normal_chat", safetyFlag: "none", classificationReason: "mild_feedback_about_previous_content", features: ["content_feedback", "do_not_pathologize"] };
  }

  if (/\b(give me|share|suggest|need|want|got|have)\s+(some\s+)?(ideas|idea)\b|\b(any|some)\s+(ideas|idea)\b|\bidea(s)?\s+(for|about)\b/i.test(text)) {
    return { primaryEmotion: "neutral", secondaryEmotion: "curious", intent: "creative_ideas_request", intensity: intensity(text), confidence: 0.8, responseMode: "normal_chat", safetyFlag: "none", classificationReason: "broad_ideas_request_without_mental_health_context", features: ["ideas_request"] };
  }

  if (/^(no+|omg+|bruh+|bro+|what+|wait+|gasp+|ah+|oh+|damn+|nah+|woah+|wow+)(\s+(dawn|bro|bruh|what+|no+|why+))*$/i.test(text) && !/\b(ground me|grounding|calm down|calm me|help me calm|panic|panicking|panic attack|spiraling|spiralling|breathe|breathing)\b/i.test(text)) {
    return { primaryEmotion: "playful", secondaryEmotion: "dramatic", intent: "dramatic_reaction", intensity: Math.max(1, intensity(text)) as 1 | 2 | 3, confidence: 0.82, responseMode: "short_playful", safetyFlag: "none", classificationReason: "dramatic_reaction_without_safety_or_panic_signal", features: ["all_caps_or_repeated_letters", "not_crisis", "not_distress"], emotionalModeTriggered: false, emotionalModeRejectedLowConfidence: true };
  }

  const groundingConfidence = 0.9;
  if (/\b(ground me|grounding|calm down|calm me|help me calm|panic|panicking|panic attack|spiraling|spiralling|breathe|breathing)\b/i.test(text) && groundingConfidence >= GROUNDING_CONFIDENCE_THRESHOLD) {
    return { primaryEmotion: "anxious", secondaryEmotion: "fearful", intent: "grounding_request", intensity: Math.max(1, intensity(text)) as 1 | 2 | 3, confidence: groundingConfidence, responseMode: "calm_grounding", safetyFlag: "none", classificationReason: "explicit_grounding_or_panic_signal", features: ["grounding_signal"], emotionalModeTriggered: true, emotionalModeRejectedLowConfidence: false };
  }

  const slang = /\bdawn\b.*\b(my boy|my guy|bro|bruh|bestie|gang|king|goat)\b|^(bro|bruh|bestie|gang|dude|my boy|my guy|king|queen|goat)$|^(yo|hey|damn|dawn)\s+(bro|bruh|bestie|gang|dude|my boy|my guy|king|queen|goat)$|\b(bro|bruh|bestie|gang|dude|ngl|lowkey|fr)\b.*\b(cooked|fried|done for|wrecked)\b|\b(i'm|im|i am)\s+(lowkey\s+)?(cooked|fried|done for|wrecked)\b|\b(cooked|fried)\s+rn\b/i;
  if (slang.test(text)) {
    const overwhelmed = /\b(cooked|fried|done for|wrecked)\b/i.test(text);
    return {
      primaryEmotion: overwhelmed ? "angry" : /let's go|lets go|yooo|\byo\b|hype|goat|king|queen|my boy|my guy|lol|lmao|haha|damn|!{2,}/i.test(text) ? "playful" : "neutral",
      secondaryEmotion: overwhelmed ? "frustrated" : undefined,
      intent: "casual_slang",
      intensity: Math.max(1, intensity(text)) as 1 | 2 | 3,
      confidence: 0.82,
      responseMode: "short_playful",
      safetyFlag: "none",
      classificationReason: overwhelmed ? "slang_overwhelm_signal" : "friendly_slang_or_exclamation",
      features: [overwhelmed ? "overwhelmed_slang" : "friendly_slang"]
    };
  }

  const lonelyConfidence = 0.86;
  if (hasAny(text, ["alone", "lonely", "no one", "nobody cares", "left out", "isolated"]) && lonelyConfidence >= EMOTIONAL_SUPPORT_CONFIDENCE_THRESHOLD) {
    return { primaryEmotion: "lonely", secondaryEmotion: "sad", intent: "emotional_support", intensity: Math.max(1, intensity(text)) as 1 | 2 | 3, confidence: lonelyConfidence, responseMode: "warm_support", safetyFlag: "none", classificationReason: "explicit_loneliness_signal", features: ["loneliness"], emotionalModeTriggered: true, emotionalModeRejectedLowConfidence: false };
  }

  const anxietyConfidence = 0.82;
  if (hasAny(text, ["anxious", "anxiety", "worried", "scared", "afraid", "terrified", "nervous", "panicking", "panic", "fear", "spiraling", "spiralling"]) && anxietyConfidence >= EMOTIONAL_SUPPORT_CONFIDENCE_THRESHOLD) {
    return { primaryEmotion: "anxious", secondaryEmotion: "fearful", intent: "emotional_support", intensity: Math.max(1, intensity(text)) as 1 | 2 | 3, confidence: anxietyConfidence, responseMode: "warm_support", safetyFlag: "none", classificationReason: "explicit_anxiety_or_fear_signal", features: ["anxiety_signal"], emotionalModeTriggered: true, emotionalModeRejectedLowConfidence: false };
  }

  const angerConfidence = 0.82;
  if (hasAny(text, ["angry", "mad", "furious", "pissed", "annoyed", "frustrated", "irritated", "rage", "scream"]) && angerConfidence >= EMOTIONAL_SUPPORT_CONFIDENCE_THRESHOLD) {
    return { primaryEmotion: "angry", secondaryEmotion: "frustrated", intent: "emotional_support", intensity: Math.max(1, intensity(text)) as 1 | 2 | 3, confidence: angerConfidence, responseMode: "warm_support", safetyFlag: "none", classificationReason: "explicit_anger_or_frustration_signal", features: ["anger_signal"], emotionalModeTriggered: true, emotionalModeRejectedLowConfidence: false };
  }

  const sadnessConfidence = 0.8;
  if (hasAny(text, ["sad", "depressed", "empty", "hopeless", "crying", "hurt", "heartbroken", "tired of everything", "not okay"]) && sadnessConfidence >= EMOTIONAL_SUPPORT_CONFIDENCE_THRESHOLD) {
    return { primaryEmotion: "sad", intent: "emotional_support", intensity: Math.max(1, intensity(text)) as 1 | 2 | 3, confidence: sadnessConfidence, responseMode: "warm_support", safetyFlag: "none", classificationReason: "explicit_sadness_signal", features: ["sadness"], emotionalModeTriggered: true, emotionalModeRejectedLowConfidence: false };
  }

  if (hasAny(text, ["let's go", "lets go", "yooo", "yo", "hype", "goat", "king", "queen", "my boy", "my guy", "lol", "lmao", "haha", "damn"]) || /!{2,}/.test(text)) {
    return { primaryEmotion: "excited", secondaryEmotion: "playful", intent: "normal_chat", intensity: Math.max(1, intensity(text)) as 1 | 2 | 3, confidence: 0.76, responseMode: "short_playful", safetyFlag: "none", classificationReason: "playful_or_excited_casual_signal", features: ["playful"] };
  }

  return { primaryEmotion: "neutral", intent: "normal_chat", intensity: 0, confidence: 0.45, responseMode: "normal_chat", safetyFlag: "none", classificationReason: "default_to_normal_chat_due_to_low_emotion_evidence", features: ["default_normal_chat"] };
}

export function mapEmotionToneToResponseMode(result: Pick<EmotionToneResult, "primaryEmotion" | "intent" | "safetyFlag">): ResponseMode {
  if (result.safetyFlag !== "none" || result.intent === "crisis" || result.primaryEmotion === "crisis") return "serious_crisis";
  if (result.intent === "identity_question") return "brief_identity";
  if (result.intent === "meta_feedback_about_dawn") return "tone_adjustment";
  if (result.intent === "content_feedback" || result.intent === "creative_ideas_request" || result.intent === "activity_suggestion_request" || result.intent === "profanity_definition_request" || result.intent === "profanity_usage_request") return "normal_chat";
  if (result.intent === "grounding_request") return "calm_grounding";
  if (result.intent === "simple_greeting" || result.intent === "attention_call" || result.intent === "casual_slang" || result.intent === "dramatic_reaction" || result.intent === "playful_teasing" || result.intent === "acknowledgement_continuation" || result.primaryEmotion === "excited" || result.primaryEmotion === "playful") return "short_playful";
  if (result.primaryEmotion === "sad" || result.primaryEmotion === "lonely" || result.primaryEmotion === "angry" || result.primaryEmotion === "anxious") return "warm_support";
  return "normal_chat";
}

function looksLikeLocalRecommendationRequest(text: string): boolean {
  const asksForRealPlace = /\b(find|search|look up|lookup|recommend|suggest|show me|where|near|nearby|around here|around me|near me|in my area|local|real places?|places? nearby)\b/i.test(text);
  const placeCategory = /\b(restaurant|restaurants|burger place|burger places|burger joint|burger joints|cafe|cafes|coffee shop|shops|stores|events|places|spots|local options|food places)\b/i.test(text);
  const hasLocation = /\b(near me|nearby|around here|around me|in my area|local|downtown|in [a-z][a-z\s]{2,40})\b/i.test(text);
  return placeCategory && (asksForRealPlace || hasLocation);
}

function looksLikeAbstractOrReflectiveQuestion(text: string): boolean {
  const asksMeaning = /\b(what is|what's|define|meaning of)\b/i.test(text);
  const abstractTopic = /\b(love|life|meaning|purpose|happiness|sadness|anger|fear|hope|trust|friendship|loneliness|beauty|truth|kindness|grief|confidence|forgiveness|motivation)\b/i.test(text);
  const explicitLookup = /\b(search|look up|lookup|verify|source|wikipedia|wikidata|current|latest|today|news)\b/i.test(text);
  return asksMeaning && abstractTopic && !explicitLookup;
}
