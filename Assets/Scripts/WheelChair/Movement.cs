using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    public float moveForce = 10f;
    public float turnSpeed = 100f; // Dönüþ hýzý

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // Rotasyonu kod ile kontrol edeceðiz
    }

    void FixedUpdate()
    {
        float moveZ = Input.GetAxis("Vertical");   // W/S ile ileri-geri
        float turn = Input.GetAxis("Horizontal");  // A/D ile sað-sol

        // Ýleri-geri hareket
        Vector3 force = transform.forward * moveZ * moveForce;
        rb.AddForce(force);

        // Y ekseninde dönme (saða-sola bakma)
        if (Mathf.Abs(turn) > 0.01f)
        {
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turn * turnSpeed * Time.fixedDeltaTime, 0f));
        }
    }
}
