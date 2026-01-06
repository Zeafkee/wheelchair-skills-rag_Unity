using UnityEngine;
using UnityEngine.UI;
using WheelchairSkills.Training;

namespace WheelchairSkills.UI
{
    /// <summary>
    /// Skill seçim butonları için basit script
    /// Her butona skill ID atanır ve tıklandığında ExerciseManager'a bildirilir
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class SkillButton : MonoBehaviour
    {
        [Header("Skill Settings")]
        [Tooltip("Bu butonun temsil ettiği skill ID (örn: 1, 15, 25)")]
        public string skillId;

        [Header("References")]
        [Tooltip("ExerciseManager referansı")]
        public ExerciseManager exerciseManager;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            
            if (button != null)
            {
                button.onClick.AddListener(OnButtonClick);
            }

            // Cache ExerciseManager reference in Awake to avoid runtime lookups
            if (exerciseManager == null)
            {
                exerciseManager = FindFirstObjectByType<ExerciseManager>();
                
                if (exerciseManager == null)
                {
                    Debug.LogWarning("[SkillButton] ExerciseManager not found in scene! Please assign it in the Inspector.");
                }
            }
        }

        private void OnButtonClick()
        {
            if (exerciseManager == null)
            {
                Debug.LogError("[SkillButton] ExerciseManager is not assigned!");
                return;
            }

            if (string.IsNullOrEmpty(skillId))
            {
                Debug.LogWarning("[SkillButton] Skill ID is not set!");
                return;
            }

            Debug.Log($"[SkillButton] Button clicked for skill: {skillId}");
            exerciseManager.OnSkillSelected(skillId);
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnButtonClick);
            }
        }
    }
}
