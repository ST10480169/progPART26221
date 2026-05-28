using System;
using System.Collections.Generic;
using System.Linq;

namespace GraceAI
{
    public class Chatbot
    {
        public MemoryManager Memory { get; } = new MemoryManager();
        public SentimentAnalyzer Sentiment { get; } = new SentimentAnalyzer();

        public bool IsNameKnown => !string.IsNullOrEmpty(Memory.UserName);
        private bool _awaitingName = true;

        private static readonly Dictionary<string, string> KeywordMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "phishing",       "phishing" },
            { "phish",          "phishing" },
            { "fake email",     "phishing" },
            { "spoofing",       "phishing" },
            { "smishing",       "phishing" },
            { "vishing",        "phishing" },
            { "spear phishing", "phishing" },

            { "password",       "password safety" },
            { "passwords",      "password safety" },
            { "passphrase",     "password safety" },
            { "credential",     "password safety" },
            { "credentials",    "password safety" },
            { "login",          "password safety" },

            { "two-factor",     "two-factor authentication" },
            { "2fa",            "two-factor authentication" },
            { "mfa",            "two-factor authentication" },
            { "authenticator",  "two-factor authentication" },
            { "otp",            "two-factor authentication" },
            { "one-time",       "two-factor authentication" },

            { "ransomware",     "ransomware" },
            { "ransom",         "ransomware" },
            { "wannacry",       "ransomware" },
            { "encrypt",        "ransomware" },

            { "social engineering", "social engineering" },
            { "social engineer",    "social engineering" },
            { "pretexting",         "social engineering" },
            { "baiting",            "social engineering" },
            { "tailgating",         "social engineering" },
            { "manipulation",       "social engineering" },

            { "browsing",       "safe browsing" },
            { "browser",        "safe browsing" },
            { "https",          "safe browsing" },
            { "vpn",            "safe browsing" },
            { "website",        "safe browsing" },
            { "internet",       "safe browsing" },
            { "url",            "safe browsing" },

            { "malware",        "malware" },
            { "virus",          "malware" },
            { "trojan",         "malware" },
            { "spyware",        "malware" },
            { "worm",           "malware" },
            { "rootkit",        "malware" },
            { "keylogger",      "malware" },
            { "adware",         "malware" },
            { "antivirus",      "malware" },

            { "cyber hygiene",  "cyber hygiene" },
            { "hygiene",        "cyber hygiene" },
            { "digital hygiene","cyber hygiene" },
            { "habits",         "cyber hygiene" },
            { "backup",         "cyber hygiene" },
            { "update",         "cyber hygiene" },
            { "patch",          "cyber hygiene" },

            { "scam",           "scams" },
            { "scams",          "scams" },
            { "fraud",          "scams" },
            { "swindle",        "scams" },
            { "419",            "scams" },
            { "advance fee",    "scams" },
            { "romance scam",   "scams" },

            { "privacy",        "privacy" },
            { "personal data",  "privacy" },
            { "popia",          "privacy" },
            { "gdpr",           "privacy" },
            { "data protection","privacy" },
            { "tracking",       "privacy" },
            { "surveillance",   "privacy" },
        };

        private static readonly HashSet<string> GreetingWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "hi", "hello", "hey", "howdy", "greetings", "good morning",
            "good afternoon", "good evening", "sup", "what's up", "yo"
        };

        private static readonly HashSet<string> FarewellWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "exit", "quit", "bye", "goodbye", "farewell", "close", "stop", "leave"
        };

        public ChatResult Process(string input)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(input))
                    return ChatResult.BotOnly("⚠ Please type something before sending. I'm here to help!", "warning");

                Memory.IncrementMessages();
                input = input.Trim();
                var lower = input.ToLower();

                if (FarewellWords.Any(f => lower == f || lower.StartsWith(f + " ")))
                    return ChatResult.Exit(BuildFarewellMessage());

                if (lower == "help" || lower == "?")
                    return ChatResult.BotOnly(Responses.HelpText, "info");

                if (lower == "clear")
                    return ChatResult.Special("clear");

                if (_awaitingName && !IsNameKnown)
                    return HandleNameCapture(input);

                if (lower.StartsWith("my name is "))
                {
                    var name = input.Substring(11).Trim();
                    if (!string.IsNullOrEmpty(name))
                    {
                        Memory.UserName = name;
                        Memory.Remember("name", name);
                        return ChatResult.BotOnly(
                            Responses.GetRandom(Responses.NameResponses(name)), "success");
                    }
                }

                if (lower.StartsWith("i feel ") || lower.StartsWith("i am feeling ") || lower.StartsWith("feeling "))
                {
                    var sentResult = HandleSentiment(input);
                    if (sentResult != null) return sentResult;
                }

                if (lower.Contains("how are you") || lower.Contains("you doing") || lower.Contains("you okay"))
                    return ChatResult.BotOnly(
                        $"I'm running at optimal efficiency, {NameOrFriend()}! 🤖 I don't have feelings, but I am fully charged and ready to boost your cybersecurity awareness. What would you like to explore?",
                        "bot");

                var sentiment = Sentiment.DetectSentiment(input);
                if (sentiment != "neutral")
                {
                    Memory.UserMood = Sentiment.SentimentToEmoji(sentiment);
                    var sentimentReply = Sentiment.GetSentimentResponse(sentiment);
                    if (!string.IsNullOrEmpty(sentimentReply))
                        return ChatResult.WithSentiment(sentimentReply, sentiment);
                }

                if (GreetingWords.Any(g => lower == g || lower.StartsWith(g + " ") || lower.StartsWith(g + ",")))
                    return ChatResult.BotOnly(BuildGreetingResponse(), "bot");

                var detectedTopic = DetectTopic(lower);
                if (detectedTopic != null)
                {
                    Memory.AddTopicToHistory(detectedTopic);
                    var topicResponse = GetTopicResponse(detectedTopic);
                    return ChatResult.Topic(topicResponse, detectedTopic);
                }

                if (lower.Contains("my name") || lower.Contains("who am i"))
                    return ChatResult.BotOnly(
                        IsNameKnown
                            ? $"Your name is {Memory.UserName}! 🧠 I remembered it from when you introduced yourself."
                            : "I don't know your name yet! Tell me by saying 'My name is [your name]'.",
                        "info");

                if (lower.Contains("favourite topic") || lower.Contains("favorite topic") || lower.Contains("last topic"))
                    return ChatResult.BotOnly(
                        !string.IsNullOrEmpty(Memory.FavouriteTopic)
                            ? $"The last topic you explored was **{Memory.FavouriteTopic}**. Would you like to continue with that, or try something new?"
                            : "You haven't explored any topics yet! Check the left panel for available topics.",
                        "info");

                if (lower.Contains("what do you know about me") || lower.Contains("what do you remember"))
                    return ChatResult.BotOnly(BuildMemoryRecall(), "info");

                return ChatResult.BotOnly(Responses.GetRandomDefault(), "warning");
            }
            catch (Exception ex)
            {
                return ChatResult.BotOnly(
                    $"⚠ Something unexpected happened on my end. Please try again! (Error: {ex.Message})",
                    "error");
            }
        }

        public ChatResult ProcessTopic(string topic)
        {
            try
            {
                Memory.IncrementMessages();
                Memory.AddTopicToHistory(topic);
                return ChatResult.Topic(GetTopicResponse(topic), topic);
            }
            catch (Exception ex)
            {
                return ChatResult.BotOnly($"⚠ Could not load topic. {ex.Message}", "error");
            }
        }

        private ChatResult HandleNameCapture(string input)
        {
            if (input.Length < 2 || input.Length > 50 || input.All(char.IsDigit))
                return ChatResult.BotOnly(
                    "⚠ That doesn't look like a name. Please enter your first name so I can address you properly!",
                    "warning");

            var name = char.ToUpper(input[0]) + input.Substring(1).ToLower();
            Memory.UserName = name;
            Memory.Remember("name", name);
            _awaitingName = false;

            return ChatResult.BotOnly(
                Responses.GetRandom(Responses.NameResponses(name)) +
                "\n\n💡 Tip: Type 'help' to see everything I can do, or click a topic on the left!",
                "success");
        }

        private ChatResult? HandleSentiment(string input)
        {
            var sentiment = Sentiment.DetectSentiment(input);
            if (sentiment == "neutral") return null;

            Memory.UserMood = Sentiment.SentimentToEmoji(sentiment);
            var reply = Sentiment.GetSentimentResponse(sentiment);
            return string.IsNullOrEmpty(reply) ? null : ChatResult.WithSentiment(reply, sentiment);
        }

        private string DetectTopic(string lower)
        {
            string[] directTopics = {
                "phishing", "password safety", "two-factor authentication",
                "ransomware", "social engineering", "safe browsing",
                "malware", "cyber hygiene", "scams", "privacy"
            };
            foreach (var t in directTopics)
                if (lower.Contains(t)) return t;

            var orderedKeywords = KeywordMap.Keys.OrderByDescending(k => k.Length);
            foreach (var kw in orderedKeywords)
                if (lower.Contains(kw)) return KeywordMap[kw];

            return null!;
        }

        private string GetTopicResponse(string topic) => topic.ToLower() switch
        {
            "phishing" => Responses.GetRandom(Responses.PhishingResponses),
            "password safety" => Responses.GetRandom(Responses.PasswordSafetyResponses),
            "two-factor authentication" => Responses.GetRandom(Responses.TwoFactorResponses),
            "ransomware" => Responses.GetRandom(Responses.RansomwareResponses),
            "social engineering" => Responses.GetRandom(Responses.SocialEngineeringResponses),
            "safe browsing" => Responses.GetRandom(Responses.SafeBrowsingResponses),
            "malware" => Responses.GetRandom(Responses.MalwareResponses),
            "cyber hygiene" => Responses.GetRandom(Responses.CyberHygieneResponses),
            "scams" => Responses.GetRandom(Responses.ScamResponses),
            "privacy" => Responses.GetRandom(Responses.PrivacyResponses),
            _ => Responses.GetRandomDefault()
        };

        private string BuildGreetingResponse()
        {
            if (IsNameKnown)
                return $"Hey {Memory.UserName}! 👋 Great to see you again. What cybersecurity topic can I help you with?";
            return Responses.GetRandom(Responses.GreetingResponses);
        }

        private string BuildFarewellMessage()
        {
            var name = IsNameKnown ? $", {Memory.UserName}" : string.Empty;
            return $"Goodbye{name}! 🛡 Stay safe online — remember:\n\n" +
                   "• Use strong, unique passwords\n" +
                   "• Enable 2FA everywhere\n" +
                   "• Think before you click!\n\n" +
                   $"You explored {Memory.TopicsExplored} topic(s) this session. Come back anytime!";
        }

        private string BuildMemoryRecall()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"🧠 Here's what I remember about you:\n");
            sb.AppendLine($"• Name: {(IsNameKnown ? Memory.UserName : "Not told yet")}");
            sb.AppendLine($"• Mood: {(string.IsNullOrEmpty(Memory.UserMood) ? "Not shared yet" : Memory.UserMood)}");
            sb.AppendLine($"• Favourite topic: {(string.IsNullOrEmpty(Memory.FavouriteTopic) ? "None explored yet" : Memory.FavouriteTopic)}");
            sb.AppendLine($"• Topics explored: {Memory.TopicsExplored}");
            sb.AppendLine($"• Messages sent: {Memory.MessageCount}");
            if (Memory.TopicHistory.Count > 0)
            {
                sb.AppendLine($"\nTopics covered this session:");
                foreach (var t in Memory.TopicHistory)
                    sb.AppendLine($"  ✅ {t}");
            }
            return sb.ToString().TrimEnd();
        }

        private string NameOrFriend() => IsNameKnown ? Memory.UserName : "friend";

        public void ResetSession()
        {
            Memory.Reset();
            _awaitingName = true;
        }
    }

    public class ChatResult
    {
        public string BotMessage { get; set; } = string.Empty;
        public string MessageType { get; set; } = "bot";
        public string DetectedTopic { get; set; } = string.Empty;
        public string DetectedSentiment { get; set; } = string.Empty;
        public bool ShouldExit { get; set; } = false;
        public bool IsSpecialCommand { get; set; } = false;
        public string SpecialCommand { get; set; } = string.Empty;

        public static ChatResult BotOnly(string msg, string type = "bot") =>
            new() { BotMessage = msg, MessageType = type };

        public static ChatResult Topic(string msg, string topic) =>
            new() { BotMessage = msg, MessageType = "topic", DetectedTopic = topic };

        public static ChatResult WithSentiment(string msg, string sentiment) =>
            new() { BotMessage = msg, MessageType = "sentiment", DetectedSentiment = sentiment };

        public static ChatResult Exit(string msg) =>
            new() { BotMessage = msg, MessageType = "farewell", ShouldExit = true };

        public static ChatResult Special(string command) =>
            new() { IsSpecialCommand = true, SpecialCommand = command };
    }
}