#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectVG.Domain.Chat.Service;

namespace ProjectVG.Domain.Chat.View
{
    /// <summary>
    /// 텍스트 입력 UI 컴포넌트
    /// </summary>
    public class TextInputView : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private Button _btnSend;
        
        private ChatManager _chatManager;
        
        public event Action<string>? OnTextMessageSent;
        public event Action<string>? OnError;
        
        private void Start()
        {
            Initialize();
        }
        
        /// <summary>
        /// TextInputView 초기화
        /// </summary>
        public void Initialize()
        {
            SetupComponents();
            SetupEventHandlers();
            SetupChatManager();
        }
        
        #region 초기화 설정
        
        /// <summary>
        /// 컴포넌트 설정
        /// </summary>
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
        
        /// <summary>
        /// 이벤트 핸들러 설정
        /// </summary>
        private void SetupEventHandlers()
        {
            if (_btnSend != null)
                _btnSend.onClick.AddListener(OnSendButtonClicked);
                
            if (_inputField != null)
                _inputField.onSubmit.AddListener(OnInputFieldSubmitted);
        }
        
        /// <summary>
        /// ChatManager 설정
        /// </summary>
        /// <param name="chatManager">설정할 ChatManager</param>
        public void SetChatManager(ChatManager chatManager)
        {
            _chatManager = chatManager;
        }
        
        /// <summary>
        /// ChatManager 자동 설정 (null인 경우 자동 생성)
        /// </summary>
        public void SetupChatManager()
        {
            if (_chatManager == null)
            {
                _chatManager = FindObjectOfType<ChatManager>();
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
        
        #endregion
        
        /// <summary>
        /// 텍스트 메시지 전송
        /// </summary>
        /// <param name="message">전송할 메시지</param>
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
        
        /// <summary>
        /// 입력 필드 초기화
        /// </summary>
        public void ClearInput()
        {
            if (_inputField != null)
            {
                _inputField.text = string.Empty;
                _inputField.ActivateInputField();
            }
        }
        
        #region 이벤트 핸들러
        
        /// <summary>
        /// 전송 버튼 클릭 처리
        /// </summary>
        private void OnSendButtonClicked()
        {
            if (_inputField != null && !string.IsNullOrWhiteSpace(_inputField.text))
            {
                SendTextMessage(_inputField.text);
            }
        }
        
        /// <summary>
        /// 입력 필드 제출 처리
        /// </summary>
        /// <param name="text">입력된 텍스트</param>
        private void OnInputFieldSubmitted(string text)
        {
            SendTextMessage(text);
        }
        
        #endregion
    }
} 