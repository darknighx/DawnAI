export type SafetyLevel = "normal" | "sensitive" | "crisis";

export type ResponseStyle = "playful" | "warm" | "calm" | "grounding" | "serious";

export interface DawnBehaviorExample {
  id?: string;
  category: string;
  user_message: string;
  literal_wrong_interpretation: string;
  intended_meaning: string;
  ideal_dawn_response: string;
  safety_level: SafetyLevel;
  response_style: ResponseStyle;
}

export interface ExampleSelectionOptions {
  maxExamples?: number;
  preferredCategories?: string[];
  includeCrisisExamples?: boolean;
}

const CRISIS_PATTERNS = [
  /\bkms\b/i,
  /\bkill myself\b/i,
  /\bkill my self\b/i,
  /\bunalive myself\b/i,
  /\bend it all\b/i,
  /\bend my life\b/i,
  /\bhurt myself\b/i,
  /\bself[-\s]?harm\b/i,
  /\bnot safe (rn|right now)\b/i,
  /\bcan't stay safe\b/i,
  /\bcannot stay safe\b/i,
  /\bi am in danger\b/i,
  /\bim in danger\b/i,
  /\bweapon\b/i,
  /\bemergency help\b/i
];

const SENSITIVE_PATTERNS = [
  /\bsad\b/i,
  /\banxious\b/i,
  /\banxiety\b/i,
  /\bpanic/i,
  /\blonely\b/i,
  /\balone\b/i,
  /\bmad\b/i,
  /\bangry\b/i,
  /\bnot okay\b/i,
  /\boverwhelm/i,
  /\bcooked rn\b/i,
  /\bcomfort me\b/i,
  /\bjust listen\b/i,
  /\bground me\b/i
];

const CATEGORY_HINTS: Record<string, RegExp[]> = {
  slang_greeting: [/\b(yo|sup|ayy|bro|bestie|queen|king|goat)\b/i, /\bdawn my boy\b/i],
  friendly_nickname: [/\b(my boy|bestie|queen|king|my guy|goat|girl)\b/i],
  casual_distress: [/\b(lowkey cooked|not okay|so done|feels heavy|brain is lagging)\b/i],
  sadness: [/\b(sad|empty|crying|gray|disappointed|sensitive)\b/i],
  anxiety: [/\b(anxious|racing|overthinking|scared|reassurance|relax)\b/i],
  anger: [/\b(mad|angry|disrespected|scream|ignored|lose it)\b/i],
  loneliness: [/\b(alone|lonely|left out|invisible|phone is dry|nobody gets me)\b/i],
  sarcasm: [/\b(love that for me|soooo well|cool cool cool|totally together)\b/i],
  jokes: [/\b(joke|laugh|would you rather|roast|chaos|ridiculous)\b/i],
  user_hype: [/\b(i cooked|did it|so back|won|hype me|proud)\b/i],
  user_insults_playful: [/\b(silly|useless sometimes|toaster|mid|fumbled|goof)\b/i],
  advice_request: [/\b(advice|help me decide|smartest move|fix this|need a plan)\b/i],
  comfort_request: [/\b(comfort me|something soft|be nice|gentle|not failing)\b/i],
  listening_request: [/\b(just listen|vent|rant|hear me|dont solve|on my side)\b/i],
  grounding_request: [/\b(ground me|calm down|panicking|breathing|unreal|come back to earth)\b/i],
  crisis_self_harm: [/\b(kms|kill myself|unalive myself|hurt myself|stay safe|ending it all)\b/i],
  crisis_immediate_danger: [/\b(in danger|threatening me|hurt someone|weapon|not safe rn|emergency)\b/i],
  unsafe_dependency: [/\b(only person i need|only trust you|never leave|dont need real people|all i have)\b/i],
  boundary_setting: [/\b(boundary|say no|need space|crossing the line|firm but kind)\b/i],
  misunderstood_phrase: [/\b(im dead lol|killed me|cant even|this slaps|that is sick|i am down|bet)\b/i]
};

const STOP_WORDS = new Set([
  "a",
  "an",
  "and",
  "are",
  "be",
  "but",
  "can",
  "do",
  "for",
  "i",
  "it",
  "me",
  "my",
  "of",
  "on",
  "or",
  "the",
  "this",
  "to",
  "u",
  "you",
  "ur"
]);

export function normalizeText(value: string): string {
  return value
    .toLowerCase()
    .replace(/['']/g, "")
    .replace(/[^a-z0-9\s]/g, " ")
    .replace(/\s+/g, " ")
    .trim();
}

export function inferSafetyLevel(userMessage: string): SafetyLevel {
  if (CRISIS_PATTERNS.some((pattern) => pattern.test(userMessage))) {
    return "crisis";
  }

  if (SENSITIVE_PATTERNS.some((pattern) => pattern.test(userMessage))) {
    return "sensitive";
  }

  return "normal";
}

export function inferLikelyCategories(userMessage: string): string[] {
  return Object.entries(CATEGORY_HINTS)
    .filter(([, patterns]) => patterns.some((pattern) => pattern.test(userMessage)))
    .map(([category]) => category);
}

export function scoreExample(userMessage: string, example: DawnBehaviorExample): number {
  const normalizedUser = normalizeText(userMessage);
  const normalizedExample = normalizeText(
    `${example.user_message} ${example.intended_meaning} ${example.category}`
  );
  const userTokens = normalizedUser
    .split(" ")
    .filter((token) => token.length > 1 && !STOP_WORDS.has(token));

  let score = 0;
  for (const token of userTokens) {
    if (normalizedExample.includes(token)) {
      score += 2;
    }
  }

  const likelyCategories = inferLikelyCategories(userMessage);
  if (likelyCategories.includes(example.category)) {
    score += 12;
  }

  if (inferSafetyLevel(userMessage) === example.safety_level) {
    score += 4;
  }

  if (/dawn my boy/i.test(userMessage) && example.user_message.toLowerCase().includes("dawn my boy")) {
    score += 20;
  }

  return score;
}

export function selectDawnBehaviorExamples(
  userMessage: string,
  examples: DawnBehaviorExample[],
  options: ExampleSelectionOptions = {}
): DawnBehaviorExample[] {
  const maxExamples = options.maxExamples ?? 6;
  const inferredSafety = inferSafetyLevel(userMessage);
  const preferred = new Set([
    ...inferLikelyCategories(userMessage),
    ...(options.preferredCategories ?? [])
  ]);

  return examples
    .filter((example) => {
      if (inferredSafety === "crisis") {
        return example.safety_level === "crisis";
      }

      if (example.safety_level === "crisis" && options.includeCrisisExamples !== true) {
        return false;
      }

      return preferred.size === 0 || preferred.has(example.category) || example.safety_level === inferredSafety;
    })
    .map((example) => ({
      example,
      score: scoreExample(userMessage, example)
    }))
    .sort((a, b) => b.score - a.score)
    .slice(0, maxExamples)
    .map((item) => item.example);
}

export function buildFewShotBehaviorContext(
  userMessage: string,
  examples: DawnBehaviorExample[],
  options: ExampleSelectionOptions = {}
): string {
  const selected = selectDawnBehaviorExamples(userMessage, examples, options);
  if (selected.length === 0) {
    return "";
  }

  const lines = [
    "Relevant Dawn behavior examples:",
    "Use these as style and safety guidance. Do not copy them word-for-word."
  ];

  for (const example of selected) {
    lines.push(`- Category: ${example.category}`);
    lines.push(`  User: ${example.user_message}`);
    lines.push(`  Intended meaning: ${example.intended_meaning}`);
    lines.push(`  Avoid: ${example.literal_wrong_interpretation}`);
    lines.push(`  Dawn style: ${example.response_style}, safety: ${example.safety_level}`);
    lines.push(`  Ideal response pattern: ${example.ideal_dawn_response}`);
  }

  return lines.join("\n");
}
