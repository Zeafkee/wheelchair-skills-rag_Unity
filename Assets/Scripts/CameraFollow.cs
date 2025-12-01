using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;         // Takip edilecek nesne
    public Vector3 offset = new Vector3(0, 2, -3); // Kameranýn konumu

    void LateUpdate()
    {
        // Kamerayý karakterin arkasýnda tutmak için offset'i karakterin rotasyonuna göre uygula
        Vector3 desiredPosition = target.position + target.rotation * offset;
        transform.position = desiredPosition;
        transform.LookAt(target);
    }
}