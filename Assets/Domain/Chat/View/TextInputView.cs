#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectVG.Domain.Chat.Service;

namespace ProjectVG.Domain.Chat.View
{
    public class TextInputView : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private Button _btnSend;
        
        private ChatManager? _chatManager;
        
        public event Action<string>? OnTextMessageSent;
        public event Action<string>? OnError;
        
        #region Unity Lifecycle
        
        private void Start()
        {
            Initialize();
        }
        
        #endregion
        
        #region Public Methods
        
        public void Initialize()
        {
            SetupComponents();
            SetupEventHandlers();
            SetupChatManager();
        }
        
        public void SetChatManager(ChatManager chatManager)
        {
            _chatManager = chatManager;
        }
        
        public void SendTextMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;
                
            try
            {
                if (_chatManager != null)
                {
                    _chatManager.SendUserMessage(message);
                }
                
                OnTextMessageSent?.Invoke(message);
                ClearInput();
                
                Debug.Log($"텍스트 메시지 전송: {message}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"텍스트 메시지 전송 실패: {ex.Message}");
                OnError?.Invoke($"메시지 전송 실패: {ex.Message}");
            }
        }
        
        public void ClearInput()
        {
            if (_inputField != null)
            {
                _inputField.text = string.Empty;
                _inputField.ActivateInputField();
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private void SetupComponents()
        {
            if (_inputField == null)
            {
                _inputField = GetComponentInChildren<TMP_InputField>();
                if (_inputField == null)
                {
                    Debug.LogWarning("TextInputView: TMP_InputField를 찾을 수 없습니다.");
                }
            }
                
            if (_btnSend == null)
            {
                _btnSend = transform.Find("BtnSend")?.GetComponent<Button>();
                if (_btnSend == null)
                {
                    Debug.LogWarning("TextInputView: BtnSend 버튼을 찾을 수 없습니다.");
                }
            }
        }
        
        private void SetupEventHandlers()
        {
            if (_btnSend != null)
                _btnSend.onClick.AddListener(OnSendButtonClicked);
                
            if (_inputField != null)
                _inputField.onSubmit.AddListener(OnInputFieldSubmitted);
        }
        
        public void SetupChatManager()
        {
            if (_chatManager == null)
            {
                _chatManager = FindAnyObjectByType<ChatManager>();
                if (_chatManager == null)
                {
                    Debug.LogWarning("TextInputView: ChatManager를 찾을 수 없습니다. 수동으로 SetChatManager를 호출해주세요.");
                }
                else
                {
                    Debug.Log("TextInputView: ChatManager를 자동으로 찾아서 설정했습니다.");
                }
            }
        }
        
        private void OnSendButtonClicked()
        {
            if (_inputField != null && !string.IsNullOrWhiteSpace(_inputField.text))
            {
                SendTextMessage(_inputField.text);
            }
        }
        
        private void OnInputFieldSubmitted(string text)
        {
            SendTextMessage(text);
        }
        
        #endregion
    }
} 