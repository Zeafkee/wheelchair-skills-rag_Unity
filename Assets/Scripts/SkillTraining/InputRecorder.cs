using UnityEngine;

namespace WheelchairSkills.Training
{
    /// <summary>
    /// Records keyboard inputs during skill training
    /// Listens for mapped keys and forwards them to SkillAttemptTracker
    /// </summary>
    public class InputRecorder : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Enable/disable input recording")]
        public bool recordingEnabled = true;

        [Tooltip("Show debug messages for recorded inputs")]
        public bool showDebugMessages = false;

        private void Update()
        {
            if (!recordingEnabled)
            {
                return;
            }

            // Only record if there's an active attempt
            if (!SkillAttemptTracker.Instance.IsAttemptActive)
            {
                return;
            }

            // Check all mapped keys
            KeyCode[] mappedKeys = InputMapping.GetAllMappedKeys();
            foreach (KeyCode key in mappedKeys)
            {
                if (Input.GetKeyDown(key))
                {
                    if (showDebugMessages)
                    {
                        string action = InputMapping.GetAction(key);
                        string description = InputMapping.GetDescription(action);
                        Debug.Log($"Input recorded: {key} -> {action} ({description})");
                    }

                    // Process the input through the tracker
                    SkillAttemptTracker.Instance.ProcessInput(key);
                }
            }
        }

        /// <summary>
        /// Enable input recording
        /// </summary>
        public void EnableRecording()
        {
            recordingEnabled = true;
        }

        /// <summary>
        /// Disable input recording
        /// </summary>
        public void DisableRecording()
        {
            recordingEnabled = false;
        }

        /// <summary>
        /// Toggle input recording
        /// </summary>
        public void ToggleRecording()
        {
            recordingEnabled = !recordingEnabled;
        }
    }
}
