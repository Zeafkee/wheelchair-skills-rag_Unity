using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    public float moveForce = 10f;
    public float turnSpeed = 100f; 

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // Orijinal kararlı hali: Tüm rotasyonlar kilitli
    }

    void FixedUpdate()
    {
        float moveZ = Input.GetAxis("Vertical");   
        float turn = Input.GetAxis("Horizontal");  

        Vector3 force = transform.forward * moveZ * moveForce;
        rb.AddForce(force);

        if (Mathf.Abs(turn) > 0.01f)
        {
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turn * turnSpeed * Time.fixedDeltaTime, 0f));
        }
    }
}