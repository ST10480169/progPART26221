using System.Collections.Generic;

namespace GraceAI
{
    public class MemoryManager
    {
        public string UserName { get; set; } = string.Empty;
        public string UserMood { get; set; } = string.Empty;
        public string FavouriteTopic { get; set; } = string.Empty;
        public int MessageCount { get; set; } = 0;
        public int TopicsExplored { get; set; } = 0;

        private readonly Dictionary<string, string> _memory = new();
        private readonly List<string> _topicHistory = new();

        public void Remember(string key, string value)
        {
            _memory[key.ToLower()] = value;
        }

        public string Recall(string key)
        {
            return _memory.TryGetValue(key.ToLower(), out var value)
                ? value
                : string.Empty;
        }

        public bool Has(string key)
        {
            return _memory.ContainsKey(key.ToLower());
        }

        public bool IsNameKnown()
        {
            return !string.IsNullOrWhiteSpace(UserName);
        }

        public void AddTopicToHistory(string topic)
        {
            var t = topic.ToLower().Trim();

            if (!_topicHistory.Contains(t))
            {
                _topicHistory.Add(t);
                TopicsExplored = _topicHistory.Count;
                FavouriteTopic = topic;
            }

            Remember("last_topic", topic);
        }

        public IReadOnlyList<string> TopicHistory => _topicHistory.AsReadOnly();

        public void IncrementMessages()
        {
            MessageCount++;
        }

        public string BuildPersonalisedGreeting()
        {
            if (!string.IsNullOrEmpty(UserName) &&
                !string.IsNullOrEmpty(FavouriteTopic))
            {
                return $"Welcome back, {UserName}! Last time you were interested in {FavouriteTopic}. Want to continue from there?";
            }

            if (!string.IsNullOrEmpty(UserName))
            {
                return $"Hello again, {UserName}! What cybersecurity topic can I help you with today?";
            }

            return "Hello! I'm GRACE — your Cybersecurity Awareness Bot. What's your name?";
        }

        public void Reset()
        {
            UserName = string.Empty;
            UserMood = string.Empty;
            FavouriteTopic = string.Empty;
            MessageCount = 0;
            TopicsExplored = 0;

            _memory.Clear();
            _topicHistory.Clear();
        }

        public int AwarenessScore
        {
            get
            {
                int score = 0;

                score += System.Math.Min(TopicsExplored * 10, 50);
                score += System.Math.Min(MessageCount * 2, 30);

                if (!string.IsNullOrEmpty(UserName))
                    score += 10;

                if (!string.IsNullOrEmpty(UserMood))
                    score += 10;

                return System.Math.Min(score, 100);
            }
        }
    }
}