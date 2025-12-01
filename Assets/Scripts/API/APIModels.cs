using System;
using System.Collections.Generic;
using UnityEngine;

namespace WheelchairSkills.API
{
    /// <summary>
    /// JSON veri modelleri
    /// </summary>

    [Serializable]
    public class Skill
    {
        public string id;
        public string name;
        public string description;
        public string difficulty;
        public List<string> required_actions;
    }

    [Serializable]
    public class SkillsResponse
    {
        public List<Skill> skills;
    }

    [Serializable]
    public class StartAttemptRequest
    {
        public string skill_id;
        public string user_id;
    }

    [Serializable]
    public class StartAttemptResponse
    {
        public string attempt_id;
        public string skill_id;
        public string status;
        public string timestamp;
    }

    [Serializable]
    public class RecordInputRequest
    {
        public string attempt_id;
        public string action;
        public float timestamp;
        public Dictionary<string, object> metadata;

        public RecordInputRequest()
        {
            metadata = new Dictionary<string, object>();
        }
    }

    [Serializable]
    public class RecordInputResponse
    {
        public string status;
        public string message;
    }

    [Serializable]
    public class EndAttemptRequest
    {
        public string attempt_id;
        public string status; // "completed", "failed", "abandoned"
        public float duration;
    }

    [Serializable]
    public class EndAttemptResponse
    {
        public string status;
        public string message;
        public AttemptSummary summary;
    }

    [Serializable]
    public class AttemptSummary
    {
        public string attempt_id;
        public int total_inputs;
        public float duration;
        public float success_rate;
    }

    [Serializable]
    public class GetFeedbackResponse
    {
        public string attempt_id;
        public List<FeedbackItem> feedback;
        public string overall_assessment;
        public List<string> recommendations;
    }

    [Serializable]
    public class FeedbackItem
    {
        public string timestamp;
        public string action;
        public string feedback_type; // "correct", "incorrect", "suggestion"
        public string message;
    }

    [Serializable]
    public class DynamicHintRequest
    {
        public string skill_id;
        public string current_state;
        public List<string> recent_actions;

        public DynamicHintRequest()
        {
            recent_actions = new List<string>();
        }
    }

    [Serializable]
    public class DynamicHintResponse
    {
        public string hint;
        public string context;
        public List<string> suggested_actions;
    }

    [Serializable]
    public class ContextualHelpRequest
    {
        public string skill_id;
        public string query;
    }

    [Serializable]
    public class ContextualHelpResponse
    {
        public string answer;
        public List<string> relevant_sections;
        public List<string> related_skills;
    }

    [Serializable]
    public class ErrorResponse
    {
        public string error;
        public string message;
        public string detail;
    }
}
