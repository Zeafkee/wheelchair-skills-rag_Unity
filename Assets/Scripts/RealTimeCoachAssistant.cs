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

    [Header("Rotation Settings")]
    public float rotateSpeed = 120f;
    public float popAngle = -30f;   // X basılıyken
    public float uprightAngle = 0f; // V basılıyken

    [Header("Behavior")]
    public float stepTimeoutSeconds = 20f;

    private string currentAttemptId;
    private string currentSkillId;
    private List<PracticeStep> steps;
    private int currentStepIndex;

    private Dictionary<string, Func<bool>> actionChecks;

    private Rigidbody rb;
    private RigidbodyConstraints originalConstraints;

    // ======================= UNITY =======================

    private void Awake()
    {
        if (wheelchair == null)
            wheelchair = FindFirstObjectByType<WheelchairController>();

        rb = wheelchair.rb;

        // Tutorial boyunca Y ve Z kilitli olacak
        originalConstraints = RigidbodyConstraints.FreezeRotationX |
                              RigidbodyConstraints.FreezeRotationY |
                              RigidbodyConstraints.FreezeRotationZ;

        rb.constraints = originalConstraints;

        BuildDefaultActionChecks();
    }

    private void BuildDefaultActionChecks()
    {
        actionChecks = new Dictionary<string, Func<bool>>(StringComparer.OrdinalIgnoreCase)
        {
            { "move_forward", () => Input.GetKey(KeyCode.W) },
            { "move_backward", () => Input.GetKey(KeyCode.S) },
            { "turn_left", () => Input.GetKey(KeyCode.A) },
            { "turn_right", () => Input.GetKey(KeyCode.D) },
            { "lean_forward", () => Input.GetKey(KeyCode.V) },
            { "lean_backward", () => Input.GetKey(KeyCode.B) },
            { "pop_casters", () => Input.GetKey(KeyCode.X) },
            { "brake", () => Input.GetKey(KeyCode.Space) }
        };
    }

    // ======================= ROTATION =======================

    private void AllowOnlyXRotation()
    {
        rb.constraints =
            RigidbodyConstraints.FreezeRotationY |
            RigidbodyConstraints.FreezeRotationZ;
    }

    private void LockAllRotations()
    {
        rb.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationY |
            RigidbodyConstraints.FreezeRotationZ;
    }

    private void RotateXTo(float targetX)
    {
        AllowOnlyXRotation();

        float currentX = rb.rotation.eulerAngles.x;
        if (currentX > 180f) currentX -= 360f;

        float nextX = Mathf.MoveTowards(
            currentX,
            targetX,
            rotateSpeed * Time.deltaTime
        );

        rb.MoveRotation(
            Quaternion.Euler(nextX, rb.rotation.eulerAngles.y, 0f)
        );

        // Mutlak stabilite
        rb.angularVelocity = Vector3.zero;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
    }

    // ======================= TUTORIAL =======================

    public IEnumerator StartTutorial(AskPracticeResponse ragResp)
    {
        if (ragResp == null || ragResp.steps == null) yield break;

        steps = ragResp.steps;
        currentSkillId = ragResp.skill_id;

        yield return StartCoroutine(StartAttemptCoroutine(userId, currentSkillId));

        if (playerControllerToDisable != null)
            playerControllerToDisable.enabled = false;

        for (currentStepIndex = 0; currentStepIndex < steps.Count; currentStepIndex++)
        {
            PracticeStep step = steps[currentStepIndex];

            yield return StartCoroutine(WaitForInputRelease());

            bool stepSucceeded = false;
            bool inputStarted = false;
            float startTime = Time.time;

            while (true)
            {
                if (stepTimeoutSeconds > 0 &&
                    Time.time - startTime > stepTimeoutSeconds)
                    break;

                bool xPressed = Input.GetKey(KeyCode.X);
                bool vPressed = Input.GetKey(KeyCode.V);

                if (xPressed)
                {
                    RotateXTo(popAngle);
                }
                else if (vPressed)
                {
                    RotateXTo(uprightAngle);
                }
                else
                {
                    RotateXTo(uprightAngle);

                    float x = rb.rotation.eulerAngles.x;
                    if (x > 180f) x -= 360f;

                    if (Mathf.Abs(x) < 0.5f)
                        LockAllRotations();
                }

                bool performingExpected = false;

                if (step.expected_actions != null)
                {
                    foreach (var exp in step.expected_actions)
                    {
                        if (actionChecks.ContainsKey(exp) &&
                            actionChecks[exp]())
                        {
                            performingExpected = true;
                            inputStarted = true;
                            stepSucceeded = true;
                            break;
                        }
                    }
                }

                if (!performingExpected && inputStarted)
                    break;

                yield return null;
            }

            if (!stepSucceeded)
            {
                EndTutorial(false);
                yield break;
            }
        }

        EndTutorial(true);
    }

    private void EndTutorial(bool success)
    {
        LockAllRotations();

        if (playerControllerToDisable != null)
            playerControllerToDisable.enabled = true;

        StartCoroutine(CompleteAttemptCoroutine(currentAttemptId, success));
    }

    // ======================= BACKEND =======================

    IEnumerator StartAttemptCoroutine(string uId, string sId)
    {
        string url = $"{backendBaseUrl}/user/{uId}/skill/{sId}/start-attempt";



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

    IEnumerator CompleteAttemptCoroutine(string attemptId, bool success)
    {
        if (string.IsNullOrEmpty(attemptId)) yield break;

        string url = $"{backendBaseUrl}/attempt/{attemptId}/complete";
        string json = "{\"success\": " + (success ? "true" : "false") + "}";

        using (UnityWebRequest r = new UnityWebRequest(url, "POST"))
        {
            r.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            r.downloadHandler = new DownloadHandlerBuffer();
            r.SetRequestHeader("Content-Type", "application/json");
            yield return r.SendWebRequest();
        }
    }

    // ======================= UTILS =======================

    private IEnumerator WaitForInputRelease()
    {
        while (Input.anyKey)
            yield return null;

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
