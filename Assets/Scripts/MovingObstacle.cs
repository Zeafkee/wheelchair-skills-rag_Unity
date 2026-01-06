using System.Collections.Generic;
using UnityEngine;

namespace WheelchairSkills.Training
{
    /// <summary>
    /// Hareketli engeller için basit waypoint tabanlı hareket sistemi
    /// Skill 30 (Avoids moving obstacles) için kullanılır
    /// </summary>
    public class MovingObstacle : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Hareket hızı (m/s)")]
        public float speed = 2f;

        [Header("Waypoint Settings")]
        [Tooltip("Waypoint'ler - engel bu noktalar arasında hareket eder")]
        public List<Transform> waypoints = new List<Transform>();

        [Tooltip("Waypoint'e ulaşıldığında kabul edilecek mesafe")]
        public float waypointReachDistance = 0.1f;

        [Tooltip("Waypoint'lere ulaştıktan sonra geri dön (ping-pong) veya başa dön (loop)")]
        public bool pingPong = true;

        [Header("Runtime Info")]
        [Tooltip("Debug için - mevcut hedef waypoint index")]
        [SerializeField]
        private int currentWaypointIndex = 0;

        [Tooltip("Debug için - hareket yönü (ping-pong modunda)")]
        [SerializeField]
        private int direction = 1; // 1: ileri, -1: geri

        private void Start()
        {
            // Başlangıçta en az 2 waypoint olmalı
            if (waypoints.Count < 2)
            {
                Debug.LogWarning($"[MovingObstacle] {gameObject.name} has less than 2 waypoints. Movement disabled.");
                enabled = false;
                return;
            }

            // İlk waypoint'e yerleştir
            if (waypoints[0] != null)
            {
                transform.position = waypoints[0].position;
            }
        }

        private void Update()
        {
            if (waypoints.Count < 2)
                return;

            MoveTowardsWaypoint();
        }

        private void MoveTowardsWaypoint()
        {
            // Mevcut hedef waypoint
            Transform targetWaypoint = waypoints[currentWaypointIndex];

            if (targetWaypoint == null)
            {
                Debug.LogWarning($"[MovingObstacle] Waypoint at index {currentWaypointIndex} is null!");
                return;
            }

            // Hedefe doğru hareket et
            Vector3 targetPosition = targetWaypoint.position;
            float step = speed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, step);

            // Hedefe yönlen (opsiyonel - hareket yönüne bak)
            Vector3 direction3D = (targetPosition - transform.position).normalized;
            if (direction3D.magnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction3D);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
            }

            // Hedefe ulaştı mı kontrol et
            float distanceToWaypoint = Vector3.Distance(transform.position, targetPosition);
            if (distanceToWaypoint <= waypointReachDistance)
            {
                ReachedWaypoint();
            }
        }

        private void ReachedWaypoint()
        {
            if (pingPong)
            {
                // Ping-pong mod: İleri-geri hareket
                currentWaypointIndex += direction;

                // Sınırları kontrol et
                if (currentWaypointIndex >= waypoints.Count)
                {
                    currentWaypointIndex = waypoints.Count - 2;
                    direction = -1; // Yönü ters çevir
                }
                else if (currentWaypointIndex < 0)
                {
                    currentWaypointIndex = 1;
                    direction = 1; // Yönü ters çevir
                }
            }
            else
            {
                // Loop mod: Başa dön
                currentWaypointIndex++;
                if (currentWaypointIndex >= waypoints.Count)
                {
                    currentWaypointIndex = 0;
                }
            }
        }

        // Gizmos ile waypoint'leri görselleştir
        private void OnDrawGizmos()
        {
            if (waypoints == null || waypoints.Count < 2)
                return;

            // Waypoint'leri çiz
            Gizmos.color = Color.yellow;
            for (int i = 0; i < waypoints.Count; i++)
            {
                if (waypoints[i] != null)
                {
                    Gizmos.DrawSphere(waypoints[i].position, 0.3f);

                    // Waypoint numarasını göster (sadece Scene view'da)
                    #if UNITY_EDITOR
                    UnityEditor.Handles.Label(waypoints[i].position + Vector3.up * 0.5f, $"WP {i}");
                    #endif
                }
            }

            // Waypoint'ler arası bağlantıları çiz
            Gizmos.color = Color.cyan;
            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                if (waypoints[i] != null && waypoints[i + 1] != null)
                {
                    Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
                }
            }

            // Loop modunda son waypoint'ten ilke dön
            if (!pingPong && waypoints.Count > 1)
            {
                if (waypoints[waypoints.Count - 1] != null && waypoints[0] != null)
                {
                    Gizmos.DrawLine(waypoints[waypoints.Count - 1].position, waypoints[0].position);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (waypoints == null || waypoints.Count == 0)
                return;

            // Seçili iken mevcut hedef waypoint'i vurgula
            if (currentWaypointIndex >= 0 && currentWaypointIndex < waypoints.Count)
            {
                if (waypoints[currentWaypointIndex] != null)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(waypoints[currentWaypointIndex].position, 0.5f);
                }
            }
        }
    }
}
