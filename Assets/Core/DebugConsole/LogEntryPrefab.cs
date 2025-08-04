using UnityEngine;
using TMPro;

namespace ProjectVG.Core.Utils
{
    public class LogEntryPrefab : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TextMeshProUGUI? _logText;
        
        public void SetLogMessage(string message)
        {
            if (_logText != null)
            {
                _logText.text = message;
            }
        }
        
        public void SetLogColor(Color color)
        {
            if (_logText != null)
            {
                _logText.color = color;
            }
        }
    }
} 