using System.Collections.Generic;
using UnityEngine;

namespace WheelchairSkills.Training
{
    /// <summary>
    /// Kullanıcı tuş girişlerini dinleyip tracker'a ileten script
    /// </summary>
    public class InputRecorder : MonoBehaviour
    {
        [Header("References")]
        public SkillAttemptTracker attemptTracker;

        [Header("Recording Settings")]
        public bool isRecording = true;
        public bool recordOnlyMappedKeys = true;
        public bool includeMetadata = true;

        [Header("Debug")]
        public bool showDebugLogs = false;

        private void Awake()
        {
            attemptTracker = SkillAttemptTracker.Instance;
        }
        private void Start()
        {
           
        }

        private void Update()
        {
            if (!isRecording || attemptTracker == null || !attemptTracker.isAttemptActive)
            {
                return;
            }

            // Tüm eşlenmiş tuşları kontrol et
            if (recordOnlyMappedKeys)
            {
                foreach (KeyCode key in InputMapping.GetAllKeys())
                {
                    if (Input.GetKeyDown(key))
                    {
                        OnKeyPressed(key);
                    }
                }
            }
            else
            {
                // Tüm tuşları kontrol et (daha yavaş ama kapsamlı)
                foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
                {
                    if (Input.GetKeyDown(key))
                    {
                        OnKeyPressed(key);
                    }
                }
            }
        }

        private void OnKeyPressed(KeyCode key)
        {
            string action = InputMapping.GetAction(key);
            
            // Eğer eşleme yoksa ve sadece eşlenmiş tuşları kaydediyorsak, atla
            if (string.IsNullOrEmpty(action) && recordOnlyMappedKeys)
            {
                return;
            }

            // Action yoksa KeyCode adını kullan
            if (string.IsNullOrEmpty(action))
            {
                action = key.ToString();
            }

            // Metadata oluştur
            Dictionary<string, object> metadata = null;
            if (includeMetadata)
            {
                metadata = CreateMetadata(key);
            }

            // Tracker'a kaydet
            attemptTracker.RecordInput(action, metadata);

            if (showDebugLogs)
            {
                Debug.Log($"Input recorded: {key} -> {action}");
            }
        }

        private Dictionary<string, object> CreateMetadata(KeyCode key)
        {
            Dictionary<string, object> metadata = new Dictionary<string, object>
            {
                { "key_code", key.ToString() },
                { "timestamp_unity", Time.time },
                { "frame", Time.frameCount }
            };

            // Ek bağlamsal bilgi ekle
            if (Camera.main != null)
            {
                metadata["camera_position"] = Camera.main.transform.position.ToString();
                metadata["camera_rotation"] = Camera.main.transform.rotation.eulerAngles.ToString();
            }

            // Karakter pozisyonu (eğer varsa)
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                metadata["player_position"] = player.transform.position.ToString();
                metadata["player_rotation"] = player.transform.rotation.eulerAngles.ToString();
            }

            return metadata;
        }

        /// <summary>
        /// Kayıt durumunu açıp kapatır
        /// </summary>
        public void ToggleRecording()
        {
            isRecording = !isRecording;
            Debug.Log($"Input recording: {(isRecording ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Kayıt durumunu ayarlar
        /// </summary>
        public void SetRecording(bool enabled)
        {
            isRecording = enabled;
            Debug.Log($"Input recording: {(isRecording ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Manuel olarak bir aksiyonu kaydeder
        /// </summary>
        public void RecordAction(string action, Dictionary<string, object> metadata = null)
        {
            if (attemptTracker != null && attemptTracker.isAttemptActive)
            {
                attemptTracker.RecordInput(action, metadata);
            }
        }
    }
}
