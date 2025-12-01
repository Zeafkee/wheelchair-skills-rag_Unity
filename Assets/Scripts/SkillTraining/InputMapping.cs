using System.Collections.Generic;
using UnityEngine;

namespace WheelchairSkills.Training
{
    /// <summary>
    /// Maps keyboard inputs to action names and descriptions (Turkish)
    /// </summary>
    public static class InputMapping
    {
        // KeyCode to action string mapping
        private static readonly Dictionary<KeyCode, string> keyToAction = new Dictionary<KeyCode, string>
        {
            { KeyCode.W, "forward" },
            { KeyCode.S, "backward" },
            { KeyCode.A, "turn_left" },
            { KeyCode.D, "turn_right" },
            { KeyCode.X, "brake" },
            { KeyCode.V, "reverse" },
            { KeyCode.Space, "jump" },
            { KeyCode.E, "interact" },
            { KeyCode.R, "reset" },
            { KeyCode.Q, "strafe_left" },
            { KeyCode.C, "strafe_right" },
            { KeyCode.LeftShift, "boost" },
            { KeyCode.LeftControl, "slow" }
        };

        // Action to Turkish description mapping
        private static readonly Dictionary<string, string> actionDescriptions = new Dictionary<string, string>
        {
            { "forward", "İleri hareket" },
            { "backward", "Geri hareket" },
            { "turn_left", "Sola dönüş" },
            { "turn_right", "Sağa dönüş" },
            { "brake", "Fren" },
            { "reverse", "Geri vites" },
            { "jump", "Zıplama" },
            { "interact", "Etkileşim" },
            { "reset", "Sıfırlama" },
            { "strafe_left", "Sola kayma" },
            { "strafe_right", "Sağa kayma" },
            { "boost", "Hızlanma" },
            { "slow", "Yavaşlama" }
        };

        /// <summary>
        /// Get action name from KeyCode
        /// </summary>
        public static string GetAction(KeyCode key)
        {
            return keyToAction.ContainsKey(key) ? keyToAction[key] : null;
        }

        /// <summary>
        /// Get Turkish description for action
        /// </summary>
        public static string GetDescription(string action)
        {
            return actionDescriptions.ContainsKey(action) ? actionDescriptions[action] : action;
        }

        /// <summary>
        /// Get Turkish description directly from KeyCode
        /// </summary>
        public static string GetDescriptionFromKey(KeyCode key)
        {
            string action = GetAction(key);
            return action != null ? GetDescription(action) : null;
        }

        /// <summary>
        /// Check if a key is mapped
        /// </summary>
        public static bool IsKeyMapped(KeyCode key)
        {
            return keyToAction.ContainsKey(key);
        }

        /// <summary>
        /// Get all mapped keys
        /// </summary>
        public static KeyCode[] GetAllMappedKeys()
        {
            KeyCode[] keys = new KeyCode[keyToAction.Count];
            keyToAction.Keys.CopyTo(keys, 0);
            return keys;
        }
    }
}
