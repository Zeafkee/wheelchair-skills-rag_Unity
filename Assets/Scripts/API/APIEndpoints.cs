using UnityEngine;

namespace WheelchairSkills.API
{
    /// <summary>
    /// API endpoint definitions for RAG backend communication
    /// </summary>
    public static class APIEndpoints
    {
        // Base URL for the backend API
        public const string BASE_URL = "http://localhost:8000";
        
        // User endpoints
        public const string CREATE_USER = "/api/users";
        public const string GET_USER = "/api/users/{0}";
        public const string GET_USER_PROGRESS = "/api/users/{0}/progress";
        
        // Skill Attempt endpoints
        public const string START_SKILL_ATTEMPT = "/api/skill-attempts/start";
        public const string RECORD_INPUT = "/api/skill-attempts/{0}/input";
        public const string COMPLETE_SKILL_ATTEMPT = "/api/skill-attempts/{0}/complete";
        public const string GET_SKILL_ATTEMPT = "/api/skill-attempts/{0}";
        
        // RAG endpoints
        public const string GET_TRAINING_PLAN = "/api/rag/training-plan";
        public const string GET_SKILL_GUIDANCE = "/api/rag/guidance";
        public const string ANALYZE_PERFORMANCE = "/api/rag/analyze";
        
        /// <summary>
        /// Helper method to format endpoint URLs with parameters
        /// </summary>
        public static string Format(string endpoint, params object[] args)
        {
            return BASE_URL + string.Format(endpoint, args);
        }
    }
}
