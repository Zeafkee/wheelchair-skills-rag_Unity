using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelchairController : MonoBehaviour
{
    public Rigidbody rb;
    public float baseDriveForce = 350f; // tune in scene
    public float popTorque = 250f;
    public Transform frontCasterTransform; // set in Inspector to front-caster pivot
    public float groundY = 0f;

    // runtime
    private bool controlsEnabled = true;
    private float peakForceThisHold = 0f;

    // attempt tracking
    public string CurrentAttemptId { get; set; }

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        groundY = transform.position.y;
    }

    private void FixedUpdate()
    {
        if (!controlsEnabled) return;
        // normal physics-based control can be here (if not in tutorial)
    }

    public void ApplyDriveForHold(float holdDuration, float maxHoldTime = 3f)
    {
        // Convert holdDuration -> power (0..1)
        float power = Mathf.Clamp01(holdDuration / maxHoldTime);
        // non-linear mapping for better feel
        float mapped = Mathf.Pow(power, 0.8f);
        float force = baseDriveForce * (0.3f + mapped * 1.0f);
        // apply forward force
        Vector3 forward = transform.forward;
        rb.AddForce(forward * force, ForceMode.Force);
        // record peak for telemetry
        peakForceThisHold = Mathf.Max(peakForceThisHold, force);
    }

    public IEnumerator PopCastersRoutine(float duration = 0.2f)
    {
        float start = Time.time;
        while (Time.time - start < duration)
        {
            rb.AddTorque(transform.right * popTorque, ForceMode.Force);
            yield return new WaitForFixedUpdate();
        }
        yield return null;
    }

    public bool AreFrontCastersLifted(float heightThreshold)
    {
        if (frontCasterTransform == null) return false;
        return (frontCasterTransform.position.y - groundY) > heightThreshold;
    }

    public bool IsStationary(float velThreshold = 0.05f)
    {
        if (rb == null) return true;
        return rb.linearVelocity.magnitude < velThreshold;
    }

    public void SetPlayerEnabled(bool enabled)
    {
        controlsEnabled = enabled;
    }

    // Expose peak force for telemetry and then reset for next hold
    public float GetAndResetPeakForce()
    {
        float v = peakForceThisHold;
        peakForceThisHold = 0f;
        return v;
    }
}