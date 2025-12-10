using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Listens to input during tutorial and reports telemetry to TutorialManager/WheelchairController
public class InputRecorder : MonoBehaviour
{
    public WheelchairController player;
    public float maxHoldTime = 3f;

    private float wHoldStart = 0f;
    private bool wHolding = false;

    void Update()
    {
        if (player == null) return;
        // Pop casters
        if (Input.GetKeyDown(KeyCode.X))
        {
            StartCoroutine(DoPopCasters());
        }

        // Drive hold
        if (Input.GetKeyDown(KeyCode.W))
        {
            wHoldStart = Time.time;
            wHolding = true;
        }
        if (Input.GetKeyUp(KeyCode.W) && wHolding)
        {
            float hold = Time.time - wHoldStart;
            wHolding = false;
            // Apply drive force in controller (physics applied in fixed update)
            player.ApplyDriveForHold(hold, maxHoldTime);
            // compute approximate distance moved using Rigidbody displacement - easier: sample before/after in TutorialManager
            // Send to tutorial manager
            float distanceEstimate = EstimateDistanceAfterDrive(player, hold);
            TutorialManager.Instance.OnActionPerformed(TutorialActionType.DriveForward, hold, player ? player.GetComponent<Rigidbody>().linearVelocity.magnitude : 0f, distanceEstimate, false);
        }
    }

    IEnumerator DoPopCasters()
    {
        // start routine for pop casters
        if (player != null)
        {
            yield return player.StartCoroutine(player.PopCastersRoutine(0.25f));
            // allow small physics settle
            yield return new WaitForSeconds(0.1f);
            // check whether front casters lifted
            bool lifted = player.AreFrontCastersLifted(0.05f);
            TutorialManager.Instance.OnActionPerformed(TutorialActionType.PopCasters, 0f, 0f, 0f, false);
        }
    }

    private float EstimateDistanceAfterDrive(WheelchairController wc, float hold)
    {
        // Simple approximate: distance = average speed * duration
        var rb = wc.GetComponent<Rigidbody>();
        if (rb == null) return 0f;
        float avgSpeed = rb.linearVelocity.magnitude;
        return avgSpeed * hold;
    }
}