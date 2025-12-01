using System.Collections.Generic;
using UnityEngine;

namespace WheelchairSkills.Training
{
    /// <summary>
    /// Unity KeyCode <-> Backend Action string eşlemesi
    /// </summary>
    public static class InputMapping
    {
        // KeyCode'dan backend action string'ine eşleme
        private static Dictionary<KeyCode, string> keyToAction = new Dictionary<KeyCode, string>()
        {
            // Hareket tuşları
            { KeyCode.W, "move_forward" },
            { KeyCode.S, "move_backward" },
            { KeyCode.A, "turn_left" },
            { KeyCode.D, "turn_right" },
            
            // Ok tuşları alternatif
            { KeyCode.UpArrow, "move_forward" },
            { KeyCode.DownArrow, "move_backward" },
            { KeyCode.LeftArrow, "turn_left" },
            { KeyCode.RightArrow, "turn_right" },
            
            // Özel aksiyonlar
            { KeyCode.Space, "brake" },
            { KeyCode.LeftShift, "boost" },
            { KeyCode.E, "interact" },
            { KeyCode.Q, "special_action" },
            
            // UI ve kontrol tuşları
            { KeyCode.Escape, "pause" },
            { KeyCode.Return, "confirm" },
            { KeyCode.Backspace, "back" },
            { KeyCode.Tab, "switch_view" },
            
            // Beceri spesifik tuşlar
            { KeyCode.Alpha1, "skill_1" },
            { KeyCode.Alpha2, "skill_2" },
            { KeyCode.Alpha3, "skill_3" },
            { KeyCode.Alpha4, "skill_4" },
            { KeyCode.Alpha5, "skill_5" },
        };

        // Action string'den KeyCode'a eşleme (ters çeviri için)
        private static Dictionary<string, KeyCode> actionToKey;

        static InputMapping()
        {
            // Ters eşlemeyi oluştur
            actionToKey = new Dictionary<string, KeyCode>();
            foreach (var kvp in keyToAction)
            {
                if (!actionToKey.ContainsKey(kvp.Value))
                {
                    actionToKey[kvp.Value] = kvp.Key;
                }
            }
        }

        /// <summary>
        /// KeyCode'u backend action string'ine çevirir
        /// </summary>
        public static string GetAction(KeyCode keyCode)
        {
            if (keyToAction.TryGetValue(keyCode, out string action))
            {
                return action;
            }
            return null;
        }

        /// <summary>
        /// Backend action string'ini KeyCode'a çevirir
        /// </summary>
        public static KeyCode GetKeyCode(string action)
        {
            if (actionToKey.TryGetValue(action, out KeyCode keyCode))
            {
                return keyCode;
            }
            return KeyCode.None;
        }

        /// <summary>
        /// Bir KeyCode'un eşlemede olup olmadığını kontrol eder
        /// </summary>
        public static bool IsValidKey(KeyCode keyCode)
        {
            return keyToAction.ContainsKey(keyCode);
        }

        /// <summary>
        /// Bir action string'inin eşlemede olup olmadığını kontrol eder
        /// </summary>
        public static bool IsValidAction(string action)
        {
            return actionToKey.ContainsKey(action);
        }

        /// <summary>
        /// Tüm eşlenmiş tuşları döndürür
        /// </summary>
        public static IEnumerable<KeyCode> GetAllKeys()
        {
            return keyToAction.Keys;
        }

        /// <summary>
        /// Tüm action string'lerini döndürür
        /// </summary>
        public static IEnumerable<string> GetAllActions()
        {
            return actionToKey.Keys;
        }
    }
}
