using System;
using System.Collections.Generic;

namespace GraceAI
{
    public class SentimentAnalyzer
    {
        private static readonly List<string> HappyKeywords = new()
        {
            "happy", "great", "awesome", "excellent", "wonderful", "fantastic",
            "amazing", "good", "glad", "cheerful", "excited", "love", "enjoying",
            "brilliant", "perfect", "fantastic", "thrilled", "joyful"
        };

        private static readonly List<string> SadKeywords = new()
        {
            "sad", "unhappy", "depressed", "down", "miserable", "upset", "crying",
            "terrible", "awful", "horrible", "gloomy", "heartbroken", "lonely",
            "disappointed", "hopeless", "blue", "low"
        };

        private static readonly List<string> AngryKeywords = new()
        {
            "angry", "mad", "furious", "annoyed", "frustrated", "rage", "hate",
            "irritated", "livid", "outraged", "infuriated", "enraged", "fuming",
            "irritating", "aggravated", "disgusted"
        };

        private static readonly List<string> StressedKeywords = new()
        {
            "stressed", "overwhelmed", "anxious", "worried", "nervous", "panic",
            "pressure", "tense", "uneasy", "apprehensive", "distressed", "frantic",
            "swamped", "overloaded", "burnt out", "burnout"
        };

        private static readonly List<string> TiredKeywords = new()
        {
            "tired", "exhausted", "sleepy", "fatigued", "drained", "weary",
            "worn out", "drowsy", "groggy", "lethargic", "sluggish", "burned out"
        };

        private static readonly Dictionary<string, List<string>> SentimentResponses = new()
        {
            ["happy"] = new List<string>
            {
                "That's wonderful to hear! 😊 A positive mindset is actually your first line of defence in cybersecurity — happy people are more alert! Let's keep the good vibes going.",
                "Love the energy! 🎉 Good things happen when you're in a great headspace. Ready to level up your cybersecurity knowledge?",
                "So glad you're feeling good! 😄 Let's channel that positivity into making your digital life safer too!"
            },
            ["sad"] = new List<string>
            {
                "I'm sorry to hear you're not feeling great. 💙 I'm here for you — and did you know that cybercriminals often target people when they're emotionally vulnerable? Let me help keep you safe.",
                "That sounds tough. 😔 Take a breath — I'm here to help. Let's focus on something productive: learning how to stay safe online.",
                "I hear you, and I'm sorry you're going through that. 💙 Remember, learning new things can be a great distraction — what cybersecurity topic interests you?"
            },
            ["angry"] = new List<string>
            {
                "I understand your frustration! 😤 Anger is valid — and cybercriminals count on people making rash decisions when upset. Let's take a breath and stay sharp together.",
                "Take it easy! 🔥 Social engineers actually exploit angry emotions to get people to act without thinking. You're smarter than that — I've got your back.",
                "I hear your frustration. 💢 Let's redirect that energy into something useful — protecting yourself online!"
            },
            ["stressed"] = new List<string>
            {
                "I can tell you're under pressure. 😰 Stressed users are a top target for phishing attacks because stress impairs decision-making. Let me help you slow down and stay alert.",
                "Take a deep breath. 🌬 Cybercriminals love creating urgency to stress you out — now you know their trick. I've got your back.",
                "You're not alone! 😟 Stress and cybersecurity go hand-in-hand. Slowing down before clicking anything suspicious is your superpower right now."
            },
            ["tired"] = new List<string>
            {
                "Get some rest when you can! 😴 Fatigue is a major risk factor for falling for scams — tired brains miss red flags. I'll keep watch while you recharge.",
                "Being tired can make you more susceptible to cyber tricks. ☕ Take breaks, stay hydrated, and never make important security decisions when exhausted!",
                "I hear you — rest is important! 💤 In the meantime, let me give you quick, easy cybersecurity tips that don't require much brainpower right now."
            }
        };

        private static readonly Random _rand = new();

        public string DetectSentiment(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "neutral";

            var lower = input.ToLower();

            if (ContainsAny(lower, HappyKeywords)) return "happy";
            if (ContainsAny(lower, AngryKeywords)) return "angry";
            if (ContainsAny(lower, StressedKeywords)) return "stressed";
            if (ContainsAny(lower, TiredKeywords)) return "tired";
            if (ContainsAny(lower, SadKeywords)) return "sad";

            return "neutral";
        }

        public string GetSentimentResponse(string sentiment)
        {
            if (sentiment == "neutral") return string.Empty;

            if (SentimentResponses.TryGetValue(sentiment, out var responses) && responses.Count > 0)
                return responses[_rand.Next(responses.Count)];

            return string.Empty;
        }

        public string SentimentToEmoji(string sentiment) => sentiment switch
        {
            "happy" => "😊 Happy",
            "sad" => "😔 Sad",
            "angry" => "😤 Angry",
            "stressed" => "😰 Stressed",
            "tired" => "😴 Tired",
            _ => "😐 Neutral"
        };

        private static bool ContainsAny(string text, List<string> keywords)
        {
            foreach (var kw in keywords)
                if (text.Contains(kw, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}