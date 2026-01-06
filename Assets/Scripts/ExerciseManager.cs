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

        [Header("Direction Selection")]
        [Tooltip("Yön seçimi paneli (Turn skill'leri için)")]
        public GameObject directionSelectionPanel;
        
        [Tooltip("Sol dönüş butonu")]
        public Button turnLeftButton;
        
        [Tooltip("Sağ dönüş butonu")]
        public Button turnRightButton;

        // Skill-Zone mapping
        private Dictionary<string, Transform> skillZoneMap;
        
        // Turn skills that require direction selection
        private HashSet<string> turnSkills = new HashSet<string>
        {
            "3",   // Turns while moving forwards (90°)
            "4",   // Turns while moving backwards (90°)
            "5",   // Turns in place (180°)
            "28"   // Turns in wheelie position (180°)
        };
        
        // Pending skill ID during direction selection
        private string pendingSkillId;

        private void Awake()
        {
            InitializeSkillZoneMapping();
            SetupEventListeners();
            InitializeReferences();
        }

        private void InitializeReferences()
        {
            // Cache references to avoid expensive runtime lookups
            if (wheelchair == null)
            {
                wheelchair = FindFirstObjectByType<WheelchairController>();
            }

            if (realtimeCoachTutorial == null)
            {
                realtimeCoachTutorial = FindFirstObjectByType<RealtimeCoachTutorial>();
            }
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
            
            // Direction selection button listeners
            if (turnLeftButton != null)
            {
                turnLeftButton.onClick.AddListener(() => OnDirectionSelected("left"));
            }
            
            if (turnRightButton != null)
            {
                turnRightButton.onClick.AddListener(() => OnDirectionSelected("right"));
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

            // Check if this is a turn skill that requires direction selection
            if (turnSkills.Contains(skillId))
            {
                pendingSkillId = skillId;
                
                // Show direction selection panel
                if (directionSelectionPanel != null)
                {
                    directionSelectionPanel.SetActive(true);
                }
                
                Debug.Log($"[ExerciseManager] Direction selection panel opened for skill {skillId}");
                return; // Wait for direction selection
            }

            // For non-turn skills, proceed directly
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
                Debug.LogError("[ExerciseManager] Wheelchair reference not set!");
                return;
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
        /// Yön seçimi yapıldığında çağrılır (Turn skill'leri için)
        /// </summary>
        /// <param name="direction">"left" veya "right"</param>
        public void OnDirectionSelected(string direction)
        {
            if (string.IsNullOrEmpty(pendingSkillId))
            {
                Debug.LogWarning("[ExerciseManager] No pending skill for direction selection!");
                return;
            }

            Debug.Log($"[ExerciseManager] Direction selected: {direction} for skill {pendingSkillId}");

            // Direction selection panelini kapat
            if (directionSelectionPanel != null)
            {
                directionSelectionPanel.SetActive(false);
            }

            // Skill selection panelini de kapat
            if (skillSelectionPanel != null)
            {
                skillSelectionPanel.SetActive(false);
            }

            // Zone'a teleport et
            if (skillZoneMap.TryGetValue(pendingSkillId, out Transform targetZone))
            {
                TeleportToZone(targetZone);
            }
            else
            {
                Debug.LogWarning($"[ExerciseManager] No zone mapped for skill {pendingSkillId}");
            }

            // Yönlü soru ile training'i başlat
            string question = GetDirectionalQuestion(pendingSkillId, direction);
            StartSkillWithQuestion(pendingSkillId, question);
            
            // Clear pending skill
            pendingSkillId = null;
        }

        /// <summary>
        /// RAG sisteminden tutorial adımlarını alır ve RealtimeCoachTutorial'ı başlatır
        /// </summary>
        /// <param name="skillId">Skill ID</param>
        private void StartSkillTraining(string skillId)
        {
            string question = GetQuestionForSkill(skillId);
            StartSkillWithQuestion(skillId, question);
        }

        /// <summary>
        /// Özel soru ile skill training başlatır (direction selection için)
        /// </summary>
        /// <param name="skillId">Skill ID</param>
        /// <param name="question">RAG sistemine gönderilecek soru</param>
        private void StartSkillWithQuestion(string skillId, string question)
        {
            if (realtimeCoachTutorial == null)
            {
                Debug.LogError("[ExerciseManager] RealtimeCoachTutorial reference not set!");
                return;
            }

            Debug.Log($"[ExerciseManager] Starting training for skill {skillId} with question: {question}");

            // RAG sisteminden practice adımlarını al
            AskPracticeRequest request = new AskPracticeRequest(question);

            StartCoroutine(APIClient.Instance.GetAskPractice(
                request,
                OnAskPracticeSuccess,
                OnAskPracticeError
            ));
        }

        /// <summary>
        /// Turn skill için yöne göre özel soru oluşturur
        /// </summary>
        /// <param name="skillId">Skill ID</param>
        /// <param name="direction">"left" veya "right"</param>
        /// <returns>Yöne özel soru metni</returns>
        private string GetDirectionalQuestion(string skillId, string direction)
        {
            return skillId switch
            {
                "3" => $"How do I turn {direction} 90 degrees while moving forward in a wheelchair?",
                "4" => $"How do I turn {direction} 90 degrees while moving backward in a wheelchair?",
                "5" => $"How do I turn {direction} 180 degrees in place while sitting in a wheelchair?",
                "28" => $"How do I turn {direction} 180 degrees in place while in a wheelie position?",
                _ => GetQuestionForSkill(skillId) // Fallback to default
            };
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
            
            // Direction selection button listener'ları temizle
            if (turnLeftButton != null)
            {
                turnLeftButton.onClick.RemoveAllListeners();
            }
            
            if (turnRightButton != null)
            {
                turnRightButton.onClick.RemoveAllListeners();
            }
        }
    }
}
