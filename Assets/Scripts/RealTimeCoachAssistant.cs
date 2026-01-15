using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
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

    [Header("UI References")]
    public TextMeshProUGUI stepInstructionText;
    public TextMeshProUGUI stepCueText;
    public TextMeshProUGUI stepInputHintText;
    public TextMeshProUGUI holdProgressText;
    public Image holdProgressBar;

    [Header("Hold Settings")]
    public float requiredHoldDuration = 1.0f;
    public bool cumulativeHoldForSameAction = true;

    private string currentAttemptId = null;
    private string currentSkillId = null;
    private List<PracticeStep> steps = null;
    private int currentStepIndex = 0;

    private Dictionary<string, Func<bool>> actionChecks;
    private Dictionary<string, string> actionToKeyMsg;
    private Rigidbody rb;

    // Hold duration tracking
    private float currentHoldTime = 0f;
    private float holdStartTime = 0f;
    private string currentHoldingAction = null;
    private string previousStepAction = null;
    private float currentStepRequiredHold = 0f;

    // Static mapping for action to key names
    private static readonly Dictionary<string, string> actionToKeyName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "move_forward", "W" },
        { "move_backward", "S" },
        { "turn_left", "A" },
        { "turn_right", "D" },
        { "pop_casters", "X" },
        { "brake", "SPACE" }
    };

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
        previousStepAction = null;

        yield return StartCoroutine(StartAttempt(userId, currentSkillId));
        if (string.IsNullOrEmpty(currentAttemptId)) yield break;

        for (currentStepIndex = 0; currentStepIndex < steps.Count; currentStepIndex++)
        {
            PracticeStep step = steps[currentStepIndex];
            
            // Determine hold duration for this step
            // Check if any current expected actions match the previous action
            currentStepRequiredHold = requiredHoldDuration;
            
            if (cumulativeHoldForSameAction && !string.IsNullOrEmpty(previousStepAction) 
                && step.expected_actions != null && step.expected_actions.Count > 0)
            {
                // If any current expected action matches previous action, increase hold duration
                if (step.expected_actions.Any(action => previousStepAction.Equals(action, StringComparison.OrdinalIgnoreCase)))
                {
                    currentStepRequiredHold = requiredHoldDuration * 2f;
                }
            }
            
            // Update UI - Instruction
            if (stepInstructionText != null)
            {
                stepInstructionText.text = step.text;
            }
            
            // Update UI - Cue/Note
            if (stepCueText != null)
            {
                if (!string.IsNullOrEmpty(step.cue))
                {
                    stepCueText.text = $"💡 {step.cue}";
                }
                else
                {
                    stepCueText.text = "";
                }
            }
            
            // Update UI - Input Hint
            if (stepInputHintText != null && step.expected_actions != null && step.expected_actions.Count > 0)
            {
                string keysDisplay = string.Join(" OR ", step.expected_actions.Select(GetKeyNameForAction));
                stepInputHintText.text = $"Hold {keysDisplay} for {currentStepRequiredHold:F1}s";
            }
            
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
            if (!string.IsNullOrEmpty(step.cue))
            {
                Debug.Log($"[Tutorial] CUE: {step.cue}");
            }
            
            yield return StartCoroutine(WaitForInputRelease());

            // Reset hold tracking for new step
            ResetHoldTracking();

            bool stepSucceeded = false;
            bool inputStarted = false;
            float startTime = Time.time;
            string expectedActionForRecord = null;
            string actualActionForRecord = null;

            while (true)
            {
                if (stepTimeoutSeconds > 0 && Time.time - startTime > stepTimeoutSeconds) break;

                // 1. Check if WRONG actions are performed (before checking expected)
                foreach (var actionName in actionChecks.Keys)
                {
                    // Skip if it's one of the expected ones
                    if (step.expected_actions != null && step.expected_actions.Contains(actionName)) continue;

                    if (IsActionPerformed(actionName))
                    {
                        // Wrong action detected - IMMEDIATE FAIL
                        inputStarted = true;
                        stepSucceeded = false;
                        expectedActionForRecord = (step.expected_actions != null && step.expected_actions.Count > 0) ? step.expected_actions[0] : "unknown";
                        actualActionForRecord = actionName;
                        
                        Debug.LogWarning($"[Tutorial] WRONG ACTION: {actionName} (Expected: {expectedActionForRecord})");
                        
                        yield return StartCoroutine(RecordInput(currentAttemptId, step.step_number, expectedActionForRecord, actualActionForRecord));
                        
                        // Record error if enabled
                        if (recordErrors)
                        {
                            string errorType = DetermineErrorType(expectedActionForRecord, actualActionForRecord);
                            yield return StartCoroutine(RecordError(currentAttemptId, step.step_number, errorType, expectedActionForRecord, actualActionForRecord));
                        }
                        
                        EndTutorialSession(false);
                        yield break;
                    }
                }

                // 2. Check if expected actions are being held
                if (step.expected_actions != null)
                {
                    bool anyExpectedKeyPressed = false;
                    string pressedExpectedAction = null;
                    
                    foreach (var exp in step.expected_actions)
                    {
                        if (IsActionPerformed(exp))
                        {
                            anyExpectedKeyPressed = true;
                            pressedExpectedAction = exp;
                            break;
                        }
                    }
                    
                    if (anyExpectedKeyPressed)
                    {
                        // Start or continue holding
                        if (currentHoldingAction == null)
                        {
                            // Start holding
                            StartHoldTracking(pressedExpectedAction);
                        }
                        else if (currentHoldingAction == pressedExpectedAction)
                        {
                            // Continue holding the same key - calculate elapsed time
                            currentHoldTime = Time.time - holdStartTime;
                            
                            // Update hold progress UI
                            if (holdProgressText != null)
                            {
                                holdProgressText.text = $"Hold: {currentHoldTime:F1}s / {currentStepRequiredHold:F1}s";
                            }
                            if (holdProgressBar != null)
                            {
                                holdProgressBar.fillAmount = Mathf.Clamp01(currentHoldTime / currentStepRequiredHold);
                            }
                            
                            // Check if hold duration met
                            if (currentHoldTime >= currentStepRequiredHold)
                            {
                                inputStarted = true;
                                stepSucceeded = true;
                                expectedActionForRecord = pressedExpectedAction;
                                actualActionForRecord = pressedExpectedAction;
                                break;
                            }
                        }
                        else
                        {
                            // User switched to a different expected action - reset and start over
                            float elapsedTime = Time.time - holdStartTime;
                            Debug.Log($"[Tutorial] Action switched from {currentHoldingAction} to {pressedExpectedAction} (was at {elapsedTime:F1}s), resetting hold");
                            ResetHoldTracking();
                            StartHoldTracking(pressedExpectedAction);
                        }
                    }
                    else
                    {
                        // Key released - reset hold
                        if (currentHoldingAction != null)
                        {
                            float elapsedTime = Time.time - holdStartTime;
                            Debug.Log($"[Tutorial] Key released (was at {elapsedTime:F1}s), resetting hold");
                            ResetHoldTracking();
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
                
                // Store the action for next step's cumulative check
                previousStepAction = actualActionForRecord;
            }
            else
            {
                EndTutorialSession(false);
                yield break;
            }
        }
        ResetTextes();
        if(stepInstructionText != null)
        {
            stepInstructionText.text = "Tutorial Completed! Well done!";
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

    IEnumerator RecordError(string attemptId, int stepNumber, string errorType, string expectedAction, string actualAction)
    {
        string url = backendBaseUrl + "/attempt/" + attemptId + "/record-error";
        
        // Escape strings to prevent JSON injection
        string json = "{" +
            "\"step_number\": " + stepNumber + ", " +
            "\"error_type\": \"" + EscapeJsonString(errorType) + "\", " +
            "\"expected_action\": \"" + EscapeJsonString(expectedAction) + "\", " +
            "\"actual_action\": \"" + EscapeJsonString(actualAction) + "\"" +
        "}";
        
        using (UnityWebRequest r = new UnityWebRequest(url, "POST"))
        {
            r.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            r.downloadHandler = new DownloadHandlerBuffer();
            r.SetRequestHeader("Content-Type", "application/json");
            yield return r.SendWebRequest();
            
            if (r.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[Tutorial] Error recorded: {errorType}");
                ResetTextes();
                if(stepInstructionText != null)
                {
                    stepInstructionText.text = $"Error Recorded: {errorType.Replace("_", " ")}. Please try again.";
                }
            }
            else
            {
                Debug.LogWarning($"[Tutorial] Failed to record error: {r.error}");
            }
        }
    }
    
    private string EscapeJsonString(string str)
    {
        if (string.IsNullOrEmpty(str)) return str;
        return str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
    }

    private string DetermineErrorType(string expected, string actual)
    {
        // Normalize to lowercase for comparison
        string exp = expected.ToLower();
        string act = actual.ToLower();
        
        // wrong_direction: move_forward <-> move_backward
        if ((exp == "move_forward" && act == "move_backward") || 
            (exp == "move_backward" && act == "move_forward"))
        {
            return "wrong_direction";
        }
        
        // wrong_turn_direction: turn_left <-> turn_right
        if ((exp == "turn_left" && act == "turn_right") || 
            (exp == "turn_right" && act == "turn_left"))
        {
            return "wrong_turn_direction";
        }
        
        // stopped_instead_of_moving: expected move, got brake
        if ((exp == "move_forward" || exp == "move_backward" || exp == "turn_left" || exp == "turn_right") && 
            act == "brake")
        {
            return "stopped_instead_of_moving";
        }
        
        // moved_instead_of_stopping: expected brake, got move
        if (exp == "brake" && 
            (act == "move_forward" || act == "move_backward" || act == "turn_left" || act == "turn_right"))
        {
            return "moved_instead_of_stopping";
        }
        
        // missed_pop_casters: expected pop_casters, got something else
        if (exp == "pop_casters" && act != "pop_casters")
        {
            return "missed_pop_casters";
        }
        
        // wrong_input: default case
        return "wrong_input";
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

    private string GetKeyNameForAction(string action)
    {
        if (actionToKeyName.TryGetValue(action, out string keyName))
        {
            return keyName;
        }
        return action;
    }

    private void ResetHoldTracking()
    {
        currentHoldTime = 0f;
        holdStartTime = 0f;
        currentHoldingAction = null;
        
        // Reset UI
        if (holdProgressText != null)
        {
            holdProgressText.text = "";
        }
        if (holdProgressBar != null)
        {
            holdProgressBar.fillAmount = 0f;
        }
    }

    private void StartHoldTracking(string action)
    {
        currentHoldingAction = action;
        holdStartTime = Time.time;
    }

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
    public void ResetTextes()
    {
        

        if (holdProgressText != null)
        {
            holdProgressText.text = "";
        }
        if (stepInstructionText != null)
        {
            stepInstructionText.text = "";
        }
        if (stepCueText != null)
        {
            stepCueText.text = "";
        }
        if (stepInputHintText != null)
        {
            stepInputHintText.text = "";
        }


    }
}