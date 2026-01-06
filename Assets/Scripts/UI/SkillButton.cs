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
        }

        private void OnButtonClick()
        {
            if (exerciseManager == null)
            {
                // ExerciseManager bulunamazsa sahnede ara
                exerciseManager = FindFirstObjectByType<ExerciseManager>();
                
                if (exerciseManager == null)
                {
                    Debug.LogError("[SkillButton] ExerciseManager not found in scene!");
                    return;
                }
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
