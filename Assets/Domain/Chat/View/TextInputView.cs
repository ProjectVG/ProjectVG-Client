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
        [SerializeField] private TMP_InputField? _inputField;
        [SerializeField] private Button? _btnSend;
        
        private ChatManager? _chatManager;
        private bool _isProcessingSubmit = false;
        
        public event Action<string>? OnTextMessageSent;
        public event Action<string>? OnError;
        
        #region Unity Lifecycle
        
        private void Start()
        {
            Initialize();
        }
        
        #endregion
        
        #region Public Methods
        
        private void Initialize()
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
            {
                OnError?.Invoke("빈 메시지는 전송할 수 없습니다.");
                return;
            }
                
            try
            {
                _chatManager?.SendUserMessage(message);
                OnTextMessageSent?.Invoke(message);
                ClearInput();
                
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TextInputView] 텍스트 메시지 전송 실패: {ex.Message}");
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
        
        private void SetupChatManager()
        {
            if (_chatManager == null)
            {
                _chatManager = FindAnyObjectByType<ChatManager>();
                if (_chatManager == null)
                {
                    Debug.LogWarning("[TextInputView] ChatManager를 찾을 수 없습니다. 수동으로 SetChatManager를 호출해주세요.");
                }
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
                    Debug.LogWarning("[TextInputView] TMP_InputField를 찾을 수 없습니다.");
                }
            }
                
            if (_btnSend == null)
            {
                _btnSend = transform.Find("BtnSend")?.GetComponent<Button>();
                if (_btnSend == null)
                {
                    Debug.LogWarning("[TextInputView] BtnSend 버튼을 찾을 수 없습니다.");
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
        
        private void OnSendButtonClicked()
        {
            if (_inputField != null && !string.IsNullOrWhiteSpace(_inputField.text))
            {
                SendTextMessage(_inputField.text);
            }
        }
        
        private void OnInputFieldSubmitted(string text)
        {
            if (_isProcessingSubmit) 
            {
                return;
            }
            
            _isProcessingSubmit = true;
            SendTextMessage(text);
            
            // 다음 프레임에서 플래그 리셋
            StartCoroutine(ResetSubmitFlag());
        }
        
        private System.Collections.IEnumerator ResetSubmitFlag()
        {
            yield return null;
            _isProcessingSubmit = false;
        }
        
        #endregion
    }
} 