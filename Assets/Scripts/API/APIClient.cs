using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace WheelchairSkills.API
{
    /// <summary>
    /// UnityWebRequest kullanan HTTP istemcisi (Singleton pattern)
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

        #region GET Requests

        public IEnumerator GetSkills(Action<SkillsResponse> onSuccess, Action<string> onError)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(APIEndpoints.GetSkillsEndpoint))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        SkillsResponse response = JsonUtility.FromJson<SkillsResponse>(request.downloadHandler.text);
                        onSuccess?.Invoke(response);
                    }
                    catch (Exception ex)
                    {
                        onError?.Invoke($"JSON parse error: {ex.Message}");
                    }
                }
                else
                {
                    onError?.Invoke($"Request failed: {request.error}");
                }
            }
        }

        public IEnumerator GetSkillById(string skillId, Action<Skill> onSuccess, Action<string> onError)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(APIEndpoints.GetSkillByIdEndpoint(skillId)))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        Skill response = JsonUtility.FromJson<Skill>(request.downloadHandler.text);
                        onSuccess?.Invoke(response);
                    }
                    catch (Exception ex)
                    {
                        onError?.Invoke($"JSON parse error: {ex.Message}");
                    }
                }
                else
                {
                    onError?.Invoke($"Request failed: {request.error}");
                }
            }
        }

        public IEnumerator GetAttemptFeedback(string attemptId, Action<GetFeedbackResponse> onSuccess, Action<string> onError)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(APIEndpoints.GetAttemptFeedbackEndpoint(attemptId)))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        GetFeedbackResponse response = JsonUtility.FromJson<GetFeedbackResponse>(request.downloadHandler.text);
                        onSuccess?.Invoke(response);
                    }
                    catch (Exception ex)
                    {
                        onError?.Invoke($"JSON parse error: {ex.Message}");
                    }
                }
                else
                {
                    onError?.Invoke($"Request failed: {request.error}");
                }
            }
        }

        #endregion

        #region POST Requests

        public IEnumerator StartAttempt(StartAttemptRequest requestData, Action<StartAttemptResponse> onSuccess, Action<string> onError)
        {
            string jsonData = JsonUtility.ToJson(requestData);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

            using (UnityWebRequest request = new UnityWebRequest(APIEndpoints.StartAttemptEndpoint, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        StartAttemptResponse response = JsonUtility.FromJson<StartAttemptResponse>(request.downloadHandler.text);
                        onSuccess?.Invoke(response);
                    }
                    catch (Exception ex)
                    {
                        onError?.Invoke($"JSON parse error: {ex.Message}");
                    }
                }
                else
                {
                    onError?.Invoke($"Request failed: {request.error}");
                }
            }
        }

        public IEnumerator RecordInput(RecordInputRequest requestData, Action<RecordInputResponse> onSuccess, Action<string> onError)
        {
            string jsonData = JsonUtility.ToJson(requestData);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

            using (UnityWebRequest request = new UnityWebRequest(APIEndpoints.RecordInputEndpoint, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        RecordInputResponse response = JsonUtility.FromJson<RecordInputResponse>(request.downloadHandler.text);
                        onSuccess?.Invoke(response);
                    }
                    catch (Exception ex)
                    {
                        onError?.Invoke($"JSON parse error: {ex.Message}");
                    }
                }
                else
                {
                    onError?.Invoke($"Request failed: {request.error}");
                }
            }
        }

        public IEnumerator EndAttempt(EndAttemptRequest requestData, Action<EndAttemptResponse> onSuccess, Action<string> onError)
        {
            string jsonData = JsonUtility.ToJson(requestData);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

            using (UnityWebRequest request = new UnityWebRequest(APIEndpoints.EndAttemptEndpoint, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        EndAttemptResponse response = JsonUtility.FromJson<EndAttemptResponse>(request.downloadHandler.text);
                        onSuccess?.Invoke(response);
                    }
                    catch (Exception ex)
                    {
                        onError?.Invoke($"JSON parse error: {ex.Message}");
                    }
                }
                else
                {
                    onError?.Invoke($"Request failed: {request.error}");
                }
            }
        }

        public IEnumerator GetDynamicHint(DynamicHintRequest requestData, Action<DynamicHintResponse> onSuccess, Action<string> onError)
        {
            string jsonData = JsonUtility.ToJson(requestData);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

            using (UnityWebRequest request = new UnityWebRequest(APIEndpoints.GetDynamicHintEndpoint, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        DynamicHintResponse response = JsonUtility.FromJson<DynamicHintResponse>(request.downloadHandler.text);
                        onSuccess?.Invoke(response);
                    }
                    catch (Exception ex)
                    {
                        onError?.Invoke($"JSON parse error: {ex.Message}");
                    }
                }
                else
                {
                    onError?.Invoke($"Request failed: {request.error}");
                }
            }
        }

        public IEnumerator GetContextualHelp(ContextualHelpRequest requestData, Action<ContextualHelpResponse> onSuccess, Action<string> onError)
        {
            string jsonData = JsonUtility.ToJson(requestData);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

            using (UnityWebRequest request = new UnityWebRequest(APIEndpoints.GetContextualHelpEndpoint, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        ContextualHelpResponse response = JsonUtility.FromJson<ContextualHelpResponse>(request.downloadHandler.text);
                        onSuccess?.Invoke(response);
                    }
                    catch (Exception ex)
                    {
                        onError?.Invoke($"JSON parse error: {ex.Message}");
                    }
                }
                else
                {
                    onError?.Invoke($"Request failed: {request.error}");
                }
            }
        }

        #endregion
    }
}
