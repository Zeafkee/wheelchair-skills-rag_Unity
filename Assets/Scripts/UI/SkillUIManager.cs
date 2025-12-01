using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WheelchairSkills.API;
using WheelchairSkills.Training;

namespace WheelchairSkills.UI
{
    /// <summary>
    /// Manages UI for skill training system
    /// Handles panel management, skill selection, and feedback display
    /// </summary>
    public class SkillUIManager : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject selectionPanel;
        [SerializeField] private GameObject trainingPanel;
        [SerializeField] private GameObject resultsPanel;

        [Header("Selection Panel")]
        [SerializeField] private Transform skillButtonContainer;
        [SerializeField] private GameObject skillButtonPrefab;
        [SerializeField] private TMP_InputField userIdInputField;

        [Header("Training Panel")]
        [SerializeField] private TextMeshProUGUI skillNameText;
        [SerializeField] private TextMeshProUGUI currentStepText;
        [SerializeField] private TextMeshProUGUI stepProgressText;
        [SerializeField] private TextMeshProUGUI feedbackText;
        [SerializeField] private Button cancelButton;

        [Header("Results Panel")]
        [SerializeField] private TextMeshProUGUI resultsTitle;
        [SerializeField] private TextMeshProUGUI performanceText;
        [SerializeField] private TextMeshProUGUI feedbackResultText;
        [SerializeField] private Button backToSelectionButton;
        [SerializeField] private Button retryButton;

        [Header("Skills Configuration")]
        [SerializeField] private string[] availableSkills = new string[]
        {
            "İleri Hareket",
            "Geri Hareket",
            "Dönüş Yapma",
            "Engel Aşma",
            "Rampa Tırmanma",
            "Kapı Açma"
        };

        private string currentUserId;
        private string currentSkillName;

        private void Start()
        {
            // Subscribe to tracker events
            SkillAttemptTracker.Instance.OnFeedbackReceived += HandleFeedback;
            SkillAttemptTracker.Instance.OnStepChanged += HandleStepChanged;
            SkillAttemptTracker.Instance.OnAttemptCompleted += HandleAttemptCompleted;
            SkillAttemptTracker.Instance.OnError += HandleError;

            // Setup button listeners
            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancelAttempt);
            if (backToSelectionButton != null)
                backToSelectionButton.onClick.AddListener(OnBackToSelection);
            if (retryButton != null)
                retryButton.onClick.AddListener(OnRetry);

            // Initialize UI
            ShowSelectionPanel();
            CreateSkillButtons();
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (SkillAttemptTracker.Instance != null)
            {
                SkillAttemptTracker.Instance.OnFeedbackReceived -= HandleFeedback;
                SkillAttemptTracker.Instance.OnStepChanged -= HandleStepChanged;
                SkillAttemptTracker.Instance.OnAttemptCompleted -= HandleAttemptCompleted;
                SkillAttemptTracker.Instance.OnError -= HandleError;
            }
        }

        /// <summary>
        /// Create skill selection buttons
        /// </summary>
        private void CreateSkillButtons()
        {
            if (skillButtonContainer == null || skillButtonPrefab == null)
            {
                Debug.LogWarning("Skill button container or prefab not assigned");
                return;
            }

            // Clear existing buttons
            foreach (Transform child in skillButtonContainer)
            {
                Destroy(child.gameObject);
            }

            // Create button for each skill
            foreach (string skillName in availableSkills)
            {
                GameObject buttonObj = Instantiate(skillButtonPrefab, skillButtonContainer);
                Button button = buttonObj.GetComponent<Button>();
                TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

                if (buttonText != null)
                {
                    buttonText.text = skillName;
                }

                // Capture skillName in closure
                string skill = skillName;
                button.onClick.AddListener(() => OnSkillSelected(skill));
            }
        }

        /// <summary>
        /// Handle skill selection
        /// </summary>
        private void OnSkillSelected(string skillName)
        {
            // Get user ID from input field
            if (userIdInputField != null && !string.IsNullOrEmpty(userIdInputField.text))
            {
                currentUserId = userIdInputField.text;
            }
            else
            {
                // Use default user ID or show error
                currentUserId = "default_user";
                Debug.LogWarning("No user ID provided, using default");
            }

            currentSkillName = skillName;
            ShowTrainingPanel();
            StartSkillTraining();
        }

        /// <summary>
        /// Start skill training
        /// </summary>
        private void StartSkillTraining()
        {
            if (skillNameText != null)
            {
                skillNameText.text = currentSkillName;
            }

            if (feedbackText != null)
            {
                feedbackText.text = "Beceri denemesi başlatılıyor...";
            }

            SkillAttemptTracker.Instance.StartSkillAttempt(currentUserId, currentSkillName);
        }

        /// <summary>
        /// Handle feedback from tracker
        /// </summary>
        private void HandleFeedback(string feedback)
        {
            if (feedbackText != null)
            {
                feedbackText.text = feedback;
            }
        }

        /// <summary>
        /// Handle step change
        /// </summary>
        private void HandleStepChanged(int stepIndex)
        {
            StepData currentStep = SkillAttemptTracker.Instance.GetCurrentStep();
            
            if (currentStepText != null && currentStep != null)
            {
                currentStepText.text = $"Adım {currentStep.step_number}: {currentStep.description}";
                
                // Show required actions
                if (currentStep.required_actions != null && currentStep.required_actions.Length > 0)
                {
                    string actions = string.Join(", ", currentStep.required_actions);
                    currentStepText.text += $"\n\nGerekli aksiyonlar: {actions}";
                }
            }

            if (stepProgressText != null)
            {
                int totalSteps = SkillAttemptTracker.Instance.TotalSteps;
                stepProgressText.text = $"İlerleme: {stepIndex + 1} / {totalSteps}";
            }
        }

        /// <summary>
        /// Handle attempt completion
        /// </summary>
        private void HandleAttemptCompleted(bool success, PerformanceData performance)
        {
            ShowResultsPanel();

            if (resultsTitle != null)
            {
                resultsTitle.text = success ? "Başarılı!" : "Tamamlanamadı";
            }

            if (performanceText != null && performance != null)
            {
                performanceText.text = $"Süre: {performance.completion_time:F2}s\n" +
                                      $"Doğruluk: {performance.accuracy:P0}\n" +
                                      $"Hatalar: {performance.errors_count}";
            }

            if (feedbackResultText != null && performance != null)
            {
                feedbackResultText.text = performance.feedback;
            }
        }

        /// <summary>
        /// Handle errors
        /// </summary>
        private void HandleError(string error)
        {
            Debug.LogError($"UI Error: {error}");
            if (feedbackText != null)
            {
                feedbackText.text = $"Hata: {error}";
            }
        }

        /// <summary>
        /// Cancel current attempt
        /// </summary>
        private void OnCancelAttempt()
        {
            SkillAttemptTracker.Instance.CancelAttempt();
            ShowSelectionPanel();
        }

        /// <summary>
        /// Return to selection panel
        /// </summary>
        private void OnBackToSelection()
        {
            ShowSelectionPanel();
        }

        /// <summary>
        /// Retry current skill
        /// </summary>
        private void OnRetry()
        {
            ShowTrainingPanel();
            StartSkillTraining();
        }

        /// <summary>
        /// Show selection panel
        /// </summary>
        private void ShowSelectionPanel()
        {
            SetPanelActive(selectionPanel, true);
            SetPanelActive(trainingPanel, false);
            SetPanelActive(resultsPanel, false);
        }

        /// <summary>
        /// Show training panel
        /// </summary>
        private void ShowTrainingPanel()
        {
            SetPanelActive(selectionPanel, false);
            SetPanelActive(trainingPanel, true);
            SetPanelActive(resultsPanel, false);
        }

        /// <summary>
        /// Show results panel
        /// </summary>
        private void ShowResultsPanel()
        {
            SetPanelActive(selectionPanel, false);
            SetPanelActive(trainingPanel, false);
            SetPanelActive(resultsPanel, true);
        }

        /// <summary>
        /// Helper to set panel active state
        /// </summary>
        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }

        /// <summary>
        /// Get training plan for current user
        /// </summary>
        public void GetTrainingPlan()
        {
            if (string.IsNullOrEmpty(currentUserId))
            {
                Debug.LogWarning("No user ID set");
                return;
            }

            APIClient.Instance.GetTrainingPlan(
                currentUserId,
                onSuccess: (plan) =>
                {
                    Debug.Log($"Training plan received: {plan.reasoning}");
                    // Could update UI with recommended skills
                },
                onError: (error) =>
                {
                    Debug.LogError($"Failed to get training plan: {error}");
                }
            );
        }

        /// <summary>
        /// Get guidance for a specific skill
        /// </summary>
        public void GetSkillGuidance(string skillName)
        {
            APIClient.Instance.GetSkillGuidance(
                skillName,
                "Yeni kullanıcı", // User context
                onSuccess: (guidance) =>
                {
                    Debug.Log($"Skill guidance received for {guidance.skill_name}");
                    // Could show guidance in UI
                },
                onError: (error) =>
                {
                    Debug.LogError($"Failed to get skill guidance: {error}");
                }
            );
        }
    }
}
