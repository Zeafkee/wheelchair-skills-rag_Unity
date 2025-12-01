using System;
using System.Collections.Generic;
using UnityEngine;
using WheelchairSkills.API;

namespace WheelchairSkills.Training
{
    /// <summary>
    /// Singleton tracker for managing skill training attempts
    /// Tracks attempt state, validates inputs, and communicates with backend
    /// </summary>
    public class SkillAttemptTracker : MonoBehaviour
    {
        private static SkillAttemptTracker _instance;
        
        public static SkillAttemptTracker Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("SkillAttemptTracker");
                    _instance = go.AddComponent<SkillAttemptTracker>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        // Events for UI updates
        public event Action<string> OnFeedbackReceived;
        public event Action<int> OnStepChanged;
        public event Action<bool, PerformanceData> OnAttemptCompleted;
        public event Action<string> OnError;

        // Attempt state
        private bool isAttemptActive = false;
        private string currentAttemptId;
        private string currentUserId;
        private string currentSkillName;
        private int currentStepIndex = 0;
        private float attemptStartTime;
        private int errorsCount = 0;
        private List<string> recordedInputs = new List<string>();
        
        // Step data
        private StepData[] steps;

        // Properties
        public bool IsAttemptActive => isAttemptActive;
        public int CurrentStepIndex => currentStepIndex;
        public int TotalSteps => steps?.Length ?? 0;
        public string CurrentSkillName => currentSkillName;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Start a new skill training attempt
        /// </summary>
        public void StartSkillAttempt(string userId, string skillName)
        {
            if (isAttemptActive)
            {
                Debug.LogWarning("Attempt already active. Complete current attempt first.");
                return;
            }

            currentUserId = userId;
            currentSkillName = skillName;

            APIClient.Instance.StartSkillAttempt(userId, skillName,
                onSuccess: (response) =>
                {
                    currentAttemptId = response.attempt_id;
                    steps = response.steps;
                    currentStepIndex = 0;
                    errorsCount = 0;
                    recordedInputs.Clear();
                    isAttemptActive = true;
                    attemptStartTime = Time.time;
                    
                    Debug.Log($"Skill attempt started: {response.attempt_id}");
                    OnStepChanged?.Invoke(currentStepIndex);
                },
                onError: (error) =>
                {
                    Debug.LogError($"Failed to start skill attempt: {error}");
                    OnError?.Invoke($"Beceri denemesi başlatılamadı: {error}");
                }
            );
        }

        /// <summary>
        /// Process user input during active attempt
        /// </summary>
        public void ProcessInput(KeyCode key)
        {
            if (!isAttemptActive)
            {
                return;
            }

            string action = InputMapping.GetAction(key);
            if (action == null)
            {
                return; // Key not mapped
            }

            string actionDescription = InputMapping.GetDescription(action);
            float timestamp = Time.time - attemptStartTime;

            // Record the input
            recordedInputs.Add(action);

            // Send to backend
            APIClient.Instance.RecordInput(
                currentAttemptId,
                action,
                actionDescription,
                timestamp,
                currentStepIndex,
                onSuccess: (response) =>
                {
                    OnFeedbackReceived?.Invoke(response.feedback);
                    
                    if (response.step_completed)
                    {
                        currentStepIndex = response.next_step;
                        OnStepChanged?.Invoke(currentStepIndex);
                        
                        // Check if all steps completed
                        if (currentStepIndex >= steps.Length)
                        {
                            CompleteAttempt(true);
                        }
                    }
                    else if (!response.success)
                    {
                        errorsCount++;
                    }
                },
                onError: (error) =>
                {
                    Debug.LogError($"Failed to record input: {error}");
                    errorsCount++;
                }
            );
        }

        /// <summary>
        /// Complete the current attempt
        /// </summary>
        public void CompleteAttempt(bool success)
        {
            if (!isAttemptActive)
            {
                return;
            }

            float completionTime = Time.time - attemptStartTime;
            int stepsCompleted = currentStepIndex;

            APIClient.Instance.CompleteSkillAttempt(
                currentAttemptId,
                success,
                completionTime,
                stepsCompleted,
                errorsCount,
                onSuccess: (response) =>
                {
                    Debug.Log($"Attempt completed: {response.message}");
                    OnAttemptCompleted?.Invoke(success, response.performance);
                    ResetAttemptState();
                },
                onError: (error) =>
                {
                    Debug.LogError($"Failed to complete attempt: {error}");
                    OnError?.Invoke($"Deneme tamamlanamadı: {error}");
                    ResetAttemptState();
                }
            );
        }

        /// <summary>
        /// Cancel the current attempt
        /// </summary>
        public void CancelAttempt()
        {
            if (isAttemptActive)
            {
                CompleteAttempt(false);
            }
        }

        /// <summary>
        /// Get current step information
        /// </summary>
        public StepData GetCurrentStep()
        {
            if (steps != null && currentStepIndex < steps.Length)
            {
                return steps[currentStepIndex];
            }
            return null;
        }

        /// <summary>
        /// Get all steps for current skill
        /// </summary>
        public StepData[] GetAllSteps()
        {
            return steps;
        }

        /// <summary>
        /// Reset attempt state
        /// </summary>
        private void ResetAttemptState()
        {
            isAttemptActive = false;
            currentAttemptId = null;
            currentStepIndex = 0;
            errorsCount = 0;
            recordedInputs.Clear();
            steps = null;
        }

        /// <summary>
        /// Get attempt statistics
        /// </summary>
        public (int stepsCompleted, int errorsCount, float elapsedTime) GetAttemptStats()
        {
            float elapsedTime = isAttemptActive ? Time.time - attemptStartTime : 0f;
            return (currentStepIndex, errorsCount, elapsedTime);
        }
    }
}
