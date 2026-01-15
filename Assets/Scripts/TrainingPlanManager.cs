using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

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
    public Button generateReportButton; // new button to generate AI report

    [Header("UI References - Panel")]
    public GameObject trainingPlanPanel;
    public TextMeshProUGUI panelTitleText;
    public TextMeshProUGUI planContentText;

    [Header("UI References - Loading")]
    public GameObject loadingIndicator;

    // Caches for analytics and user progress (used to augment generate-plan output)
    private AnalyticsResponse analyticsCache = null;
    private string userProgressJsonCache = null;

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

        if (generateReportButton != null)
        {
            generateReportButton.onClick.AddListener(OnGenerateReportClicked);
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

    private void OnGenerateReportClicked()
    {
        StartCoroutine(SendFilesForReport()); // fallback local file-based report sender (keeps compatibility)
    }

    // ---------------------------
    // GenerateTrainingPlan flow
    // ---------------------------
    private IEnumerator GenerateTrainingPlan()
    {
        ShowLoading(true);

        // 1) Fetch analytics (fixed endpoint that contains success_rate)
        string analyticsUrl = backendBaseUrl + "/analytics/global-errors-fixed";
        using (UnityWebRequest analyticsReq = UnityWebRequest.Get(analyticsUrl))
        {
            yield return analyticsReq.SendWebRequest();
            if (analyticsReq.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    analyticsCache = JsonUtility.FromJson<AnalyticsResponse>(analyticsReq.downloadHandler.text);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Failed to parse analytics response: " + e.Message);
                    analyticsCache = null;
                }
            }
            else
            {
                Debug.LogWarning("Failed to fetch analytics: " + analyticsReq.error);
                analyticsCache = null;
            }
        }

        // 2) Fetch user progress (raw JSON cached and used with regex parser for quick extraction)
        string progressUrl = backendBaseUrl + "/user/" + userId + "/progress";
        using (UnityWebRequest progReq = UnityWebRequest.Get(progressUrl))
        {
            yield return progReq.SendWebRequest();
            if (progReq.result == UnityWebRequest.Result.Success)
            {
                userProgressJsonCache = progReq.downloadHandler.text;
            }
            else
            {
                Debug.LogWarning("Failed to fetch user progress: " + progReq.error);
                userProgressJsonCache = null;
            }
        }

        // 3) Call generate-plan POST
        string url = backendBaseUrl + "/user/" + userId + "/generate-plan";
        const string emptyJsonBody = "{}";

        TrainingPlanResponse parsedResponse = null;
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(emptyJsonBody));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            ShowLoading(false);

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    // Parse inside try/catch but do not yield here
                    parsedResponse = JsonUtility.FromJson<TrainingPlanResponse>(request.downloadHandler.text);
                    Debug.Log("TRAINING PLAN RAW: " + request.downloadHandler.text);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to parse training plan response: {e.Message}");
                    ShowError("Failed to parse training plan data.");
                    parsedResponse = null;
                }
            }
            else
            {
                Debug.LogError($"Failed to generate training plan: {request.error}");
                ShowError($"Failed to generate training plan: {request.error}");
                yield break;
            }
        }

        // If parsed successfully, perform UI update, save and request AI report
        if (parsedResponse != null)
        {
            // Display plan (existing UI)
            DisplayTrainingPlan(parsedResponse);

            // Save the displayed training plan text to file
            SaveDisplayedPlanToFile(parsedResponse);

            // Build texts and request AI report (English) from backend endpoint
            string trainingText = BuildTrainingPlanText(parsedResponse);
            string globalText = BuildGlobalStatsDisplayText();

            // Start model-generated report request (backend /analytics/generate-report)
            yield return StartCoroutine(RequestReport(trainingText, globalText));
        }
    }

    // ---------------------------
    // ViewGlobalStats
    // ---------------------------
    private IEnumerator ViewGlobalStats()
    {
        ShowLoading(true);

        string url = backendBaseUrl + "/analytics/global-errors-fixed";

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

                    // Save displayed global stats to file and cache analytics for comparisons
                    SaveDisplayedGlobalStatsToFile(request.downloadHandler.text);
                    try
                    {
                        analyticsCache = JsonUtility.FromJson<AnalyticsResponse>(request.downloadHandler.text);
                    }
                    catch { /* ignore */ }
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

    // ---------------------------
    // ClearProgress
    // ---------------------------
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

    // ---------------------------
    // DisplayTrainingPlan
    // ---------------------------
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
                content.AppendLine($"      Attempts: {skill.attempts} | Success Rate: {(skill.success_rate * 100):F0}%");

                // add comparison if analytics cached
                var cmp = GetComparisonForSkill(skill.skill_id);
                content.AppendLine($"      You: {cmp.userPercent}% | Global: {cmp.globalPercent}%");
                content.AppendLine();
            }
        }

        // Focus Skills
        if (plan.focus_skills != null && plan.focus_skills.Length > 0)
        {
            content.AppendLine("⚠️ SKILLS NEEDING IMPROVEMENT");
            content.AppendLine("────────────────────────────��──────────");
            foreach (var skill in plan.focus_skills)
            {
                content.AppendLine($"  ❌ {skill.skill_id}");
                content.AppendLine($"      Total Errors: {skill.total_errors}");

                if (skill.error_types != null && skill.error_types.Length > 0)
                {
                    content.AppendLine($"      Error Types: {string.Join(", ", skill.error_types)}");
                }

                // add comparison
                var cmp = GetComparisonForSkill(skill.skill_id);
                content.AppendLine($"      You: {cmp.userPercent}% | Global: {cmp.globalPercent}%");
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
                int occurrences = error.occurrences;
                // If occurrences missing or zero, try to derive from analytics cache
                if (occurrences == 0 && analyticsCache != null)
                {
                    occurrences = GetOccurrencesForSkillStep(error.skill_id, error.step_number);
                }

                content.AppendLine($"  • Step {error.step_number} in {error.skill_id}");
                content.AppendLine($"    Expected: {error.expected_action} → You did: {error.actual_action}");
                content.AppendLine($"    Occurrences: {occurrences}x");
                content.AppendLine();
            }
        }

        // Skill Comparisons (explicit list from plan + augment from analytics cache if needed)
        if (plan.skill_comparisons != null && plan.skill_comparisons.Length > 0)
        {
            content.AppendLine("📈 YOUR PERFORMANCE VS OTHERS");
            content.AppendLine("───────────────────────────────────────");
            foreach (var comparison in plan.skill_comparisons)
            {
                float yourRate = comparison.your_success_rate;
                float globalRate = comparison.global_avg_success_rate;

                // If globalRate is zero there might be missing backend field; try analytics cache
                if (globalRate == 0f && analyticsCache != null)
                {
                    var gs = FindGlobalSkill(comparison.skill_id);
                    if (gs != null)
                    {
                        if (gs.success_rate > 0f) globalRate = gs.success_rate;
                        else if (gs.failure_rate > 0f) globalRate = 1f - gs.failure_rate;
                        else if (gs.total_attempts > 0) globalRate = (float)(gs.total_attempts - gs.failed_attempts) / gs.total_attempts;
                    }
                }

                string indicator = yourRate >= globalRate ? "⬆" : "⬇";
                string performance = yourRate >= globalRate ? "Above Average" : "Below Average";

                content.AppendLine($"  {indicator} {comparison.skill_id}");
                content.AppendLine($"      You: {(yourRate * 100):F0}% | Global: {(globalRate * 100):F0}% ({performance})");
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

    // ---------------------------
    // DisplayGlobalStats
    // ---------------------------
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
                content.AppendLine($"    Attempts: {skill.total_attempts} | Success Rate: {(successRate * 100):F0}%");
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
                // action_confusion from backend uses fields 'expected' and 'actual'
                string expected = confusion.expected ?? confusion.expected_action;
                string actual = confusion.actual ?? confusion.actual_action;
                int count = confusion.count;
                content.AppendLine($"  • Expected: {expected}");
                content.AppendLine($"    Often confused with: {actual}");
                content.AppendLine($"    Occurrences: {count}x");
                content.AppendLine();
            }
        }

        string output = content.ToString();
        if (planContentText != null)
        {
            planContentText.text = output;
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

    #region Analytics & Comparison Helpers

    // Extract user's success_rate for a given skill from the raw user progress JSON
    private float GetUserSuccessRateFromProgressJson(string progressJson, string skillId)
    {
        if (string.IsNullOrEmpty(progressJson) || string.IsNullOrEmpty(skillId))
            return 0f;

        // Find the position of the skill key in the JSON text
        string key = $"\"{skillId}\"";
        int idx = progressJson.IndexOf(key, StringComparison.Ordinal);
        if (idx < 0)
            return 0f;

        int braceStart = progressJson.IndexOf('{', idx);
        if (braceStart < 0)
            return 0f;

        int windowEnd = Math.Min(progressJson.Length, braceStart + 2000);
        string window = progressJson.Substring(braceStart, windowEnd - braceStart);

        var m = Regex.Match(window, "\"success_rate\"\\s*:\\s*([0-9]*\\.?[0-9]+)", RegexOptions.Singleline);
        if (!m.Success)
            return 0f;

        if (float.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float val))
            return val;

        return 0f;
    }

    private SkillSummary FindGlobalSkill(string skillId)
    {
        if (analyticsCache == null || analyticsCache.skill_summary == null)
            return null;
        foreach (var s in analyticsCache.skill_summary)
        {
            if (string.Equals(s.skill_id, skillId, StringComparison.Ordinal))
                return s;
        }
        return null;
    }

    private int GetOccurrencesForSkillStep(string skillId, int stepNumber)
    {
        if (analyticsCache == null || analyticsCache.problematic_steps == null)
            return 0;
        foreach (var p in analyticsCache.problematic_steps)
        {
            if (string.Equals(p.skill_id, skillId, StringComparison.Ordinal) && p.step_number == stepNumber)
                return p.error_count;
        }
        return 0;
    }

    // Returns (userPercent, globalPercent, occurrences)
    private (int userPercent, int globalPercent, int occurrences) GetComparisonForSkill(string skillId)
    {
        int globalPercent = 0;
        var gs = FindGlobalSkill(skillId);
        if (gs != null)
        {
            float gRate = 0f;
            if (gs.success_rate > 0f) gRate = gs.success_rate;
            else if (gs.failure_rate > 0f) gRate = 1f - gs.failure_rate;
            else if (gs.total_attempts > 0) gRate = (float)(gs.total_attempts - gs.failed_attempts) / gs.total_attempts;
            globalPercent = Mathf.RoundToInt(gRate * 100f);
        }

        int userPercent = 0;
        if (!string.IsNullOrEmpty(userProgressJsonCache))
        {
            float urate = GetUserSuccessRateFromProgressJson(userProgressJsonCache, skillId);
            userPercent = Mathf.RoundToInt(urate * 100f);
        }

        int occurrences = 0;
        if (gs != null && !string.IsNullOrEmpty(gs.most_problematic_step) && int.TryParse(gs.most_problematic_step, out int stepNum))
        {
            occurrences = GetOccurrencesForSkillStep(skillId, stepNum);
        }

        return (userPercent, globalPercent, occurrences);
    }

    #endregion

    #region Save to File Helpers

    // Returns folder path to save files. Using the parent of dataPath ensures it saves to the project root (not inside Assets) in Editor and the app root in builds.
    private string GetSaveFolder()
    {
        return Path.GetDirectoryName(Application.dataPath);
    }

    private string SanitizeFilename(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }

    private void SaveTextToFile(string filename, string text)
    {
        try
        {
            string folder = GetSaveFolder();
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string fullPath = Path.Combine(folder, SanitizeFilename(filename));
            File.WriteAllText(fullPath, text, Encoding.UTF8);
            Debug.Log($"Saved file: {fullPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to save file: " + ex.Message);
        }
    }

    private void SaveDisplayedPlanToFile(TrainingPlanResponse plan)
    {
        // Rebuild the displayed text using the same logic as DisplayTrainingPlan
        StringBuilder content = new StringBuilder();
        content.AppendLine("═══════════════════════════════════════");
        content.AppendLine($"  👤 User: {plan.user_id}");
        content.AppendLine($"  📊 Phase: {plan.current_phase}");
        content.AppendLine($"  🕐 Generated: {FormatTimestamp(plan.generated_at)}");
        content.AppendLine("═══════════════════════════════════════");
        content.AppendLine();

        if (plan.recommended_skills != null && plan.recommended_skills.Length > 0)
        {
            content.AppendLine("🎯 RECOMMENDED SKILLS TO PRACTICE");
            content.AppendLine("───────────────────────────────────────");
            foreach (var skill in plan.recommended_skills)
            {
                content.AppendLine($"  🔴 {skill.skill_name}");
                content.AppendLine($"      Reason: {skill.reason}");
                content.AppendLine($"      Attempts: {skill.attempts} | Success Rate: {(skill.success_rate * 100):F0}%");
                var cmp = GetComparisonForSkill(skill.skill_id);
                content.AppendLine($"      You: {cmp.userPercent}% | Global: {cmp.globalPercent}%");
                content.AppendLine();
            }
        }

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
                var cmp = GetComparisonForSkill(skill.skill_id);
                content.AppendLine($"      You: {cmp.userPercent}% | Global: {cmp.globalPercent}%");
                content.AppendLine();
            }
        }

        if (plan.your_common_errors != null && plan.your_common_errors.Length > 0)
        {
            content.AppendLine("🔍 YOUR COMMON MISTAKES");
            content.AppendLine("───────────────────────────────────────");
            foreach (var error in plan.your_common_errors)
            {
                int occurrences = error.occurrences;
                if (occurrences == 0 && analyticsCache != null)
                {
                    occurrences = GetOccurrencesForSkillStep(error.skill_id, error.step_number);
                }
                content.AppendLine($"  • Step {error.step_number} in {error.skill_id}");
                content.AppendLine($"    Expected: {error.expected_action} → You did: {error.actual_action}");
                content.AppendLine($"    Occurrences: {occurrences}x");
                content.AppendLine();
            }
        }

        string filename = $"training_plan_{userId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt";
        SaveTextToFile(filename, content.ToString());
    }

    private void SaveDisplayedGlobalStatsToFile(string rawJson)
    {
        // Option A: save the raw JSON
        string rawFilename = $"global_stats_raw_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
        SaveTextToFile(rawFilename, rawJson);

        // Option B: also save the nicely formatted display text similar to DisplayGlobalStats
        if (analyticsCache != null)
        {
            StringBuilder content = new StringBuilder();

            content.AppendLine("═══════════════════════════════════════");
            content.AppendLine("  📊 Total Attempts: " + (analyticsCache != null ? analyticsCache.total_attempts.ToString() : "0"));
            content.AppendLine("  👥 Total Users: " + (analyticsCache != null ? analyticsCache.total_users.ToString() : "0"));
            content.AppendLine("═══════════════════════════════════════");
            content.AppendLine();

            if (analyticsCache.skill_summary != null && analyticsCache.skill_summary.Length > 0)
            {
                content.AppendLine("📋 SKILL SUMMARY");
                content.AppendLine("───────────────────────────────────────");
                foreach (var skill in analyticsCache.skill_summary)
                {
                    float successRate = skill.total_attempts > 0 ? skill.success_rate : 0f;
                    content.AppendLine("  • " + skill.skill_id);
                    content.AppendLine("    Attempts: " + skill.total_attempts + " | Success Rate: " + (successRate * 100).ToString("F0") + "%");
                    content.AppendLine("    Errors: " + skill.total_errors);
                    content.AppendLine();
                }
            }

            string filename = $"global_stats_display_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt";
            SaveTextToFile(filename, content.ToString());
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

    // Unified SkillSummary used for analytics and display
    [Serializable]
    public class SkillSummary
    {
        public string skill_id;
        public int total_attempts;
        public int successful_attempts;
        public int failed_attempts;
        public float success_rate;
        public float failure_rate;
        public int total_errors;
        public string most_problematic_step;
    }

    [Serializable]
    public class ProblematicStep
    {
        public string skill_id;
        public int step_number;
        public int error_count;
        public string most_common_error;
        // legacy field for UI model compatibility
        public string[] common_error_types;
    }

    [Serializable]
    public class ActionConfusion
    {
        // backend may return "expected"/"actual" or "expected_action"/"actual_action"
        public string expected;
        public string actual;
        public string expected_action;
        public string actual_action;
        public int count;
        public string description;
    }
    [Serializable]
    public class AnalyticsResponse
    {
        public int total_attempts;
        public int total_users;
        public SkillSummary[] skill_summary;
        public ProblematicStep[] problematic_steps;
        public ActionConfusion[] action_confusion;
        public string generated_at;
    }

    [Serializable]
    private class ReportResponse
    {
        public string report;
    }

    #endregion

    #region Report generation helpers (Unity -> backend)

    // Build the same display text for the training plan and return it
    private string BuildTrainingPlanText(TrainingPlanResponse plan)
    {
        var content = new StringBuilder();
        content.AppendLine("═══════════════════════════════════════");
        content.AppendLine($"  👤 User: {plan.user_id}");
        content.AppendLine($"  📊 Phase: {plan.current_phase}");
        content.AppendLine($"  🕐 Generated: {FormatTimestamp(plan.generated_at)}");
        content.AppendLine("═══════════════════════════════════════");
        content.AppendLine();

        if (plan.recommended_skills != null && plan.recommended_skills.Length > 0)
        {
            content.AppendLine("🎯 RECOMMENDED SKILLS TO PRACTICE");
            content.AppendLine("───────────────────────────────────────");
            foreach (var skill in plan.recommended_skills)
            {
                content.AppendLine($"  🔴 {skill.skill_name}");
                content.AppendLine($"      Reason: {skill.reason}");
                content.AppendLine($"      Attempts: {skill.attempts} | Success Rate: {(skill.success_rate * 100):F0}%");
                var cmp = GetComparisonForSkill(skill.skill_id);
                content.AppendLine($"      You: {cmp.userPercent}% | Global: {cmp.globalPercent}%");
                content.AppendLine();
            }
        }

        if (plan.focus_skills != null && plan.focus_skills.Length > 0)
        {
            content.AppendLine("⚠️ SKILLS NEEDING IMPROVEMENT");
            content.AppendLine("───────────────────────────────────────");
            foreach (var skill in plan.focus_skills)
            {
                content.AppendLine($"  ❌ {skill.skill_id}");
                content.AppendLine($"      Total Errors: {skill.total_errors}");
                if (skill.error_types != null && skill.error_types.Length > 0)
                    content.AppendLine($"      Error Types: {string.Join(", ", skill.error_types)}");
                var cmp = GetComparisonForSkill(skill.skill_id);
                content.AppendLine($"      You: {cmp.userPercent}% | Global: {cmp.globalPercent}%");
                content.AppendLine();
            }
        }

        if (plan.your_common_errors != null && plan.your_common_errors.Length > 0)
        {
            content.AppendLine("🔍 YOUR COMMON MISTAKES");
            content.AppendLine("───────────────────────────────────────");
            foreach (var error in plan.your_common_errors)
            {
                int occurrences = error.occurrences;
                if (occurrences == 0 && analyticsCache != null)
                {
                    occurrences = GetOccurrencesForSkillStep(error.skill_id, error.step_number);
                }

                content.AppendLine($"  • Step {error.step_number} in {error.skill_id}");
                content.AppendLine($"    Expected: {error.expected_action} → You did: {error.actual_action}");
                content.AppendLine($"    Occurrences: {occurrences}x");
                content.AppendLine();
            }
        }

        if (plan.skill_comparisons != null && plan.skill_comparisons.Length > 0)
        {
            content.AppendLine("📈 YOUR PERFORMANCE VS OTHERS");
            content.AppendLine("───────────────────────────────────────");
            foreach (var comparison in plan.skill_comparisons)
            {
                float yourRate = comparison.your_success_rate;
                float globalRate = comparison.global_avg_success_rate;
                if (globalRate == 0f && analyticsCache != null)
                {
                    var gs = FindGlobalSkill(comparison.skill_id);
                    if (gs != null)
                    {
                        if (gs.success_rate > 0f) globalRate = gs.success_rate;
                        else if (gs.failure_rate > 0f) globalRate = 1f - gs.failure_rate;
                        else if (gs.total_attempts > 0) globalRate = (float)(gs.total_attempts - gs.failed_attempts) / gs.total_attempts;
                    }
                }
                string indicator = yourRate >= globalRate ? "⬆" : "⬇";
                string performance = yourRate >= globalRate ? "Above Average" : "Below Average";
                content.AppendLine($"  {indicator} {comparison.skill_id}");
                content.AppendLine($"      You: {(yourRate * 100):F0}% | Global: {(globalRate * 100):F0}% ({performance})");
                content.AppendLine();
            }
        }

        if (plan.session_goals != null && plan.session_goals.Length > 0)
        {
            content.AppendLine("🎯 SESSION GOALS");
            content.AppendLine("───────────────────────────────────────");
            foreach (var goal in plan.session_goals)
                content.AppendLine($"  • {goal}");
            content.AppendLine();
        }

        if (plan.notes != null && plan.notes.Length > 0)
        {
            content.AppendLine("📝 NOTES");
            content.AppendLine("───────────────────────────────────────");
            foreach (var note in plan.notes)
                content.AppendLine($"  • {note}");
        }

        return content.ToString();
    }

    // Build a display-style global stats text from analyticsCache (used as global_stats_text)
    private string BuildGlobalStatsDisplayText()
    {
        if (analyticsCache == null)
        {
            return "";
        }

        var content = new StringBuilder();
        content.AppendLine("════════════════════════════��══════════");
        content.AppendLine($"  📊 Total Attempts: {analyticsCache.total_attempts}");
        content.AppendLine($"  👥 Total Users: {analyticsCache.total_users}");
        content.AppendLine("═══════════════════════════════════════");
        content.AppendLine();

        if (analyticsCache.skill_summary != null && analyticsCache.skill_summary.Length > 0)
        {
            content.AppendLine("📋 SKILL SUMMARY");
            content.AppendLine("───────────────────────────────────────");
            foreach (var skill in analyticsCache.skill_summary)
            {
                float successRate = skill.total_attempts > 0 ? skill.success_rate : 0f;
                content.AppendLine($"  • {skill.skill_id}");
                content.AppendLine($"    Attempts: {skill.total_attempts} | Success Rate: {(successRate * 100):F0}%");
                content.AppendLine($"    Errors: {skill.total_errors}");
                content.AppendLine();
            }
        }

        if (analyticsCache.problematic_steps != null && analyticsCache.problematic_steps.Length > 0)
        {
            content.AppendLine("⚠️ MOST PROBLEMATIC STEPS");
            content.AppendLine("───────────────────────────────────────");
            foreach (var step in analyticsCache.problematic_steps)
            {
                content.AppendLine($"  • Step {step.step_number} in {step.skill_id}");
                content.AppendLine($"    Error Count: {step.error_count}");
                content.AppendLine();
            }
        }

        return content.ToString();
    }

    // Coroutine: POST both texts to /analytics/generate-report and display/save the returned English report
    private IEnumerator RequestReport(string trainingText, string globalText)
    {
        ShowLoading(true);

        if (string.IsNullOrEmpty(trainingText) && string.IsNullOrEmpty(globalText))
        {
            ShowLoading(false);
            Debug.LogWarning("No training/global text to send for report.");
            yield break;
        }

        // Build JSON payload manually and escape strings
        string payloadJson = "{\"user_id\":\"" + EscapeJson(userId) + "\"";
        if (!string.IsNullOrEmpty(trainingText))
            payloadJson += ",\"training_plan_text\":\"" + EscapeJson(trainingText) + "\"";
        if (!string.IsNullOrEmpty(globalText))
            payloadJson += ",\"global_stats_text\":\"" + EscapeJson(globalText) + "\"";
        payloadJson += "}";

        string url = backendBaseUrl + "/analytics/generate-report";
        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(payloadJson);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            ShowLoading(false);

            // log raw response for debugging
            Debug.Log("Report endpoint raw response: " + req.downloadHandler.text);

            if (req.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var resp = JsonUtility.FromJson<ReportResponse>(req.downloadHandler.text);
                    string reportText = (resp != null && !string.IsNullOrEmpty(resp.report)) ? resp.report : null;

                    if (string.IsNullOrEmpty(reportText))
                    {
                        Debug.LogWarning("AI report empty despite 200 OK. Raw: " + req.downloadHandler.text);
                        ShowError("AI returned empty report.");
                        yield break;
                    }

                    // Append the English report to the currently displayed plan (or replace panel content)
                    if (planContentText != null)
                    {
                        planContentText.text = planContentText.text + "\n\n===== AI Report (EN) =====\n\n" + reportText;
                    }

                    // Save AI report to file
                    string filename = $"ai_report_{userId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt";
                    SaveTextToFile(filename, reportText);
                }
                catch (Exception e)
                {
                    Debug.LogError("Failed to parse report response: " + e.Message);
                    Debug.LogError("Raw body: " + req.downloadHandler.text);
                    ShowError("Failed to parse AI response.");
                }
            }
            else
            {
                Debug.LogError("Report generation failed: " + req.error + " - " + req.downloadHandler.text);
                ShowError("Report generation failed: " + req.error);
            }
        }
    }

    // Helper used by RequestReport and other payload building
    private string EscapeJson(string s)
    {
        if (s == null) return "";
        StringBuilder sb = new StringBuilder();
        foreach (char c in s)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '\"': sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (char.IsControl(c))
                        sb.Append("\\u" + ((int)c).ToString("x4"));
                    else
                        sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }

    #endregion

    #region Local file -> report helper (used by "generate report" button fallback)

    // Coroutine to find latest training_plan and global_stats files, read them and send to backend
    private IEnumerator SendFilesForReport()
    {
        ShowLoading(true);

        // find save folder (same function used earlier)
        string folder = GetSaveFolder();

        // find latest training_plan file
        string[] trainingFiles = Directory.GetFiles(folder, $"training_plan_{userId}_*.txt");
        string[] globalDisplayFiles = Directory.GetFiles(folder, "global_stats_display_*.txt");

        string trainingText = "";
        string globalText = "";

        if (trainingFiles.Length > 0)
        {
            Array.Sort(trainingFiles);
            string latestTraining = trainingFiles[trainingFiles.Length - 1];
            trainingText = File.ReadAllText(latestTraining, Encoding.UTF8);
            Debug.Log($"Found training file: {latestTraining} ({trainingText.Length} chars)");
        }
        else
        {
            Debug.LogWarning("No training_plan file found in " + folder);
        }

        if (globalDisplayFiles.Length > 0)
        {
            Array.Sort(globalDisplayFiles);
            string latestGlobal = globalDisplayFiles[globalDisplayFiles.Length - 1];
            globalText = File.ReadAllText(latestGlobal, Encoding.UTF8);
            Debug.Log($"Found global stats file: {latestGlobal} ({globalText.Length} chars)");
        }
        else
        {
            Debug.LogWarning("No global_stats_display file found in " + folder);
        }

        // Validate at least one non-empty text to send
        if (string.IsNullOrEmpty(trainingText) && string.IsNullOrEmpty(globalText))
        {
            ShowLoading(false);
            Debug.LogError("No training_plan_text or global_stats_text available to send.");
            ShowError("No training plan or global stats files found to generate report.");
            yield break;
        }

        // Build JSON payload manually and send (RequestReport handles escaping/POST)
        yield return StartCoroutine(RequestReport(trainingText, globalText));
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

        if (generateReportButton != null)
        {
            generateReportButton.onClick.RemoveListener(OnGenerateReportClicked);
        }
    }
}