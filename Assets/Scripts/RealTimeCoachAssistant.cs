using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using WheelchairSkills.API;

public class RealtimeCoachTutorial : MonoBehaviour
{
    [Header("Backend")]
    public string backendBaseUrl = "http://localhost:8000";
    public string userId = "sefa001";

    [Header("Player / Control")]
    public WheelchairController wheelchair;
    public MonoBehaviour playerControllerToDisable = null;

    [Header("Kinematic Settings")]
    public float moveSpeed = 2f;
    public float turnSpeed = 80f;
    public float rotateSpeed = 120f;
    public float popAngle = -30f;

    [Header("Behavior")]
    public float stepTimeoutSeconds = 15f;
    public bool recordErrors = true;

    private string currentAttemptId = null;
    private string currentSkillId = null;
    private List<PracticeStep> steps = null;
    private int currentStepIndex = 0;

    private Dictionary<string, Func<bool>> actionChecks;
    private Dictionary<string, string> actionToKeyMsg;
    private Rigidbody rb;

    private void Awake()
    {
        if (wheelchair == null)
            wheelchair = GameObject.FindFirstObjectByType<WheelchairController>();
        
        if (wheelchair != null)
        {
            rb = wheelchair.rb;
        }

        BuildDefaultActionChecks();
        BuildActionDescriptions();
    }

    private void BuildActionDescriptions()
    {
        actionToKeyMsg = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "move_forward", "Press W" },
            { "move_backward", "Press S" },
            { "turn_left", "Press A" },
            { "turn_right", "Press D" },
            { "pop_casters", "Press X" },
            { "brake", "Press SPACE" }
        };
    }

    private void BuildDefaultActionChecks()
    {
        actionChecks = new Dictionary<string, Func<bool>>(StringComparer.OrdinalIgnoreCase)
        {
            { "move_forward", () => Input.GetKey(KeyCode.W) },
            { "move_backward", () => Input.GetKey(KeyCode.S) },
            { "turn_left", () => Input.GetKey(KeyCode.A) },
            { "turn_right", () => Input.GetKey(KeyCode.D) },
            { "pop_casters", () => Input.GetKey(KeyCode.X) },
            { "brake", () => Input.GetKey(KeyCode.Space) }
        };
    }

    public IEnumerator StartTutorial(AskPracticeResponse ragResp)
    {
        if (ragResp == null || ragResp.steps == null || ragResp.steps.Count == 0) yield break;
        
        steps = ragResp.steps;
        currentSkillId = ragResp.skill_id;

        yield return StartCoroutine(StartAttempt(userId, currentSkillId));
        if (string.IsNullOrEmpty(currentAttemptId)) yield break;

        for (currentStepIndex = 0; currentStepIndex < steps.Count; currentStepIndex++)
        {
            PracticeStep step = steps[currentStepIndex];
            
            string inputHint = "";
            if (step.expected_actions != null && step.expected_actions.Count > 0)
            {
                List<string> keys = new List<string>();
                foreach(var act in step.expected_actions)
                {
                    if (actionToKeyMsg.TryGetValue(act, out string msg)) keys.Add(msg);
                    else keys.Add(act);
                }
                inputHint = " -> [ " + string.Join(" OR ", keys) + " ]";
            }
            Debug.Log($"[Tutorial] STEP #{step.step_number}: {step.text}{inputHint}");
            
            yield return StartCoroutine(WaitForInputRelease());

            bool stepSucceeded = false;
            bool inputStarted = false;
            float startTime = Time.time;
            string expectedActionForRecord = null;
            string actualActionForRecord = null;

            while (true)
            {
                if (stepTimeoutSeconds > 0 && Time.time - startTime > stepTimeoutSeconds) break;

                bool performingExpected = false;
                if (step.expected_actions != null)
                {
                    foreach (var exp in step.expected_actions)
                    {
                        if (IsActionPerformed(exp))
                        {
                            performingExpected = true;
                            inputStarted = true;
                            stepSucceeded = true;
                            expectedActionForRecord = exp;
                            actualActionForRecord = exp;
                            break;
                        }
                    }
                }

                if (stepSucceeded) break;
                yield return null;
            }

            if (stepSucceeded)
            {
                Debug.Log($"[Tutorial] Step #{step.step_number} complete!");
                yield return StartCoroutine(RecordInput(currentAttemptId, step.step_number, expectedActionForRecord, actualActionForRecord));
            }
            else
            {
                EndTutorialSession(false);
                yield break;
            }
        }

        Debug.Log("[Tutorial] Skill attempt completed successfully!");
        EndTutorialSession(true);
    }

    private void EndTutorialSession(bool success)
    {
        StartCoroutine(CompleteAttempt(currentAttemptId, success));
    }

    IEnumerator StartAttempt(string uId, string sId)
    {
        string url = backendBaseUrl + "/user/" + uId + "/skill/" + sId + "/start-attempt";
        using (UnityWebRequest r = new UnityWebRequest(url, "POST"))
        {
            r.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes("{}"));
            r.downloadHandler = new DownloadHandlerBuffer();
            r.SetRequestHeader("Content-Type", "application/json");
            yield return r.SendWebRequest();
            if (r.result == UnityWebRequest.Result.Success)
                currentAttemptId = ManualExtractField(r.downloadHandler.text, "attempt_id");
        }
    }

    IEnumerator RecordInput(string attemptId, int stepNumber, string expectedInput, string actualInput)
    {
        string url = backendBaseUrl + "/attempt/" + attemptId + "/record-input";
        string json = "{\"step_number\": " + stepNumber + ", \"expected_input\": \"" + expectedInput + "\", \"actual_input\": \"" + actualInput + "\"}";
        using (UnityWebRequest r = new UnityWebRequest(url, "POST"))
        {
            r.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            r.downloadHandler = new DownloadHandlerBuffer();
            r.SetRequestHeader("Content-Type", "application/json");
            yield return r.SendWebRequest();
        }
    }

    IEnumerator CompleteAttempt(string attemptId, bool success)
    {
        if (string.IsNullOrEmpty(attemptId)) yield break;
        string url = backendBaseUrl + "/attempt/" + attemptId + "/complete";
        string json = "{\"success\": " + (success ? "true" : "false") + "}";
        using (UnityWebRequest r = new UnityWebRequest(url, "POST"))
        {
            r.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            r.downloadHandler = new DownloadHandlerBuffer();
            r.SetRequestHeader("Content-Type", "application/json");
            yield return r.SendWebRequest();
        }
    }

    private bool IsActionPerformed(string action) => actionChecks.ContainsKey(action) && actionChecks[action]();

    private IEnumerator WaitForInputRelease()
    {
        while (Input.anyKey) yield return null;
        yield return new WaitForSeconds(0.1f);
    }

    private string ManualExtractField(string json, string key)
    {
        char q = (char)34;
        string pattern = q + key + q + ":" + q;
        int i = json.IndexOf(pattern);
        if (i < 0) return "";
        i += pattern.Length;
        int j = json.IndexOf(q, i);
        return j > i ? json.Substring(i, j - i) : "";
    }
}