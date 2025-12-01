using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WheelchairSkills.API;

namespace WheelchairSkills.Training
{
    /// <summary>
    /// Deneme durumunu yöneten, input işleyen ve API çağrılarını yapan ana sınıf
    /// </summary>
    public class SkillAttemptTracker : MonoBehaviour
    {
        [Header("Attempt Settings")]
        public string userId = "default_user";
        public bool autoStartAttempt = false;

        [Header("Current State")]
        public string currentSkillId;
        public string currentAttemptId;
        public bool isAttemptActive = false;
        public float attemptStartTime;
        public int inputCount = 0;

        [Header("Action History")]
        public List<string> recentActions = new List<string>();
        public int maxRecentActionsCount = 10;

        // Events
        public event Action<StartAttemptResponse> OnAttemptStarted;
        public event Action<EndAttemptResponse> OnAttemptEnded;
        public event Action<string> OnInputRecorded; // action name
        public event Action<string> OnError;

        private void Start()
        {
            if (autoStartAttempt && !string.IsNullOrEmpty(currentSkillId))
            {
                StartAttempt(currentSkillId);
            }
        }

        /// <summary>
        /// Yeni bir deneme başlatır
        /// </summary>
        public void StartAttempt(string skillId)
        {
            if (isAttemptActive)
            {
                Debug.LogWarning("An attempt is already active. Please end it before starting a new one.");
                return;
            }

            currentSkillId = skillId;
            StartAttemptRequest request = new StartAttemptRequest
            {
                skill_id = skillId,
                user_id = userId
            };

            StartCoroutine(APIClient.Instance.StartAttempt(
                request,
                OnStartAttemptSuccess,
                OnStartAttemptError
            ));
        }

        private void OnStartAttemptSuccess(StartAttemptResponse response)
        {
            currentAttemptId = response.attempt_id;
            isAttemptActive = true;
            attemptStartTime = Time.time;
            inputCount = 0;
            recentActions.Clear();

            Debug.Log($"Attempt started: {currentAttemptId} for skill: {currentSkillId}");
            OnAttemptStarted?.Invoke(response);
        }

        private void OnStartAttemptError(string error)
        {
            Debug.LogError($"Failed to start attempt: {error}");
            OnError?.Invoke($"Start attempt failed: {error}");
        }

        /// <summary>
        /// Bir input aksiyonunu kaydeder
        /// </summary>
        public void RecordInput(string action, Dictionary<string, object> metadata = null)
        {
            if (!isAttemptActive)
            {
                Debug.LogWarning("No active attempt. Cannot record input.");
                return;
            }

            float timestamp = Time.time - attemptStartTime;

            RecordInputRequest request = new RecordInputRequest
            {
                attempt_id = currentAttemptId,
                action = action,
                timestamp = timestamp,
                metadata = metadata ?? new Dictionary<string, object>()
            };

            // Son aksiyonları güncelle
            recentActions.Add(action);
            if (recentActions.Count > maxRecentActionsCount)
            {
                recentActions.RemoveAt(0);
            }

            inputCount++;

            StartCoroutine(APIClient.Instance.RecordInput(
                request,
                response => OnRecordInputSuccess(response, action),
                OnRecordInputError
            ));
        }

        private void OnRecordInputSuccess(RecordInputResponse response, string action)
        {
            Debug.Log($"Input recorded: {action} - {response.message}");
            OnInputRecorded?.Invoke(action);
        }

        private void OnRecordInputError(string error)
        {
            Debug.LogError($"Failed to record input: {error}");
            OnError?.Invoke($"Record input failed: {error}");
        }

        /// <summary>
        /// Aktif denemeyi sonlandırır
        /// </summary>
        public void EndAttempt(string status = "completed")
        {
            if (!isAttemptActive)
            {
                Debug.LogWarning("No active attempt to end.");
                return;
            }

            float duration = Time.time - attemptStartTime;

            EndAttemptRequest request = new EndAttemptRequest
            {
                attempt_id = currentAttemptId,
                status = status,
                duration = duration
            };

            StartCoroutine(APIClient.Instance.EndAttempt(
                request,
                OnEndAttemptSuccess,
                OnEndAttemptError
            ));
        }

        private void OnEndAttemptSuccess(EndAttemptResponse response)
        {
            isAttemptActive = false;
            Debug.Log($"Attempt ended: {response.message}");
            
            if (response.summary != null)
            {
                Debug.Log($"Summary - Total inputs: {response.summary.total_inputs}, Duration: {response.summary.duration}s, Success rate: {response.summary.success_rate}");
            }

            OnAttemptEnded?.Invoke(response);
        }

        private void OnEndAttemptError(string error)
        {
            Debug.LogError($"Failed to end attempt: {error}");
            OnError?.Invoke($"End attempt failed: {error}");
            
            // Hata durumunda da denemeyi kapat
            isAttemptActive = false;
        }

        /// <summary>
        /// Deneme geri bildirimini alır
        /// </summary>
        public void GetFeedback(Action<GetFeedbackResponse> onSuccess)
        {
            if (string.IsNullOrEmpty(currentAttemptId))
            {
                Debug.LogWarning("No attempt ID available for feedback.");
                return;
            }

            StartCoroutine(APIClient.Instance.GetAttemptFeedback(
                currentAttemptId,
                onSuccess,
                error => Debug.LogError($"Failed to get feedback: {error}")
            ));
        }

        /// <summary>
        /// Dinamik ipucu ister
        /// </summary>
        public void RequestHint(string currentState, Action<DynamicHintResponse> onSuccess)
        {
            if (!isAttemptActive)
            {
                Debug.LogWarning("No active attempt. Cannot request hint.");
                return;
            }

            DynamicHintRequest request = new DynamicHintRequest
            {
                skill_id = currentSkillId,
                current_state = currentState,
                recent_actions = new List<string>(recentActions)
            };

            StartCoroutine(APIClient.Instance.GetDynamicHint(
                request,
                onSuccess,
                error => Debug.LogError($"Failed to get hint: {error}")
            ));
        }

        /// <summary>
        /// Bağlamsal yardım ister
        /// </summary>
        public void RequestHelp(string query, Action<ContextualHelpResponse> onSuccess)
        {
            ContextualHelpRequest request = new ContextualHelpRequest
            {
                skill_id = currentSkillId,
                query = query
            };

            StartCoroutine(APIClient.Instance.GetContextualHelp(
                request,
                onSuccess,
                error => Debug.LogError($"Failed to get help: {error}")
            ));
        }

        private void OnDestroy()
        {
            // Uygulama kapandığında aktif denemeyi sonlandır
            if (isAttemptActive)
            {
                EndAttempt("abandoned");
            }
        }

        private void OnApplicationQuit()
        {
            // Uygulama kapatılırken aktif denemeyi sonlandır
            if (isAttemptActive)
            {
                EndAttempt("abandoned");
            }
        }
    }
}
