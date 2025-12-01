using UnityEngine;

namespace WheelchairSkills.API
{
    /// <summary>
    /// Backend URL ve endpoint tanımları
    /// </summary>
    public static class APIEndpoints
    {
        // Varsayılan backend URL
        public static string BaseURL = "http://localhost:8000";

        // Skill endpoints
        public static string GetSkillsEndpoint => $"{BaseURL}/skills";
        public static string GetSkillByIdEndpoint(string skillId) => $"{BaseURL}/skills/{skillId}";

        // Attempt endpoints
        public static string StartAttemptEndpoint => $"{BaseURL}/attempts/start";
        public static string RecordInputEndpoint => $"{BaseURL}/attempts/record_input";
        public static string EndAttemptEndpoint => $"{BaseURL}/attempts/end";
        public static string GetAttemptFeedbackEndpoint(string attemptId) => $"{BaseURL}/attempts/{attemptId}/feedback";

        // RAG endpoints
        public static string GetDynamicHintEndpoint => $"{BaseURL}/rag/hint";
        public static string GetContextualHelpEndpoint => $"{BaseURL}/rag/help";
    }
}
