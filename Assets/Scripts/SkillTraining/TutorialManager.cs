using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum TutorialActionType { WaitForStop, PopCasters, DriveForward }

[Serializable]
public class TutorialStep
{
    public string instruction;
    public TutorialActionType actionType;
    public float timeoutSeconds;
    public float targetDistance; // used for DriveForward
    // runtime fields:
    [NonSerialized] public float startedAt;
    [NonSerialized] public bool succeeded;
    [NonSerialized] public StepTelemetry telemetry;
    public TutorialStep(string ins, TutorialActionType t, float timeout = 5f, float targetDist = 0f)
    {
        instruction = ins;
        actionType = t;
        timeoutSeconds = timeout;
        targetDistance = targetDist;
        telemetry = null;
    }
}

[Serializable]
public class StepTelemetry
{
    public int stepNumber;
    public string expectedAction;
    public string actualAction;
    public bool success;
    public float holdDuration;
    public float peakForce;
    public float distance;
    public bool assistUsed;
    public string timestamp;
}

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("UI")]
    public GameObject tutorialPanel;
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI hintText;
    public float driveMaxHoldTime = 3f;

    private WheelchairController player;
    private List<TutorialStep> steps;
    private int stepIndex;
    private bool active = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void StartCurbTutorial(WheelchairController wc)
    {
        if (active) return;
        player = wc;
        player.SetPlayerEnabled(false);
        // Example sequence (adjust or load from backend)
        steps = new List<TutorialStep> {
            new TutorialStep("Stop and hold position", TutorialActionType.WaitForStop, 4f),
            new TutorialStep("Pop casters: press X", TutorialActionType.PopCasters, 4f),
            new TutorialStep("Drive forward onto curb: hold W", TutorialActionType.DriveForward, 6f, 0.6f)
        };
        stepIndex = 0;
        tutorialPanel.SetActive(true);
        active = true;
        StartStep();
    }

    private void StartStep()
    {
        var s = steps[stepIndex];
        s.startedAt = Time.time;
        s.succeeded = false;
        instructionText.text = s.instruction;
        hintText.text = "";
        // if WaitForStop, ensure player stops
        if (s.actionType == TutorialActionType.WaitForStop)
        {
            StartCoroutine(WaitForStopCoroutine(s));
        }
        else
        {
            // normal waiting; input events will call OnActionPerformed
        }
    }

    IEnumerator WaitForStopCoroutine(TutorialStep s)
    {
        float end = Time.time + s.timeoutSeconds;
        while (Time.time < end)
        {
            if (player.IsStationary())
            {
                OnStepSucceeded("stop", 0f, 0f, 0f, false);
                yield break;
            }
            yield return null;
        }
        OnStepFailed("stop");
    }

    // Called by InputRecorder or WheelchairController when user performs an action
    public void OnActionPerformed(TutorialActionType actionType, float holdDuration = 0f, float peakForce = 0f, float distance = 0f, bool assistUsed = false)
    {
        if (!active) return;
        var s = steps[stepIndex];
        // Validate according to step
        bool ok = false;
        string actualAction = actionType.ToString();
        switch (s.actionType)
        {
            case TutorialActionType.PopCasters:
                ok = (actionType == TutorialActionType.PopCasters) && player.AreFrontCastersLifted(0.05f);
                break;
            case TutorialActionType.DriveForward:
                ok = (actionType == TutorialActionType.DriveForward) && distance >= s.targetDistance;
                break;
            default:
                ok = false;
                break;
        }

        if (ok)
        {
            OnStepSucceeded(actualAction, holdDuration, peakForce, distance, assistUsed);
        }
        else
        {
            // allow retry until timeout
            float elapsed = Time.time - s.startedAt;
            if (elapsed > s.timeoutSeconds) OnStepFailed(actualAction);
            else
            {
                hintText.text = "Try again or increase hold";
                // still record attempt (telemetry)
                s.telemetry = new StepTelemetry
                {
                    stepNumber = stepIndex + 1,
                    expectedAction = s.actionType.ToString(),
                    actualAction = actualAction,
                    success = false,
                    holdDuration = holdDuration,
                    peakForce = peakForce,
                    distance = distance,
                    assistUsed = assistUsed,
                    timestamp = DateTime.UtcNow.ToString("o")
                };
                StartCoroutine(SendStepTelemetry(s.telemetry));
            }
        }
    }

    private void OnStepSucceeded(string actualAction, float holdDuration, float peakForce, float distance, bool assistUsed)
    {
        var s = steps[stepIndex];
        s.succeeded = true;
        s.telemetry = new StepTelemetry
        {
            stepNumber = stepIndex + 1,
            expectedAction = s.actionType.ToString(),
            actualAction = actualAction,
            success = true,
            holdDuration = holdDuration,
            peakForce = peakForce,
            distance = distance,
            assistUsed = assistUsed,
            timestamp = DateTime.UtcNow.ToString("o")
        };
        StartCoroutine(SendStepTelemetry(s.telemetry));
        // progress
        stepIndex++;
        if (stepIndex < steps.Count) StartStep();
        else FinishTutorial(true);
    }

    private void OnStepFailed(string actualAction)
    {
        var s = steps[stepIndex];
        s.succeeded = false;
        s.telemetry = new StepTelemetry
        {
            stepNumber = stepIndex + 1,
            expectedAction = s.actionType.ToString(),
            actualAction = actualAction,
            success = false,
            holdDuration = 0f,
            peakForce = 0f,
            distance = 0f,
            assistUsed = false,
            timestamp = DateTime.UtcNow.ToString("o")
        };
        StartCoroutine(SendStepTelemetry(s.telemetry));
        FinishTutorial(false);
    }

    IEnumerator SendStepTelemetry(StepTelemetry data)
    {
        // uses TelemetryApi (below file) to send; retries can be added
        if (player == null || string.IsNullOrEmpty(player.CurrentAttemptId)) yield break;
        yield return TelemetryAPI.SendStepSummary(player.CurrentAttemptId, data, (ok) => {
            // optional callback
        }, (err) => {
            Debug.LogWarning("Telemetry send failed: " + err);
        });
    }

    private void FinishTutorial(bool success)
    {
        tutorialPanel.SetActive(false);
        player.SetPlayerEnabled(true);
        active = false;
        // complete attempt on backend
        if (!string.IsNullOrEmpty(player.CurrentAttemptId))
        {
            StartCoroutine(TelemetryAPI.CompleteAttemptCoroutine(player.CurrentAttemptId, success, (ok) => {
                Debug.Log("Attempt completed sent to backend");
            }, (err) => Debug.LogWarning(err)));
        }
    }
}