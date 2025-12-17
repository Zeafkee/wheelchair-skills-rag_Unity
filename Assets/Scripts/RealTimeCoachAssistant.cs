using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static RealtimeCoachTrigger;

/// <summary>
/// RealtimeCoachTutorial
/// - Accepts a parsed AskRAGResponse and runs step-by-step tutorial.
/// - Calls backend endpoints: start-attempt, record-input, record-error, complete.
/// - Locks/unlocks player movement if playerControllerToDisable assigned.
/// - Action detection uses actionChecks (keyboard by default). Replace with telemetry checks as needed.
/// </summary>
public class RealtimeCoachTutorial : MonoBehaviour
{
    [Header("Backend")]
    [Tooltip("Backend base url for training endpoints, e.g. http://localhost:8000")]
    public string backendBaseUrl = "http://localhost:8000";
    [Tooltip("ID of the user in backend")]
    public string userId = "sefa001";

    [Header("Player / Control")]
    [Tooltip("Optional: reference to a player controller script to enable/disable movement")]
    public MonoBehaviour playerControllerToDisable = null;

    [Header("Behavior")]
    [Tooltip("Seconds to wait for expected input before timeout (0 = infinite)")]
    public float stepTimeoutSeconds = 15f;
    [Tooltip("If true, on wrong input record_error will be sent and allow retry")]
    public bool recordErrors = true;

    // Internal state
    private string currentAttemptId = null;
    private string currentSkillId = null;
    private Step[] steps = null;
    private int currentStepIndex = 0;

    // Mapping expected_action -> check function
    private Dictionary<string, Func<bool>> actionChecks;

    private void Awake()
    {
        BuildDefaultActionChecks();
    }

    private void BuildDefaultActionChecks()
    {
        actionChecks = new Dictionary<string, Func<bool>>(StringComparer.OrdinalIgnoreCase)
        {
            { "move_forward", () => Input.GetKey(KeyCode.W) || Input.GetAxis("Vertical") > 0.1f },
            { "move_backward", () => Input.GetKey(KeyCode.S) || Input.GetAxis("Vertical") < -0.1f },
            { "turn_left", () => Input.GetKey(KeyCode.A) || Input.GetAxis("Horizontal") < -0.1f },
            { "turn_right", () => Input.GetKey(KeyCode.D) || Input.GetAxis("Horizontal") > 0.1f },
            { "lean_forward", () => Input.GetKey(KeyCode.V) },
            { "lean_backward", () => Input.GetKey(KeyCode.B) },
            { "pop_casters", () => Input.GetKey(KeyCode.X) },
            { "brake", () => Input.GetKey(KeyCode.Space) },
            { "any_move", () => Mathf.Abs(Input.GetAxis("Vertical")) > 0.05f || Mathf.Abs(Input.GetAxis("Horizontal")) > 0.05f }
        };
    }

    /// <summary>
    /// Public entrypoint. Call with parsed AskRAGResponse.
    /// </summary>
    public IEnumerator StartTutorial(AskRAGResponse ragResp)
    {
        if (ragResp == null || ragResp.steps == null || ragResp.steps.Length == 0)
        {
            Debug.LogError("[Tutorial] StartTutorial called with empty ragResp");
            yield break;
        }

        // set internal state
        steps = ragResp.steps;
        currentSkillId = ragResp.skill_id;

        // start attempt on backend
        yield return StartCoroutine(StartAttempt(userId, currentSkillId));
        if (string.IsNullOrEmpty(currentAttemptId))
        {
            Debug.LogError("[Tutorial] Failed to start attempt on backend.");
            yield break;
        }

        // disable player movement if provided
        if (playerControllerToDisable != null)
            playerControllerToDisable.enabled = false;

        // iterate steps
        for (currentStepIndex = 0; currentStepIndex < steps.Length; currentStepIndex++)
        {
            Step step = steps[currentStepIndex];
            Debug.Log($"[Tutorial] STEP #{step.step_number} - {step.text}");
            if (!string.IsNullOrEmpty(step.cue))
                Debug.Log($"[Tutorial] Cue: {step.cue}");

            bool stepSucceeded = false;
            float startTime = Time.time;
            string expectedActionForRecord = null;
            string actualActionForRecord = null;

            while (true)
            {
                // timeout
                if (stepTimeoutSeconds > 0 && Time.time - startTime > stepTimeoutSeconds)
                {
                    Debug.LogWarning($"[Tutorial] Step #{step.step_number} timed out after {stepTimeoutSeconds} seconds.");
                    break;
                }

                // check expected actions
                if (step.expected_actions != null && step.expected_actions.Length > 0)
                {
                    bool anyDetected = false;
                    foreach (var exp in step.expected_actions)
                    {
                        if (IsActionPerformed(exp))
                        {
                            anyDetected = true;
                            expectedActionForRecord = exp;
                            actualActionForRecord = exp;
                            break;
                        }
                    }

                    if (anyDetected)
                    {
                        stepSucceeded = true;
                        break;
                    }

                    // Check for wrong inputs (other mapped actions)
                    foreach (var kv in actionChecks)
                    {
                        string actionName = kv.Key;
                        if (Array.Exists(step.expected_actions, a => string.Equals(a, actionName, StringComparison.OrdinalIgnoreCase)))
                            continue; // allowed

                        if (kv.Value != null && kv.Value.Invoke())
                        {
                            actualActionForRecord = actionName;
                            expectedActionForRecord = step.expected_actions.Length > 0 ? step.expected_actions[0] : null;
                            Debug.Log($"[Tutorial] Wrong input detected: {actionName} (expected: {string.Join(",", step.expected_actions)})");

                            if (recordErrors)
                            {
                                yield return StartCoroutine(RecordInput(currentAttemptId, step.step_number, expectedActionForRecord ?? "", actualActionForRecord ?? ""));
                                yield return StartCoroutine(RecordError(currentAttemptId, step.step_number, "wrong_input", expectedActionForRecord ?? "", actualActionForRecord ?? ""));
                            }
                            // allow retry
                        }
                    }
                }
                else
                {
                    // no expected actions provided -> accept any movement
                    if (IsAnyActionPerformed())
                    {
                        stepSucceeded = true;
                        expectedActionForRecord = "any_move";
                        actualActionForRecord = "any_move";
                        break;
                    }
                }

                yield return null;
            } // waiting loop

            if (stepSucceeded)
            {
                Debug.Log($"[Tutorial] Step #{step.step_number} succeeded.");
                yield return StartCoroutine(RecordInput(currentAttemptId, step.step_number, expectedActionForRecord ?? "", actualActionForRecord ?? ""));
            }
            else
            {
                Debug.LogWarning($"[Tutorial] Step #{step.step_number} failed/timed out. Marking attempt as failed.");
                yield return StartCoroutine(CompleteAttempt(currentAttemptId, false));
                if (playerControllerToDisable != null)
                    playerControllerToDisable.enabled = true;
                yield break;
            }
        } // steps loop

        // completed all
        Debug.Log("[Tutorial] All steps completed. Marking attempt success.");
        yield return StartCoroutine(CompleteAttempt(currentAttemptId, true));

        if (playerControllerToDisable != null)
            playerControllerToDisable.enabled = true;
    }

    // ---------------- Backend helpers ----------------
    IEnumerator StartAttempt(string userId, string skillId)
    {
        string url = $"{backendBaseUrl}/user/{userId}/skill/{skillId}/start-attempt";
        var payloadJson = "{}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(payloadJson);

        UnityWebRequest r = new UnityWebRequest(url, "POST");
        r.uploadHandler = new UploadHandlerRaw(bodyRaw);
        r.downloadHandler = new DownloadHandlerBuffer();
        r.SetRequestHeader("Content-Type", "application/json");

        yield return r.SendWebRequest();

        if (r.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[Tutorial] start-attempt failed: " + r.error + " resp: " + r.downloadHandler.text);
            currentAttemptId = null;
            yield break;
        }

        string txt = r.downloadHandler.text;
        Debug.Log("[Tutorial] start-attempt resp: " + txt);

        try
        {
            var wrapper = JsonUtility.FromJson<StartAttemptResponse>(txt);
            if (wrapper != null && !string.IsNullOrEmpty(wrapper.attempt_id))
            {
                currentAttemptId = wrapper.attempt_id;
            }
            else
            {
                currentAttemptId = ManualExtractField(txt, "attempt_id");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[Tutorial] parse start-attempt response error: " + ex);
            currentAttemptId = ManualExtractField(r.downloadHandler.text, "attempt_id");
        }
    }

    [Serializable]
    private class StartAttemptResponse
    {
        public bool success;
        public string attempt_id;
        public string skill_id;
    }

    IEnumerator RecordInput(string attemptId, int stepNumber, string expectedInput, string actualInput)
    {
        string url = $"{backendBaseUrl}/attempt/{attemptId}/record-input";
        var payload = new RecordInputPayload { step_number = stepNumber, expected_input = expectedInput, actual_input = actualInput };
        string json = JsonUtility.ToJson(payload);

        UnityWebRequest r = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        r.uploadHandler = new UploadHandlerRaw(bodyRaw);
        r.downloadHandler = new DownloadHandlerBuffer();
        r.SetRequestHeader("Content-Type", "application/json");
        yield return r.SendWebRequest();

        if (r.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[Tutorial] record-input failed: {r.error} resp:{r.downloadHandler.text}");
        }
        else
        {
            Debug.Log("[Tutorial] record-input OK: " + r.downloadHandler.text);
        }
    }

    [Serializable]
    private class RecordInputPayload
    {
        public int step_number;
        public string expected_input;
        public string actual_input;
    }

    IEnumerator RecordError(string attemptId, int stepNumber, string errorType, string expectedAction, string actualAction)
    {
        string url = $"{backendBaseUrl}/attempt/{attemptId}/record-error";
        var payload = new RecordErrorPayload { step_number = stepNumber, error_type = errorType, expected_action = expectedAction, actual_action = actualAction };
        string json = JsonUtility.ToJson(payload);

        UnityWebRequest r = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        r.uploadHandler = new UploadHandlerRaw(bodyRaw);
        r.downloadHandler = new DownloadHandlerBuffer();
        r.SetRequestHeader("Content-Type", "application/json");
        yield return r.SendWebRequest();

        if (r.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[Tutorial] record-error failed: {r.error} resp:{r.downloadHandler.text}");
        }
        else
        {
            Debug.Log("[Tutorial] record-error OK: " + r.downloadHandler.text);
        }
    }

    [Serializable]
    private class RecordErrorPayload
    {
        public int step_number;
        public string error_type;
        public string expected_action;
        public string actual_action;
    }

    IEnumerator CompleteAttempt(string attemptId, bool success)
    {
        string url = $"{backendBaseUrl}/attempt/{attemptId}/complete";
        var payload = new CompleteAttemptPayload { success = success };
        string json = JsonUtility.ToJson(payload);

        UnityWebRequest r = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        r.uploadHandler = new UploadHandlerRaw(bodyRaw);
        r.downloadHandler = new DownloadHandlerBuffer();
        r.SetRequestHeader("Content-Type", "application/json");
        yield return r.SendWebRequest();

        if (r.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[Tutorial] complete attempt failed: {r.error} resp:{r.downloadHandler.text}");
        }
        else
        {
            Debug.Log("[Tutorial] complete attempt OK: " + r.downloadHandler.text);
        }
    }

    [Serializable]
    private class CompleteAttemptPayload
    {
        public bool success;
    }

    // ---------------- Utilities ----------------
    private bool IsActionPerformed(string action)
    {
        if (string.IsNullOrEmpty(action)) return false;
        if (actionChecks.TryGetValue(action, out Func<bool> checker))
        {
            try { return checker(); }
            catch { return false; }
        }
        if (actionChecks.TryGetValue("any_move", out Func<bool> anyc))
        {
            return anyc();
        }
        return false;
    }

    private bool IsAnyActionPerformed()
    {
        foreach (var kv in actionChecks)
        {
            try
            {
                if (kv.Value != null && kv.Value.Invoke())
                    return true;
            }
            catch { }
        }
        return false;
    }

    private AskRAGResponse ManualParseResponse(string json)
    {
        try
        {
            var obj = JsonUtility.FromJson<MinimalWrapper>(json);
            if (obj != null && obj.steps != null)
                return new AskRAGResponse { skill_id = obj.skill_id, steps = obj.steps };
        }
        catch { }
        return null;
    }

    [Serializable]
    private class MinimalWrapper
    {
        public string skill_id;
        public Step[] steps;
    }

    private string ManualExtractField(string json, string key)
    {
        string pattern = $"\"{key}\":\"";
        int i = json.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
        if (i < 0) return null;
        i += pattern.Length;
        int j = json.IndexOf('"', i);
        if (j < 0) return null;
        return json.Substring(i, j - i);
    }
}