export interface TextUnderstandingResult {
  originalText: string;
  normalizedText: string;
  appliedCorrections: string[];
  looksMessy: boolean;
}

const phraseCorrections: Record<string, string> = {
  "i m": "i'm",
  "can u": "can you",
  "could u": "could you",
  "would u": "would you",
  "do u": "do you",
  "did u": "did you",
  "are u": "are you",
  "r u": "are you",
  "u r": "you are",
  "talk lyk a robot": "talk like a robot",
  "talk liek a robot": "talk like a robot",
  "wat do you think abt": "what do you think about",
  "wht do you think abt": "what do you think about",
  "what do you think abt": "what do you think about",
  "how you see": "how do you see",
  "why you talk": "why do you talk",
  "why u talk": "why do you talk",
  "dont think i can stay safe": "do not think i can stay safe",
  "cant stay safe": "cannot stay safe"
};

const tokenCorrections: Record<string, string> = {
  abt: "about",
  alon: "alone",
  alonne: "alone",
  anxety: "anxiety",
  anxieti: "anxiety",
  anxius: "anxious",
  anxous: "anxious",
  boi: "boy",
  cant: "cannot",
  cn: "can",
  cokked: "cooked",
  cookedd: "cooked",
  cookd: "cooked",
  coooked: "cooked",
  cookeddd: "cooked",
  couldnt: "could not",
  dawm: "dawn",
  dawnn: "dawn",
  dawwn: "dawn",
  daw: "dawn",
  dont: "do not",
  dwan: "dawn",
  dwann: "dawn",
  dwannn: "dawn",
  fak: "fake",
  faake: "fake",
  feal: "feel",
  feelng: "feeling",
  frusterated: "frustrated",
  frustrateded: "frustrated",
  humen: "human",
  humon: "human",
  idk: "i do not know",
  im: "i'm",
  ive: "i have",
  liek: "like",
  lil: "little",
  lonley: "lonely",
  lonly: "lonely",
  lyk: "like",
  mah: "my",
  maddd: "mad",
  nite: "night",
  overwelmed: "overwhelmed",
  overwhelmd: "overwhelmed",
  panicing: "panicking",
  panickng: "panicking",
  pannicking: "panicking",
  plz: "please",
  pls: "please",
  rn: "right now",
  roobot: "robot",
  robott: "robot",
  robottt: "robot",
  sadd: "sad",
  teh: "the",
  tnight: "tonight",
  tonite: "tonight",
  u: "you",
  ur: "your",
  wanna: "want to",
  wanto: "want to",
  wat: "what",
  waht: "what",
  wht: "what",
  whyy: "why",
  wont: "will not",
  ya: "you"
};

function escapeRegExp(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

export function analyzeMessyText(text: string | undefined | null): TextUnderstandingResult {
  const originalText = text ?? "";
  const appliedCorrections: string[] = [];
  let normalizedText = originalText
    .toLowerCase()
    .replace(/\u2019|\u2018/g, "'")
    .replace(/[!?.,;:]{2,}/g, " ")
    .replace(/([a-z])\1{2,}/g, "$1$1")
    .replace(/[^\p{L}\p{N}\s']/gu, " ")
    .replace(/\s+/g, " ")
    .trim();

  for (const [wrong, right] of Object.entries(phraseCorrections)) {
    const pattern = new RegExp(`\\b${escapeRegExp(wrong)}\\b`, "gi");
    if (pattern.test(normalizedText)) {
      normalizedText = normalizedText.replace(pattern, right);
      appliedCorrections.push(`${wrong} -> ${right}`);
    }
  }

  normalizedText = normalizedText
    .split(/\s+/)
    .filter(Boolean)
    .map((token) => {
      const direct = tokenCorrections[token];
      if (direct) {
        appliedCorrections.push(`${token} -> ${direct}`);
        return direct;
      }

      const softened = token.replace(/([a-z])\1+/g, "$1");
      const fuzzy = tokenCorrections[softened];
      if (fuzzy) {
        appliedCorrections.push(`${token} -> ${fuzzy}`);
        return fuzzy;
      }

      return token;
    })
    .join(" ");

  for (const [wrong, right] of Object.entries(phraseCorrections)) {
    const pattern = new RegExp(`\\b${escapeRegExp(wrong)}\\b`, "gi");
    if (pattern.test(normalizedText)) {
      normalizedText = normalizedText.replace(pattern, right);
      appliedCorrections.push(`${wrong} -> ${right}`);
    }
  }

  const uniqueCorrections = [...new Set(appliedCorrections)].slice(0, 16);
  const looksMessy =
    uniqueCorrections.length > 0 ||
    /[!?.,;:]{2,}/.test(originalText) ||
    /([a-z])\1{2,}/i.test(originalText) ||
    /\b(im|ive|ill|dont|cant|wont|doesnt|didnt|couldnt|shouldnt)\b/i.test(originalText);

  return { originalText, normalizedText, appliedCorrections: uniqueCorrections, looksMessy };
}

export function normalizeForUnderstanding(text: string): string {
  return analyzeMessyText(text).normalizedText;
}
