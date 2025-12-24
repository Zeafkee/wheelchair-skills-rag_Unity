using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    public float moveForce = 10f;
    public float turnSpeed = 100f; 
    public float rotateSpeed = 120f;
    public float popAngle = -30f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // Orijinal kararlı hal
    }

    void FixedUpdate()
    {
        float moveZ = Input.GetAxis("Vertical");   
        float turn = Input.GetAxis("Horizontal");  

        Vector3 force = transform.forward * moveZ * moveForce;
        rb.AddForce(force);

        // Handle X Rotation (Pop Casters)
        float targetX = 0f;
        if (Input.GetKey(KeyCode.X)) targetX = popAngle;
        
        float currentX = rb.rotation.eulerAngles.x;
        if (currentX > 180) currentX -= 360;
        float nextX = Mathf.MoveTowards(currentX, targetX, rotateSpeed * Time.fixedDeltaTime);

        Quaternion deltaRot = Quaternion.Euler(0f, turn * turnSpeed * Time.fixedDeltaTime, 0f);
        // Combine current Y rotation with new X rotation
        Vector3 newEuler = (rb.rotation * deltaRot).eulerAngles;
        rb.MoveRotation(Quaternion.Euler(nextX, newEuler.y, 0f));
    }
}
