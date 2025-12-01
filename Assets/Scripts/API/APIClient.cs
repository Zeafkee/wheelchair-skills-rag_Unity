using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace WheelchairSkills.API
{
    /// <summary>
    /// Singleton API client for communicating with RAG backend
    /// Uses UnityWebRequest for HTTP communication
    /// </summary>
    public class APIClient : MonoBehaviour
    {
        private static APIClient _instance;
        
        public static APIClient Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("APIClient");
                    _instance = go.AddComponent<APIClient>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

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
        /// Generic GET request
        /// </summary>
        public IEnumerator Get<T>(string url, Action<T> onSuccess, Action<string> onError)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Content-Type", "application/json");
                
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        T response = JsonUtility.FromJson<T>(request.downloadHandler.text);
                        onSuccess?.Invoke(response);
                    }
                    catch (Exception e)
                    {
                        onError?.Invoke($"JSON Parse Error: {e.Message}");
                    }
                }
                else
                {
                    onError?.Invoke($"Request Error: {request.error}");
                }
            }
        }

        /// <summary>
        /// Generic POST request
        /// </summary>
        public IEnumerator Post<TRequest, TResponse>(string url, TRequest data, Action<TResponse> onSuccess, Action<string> onError)
        {
            string jsonData = JsonUtility.ToJson(data);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        TResponse response = JsonUtility.FromJson<TResponse>(request.downloadHandler.text);
                        onSuccess?.Invoke(response);
                    }
                    catch (Exception e)
                    {
                        onError?.Invoke($"JSON Parse Error: {e.Message}");
                    }
                }
                else
                {
                    onError?.Invoke($"Request Error: {request.error}");
                }
            }
        }

        /// <summary>
        /// Create a new user
        /// </summary>
        public void CreateUser(string username, string email, Action<UserResponse> onSuccess, Action<string> onError)
        {
            var request = new CreateUserRequest { username = username, email = email };
            string url = APIEndpoints.Format(APIEndpoints.CREATE_USER);
            StartCoroutine(Post<CreateUserRequest, UserResponse>(url, request, onSuccess, onError));
        }

        /// <summary>
        /// Get user information
        /// </summary>
        public void GetUser(string userId, Action<UserResponse> onSuccess, Action<string> onError)
        {
            string url = APIEndpoints.Format(APIEndpoints.GET_USER, userId);
            StartCoroutine(Get<UserResponse>(url, onSuccess, onError));
        }

        /// <summary>
        /// Get user progress
        /// </summary>
        public void GetUserProgress(string userId, Action<UserProgress> onSuccess, Action<string> onError)
        {
            string url = APIEndpoints.Format(APIEndpoints.GET_USER_PROGRESS, userId);
            StartCoroutine(Get<UserProgress>(url, onSuccess, onError));
        }

        /// <summary>
        /// Start a new skill attempt
        /// </summary>
        public void StartSkillAttempt(string userId, string skillName, Action<StartAttemptResponse> onSuccess, Action<string> onError)
        {
            var request = new StartAttemptRequest { user_id = userId, skill_name = skillName };
            string url = APIEndpoints.Format(APIEndpoints.START_SKILL_ATTEMPT);
            StartCoroutine(Post<StartAttemptRequest, StartAttemptResponse>(url, request, onSuccess, onError));
        }

        /// <summary>
        /// Record user input during skill attempt
        /// </summary>
        public void RecordInput(string attemptId, string action, string actionDescription, float timestamp, int currentStep, 
            Action<RecordInputResponse> onSuccess, Action<string> onError)
        {
            var request = new RecordInputRequest 
            { 
                action = action, 
                action_description = actionDescription,
                timestamp = timestamp,
                current_step = currentStep
            };
            string url = APIEndpoints.Format(APIEndpoints.RECORD_INPUT, attemptId);
            StartCoroutine(Post<RecordInputRequest, RecordInputResponse>(url, request, onSuccess, onError));
        }

        /// <summary>
        /// Complete a skill attempt
        /// </summary>
        public void CompleteSkillAttempt(string attemptId, bool success, float completionTime, int stepsCompleted, int errorsCount,
            Action<CompleteAttemptResponse> onSuccess, Action<string> onError)
        {
            var request = new CompleteAttemptRequest
            {
                success = success,
                completion_time = completionTime,
                steps_completed = stepsCompleted,
                errors_count = errorsCount
            };
            string url = APIEndpoints.Format(APIEndpoints.COMPLETE_SKILL_ATTEMPT, attemptId);
            StartCoroutine(Post<CompleteAttemptRequest, CompleteAttemptResponse>(url, request, onSuccess, onError));
        }

        /// <summary>
        /// Get skill attempt details
        /// </summary>
        public void GetSkillAttempt(string attemptId, Action<SkillAttemptResponse> onSuccess, Action<string> onError)
        {
            string url = APIEndpoints.Format(APIEndpoints.GET_SKILL_ATTEMPT, attemptId);
            StartCoroutine(Get<SkillAttemptResponse>(url, onSuccess, onError));
        }

        /// <summary>
        /// Get training plan from RAG
        /// </summary>
        public void GetTrainingPlan(string userId, Action<TrainingPlan> onSuccess, Action<string> onError)
        {
            var request = new TrainingPlanRequest { user_id = userId };
            string url = APIEndpoints.Format(APIEndpoints.GET_TRAINING_PLAN);
            StartCoroutine(Post<TrainingPlanRequest, TrainingPlan>(url, request, onSuccess, onError));
        }

        /// <summary>
        /// Get skill guidance from RAG
        /// </summary>
        public void GetSkillGuidance(string skillName, string userContext, Action<SkillGuidanceResponse> onSuccess, Action<string> onError)
        {
            var request = new SkillGuidanceRequest { skill_name = skillName, user_context = userContext };
            string url = APIEndpoints.Format(APIEndpoints.GET_SKILL_GUIDANCE);
            StartCoroutine(Post<SkillGuidanceRequest, SkillGuidanceResponse>(url, request, onSuccess, onError));
        }

        /// <summary>
        /// Analyze performance using RAG
        /// </summary>
        public void AnalyzePerformance(string attemptId, Action<AnalyzePerformanceResponse> onSuccess, Action<string> onError)
        {
            var request = new AnalyzePerformanceRequest { attempt_id = attemptId };
            string url = APIEndpoints.Format(APIEndpoints.ANALYZE_PERFORMANCE);
            StartCoroutine(Post<AnalyzePerformanceRequest, AnalyzePerformanceResponse>(url, request, onSuccess, onError));
        }
    }
}
