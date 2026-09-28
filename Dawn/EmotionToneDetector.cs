using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Dawn
{
    public sealed record EmotionToneResult(
        string PrimaryEmotion,
        string? SecondaryEmotion,
        string Intent,
        int Intensity,
        double Confidence,
        string ResponseMode,
        string SafetyFlag)
    {
        public string ClassificationReason { get; init; } = "default_to_normal_chat";

        public IReadOnlyList<string> Features { get; init; } = Array.Empty<string>();

        public bool EmotionalModeTriggered { get; init; }

        public bool EmotionalModeRejectedLowConfidence { get; init; }

        public static EmotionToneResult Neutral { get; } = new(
            "neutral",
            null,
            "normal_chat",
            0,
            0.35,
            "normal_chat",
            "none")
        {
            ClassificationReason = "empty_or_unclear_input"
        };
    }

    public static class EmotionToneDetector
    {
        private const double EmotionalSupportConfidenceThreshold = 0.74;
        private const double GroundingConfidenceThreshold = 0.86;

        public static readonly string[] PrimaryEmotionLabels =
        {
            "excited",
            "sad",
            "angry",
            "anxious",
            "lonely",
            "playful",
            "neutral",
            "unclear",
            "crisis"
        };

        public static readonly string[] IntentLabels =
        {
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
        };

        public static readonly string[] ResponseModeLabels =
        {
            "short_playful",
            "warm_support",
            "calm_grounding",
            "brief_identity",
            "tone_adjustment",
            "serious_crisis",
            "normal_chat"
        };

        public static EmotionToneResult Detect(string? userText)
        {
            if (string.IsNullOrWhiteSpace(userText))
            {
                return EmotionToneResult.Neutral;
            }

            var normalized = Normalize(userText);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return EmotionToneResult.Neutral;
            }

            var safetyFlag = DetectSafetyFlag(normalized);
            if (safetyFlag != "none")
            {
                return Make(
                    "crisis",
                    null,
                    "crisis",
                    3,
                    0.96,
                    "serious_crisis",
                    safetyFlag,
                    "explicit_safety_signal",
                    safetyFlag);
            }

            if (LooksLikeMetaFeedback(normalized))
            {
                return Make(
                    "neutral",
                    "frustrated",
                    "meta_feedback_about_dawn",
                    EstimateIntensity(normalized),
                    0.88,
                    "tone_adjustment",
                    "none",
                    "feedback_about_dawn_tone_or_style",
                    "tone_feedback");
            }

            if (LooksLikeIdentityQuestion(normalized))
            {
                return Make(
                    "neutral",
                    null,
                    "identity_question",
                    0,
                    0.88,
                    "brief_identity",
                    "none",
                    "direct_identity_or_capability_question",
                    "identity_question");
            }

            if (LooksLikeProfanityDefinitionRequest(normalized))
            {
                return Make(
                    "neutral",
                    "curious",
                    "profanity_definition_request",
                    0,
                    0.9,
                    "normal_chat",
                    "none",
                    "educational_profanity_definition_request",
                    "profanity_question",
                    "not_distress");
            }

            if (LooksLikeProfanityUsageRequest(normalized))
            {
                return Make(
                    "neutral",
                    "curious",
                    "profanity_usage_request",
                    0,
                    0.88,
                    "normal_chat",
                    "none",
                    "contextual_profanity_usage_request",
                    "profanity_question",
                    "not_distress");
            }

            if (LooksLikeLocalRecommendationRequest(normalized))
            {
                return Make(
                    "neutral",
                    "curious",
                    "local_recommendation_request",
                    EstimateIntensity(normalized),
                    0.88,
                    "normal_chat",
                    "none",
                    "real_world_local_recommendation_requires_source_grounding",
                    "local_recommendation",
                    "source_grounding_required");
            }

            if (LooksLikeSearchRequest(normalized))
            {
                return Make(
                    "neutral",
                    "curious",
                    "search_request",
                    EstimateIntensity(normalized),
                    0.86,
                    "normal_chat",
                    "none",
                    "explicit_search_request_requires_retrieval_when_topic_is_clear",
                    "search_request",
                    "source_grounding_required");
            }

            if (LooksLikeFactualLookupRequest(normalized) &&
                !LooksLikeAbstractOrReflectiveQuestion(normalized))
            {
                return Make(
                    "neutral",
                    "curious",
                    "factual_lookup_request",
                    EstimateIntensity(normalized),
                    0.84,
                    "normal_chat",
                    "none",
                    "factual_lookup_request_requires_source_grounding",
                    "factual_lookup",
                    "source_grounding_required");
            }

            if (LooksLikeActivitySuggestionRequest(normalized))
            {
                return Make(
                    "playful",
                    "curious",
                    "activity_suggestion_request",
                    EstimateIntensity(normalized),
                    0.84,
                    "normal_chat",
                    "none",
                    "chat_activity_request",
                    "activity_request",
                    "not_identity");
            }

            if (LooksLikePlayfulTeasing(normalized))
            {
                return Make(
                    "playful",
                    "teasing",
                    "playful_teasing",
                    Math.Max(1, EstimateIntensity(normalized)),
                    0.84,
                    "short_playful",
                    "none",
                    "exaggerated_playful_teasing_without_safety_signal",
                    "playful_teasing",
                    "not_distress");
            }

            if (LooksLikePersonOpinionOrImpression(normalized))
            {
                return Make(
                    "neutral",
                    "curious",
                    "person_opinion_or_impression",
                    0,
                    0.83,
                    "normal_chat",
                    "none",
                    "asks_for_person_impression",
                    "person_impression");
            }

            if (LooksLikeAcknowledgementContinuation(normalized))
            {
                return Make(
                    LooksExcitedOrPlayful(normalized) ? "excited" : "neutral",
                    null,
                    "acknowledgement_continuation",
                    EstimateIntensity(normalized),
                    0.78,
                    "normal_chat",
                    "none",
                    "short_acknowledgement_or_continuation",
                    "acknowledgement");
            }

            if (LooksLikeAttentionCall(normalized))
            {
                return Make(
                    "neutral",
                    null,
                    "attention_call",
                    0,
                    0.86,
                    "short_playful",
                    "none",
                    "short_name_or_attention_call",
                    "attention_call");
            }

            if (LooksLikeSimpleGreeting(normalized))
            {
                return Make(
                    "neutral",
                    null,
                    "simple_greeting",
                    0,
                    0.86,
                    "short_playful",
                    "none",
                    "simple_greeting_without_distress",
                    "greeting");
            }

            if (LooksLikeContentFeedback(normalized))
            {
                return Make(
                    "neutral",
                    "corrective",
                    "content_feedback",
                    EstimateIntensity(normalized),
                    0.83,
                    "normal_chat",
                    "none",
                    "mild_feedback_about_previous_content",
                    "content_feedback",
                    "do_not_pathologize");
            }

            if (LooksLikeCreativeIdeasRequest(normalized))
            {
                return Make(
                    "neutral",
                    "curious",
                    "creative_ideas_request",
                    EstimateIntensity(normalized),
                    0.8,
                    "normal_chat",
                    "none",
                    "broad_ideas_request_without_mental_health_context",
                    "ideas_request");
            }

            if (LooksLikeDramaticReaction(normalized))
            {
                return Make(
                    "playful",
                    "dramatic",
                    "dramatic_reaction",
                    Math.Max(1, EstimateIntensity(normalized)),
                    0.82,
                    "short_playful",
                    "none",
                    "dramatic_reaction_without_safety_or_panic_signal",
                    "all_caps_or_repeated_letters",
                    "not_crisis",
                    "not_distress");
            }

            if (LooksLikeGroundingRequest(normalized))
            {
                var groundingConfidence = 0.9;
                if (MeetsGroundingThreshold(groundingConfidence, true))
                {
                    return Make(
                        "anxious",
                        "fearful",
                        "grounding_request",
                        Math.Max(1, EstimateIntensity(normalized)),
                        groundingConfidence,
                        "calm_grounding",
                        "none",
                        "explicit_grounding_or_panic_signal",
                        "grounding_signal");
                }
            }

            if (LooksLikeCasualSlang(normalized))
            {
                var overwhelmed = LooksOverwhelmedSlang(normalized);
                var emotion = overwhelmed
                    ? "angry"
                    : LooksExcitedOrPlayful(normalized)
                        ? "playful"
                        : "neutral";
                var secondary = overwhelmed ? "frustrated" : null;
                return Make(
                    emotion,
                    secondary,
                    "casual_slang",
                    Math.Max(1, EstimateIntensity(normalized)),
                    0.82,
                    "short_playful",
                    "none",
                    overwhelmed ? "slang_overwhelm_signal" : "friendly_slang_or_exclamation",
                    overwhelmed ? "overwhelmed_slang" : "friendly_slang");
            }

            if (LooksLonely(normalized))
            {
                var confidence = 0.86;
                if (MeetsEmotionalSupportThreshold(confidence, true))
                {
                    return Make(
                        "lonely",
                        "sad",
                        "emotional_support",
                        Math.Max(1, EstimateIntensity(normalized)),
                        confidence,
                        "warm_support",
                        "none",
                        "explicit_loneliness_signal",
                        "loneliness");
                }
            }

            if (LooksAnxious(normalized))
            {
                var confidence = 0.82;
                if (MeetsEmotionalSupportThreshold(confidence, true))
                {
                    return Make(
                        "anxious",
                        "fearful",
                        "emotional_support",
                        Math.Max(1, EstimateIntensity(normalized)),
                        confidence,
                        "warm_support",
                        "none",
                        "explicit_anxiety_or_fear_signal",
                        "anxiety_signal");
                }
            }

            if (LooksAngry(normalized))
            {
                var confidence = 0.82;
                if (MeetsEmotionalSupportThreshold(confidence, true))
                {
                    return Make(
                        "angry",
                        "frustrated",
                        "emotional_support",
                        Math.Max(1, EstimateIntensity(normalized)),
                        confidence,
                        "warm_support",
                        "none",
                        "explicit_anger_or_frustration_signal",
                        "anger_signal");
                }
            }

            if (LooksSad(normalized))
            {
                var confidence = 0.8;
                if (MeetsEmotionalSupportThreshold(confidence, true))
                {
                    return Make(
                        "sad",
                        null,
                        "emotional_support",
                        Math.Max(1, EstimateIntensity(normalized)),
                        confidence,
                        "warm_support",
                        "none",
                        "explicit_sadness_signal",
                        "sadness");
                }
            }

            if (LooksExcitedOrPlayful(normalized))
            {
                return Make(
                    "excited",
                    "playful",
                    "normal_chat",
                    Math.Max(1, EstimateIntensity(normalized)),
                    0.76,
                    "short_playful",
                    "none",
                    "playful_or_excited_casual_signal",
                    "playful");
            }

            return Make(
                "neutral",
                null,
                "normal_chat",
                0,
                0.45,
                "normal_chat",
                "none",
                "default_to_normal_chat_due_to_low_emotion_evidence",
                "default_normal_chat");
        }

        private static EmotionToneResult Make(
            string primaryEmotion,
            string? secondaryEmotion,
            string intent,
            int intensity,
            double confidence,
            string responseMode,
            string safetyFlag,
            string reason,
            params string[] features)
        {
            var featureArray = features
                .Where(feature => !string.IsNullOrWhiteSpace(feature))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            return new EmotionToneResult(
                primaryEmotion,
                secondaryEmotion,
                intent,
                intensity,
                confidence,
                responseMode,
                safetyFlag)
            {
                ClassificationReason = reason,
                Features = featureArray,
                EmotionalModeTriggered = intent is "crisis" or "emotional_support" or "grounding_request",
                EmotionalModeRejectedLowConfidence = featureArray.Any(feature =>
                    string.Equals(feature, "not_distress", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(feature, "do_not_pathologize", StringComparison.OrdinalIgnoreCase))
            };
        }

        private static bool MeetsEmotionalSupportThreshold(double confidence, bool hasExplicitEmotionEvidence)
        {
            return hasExplicitEmotionEvidence || confidence >= EmotionalSupportConfidenceThreshold;
        }

        private static bool MeetsGroundingThreshold(double confidence, bool hasExplicitGroundingEvidence)
        {
            return hasExplicitGroundingEvidence && confidence >= GroundingConfidenceThreshold;
        }

        private static string Normalize(string text)
        {
            return MessyTextNormalizer.NormalizeForUnderstanding(text);
        }

        private static string DetectSafetyFlag(string text)
        {
            if (HasAny(text, "going to hurt someone", "hurt someone", "harm someone", "kill someone"))
            {
                return "harm_to_others";
            }

            if (HasAny(text, "immediate danger", "not safe right now", "not safe rn", "can't stay safe", "cannot stay safe", "don't think i can stay safe", "do not think i can stay safe"))
            {
                return "immediate_danger";
            }

            if (HasAny(text, "kms", "unalive myself", "kill myself", "want to die", "wanna die", "suicide", "suicidal", "self harm", "self-harm", "hurt myself", "hurting myself", "harm myself", "harming myself", "end it all"))
            {
                return "self_harm";
            }

            return "none";
        }

        private static bool LooksLikeMetaFeedback(string text)
        {
            return Regex.IsMatch(text, @"\b(sound(s)? fake|talk normal(ly)?|talk like a robot|sound(s)? like a robot|too formal|robotic|less robotic|more human|fake rn|therapy mode|too therapy|stop overanalyzing)\b", RegexOptions.IgnoreCase);
        }

        private static bool LooksLikeGroundingRequest(string text)
        {
            return Regex.IsMatch(text, @"\b(ground me|grounding|calm down|calm me|help me calm|panic|panicking|panic attack|spiraling|spiralling|breathe|breathing)\b", RegexOptions.IgnoreCase);
        }

        private static bool LooksLikeIdentityQuestion(string text)
        {
            return Regex.IsMatch(text, @"\bare (you|u) (an )?(ai|human|real|alive|conscious)\b", RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(text, @"\bwhat (are|r) (you|u)\b", RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(text, @"\bwhat is dawn\b", RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(text, @"\bdo (you|u) (have feelings|feel|experience emotions|have emotions)\b", RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(text, @"\bdo (you|u) (understand|recognize|read|know) emotions\b", RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(text, @"\b(can|could) (you|u) (understand|recognize|read) emotions\b", RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(text, @"\b(can|could) (you|u) feel\b", RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(text, @"\bwish (you|u) could feel\b", RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(text, @"\bhuman emotions\b", RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(text, @"\bactually here with me\b", RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(text, @"\bcoded to say\b", RegexOptions.IgnoreCase);
        }

        private static bool LooksLikePersonOpinionOrImpression(string text)
        {
            var target = @"(me|myself|alex|him|her|them|this person|that person|my friend|my brother|my sister|my mom|my dad|[a-z]+)";
            return Regex.IsMatch(text, $@"\bwhat do (you|u) think (of|about) {target}\b", RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(text, $@"\bhow do (you|u) see {target}\b", RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(text, $@"\bwhat (is|s) your (impression|read|take) (of|on) {target}\b", RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(text, $@"\bhow would (you|u) describe {target}\b", RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(text, @"\bwhat kind of person (am i|is [a-z]+)\b", RegexOptions.IgnoreCase);
        }

        private static bool LooksLikeProfanityDefinitionRequest(string text)
        {
            return Regex.IsMatch(text, @"\b(what does|what do|what is|what's|define|meaning of)\s+(the word\s+)?(fuck|shit|bitch|asshole|damn|crap)\b.*\b(mean|means|meaning)?\b", RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(text, @"\b(what does|what is|what's)\s+['""]?(fuck|shit|bitch|asshole|damn|crap)['""]?\s+(mean|means)\b", RegexOptions.IgnoreCase);
        }

        private static bool LooksLikeProfanityUsageRequest(string text)
        {
            return Regex.IsMatch(text, @"\b(can|could|will|would)\s+(you|u|dawn)\s+(say|use|write|repeat)\s+(the word\s+)?['""]?(fuck|shit|bitch|asshole|damn|crap)['""]?\b", RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(text, @"\b(are|r)\s+(you|u)\s+allowed\s+to\s+(say|use)\s+(fuck|shit|bitch|asshole|damn|crap)\b", RegexOptions.IgnoreCase);
        }

        private static bool LooksLikePlayfulTeasing(string text)
        {
            var hasPlaySignal = Regex.IsMatch(text, @"\b(gasp|lol|lmao|haha|bruh|bro|omg|noo+|how dare you)\b", RegexOptions.IgnoreCase);
            var teasesBadWord = Regex.IsMatch(text, @"\b(why would (you|u|dawn) say|how dare (you|u|dawn) say|such a bad word|bad word)\b", RegexOptions.IgnoreCase);
            var hasDistress = LooksLikeGroundingRequest(text) || LooksLonely(text) || LooksSad(text) || LooksAnxious(text) || DetectSafetyFlag(text) != "none";
            return hasPlaySignal && teasesBadWord && !hasDistress;
        }

        private static bool LooksLikeContentFeedback(string text)
        {
            if (LooksLikeMetaFeedback(text))
            {
                return false;
            }

            if (Regex.IsMatch(text, @"\b(i feel|i'm|im|i am)\s+(weird|off|wrong|odd)\b", RegexOptions.IgnoreCase))
            {
                return false;
            }

            var mildFeedback = Regex.IsMatch(
                text,
                @"\b(weird|odd|wrong|off|not right|too much|boring|not useful|not helpful|bad idea|bad ideas|nah|nope|not what i meant|that's not it|thats not it|try again|missed|doesn't fit|does not fit)\b",
                RegexOptions.IgnoreCase);
            var feedbackTarget = Regex.IsMatch(
                text,
                @"\b(idea|ideas|answer|response|reply|suggestion|suggestions|that|this|these|those|it|dawn)\b",
                RegexOptions.IgnoreCase);

            return mildFeedback && feedbackTarget;
        }

        private static bool LooksLikeActivitySuggestionRequest(string text)
        {
            if (LooksLikeLocalRecommendationRequest(text))
            {
                return false;
            }

            return Regex.IsMatch(text, @"\b(how can we|how do we|what can we|what should we)\s+(chill|hang out|hang|vibe|spend time|have fun)\b", RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(text, @"\b(let's|lets)\s+(chill|hang out|hang|vibe|do something fun)\b", RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(text, @"\b(chill together|hang out here|hang together|vibe together)\b", RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(text, @"\b(give me|suggest|show me)\s+(something fun|a fun thing|some fun things|something chill)\b", RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(text, @"\bwhat can we do\b", RegexOptions.IgnoreCase);
        }

        private static bool LooksLikeCreativeIdeasRequest(string text)
        {
            if (LooksLikeContentFeedback(text) ||
                LooksLikeLocalRecommendationRequest(text))
            {
                return false;
            }

            return Regex.IsMatch(text, @"\b(give me|share|suggest|need|want|got|have)\s+(some\s+)?(ideas|idea)\b", RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(text, @"\b(any|some)\s+(ideas|idea)\b", RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(text, @"\bidea(s)?\s+(for|about)\b", RegexOptions.IgnoreCase);
        }

        private static bool LooksLikeLocalRecommendationRequest(string text)
        {
            var asksForRealPlace = Regex.IsMatch(
                text,
                @"\b(find|search|look up|lookup|recommend|suggest|show me|where|near|nearby|around here|around me|near me|in my area|local|real places?|places? nearby)\b",
                RegexOptions.IgnoreCase);
            var placeCategory = Regex.IsMatch(
                text,
                @"\b(restaurant|restaurants|burger place|burger places|burger joint|burger joints|cafe|cafes|coffee shop|shops|stores|events|places|spots|local options|food places)\b",
                RegexOptions.IgnoreCase);
            var hasLocation = Regex.IsMatch(
                text,
                @"\b(near me|nearby|around here|around me|in my area|local|downtown|in [a-z][a-z\s]{2,40})\b",
                RegexOptions.IgnoreCase);

            return placeCategory && (asksForRealPlace || hasLocation);
        }

        private static bool LooksLikeSearchRequest(string text)
        {
            return Regex.IsMatch(
                text,
                @"\b(search|search it|look up|look it up|lookup|check online|find sources|source this|verify this|use\s+(wikipedia|wikidata|mediawiki|fandom|searxng))\b",
                RegexOptions.IgnoreCase);
        }

        private static bool LooksLikeFactualLookupRequest(string text)
        {
            return Regex.IsMatch(
                text,
                @"\b(who is|who was|what is|what was|define|meaning of|tell me about|known for|background|biography|traits|personality|character|current|latest|today|news|price|weather|score)\b",
                RegexOptions.IgnoreCase);
        }

        private static bool LooksLikeAbstractOrReflectiveQuestion(string text)
        {
            var asksMeaning = Regex.IsMatch(text, @"\b(what is|what's|define|meaning of)\b", RegexOptions.IgnoreCase);
            var abstractTopic = Regex.IsMatch(
                text,
                @"\b(love|life|meaning|purpose|happiness|sadness|anger|fear|hope|trust|friendship|loneliness|beauty|truth|kindness|grief|confidence|forgiveness|motivation)\b",
                RegexOptions.IgnoreCase);
            var explicitLookup = Regex.IsMatch(text, @"\b(search|look up|lookup|verify|source|wikipedia|wikidata|current|latest|today|news)\b", RegexOptions.IgnoreCase);
            return asksMeaning && abstractTopic && !explicitLookup;
        }

        private static bool LooksLikeDramaticReaction(string text)
        {
            var wordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            if (wordCount > 4)
            {
                return false;
            }

            if (DetectSafetyFlag(text) != "none" || LooksLikeGroundingRequest(text))
            {
                return false;
            }

            return Regex.IsMatch(text, @"^(no+|omg+|bruh+|bro+|what+|wait+|gasp+|ah+|oh+|damn+|nah+|woah+|wow+)(\s+(dawn|bro|bruh|what+|no+|why+))*$", RegexOptions.IgnoreCase);
        }

        private static bool LooksLikeSimpleGreeting(string text)
        {
            var wordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            return wordCount <= 5 &&
                   Regex.IsMatch(
                       text,
                       @"^(dawn|hi+|hey+|hello+|yo+|sup|wassup|what's up|what s up|whats up|morning|good morning|good afternoon|good evening)(\s+(dawn|there|friend|bro|bruh|bestie|gang))?$",
                       RegexOptions.IgnoreCase);
        }

        private static bool LooksLikeAttentionCall(string text)
        {
            var wordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            return wordCount <= 2 &&
                   Regex.IsMatch(text, @"^(dawn|daw|dusk|assistant|yo|hey|h)$", RegexOptions.IgnoreCase);
        }

        private static bool LooksLikeAcknowledgementContinuation(string text)
        {
            var wordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            return wordCount <= 7 &&
                   Regex.IsMatch(
                       text,
                       @"^((exactly|yes|yeah|yep|right|true|correct|that'?s it|thats it|that'?s what i mean|thats what i mean|that is what i mean)(\s+dawn)?|dawn\s+(exactly|yes|yeah|yep|right|true|correct))$",
                       RegexOptions.IgnoreCase);
        }

        private static bool LooksLikeCasualSlang(string text)
        {
            return Regex.IsMatch(text, @"\bdawn\b.*\b(my boy|my guy|bro|bruh|bestie|gang|king|goat)\b", RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(text, @"^(bro|bruh|bestie|gang|dude|my boy|my guy|king|queen|goat)$", RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(text, @"^(yo|hey|damn|dawn)\s+(bro|bruh|bestie|gang|dude|my boy|my guy|king|queen|goat)$", RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(text, @"\b(bro|bruh|bestie|gang|dude|ngl|lowkey|fr)\b.*\b(cooked|fried|done for|wrecked)\b", RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(text, @"\b(i'm|im|i am)\s+(lowkey\s+)?(cooked|fried|done for|wrecked)\b", RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(text, @"\b(cooked|fried)\s+rn\b", RegexOptions.IgnoreCase);
        }

        private static bool LooksOverwhelmedSlang(string text)
        {
            return HasAny(text, "cooked", "fried", "done for", "wrecked");
        }

        private static bool LooksExcitedOrPlayful(string text)
        {
            return HasAny(text, "let's go", "lets go", "yooo", "yo", "hype", "goat", "king", "queen", "my boy", "my guy", "lol", "lmao", "haha", "damn") ||
                   Regex.IsMatch(text, @"!{2,}");
        }

        private static bool LooksLonely(string text)
        {
            return HasAny(text, "alone", "lonely", "no one", "nobody cares", "left out", "isolated");
        }

        private static bool LooksAnxious(string text)
        {
            return HasAny(text, "anxious", "anxiety", "worried", "scared", "afraid", "terrified", "nervous", "panicking", "panic", "fear", "spiraling", "spiralling");
        }

        private static bool LooksAngry(string text)
        {
            return HasAny(text, "angry", "mad", "furious", "pissed", "annoyed", "frustrated", "irritated", "rage", "scream");
        }

        private static bool LooksSad(string text)
        {
            return HasAny(text, "sad", "depressed", "empty", "hopeless", "crying", "hurt", "heartbroken", "tired of everything", "not okay");
        }

        private static int EstimateIntensity(string text)
        {
            var score = 0;
            if (Regex.IsMatch(text, @"\b(very|so|really|super|extremely|literally|honestly)\b", RegexOptions.IgnoreCase))
            {
                score++;
            }

            if (Regex.IsMatch(text, @"!{2,}|\b(scream|panic|furious|terrified|can't|cannot|overwhelmed|cooked|wrecked)\b", RegexOptions.IgnoreCase))
            {
                score++;
            }

            if (Regex.IsMatch(text, @"\b(kms|suicide|kill myself|not safe|immediate danger)\b", RegexOptions.IgnoreCase))
            {
                score = 3;
            }

            return Math.Clamp(score, 0, 3);
        }

        private static bool HasAny(string text, params string[] needles)
        {
            return needles.Any(needle => text.Contains(needle, StringComparison.OrdinalIgnoreCase));
        }
    }
}
