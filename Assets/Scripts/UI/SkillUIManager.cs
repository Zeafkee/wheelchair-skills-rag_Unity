using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WheelchairSkills.API;
using WheelchairSkills.Training;

namespace WheelchairSkills.UI
{
    /// <summary>
    /// Beceri seçimi, eğitim ekranı ve sonuç ekranını yöneten UI scripti
    /// </summary>
    public class SkillUIManager : MonoBehaviour
    {
        [Header("UI Panels")]
        public GameObject skillSelectionPanel;
        public GameObject trainingPanel;
        public GameObject resultsPanel;
        public GameObject loadingPanel;

        [Header("Skill Selection UI")]
        public Transform skillListContainer;
        public GameObject skillButtonPrefab;
        public Text skillDescriptionText;

        [Header("Training UI")]
        public Text currentSkillNameText;
        public Text attemptStatusText;
        public Text inputCountText;
        public Text timerText;
        public Text hintText;
        public Button endAttemptButton;
        public Button requestHintButton;

        [Header("Results UI")]
        public Text resultsSkillNameText;
        public Text resultsDurationText;
        public Text resultsInputCountText;
        public Text resultsSuccessRateText;
        public Text feedbackText;
        public Button backToSelectionButton;
        public Button retryButton;

        [Header("References")]
        public SkillAttemptTracker attemptTracker;

        private List<Skill> availableSkills = new List<Skill>();
        private Skill currentSkill;

        private void Start()
        {
            // Panel'leri başlangıçta gizle
            ShowPanel(PanelType.SkillSelection);

            // Event listener'ları bağla
            SetupEventListeners();

            // Becerileri yükle
            LoadSkills();
        }

        private void SetupEventListeners()
        {
            if (attemptTracker != null)
            {
                attemptTracker.OnAttemptStarted += OnAttemptStarted;
                attemptTracker.OnAttemptEnded += OnAttemptEnded;
                attemptTracker.OnInputRecorded += OnInputRecorded;
            }

            if (endAttemptButton != null)
            {
                endAttemptButton.onClick.AddListener(() => attemptTracker.EndAttempt("completed"));
            }

            if (requestHintButton != null)
            {
                requestHintButton.onClick.AddListener(RequestHint);
            }

            if (backToSelectionButton != null)
            {
                backToSelectionButton.onClick.AddListener(() => ShowPanel(PanelType.SkillSelection));
            }

            if (retryButton != null)
            {
                retryButton.onClick.AddListener(RetryCurrentSkill);
            }
        }

        private void Update()
        {
            // Timer'ı güncelle
            if (attemptTracker != null && attemptTracker.isAttemptActive && timerText != null)
            {
                float elapsed = Time.time - attemptTracker.attemptStartTime;
                timerText.text = $"Time: {elapsed:F1}s";
            }
        }

        #region Skill Loading

        private void LoadSkills()
        {
            ShowPanel(PanelType.Loading);
            
            StartCoroutine(APIClient.Instance.GetSkills(
                OnSkillsLoaded,
                OnSkillsLoadError
            ));
        }

        private void OnSkillsLoaded(SkillsResponse response)
        {
            availableSkills = response.skills;
            PopulateSkillList();
            ShowPanel(PanelType.SkillSelection);
        }

        private void OnSkillsLoadError(string error)
        {
            Debug.LogError($"Failed to load skills: {error}");
            ShowPanel(PanelType.SkillSelection);
            
            // Hata mesajı göster (opsiyonel)
            if (skillDescriptionText != null)
            {
                skillDescriptionText.text = $"Error loading skills: {error}\nUsing offline mode.";
            }
        }

        private void PopulateSkillList()
        {
            if (skillListContainer == null || skillButtonPrefab == null)
            {
                Debug.LogWarning("Skill list container or prefab not assigned!");
                return;
            }

            // Önceki butonları temizle
            foreach (Transform child in skillListContainer)
            {
                Destroy(child.gameObject);
            }

            // Her beceri için bir buton oluştur
            foreach (Skill skill in availableSkills)
            {
                GameObject buttonObj = Instantiate(skillButtonPrefab, skillListContainer);
                Button button = buttonObj.GetComponent<Button>();
                Text buttonText = buttonObj.GetComponentInChildren<Text>();

                if (buttonText != null)
                {
                    buttonText.text = skill.name;
                }

                if (button != null)
                {
                    Skill capturedSkill = skill; // Closure için
                    button.onClick.AddListener(() => OnSkillSelected(capturedSkill));
                }
            }
        }

        #endregion

        #region Skill Selection

        private void OnSkillSelected(Skill skill)
        {
            currentSkill = skill;
            
            if (skillDescriptionText != null)
            {
                skillDescriptionText.text = $"{skill.name}\n\n{skill.description}\n\nDifficulty: {skill.difficulty}";
            }
        }

        public void StartSelectedSkill()
        {
            if (currentSkill == null)
            {
                Debug.LogWarning("No skill selected!");
                return;
            }

            if (attemptTracker == null)
            {
                Debug.LogError("Attempt tracker not assigned!");
                return;
            }

            // Eğitim paneline geç
            ShowPanel(PanelType.Training);
            
            if (currentSkillNameText != null)
            {
                currentSkillNameText.text = currentSkill.name;
            }

            // Denemeyi başlat
            attemptTracker.StartAttempt(currentSkill.id);
        }

        #endregion

        #region Training

        private void OnAttemptStarted(StartAttemptResponse response)
        {
            if (attemptStatusText != null)
            {
                attemptStatusText.text = "Status: In Progress";
            }

            if (inputCountText != null)
            {
                inputCountText.text = "Inputs: 0";
            }

            if (hintText != null)
            {
                hintText.text = "Press button for hint";
            }
        }

        private void OnInputRecorded(string action)
        {
            if (inputCountText != null && attemptTracker != null)
            {
                inputCountText.text = $"Inputs: {attemptTracker.inputCount}";
            }
        }

        private void RequestHint()
        {
            if (attemptTracker == null || !attemptTracker.isAttemptActive)
            {
                return;
            }

            attemptTracker.RequestHint("training", response =>
            {
                if (hintText != null)
                {
                    hintText.text = $"Hint: {response.hint}\n\nContext: {response.context}";
                }
            });
        }

        #endregion

        #region Results

        private void OnAttemptEnded(EndAttemptResponse response)
        {
            ShowPanel(PanelType.Results);

            if (resultsSkillNameText != null)
            {
                resultsSkillNameText.text = currentSkill.name;
            }

            if (response.summary != null)
            {
                if (resultsDurationText != null)
                {
                    resultsDurationText.text = $"Duration: {response.summary.duration:F1}s";
                }

                if (resultsInputCountText != null)
                {
                    resultsInputCountText.text = $"Total Inputs: {response.summary.total_inputs}";
                }

                if (resultsSuccessRateText != null)
                {
                    resultsSuccessRateText.text = $"Success Rate: {response.summary.success_rate:P0}";
                }
            }

            // Geri bildirim al
            LoadFeedback();
        }

        private void LoadFeedback()
        {
            if (attemptTracker == null)
            {
                return;
            }

            attemptTracker.GetFeedback(response =>
            {
                if (feedbackText != null)
                {
                    string feedbackContent = $"Assessment: {response.overall_assessment}\n\n";
                    
                    if (response.recommendations != null && response.recommendations.Count > 0)
                    {
                        feedbackContent += "Recommendations:\n";
                        foreach (string rec in response.recommendations)
                        {
                            feedbackContent += $"• {rec}\n";
                        }
                    }

                    feedbackText.text = feedbackContent;
                }
            });
        }

        private void RetryCurrentSkill()
        {
            if (currentSkill != null)
            {
                ShowPanel(PanelType.Training);
                attemptTracker.StartAttempt(currentSkill.id);
            }
        }

        #endregion

        #region Panel Management

        private enum PanelType
        {
            SkillSelection,
            Training,
            Results,
            Loading
        }

        private void ShowPanel(PanelType panelType)
        {
            // Tüm panelleri gizle
            if (skillSelectionPanel != null)
                skillSelectionPanel.SetActive(panelType == PanelType.SkillSelection);
            
            if (trainingPanel != null)
                trainingPanel.SetActive(panelType == PanelType.Training);
            
            if (resultsPanel != null)
                resultsPanel.SetActive(panelType == PanelType.Results);
            
            if (loadingPanel != null)
                loadingPanel.SetActive(panelType == PanelType.Loading);
        }

        #endregion

        private void OnDestroy()
        {
            // Event listener'ları temizle
            if (attemptTracker != null)
            {
                attemptTracker.OnAttemptStarted -= OnAttemptStarted;
                attemptTracker.OnAttemptEnded -= OnAttemptEnded;
                attemptTracker.OnInputRecorded -= OnInputRecorded;
            }
        }
    }
}
