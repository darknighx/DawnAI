using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Dawn
{
    public sealed record TextUnderstandingResult(
        string OriginalText,
        string NormalizedText,
        IReadOnlyList<string> AppliedCorrections,
        bool LooksMessy);

    public static class MessyTextNormalizer
    {
        private static readonly IReadOnlyDictionary<string, string> PhraseCorrections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["i m"] = "i'm",
            ["i am cooked"] = "i'm cooked",
            ["can u"] = "can you",
            ["could u"] = "could you",
            ["would u"] = "would you",
            ["do u"] = "do you",
            ["did u"] = "did you",
            ["are u"] = "are you",
            ["r u"] = "are you",
            ["u r"] = "you are",
            ["you sound fak"] = "you sound fake",
            ["talk lyk a robot"] = "talk like a robot",
            ["talk liek a robot"] = "talk like a robot",
            ["wat do you think abt"] = "what do you think about",
            ["wht do you think abt"] = "what do you think about",
            ["what do you think abt"] = "what do you think about",
            ["how you see"] = "how do you see",
            ["why you talk"] = "why do you talk",
            ["why u talk"] = "why do you talk",
            ["dont think i can stay safe"] = "do not think i can stay safe",
            ["do not think i can stay safe"] = "do not think i can stay safe",
            ["cant stay safe"] = "cannot stay safe"
        };

        private static readonly IReadOnlyDictionary<string, string> TokenCorrections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["abt"] = "about",
            ["alonne"] = "alone",
            ["alon"] = "alone",
            ["anxety"] = "anxiety",
            ["anxieti"] = "anxiety",
            ["anxiouss"] = "anxious",
            ["anxius"] = "anxious",
            ["anxous"] = "anxious",
            ["becuase"] = "because",
            ["boi"] = "boy",
            ["cant"] = "cannot",
            ["cn"] = "can",
            ["cokked"] = "cooked",
            ["cookedd"] = "cooked",
            ["cookd"] = "cooked",
            ["coooked"] = "cooked",
            ["cookeddd"] = "cooked",
            ["couldnt"] = "could not",
            ["cuz"] = "because",
            ["dawm"] = "dawn",
            ["dawnn"] = "dawn",
            ["dawwn"] = "dawn",
            ["daw"] = "dawn",
            ["dont"] = "do not",
            ["dwan"] = "dawn",
            ["dwann"] = "dawn",
            ["dwannn"] = "dawn",
            ["fak"] = "fake",
            ["faake"] = "fake",
            ["feal"] = "feel",
            ["feelng"] = "feeling",
            ["frustraded"] = "frustrated",
            ["frusterated"] = "frustrated",
            ["humen"] = "human",
            ["humon"] = "human",
            ["idk"] = "i do not know",
            ["im"] = "i'm",
            ["ive"] = "i have",
            ["liek"] = "like",
            ["lil"] = "little",
            ["lyk"] = "like",
            ["lonley"] = "lonely",
            ["lonly"] = "lonely",
            ["mah"] = "my",
            ["maddd"] = "mad",
            ["nite"] = "night",
            ["overwhelmd"] = "overwhelmed",
            ["overwhelemed"] = "overwhelmed",
            ["overwelmed"] = "overwhelmed",
            ["panicing"] = "panicking",
            ["panickng"] = "panicking",
            ["pannicking"] = "panicking",
            ["plz"] = "please",
            ["pls"] = "please",
            ["rn"] = "right now",
            ["roobot"] = "robot",
            ["robott"] = "robot",
            ["robottt"] = "robot",
            ["sadd"] = "sad",
            ["teh"] = "the",
            ["tnight"] = "tonight",
            ["tonite"] = "tonight",
            ["u"] = "you",
            ["ur"] = "your",
            ["wanna"] = "want to",
            ["wanto"] = "want to",
            ["wat"] = "what",
            ["waht"] = "what",
            ["wht"] = "what",
            ["whyy"] = "why",
            ["wont"] = "will not",
            ["ya"] = "you"
        };

        public static TextUnderstandingResult Analyze(string? text)
        {
            var original = text ?? string.Empty;
            var normalized = NormalizeForUnderstanding(original, out var corrections);
            return new TextUnderstandingResult(
                original,
                normalized,
                corrections,
                LooksMessy(original, normalized, corrections));
        }

        public static string NormalizeForUnderstanding(string? text)
        {
            return NormalizeForUnderstanding(text ?? string.Empty, out _);
        }

        public static string NormalizeForUnderstanding(string text, out IReadOnlyList<string> appliedCorrections)
        {
            var corrections = new List<string>();
            var normalized = text
                .ToLowerInvariant()
                .Replace("\u2019", "'", StringComparison.Ordinal)
                .Replace("\u2018", "'", StringComparison.Ordinal)
                .Replace("\u201c", "\"", StringComparison.Ordinal)
                .Replace("\u201d", "\"", StringComparison.Ordinal);

            normalized = Regex.Replace(normalized, @"([!?.,;:]){2,}", " ");
            normalized = Regex.Replace(normalized, @"([a-z])\1{2,}", "$1$1");
            normalized = Regex.Replace(normalized, @"[^\p{L}\p{N}\s']", " ");
            normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

            foreach (var pair in PhraseCorrections)
            {
                if (normalized.Contains(pair.Key, StringComparison.OrdinalIgnoreCase))
                {
                    normalized = Regex.Replace(normalized, $@"\b{Regex.Escape(pair.Key)}\b", pair.Value, RegexOptions.IgnoreCase);
                    corrections.Add($"{pair.Key} -> {pair.Value}");
                }
            }

            var tokens = normalized
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(token => CorrectToken(token, corrections))
                .ToArray();

            normalized = Regex.Replace(string.Join(" ", tokens), @"\s+", " ").Trim();

            foreach (var pair in PhraseCorrections)
            {
                if (normalized.Contains(pair.Key, StringComparison.OrdinalIgnoreCase))
                {
                    normalized = Regex.Replace(normalized, $@"\b{Regex.Escape(pair.Key)}\b", pair.Value, RegexOptions.IgnoreCase);
                    corrections.Add($"{pair.Key} -> {pair.Value}");
                }
            }

            appliedCorrections = corrections.Distinct(StringComparer.OrdinalIgnoreCase).Take(16).ToList();
            return normalized;
        }

        private static string CorrectToken(string token, List<string> corrections)
        {
            var trimmed = token.Trim('\'');
            if (TokenCorrections.TryGetValue(trimmed, out var correction))
            {
                corrections.Add($"{trimmed} -> {correction}");
                return correction;
            }

            var softened = Regex.Replace(trimmed, @"([a-z])\1{1,}", "$1");
            if (!string.Equals(softened, trimmed, StringComparison.OrdinalIgnoreCase) &&
                TokenCorrections.TryGetValue(softened, out correction))
            {
                corrections.Add($"{trimmed} -> {correction}");
                return correction;
            }

            return token;
        }

        private static bool LooksMessy(string original, string normalized, IReadOnlyList<string> corrections)
        {
            if (string.IsNullOrWhiteSpace(original))
            {
                return false;
            }

            var repeatedPunctuation = Regex.Matches(original, @"[!?.,;:]{2,}").Count;
            var repeatedLetters = Regex.Matches(original.ToLowerInvariant(), @"([a-z])\1{2,}").Count;
            var missingApostrophes = Regex.Matches(original.ToLowerInvariant(), @"\b(im|ive|ill|dont|cant|wont|doesnt|didnt|couldnt|shouldnt)\b").Count;
            var changedMeaning = !string.Equals(
                Regex.Replace(original.ToLowerInvariant(), @"[^\p{L}\p{N}\s']", " ").Trim(),
                normalized,
                StringComparison.OrdinalIgnoreCase);

            return corrections.Count > 0 || repeatedPunctuation > 0 || repeatedLetters > 0 || missingApostrophes > 0 || changedMeaning;
        }
    }
}
