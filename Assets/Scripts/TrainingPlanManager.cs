using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class TrainingPlanManager : MonoBehaviour
{
    [Header("Backend Settings")]
    public string backendBaseUrl = "http://localhost:8000";
    public string userId = "sefa001";

    [Header("UI References - Buttons")]
    public Button generatePlanButton;
    public Button viewGlobalStatsButton;
    public Button clearProgressButton;
    public Button closePanelButton;

    [Header("UI References - Panel")]
    public GameObject trainingPlanPanel;
    public TextMeshProUGUI panelTitleText;
    public TextMeshProUGUI planContentText;

    [Header("UI References - Loading")]
    public GameObject loadingIndicator;

    private void Start()
    {
        SetupEventListeners();
        
        // Hide panel initially
        if (trainingPlanPanel != null)
        {
            trainingPlanPanel.SetActive(false);
        }
        
        // Hide loading indicator initially
        if (loadingIndicator != null)
        {
            loadingIndicator.SetActive(false);
        }
    }

    private void SetupEventListeners()
    {
        if (generatePlanButton != null)
        {
            generatePlanButton.onClick.AddListener(OnGeneratePlanClicked);
        }

        if (viewGlobalStatsButton != null)
        {
            viewGlobalStatsButton.onClick.AddListener(OnViewGlobalStatsClicked);
        }

        if (clearProgressButton != null)
        {
            clearProgressButton.onClick.AddListener(OnClearProgressClicked);
        }

        if (closePanelButton != null)
        {
            closePanelButton.onClick.AddListener(OnClosePanelClicked);
        }
    }

    private void OnGeneratePlanClicked()
    {
        StartCoroutine(GenerateTrainingPlan());
    }

    private void OnViewGlobalStatsClicked()
    {
        StartCoroutine(ViewGlobalStats());
    }

    private void OnClearProgressClicked()
    {
        StartCoroutine(ClearProgress());
    }

    private void OnClosePanelClicked()
    {
        if (trainingPlanPanel != null)
        {
            trainingPlanPanel.SetActive(false);
        }
    }

    private IEnumerator GenerateTrainingPlan()
    {
        ShowLoading(true);
        
        string url = backendBaseUrl + "/user/" + userId + "/generate-plan";
        
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes("{}"));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            yield return request.SendWebRequest();
            
            ShowLoading(false);
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    TrainingPlanResponse response = JsonUtility.FromJson<TrainingPlanResponse>(request.downloadHandler.text);
                    DisplayTrainingPlan(response);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to parse training plan response: {e.Message}");
                    ShowError("Failed to parse training plan data.");
                }
            }
            else
            {
                Debug.LogError($"Failed to generate training plan: {request.error}");
                ShowError($"Failed to generate training plan: {request.error}");
            }
        }
    }

    private IEnumerator ViewGlobalStats()
    {
        ShowLoading(true);
        
        string url = backendBaseUrl + "/analytics/global-errors";
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            
            ShowLoading(false);
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    GlobalErrorStats response = JsonUtility.FromJson<GlobalErrorStats>(request.downloadHandler.text);
                    DisplayGlobalStats(response);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to parse global stats response: {e.Message}");
                    ShowError("Failed to parse global stats data.");
                }
            }
            else
            {
                Debug.LogError($"Failed to view global stats: {request.error}");
                ShowError($"Failed to view global stats: {request.error}");
            }
        }
    }

    private IEnumerator ClearProgress()
    {
        ShowLoading(true);
        
        string url = backendBaseUrl + "/user/" + userId + "/clear-progress";
        
        using (UnityWebRequest request = UnityWebRequest.Delete(url))
        {
            yield return request.SendWebRequest();
            
            ShowLoading(false);
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                ShowMessage("Progress Cleared", "Your training progress has been cleared successfully.");
            }
            else
            {
                Debug.LogError($"Failed to clear progress: {request.error}");
                ShowError($"Failed to clear progress: {request.error}");
            }
        }
    }

    private void DisplayTrainingPlan(TrainingPlanResponse plan)
    {
        if (trainingPlanPanel != null)
        {
            trainingPlanPanel.SetActive(true);
        }

        if (panelTitleText != null)
        {
            panelTitleText.text = "Training Plan";
        }

        StringBuilder content = new StringBuilder();
        
        // Header
        content.AppendLine("═══════════════════════════════════════");
        content.AppendLine($"  👤 User: {plan.user_id}");
        content.AppendLine($"  📊 Phase: {plan.current_phase}");
        content.AppendLine($"  🕐 Generated: {FormatTimestamp(plan.generated_at)}");
        content.AppendLine("═══════════════════════════════════════");
        content.AppendLine();

        // Recommended Skills
        if (plan.recommended_skills != null && plan.recommended_skills.Length > 0)
        {
            content.AppendLine("🎯 RECOMMENDED SKILLS TO PRACTICE");
            content.AppendLine("───────────────────────────────────────");
            foreach (var skill in plan.recommended_skills)
            {
                content.AppendLine($"  🔴 {skill.skill_name}");
                content.AppendLine($"      Reason: {skill.reason}");
                content.AppendLine($"      Attempts: {skill.attempts} | Success Rate: {skill.success_rate:P0}");
                content.AppendLine();
            }
        }

        // Focus Skills
        if (plan.focus_skills != null && plan.focus_skills.Length > 0)
        {
            content.AppendLine("⚠️ SKILLS NEEDING IMPROVEMENT");
            content.AppendLine("───────────────────────────────────────");
            foreach (var skill in plan.focus_skills)
            {
                content.AppendLine($"  ❌ {skill.skill_id}");
                content.AppendLine($"      Total Errors: {skill.total_errors}");
                
                if (skill.error_types != null && skill.error_types.Length > 0)
                {
                    content.AppendLine($"      Error Types: {string.Join(", ", skill.error_types)}");
                }
                content.AppendLine();
            }
        }

        // Common Errors
        if (plan.your_common_errors != null && plan.your_common_errors.Length > 0)
        {
            content.AppendLine("🔍 YOUR COMMON MISTAKES");
            content.AppendLine("───────────────────────────────────────");
            foreach (var error in plan.your_common_errors)
            {
                content.AppendLine($"  • Step {error.step_number} in {error.skill_id}");
                content.AppendLine($"    Expected: {error.expected_action} → You did: {error.actual_action}");
                content.AppendLine($"    Occurrences: {error.occurrences}x");
                content.AppendLine();
            }
        }

        // Skill Comparisons
        if (plan.skill_comparisons != null && plan.skill_comparisons.Length > 0)
        {
            content.AppendLine("📈 YOUR PERFORMANCE VS OTHERS");
            content.AppendLine("───────────────────────────────────────");
            foreach (var comparison in plan.skill_comparisons)
            {
                string indicator = comparison.your_success_rate >= comparison.global_avg_success_rate ? "⬆️" : "⬇️";
                string performance = comparison.your_success_rate >= comparison.global_avg_success_rate ? "Above Average" : "Below Average";
                
                content.AppendLine($"  {indicator} {comparison.skill_id}");
                content.AppendLine($"      You: {comparison.your_success_rate:P0} | Global: {comparison.global_avg_success_rate:P0} ({performance})");
                content.AppendLine();
            }
        }

        // Session Goals
        if (plan.session_goals != null && plan.session_goals.Length > 0)
        {
            content.AppendLine("🎯 SESSION GOALS");
            content.AppendLine("───────────────────────────────────────");
            foreach (var goal in plan.session_goals)
            {
                content.AppendLine($"  • {goal}");
            }
            content.AppendLine();
        }

        // Notes
        if (plan.notes != null && plan.notes.Length > 0)
        {
            content.AppendLine("📝 NOTES");
            content.AppendLine("───────────────────────────────────────");
            foreach (var note in plan.notes)
            {
                content.AppendLine($"  • {note}");
            }
        }

        if (planContentText != null)
        {
            planContentText.text = content.ToString();
        }
    }

    private void DisplayGlobalStats(GlobalErrorStats stats)
    {
        if (trainingPlanPanel != null)
        {
            trainingPlanPanel.SetActive(true);
        }

        if (panelTitleText != null)
        {
            panelTitleText.text = "Global Error Statistics";
        }

        StringBuilder content = new StringBuilder();
        
        // Header
        content.AppendLine("═══════════════════════════════════════");
        content.AppendLine($"  📊 Total Attempts: {stats.total_attempts}");
        content.AppendLine($"  👥 Total Users: {stats.total_users}");
        content.AppendLine("═══════════════════════════════════════");
        content.AppendLine();

        // Skill Summary
        if (stats.skill_summary != null && stats.skill_summary.Length > 0)
        {
            content.AppendLine("📋 SKILL SUMMARY");
            content.AppendLine("───────────────────────────────────────");
            foreach (var skill in stats.skill_summary)
            {
                float successRate = skill.total_attempts > 0 ? (float)skill.successful_attempts / skill.total_attempts : 0;
                content.AppendLine($"  • {skill.skill_id}");
                content.AppendLine($"    Attempts: {skill.total_attempts} | Success Rate: {successRate:P0}");
                content.AppendLine($"    Errors: {skill.total_errors}");
                content.AppendLine();
            }
        }

        // Problematic Steps
        if (stats.problematic_steps != null && stats.problematic_steps.Length > 0)
        {
            content.AppendLine("⚠️ MOST PROBLEMATIC STEPS");
            content.AppendLine("───────────────────────────────────────");
            foreach (var step in stats.problematic_steps)
            {
                content.AppendLine($"  • Step {step.step_number} in {step.skill_id}");
                content.AppendLine($"    Error Count: {step.error_count}");
                
                if (step.common_error_types != null && step.common_error_types.Length > 0)
                {
                    content.AppendLine($"    Common Errors: {string.Join(", ", step.common_error_types)}");
                }
                content.AppendLine();
            }
        }

        // Action Confusion
        if (stats.action_confusion != null && stats.action_confusion.Length > 0)
        {
            content.AppendLine("🔀 COMMON ACTION CONFUSIONS");
            content.AppendLine("───────────────────────────────────────");
            foreach (var confusion in stats.action_confusion)
            {
                content.AppendLine($"  • Expected: {confusion.expected_action}");
                content.AppendLine($"    Often confused with: {confusion.actual_action}");
                content.AppendLine($"    Occurrences: {confusion.count}x");
                content.AppendLine();
            }
        }

        if (planContentText != null)
        {
            planContentText.text = content.ToString();
        }
    }

    #region Helper Methods

    private string FormatTimestamp(string timestamp)
    {
        try
        {
            // Try to parse ISO 8601 format
            DateTime dt = DateTime.Parse(timestamp);
            return dt.ToString("MMM dd, yyyy HH:mm");
        }
        catch
        {
            return timestamp;
        }
    }

    private string GetProgressBar(float value, int length = 10)
    {
        int filledCount = Mathf.RoundToInt(value * length);
        StringBuilder bar = new StringBuilder("[");
        
        for (int i = 0; i < length; i++)
        {
            bar.Append(i < filledCount ? "█" : "░");
        }
        
        bar.Append("]");
        return bar.ToString();
    }

    private void ShowLoading(bool show)
    {
        if (loadingIndicator != null)
        {
            loadingIndicator.SetActive(show);
        }
    }

    private void ShowError(string message)
    {
        ShowMessage("Error", message);
    }

    private void ShowMessage(string title, string message)
    {
        if (trainingPlanPanel != null)
        {
            trainingPlanPanel.SetActive(true);
        }

        if (panelTitleText != null)
        {
            panelTitleText.text = title;
        }

        if (planContentText != null)
        {
            planContentText.text = message;
        }
    }

    #endregion

    #region JSON Response Models

    [Serializable]
    public class TrainingPlanResponse
    {
        public string user_id;
        public string current_phase;
        public string generated_at;
        public RecommendedSkill[] recommended_skills;
        public FocusSkill[] focus_skills;
        public string[] session_goals;
        public string[] notes;
        public CommonError[] your_common_errors;
        public SkillComparison[] skill_comparisons;
    }

    [Serializable]
    public class RecommendedSkill
    {
        public string skill_name;
        public string skill_id;
        public string reason;
        public int attempts;
        public float success_rate;
        public float priority_score;
    }

    [Serializable]
    public class FocusSkill
    {
        public string skill_id;
        public int total_errors;
        public string[] error_types;
    }

    [Serializable]
    public class CommonError
    {
        public string skill_id;
        public int step_number;
        public string expected_action;
        public string actual_action;
        public int occurrences;
    }

    [Serializable]
    public class SkillComparison
    {
        public string skill_id;
        public float your_success_rate;
        public float global_avg_success_rate;
    }

    [Serializable]
    public class GlobalErrorStats
    {
        public int total_attempts;
        public int total_users;
        public SkillSummary[] skill_summary;
        public ProblematicStep[] problematic_steps;
        public ActionConfusion[] action_confusion;
    }

    [Serializable]
    public class SkillSummary
    {
        public string skill_id;
        public int total_attempts;
        public int successful_attempts;
        public int total_errors;
    }

    [Serializable]
    public class ProblematicStep
    {
        public string skill_id;
        public int step_number;
        public int error_count;
        public string[] common_error_types;
    }

    [Serializable]
    public class ActionConfusion
    {
        public string expected_action;
        public string actual_action;
        public int count;
    }

    #endregion

    private void OnDestroy()
    {
        // Clean up event listeners
        if (generatePlanButton != null)
        {
            generatePlanButton.onClick.RemoveListener(OnGeneratePlanClicked);
        }

        if (viewGlobalStatsButton != null)
        {
            viewGlobalStatsButton.onClick.RemoveListener(OnViewGlobalStatsClicked);
        }

        if (clearProgressButton != null)
        {
            clearProgressButton.onClick.RemoveListener(OnClearProgressClicked);
        }

        if (closePanelButton != null)
        {
            closePanelButton.onClick.RemoveListener(OnClosePanelClicked);
        }
    }
}
