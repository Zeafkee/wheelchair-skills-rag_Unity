using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WheelchairSkills.API;

namespace WheelchairSkills.Training
{
    /// <summary>
    /// Exercise butonu ile açılan skill seçim UI'ı ve zone-based training sistemini yönetir
    /// </summary>
    public class ExerciseManager : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Ana menü paneli (Exercise butonu burada)")]
        public GameObject mainMenuPanel;
        
        [Tooltip("Skill listesi paneli")]
        public GameObject skillSelectionPanel;
        
        [Tooltip("Exercise butonu - tıklandığında skill selection açılır")]
        public Button exerciseButton;

        [Header("Training Zones")]
        [Tooltip("Skill 1-5 için (Forward, Backward, Turns)")]
        public Transform basicMovementZone;
        
        [Tooltip("Skill 15-16 için (5° eğim)")]
        public Transform inclineZone;
        
        [Tooltip("Skill 25-26 için (Kaldırım)")]
        public Transform curbZone;
        
        [Tooltip("Skill 30 için (Hareketli engeller)")]
        public Transform obstacleZone;

        [Header("References")]
        [Tooltip("Wheelchair controller referansı")]
        public WheelchairController wheelchair;
        
        [Tooltip("RealtimeCoachTutorial referansı")]
        public RealtimeCoachTutorial realtimeCoachTutorial;

        // Skill-Zone mapping
        private Dictionary<string, Transform> skillZoneMap;

        private void Awake()
        {
            InitializeSkillZoneMapping();
            SetupEventListeners();
        }

        private void InitializeSkillZoneMapping()
        {
            skillZoneMap = new Dictionary<string, Transform>
            {
                // Basic Movement Zone - Skills 1-5
                { "1", basicMovementZone },   // Rolls forwards (10m)
                { "2", basicMovementZone },   // Rolls backwards (2m)
                { "3", basicMovementZone },   // Turns while moving forwards (90°)
                { "4", basicMovementZone },   // Turns while moving backwards (90°)
                { "5", basicMovementZone },   // Rolls 100m
                
                // Incline Zone - Skills 15-16
                { "15", inclineZone },        // Ascends 5° incline
                { "16", inclineZone },        // Descends 5° incline
                
                // Curb Zone - Skills 25-26
                { "25", curbZone },           // Ascends curb (15cm)
                { "26", curbZone },           // Descends curb
                
                // Obstacle Zone - Skill 30
                { "30", obstacleZone }        // Avoids moving obstacles
            };
        }

        private void SetupEventListeners()
        {
            if (exerciseButton != null)
            {
                exerciseButton.onClick.AddListener(OpenSkillSelection);
            }
        }

        /// <summary>
        /// Exercise butonuna bağlanan metod - Skill selection panelini açar
        /// </summary>
        public void OpenSkillSelection()
        {
            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(false);
            }

            if (skillSelectionPanel != null)
            {
                skillSelectionPanel.SetActive(true);
            }

            Debug.Log("[ExerciseManager] Skill selection panel opened");
        }

        /// <summary>
        /// Skill butonlarına bağlanacak metod - Seçilen skill için training başlatır
        /// </summary>
        /// <param name="skillId">Seçilen skill ID (örn: "1", "15", "25")</param>
        public void OnSkillSelected(string skillId)
        {
            Debug.Log($"[ExerciseManager] Skill selected: {skillId}");

            // Zone'a teleport et
            if (skillZoneMap.TryGetValue(skillId, out Transform targetZone))
            {
                TeleportToZone(targetZone);
            }
            else
            {
                Debug.LogWarning($"[ExerciseManager] No zone mapped for skill {skillId}");
            }

            // Skill selection panelini kapat
            if (skillSelectionPanel != null)
            {
                skillSelectionPanel.SetActive(false);
            }

            // Training'i başlat
            StartSkillTraining(skillId);
        }

        /// <summary>
        /// Wheelchair'ı belirtilen zone'a taşır
        /// </summary>
        /// <param name="zone">Hedef zone Transform'u</param>
        private void TeleportToZone(Transform zone)
        {
            if (zone == null)
            {
                Debug.LogWarning("[ExerciseManager] Target zone is null");
                return;
            }

            if (wheelchair == null)
            {
                wheelchair = FindFirstObjectByType<WheelchairController>();
                if (wheelchair == null)
                {
                    Debug.LogError("[ExerciseManager] Wheelchair not found!");
                    return;
                }
            }

            // Wheelchair'ı zone pozisyonuna ve rotasyonuna taşı
            wheelchair.transform.position = zone.position;
            wheelchair.transform.rotation = zone.rotation;

            // Rigidbody hızını sıfırla
            if (wheelchair.rb != null)
            {
                wheelchair.rb.linearVelocity = Vector3.zero;
                wheelchair.rb.angularVelocity = Vector3.zero;
            }

            Debug.Log($"[ExerciseManager] Wheelchair teleported to {zone.name}");
        }

        /// <summary>
        /// RAG sisteminden tutorial adımlarını alır ve RealtimeCoachTutorial'ı başlatır
        /// </summary>
        /// <param name="skillId">Skill ID</param>
        private void StartSkillTraining(string skillId)
        {
            if (realtimeCoachTutorial == null)
            {
                realtimeCoachTutorial = FindFirstObjectByType<RealtimeCoachTutorial>();
                if (realtimeCoachTutorial == null)
                {
                    Debug.LogError("[ExerciseManager] RealtimeCoachTutorial not found!");
                    return;
                }
            }

            // Skill için uygun soru metnini oluştur
            string question = GetQuestionForSkill(skillId);

            // RAG sisteminden practice adımlarını al
            AskPracticeRequest request = new AskPracticeRequest(question);

            StartCoroutine(APIClient.Instance.GetAskPractice(
                request,
                OnAskPracticeSuccess,
                OnAskPracticeError
            ));
        }

        /// <summary>
        /// Skill ID'ye göre uygun RAG sorusunu oluşturur
        /// </summary>
        private string GetQuestionForSkill(string skillId)
        {
            Dictionary<string, string> skillQuestions = new Dictionary<string, string>
            {
                { "1", "How do I roll forwards 10 meters in a wheelchair?" },
                { "2", "How do I roll backwards 2 meters in a wheelchair?" },
                { "3", "How do I turn 90 degrees while moving forward in a wheelchair?" },
                { "4", "How do I turn 90 degrees while moving backward in a wheelchair?" },
                { "5", "How do I roll 100 meters in a wheelchair?" },
                { "15", "How do I ascend a 5 degree incline in a wheelchair?" },
                { "16", "How do I descend a 5 degree incline in a wheelchair?" },
                { "25", "How do I ascend a 15cm curb in a wheelchair?" },
                { "26", "How do I descend a curb in a wheelchair?" },
                { "30", "How do I avoid moving obstacles in a wheelchair?" }
            };

            if (skillQuestions.TryGetValue(skillId, out string question))
            {
                return question;
            }

            // Varsayılan soru
            return $"How do I practice skill {skillId} in a wheelchair?";
        }

        private void OnAskPracticeSuccess(AskPracticeResponse response)
        {
            if (response == null || response.steps == null || response.steps.Count == 0)
            {
                Debug.LogError("[ExerciseManager] No tutorial steps received from RAG system");
                return;
            }

            Debug.Log($"[ExerciseManager] Received {response.steps.Count} tutorial steps");

            // RealtimeCoachTutorial'ı başlat
            StartCoroutine(realtimeCoachTutorial.StartTutorial(response));
        }

        private void OnAskPracticeError(string error)
        {
            Debug.LogError($"[ExerciseManager] Failed to get practice steps: {error}");
        }

        private void OnDestroy()
        {
            // Event listener'ları temizle
            if (exerciseButton != null)
            {
                exerciseButton.onClick.RemoveListener(OpenSkillSelection);
            }
        }
    }
}
