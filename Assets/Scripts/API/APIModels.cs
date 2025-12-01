using System;
using System.Collections.Generic;
using UnityEngine;

namespace WheelchairSkills.API
{
    /// <summary>
    /// Data models for API communication, serializable with Unity's JsonUtility
    /// </summary>

    [Serializable]
    public class CreateUserRequest
    {
        public string username;
        public string email;
    }

    [Serializable]
    public class UserResponse
    {
        public string user_id;
        public string username;
        public string email;
        public string created_at;
    }

    [Serializable]
    public class StartAttemptRequest
    {
        public string user_id;
        public string skill_name;
    }

    [Serializable]
    public class StartAttemptResponse
    {
        public string attempt_id;
        public string skill_name;
        public StepData[] steps;
        public string started_at;
    }

    [Serializable]
    public class StepData
    {
        public int step_number;
        public string description;
        public string[] required_actions;
    }

    [Serializable]
    public class RecordInputRequest
    {
        public string action;
        public string action_description;
        public float timestamp;
        public int current_step;
    }

    [Serializable]
    public class RecordInputResponse
    {
        public bool success;
        public string feedback;
        public bool step_completed;
        public int next_step;
    }

    [Serializable]
    public class CompleteAttemptRequest
    {
        public bool success;
        public float completion_time;
        public int steps_completed;
        public int errors_count;
    }

    [Serializable]
    public class CompleteAttemptResponse
    {
        public bool success;
        public string message;
        public PerformanceData performance;
    }

    [Serializable]
    public class PerformanceData
    {
        public float completion_time;
        public float accuracy;
        public int errors_count;
        public string feedback;
    }

    [Serializable]
    public class SkillAttemptResponse
    {
        public string attempt_id;
        public string user_id;
        public string skill_name;
        public string status;
        public StepData[] steps;
        public InputRecord[] inputs;
        public string started_at;
        public string completed_at;
    }

    [Serializable]
    public class InputRecord
    {
        public string action;
        public string action_description;
        public float timestamp;
        public int step_number;
    }

    [Serializable]
    public class UserProgress
    {
        public string user_id;
        public SkillProgress[] skills;
        public float overall_progress;
    }

    [Serializable]
    public class SkillProgress
    {
        public string skill_name;
        public int attempts_count;
        public int successful_attempts;
        public float average_completion_time;
        public float best_accuracy;
        public string last_attempt_date;
    }

    [Serializable]
    public class TrainingPlanRequest
    {
        public string user_id;
    }

    [Serializable]
    public class TrainingPlan
    {
        public string user_id;
        public RecommendedSkill[] recommended_skills;
        public string reasoning;
    }

    [Serializable]
    public class RecommendedSkill
    {
        public string skill_name;
        public string difficulty;
        public string reason;
        public int priority;
    }

    [Serializable]
    public class SkillGuidanceRequest
    {
        public string skill_name;
        public string user_context;
    }

    [Serializable]
    public class SkillGuidanceResponse
    {
        public string skill_name;
        public string[] guidance_steps;
        public string[] tips;
        public string[] common_mistakes;
    }

    [Serializable]
    public class AnalyzePerformanceRequest
    {
        public string attempt_id;
    }

    [Serializable]
    public class AnalyzePerformanceResponse
    {
        public string attempt_id;
        public string analysis;
        public string[] strengths;
        public string[] areas_for_improvement;
        public string[] recommendations;
    }

    [Serializable]
    public class ErrorResponse
    {
        public string error;
        public string message;
        public string details;
    }

    // Wrapper classes for array serialization with JsonUtility
    [Serializable]
    public class StepDataArray
    {
        public StepData[] steps;
    }

    [Serializable]
    public class RecommendedSkillArray
    {
        public RecommendedSkill[] skills;
    }
}
