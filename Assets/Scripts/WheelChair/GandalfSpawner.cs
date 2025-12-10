using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GandalfSpawner : MonoBehaviour
{
    public GameObject characterPrefab; // spawnlanacak karakter
    public Transform player; // Player'ýn Transform'u
    public Vector3 spawnOffset = new Vector3(1, 0, 0);
    public float checkRadius = 0.5f; // Spawn alaný kontrol yarýçapý
    public LayerMask obstacleLayerMask = 1; // Sadece Default layer'ý kontrol et
    public bool enableDebugGizmos = true; // Debug çizimlerini aktif et

    private GameObject spawnedCharacter; // Tek karakter için
    private GameObject currentObject; // Hangi obje ile temas halinde olduðumuzu takip et

    void Update()
    {
        // R tuþuna basýldýðýnda karakteri yok et
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (spawnedCharacter != null)
            {
                Destroy(spawnedCharacter);
                spawnedCharacter = null;
                currentObject = null;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Object"))
        {
            if (spawnedCharacter == null)
            {
                Vector3 safeSpawnPosition = FindSafeSpawnPosition(player.position);

                if (safeSpawnPosition != Vector3.zero)
                {
                    spawnedCharacter = Instantiate(characterPrefab, safeSpawnPosition, Quaternion.identity);
                    currentObject = other.gameObject;
                }
                else
                {
                    Debug.LogWarning("Güvenli pozisyon bulunamadý, player'ýn saðýnda spawn ediliyor!");
                    spawnedCharacter = Instantiate(characterPrefab, player.position + spawnOffset, Quaternion.identity);
                    currentObject = other.gameObject;
                }
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Object") && other.gameObject == currentObject)
        {
            currentObject = null;
        }
    }

    Vector3 FindSafeSpawnPosition(Vector3 playerPosition)
    {
        Vector3[] offsets = {
            new Vector3(1, 0, 0),
            new Vector3(-1, 0, 0),
            new Vector3(0, 0, 1),
            new Vector3(0, 0, -1),
            new Vector3(1, 0, 1),
            new Vector3(-1, 0, 1),
            new Vector3(1, 0, -1),
            new Vector3(-1, 0, -1)
        };

        foreach (Vector3 offset in offsets)
        {
            Vector3 testPosition = playerPosition + offset * spawnOffset.magnitude;

            if (!Physics.CheckSphere(testPosition, checkRadius, obstacleLayerMask))
            {
                RaycastHit hit;
                if (Physics.Raycast(testPosition + Vector3.up * 5f, Vector3.down, out hit, 10f))
                {
                    // Sadece çok küçük bir offset ekleyin
                    Vector3 finalPosition = hit.point + Vector3.up * 0.01f;
                    return finalPosition;
                }
            }
        }

        return Vector3.zero;
    }

    void OnDrawGizmos()
    {
        if (enableDebugGizmos && player != null)
        {
            Vector3[] offsets = {
                new Vector3(1, 0, 0), new Vector3(-1, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 0, -1),
                new Vector3(1, 0, 1), new Vector3(-1, 0, 1), new Vector3(1, 0, -1), new Vector3(-1, 0, -1)
            };

            foreach (Vector3 offset in offsets)
            {
                Vector3 testPosition = player.position + offset * spawnOffset.magnitude;
                Gizmos.color = Physics.CheckSphere(testPosition, checkRadius, obstacleLayerMask) ? Color.red : Color.green;
                Gizmos.DrawWireSphere(testPosition, checkRadius);
            }
        }
    }
}