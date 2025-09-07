#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using ProjectVG.Domain.Chat.Service;
using ProjectVG.Infrastructure.Network.Services;
using ProjectVG.Core.Audio;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProjectVG.Domain.Chat.View
{
    public class VoiceInputView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("UI Components")]
        [SerializeField] private Button? _btnVoiceRecord;
        [SerializeField] private TextMeshProUGUI? _txtVoiceStatus;
        
        
        private ChatSystemManager? _chatManager;
        private AudioRecorder? _audioRecorder;
        private STTService? _sttService;
        private bool _isRecording = false;
        private float _recordingStartTime;
        private bool _isKeyboardRecording = false;
        
        public event Action<string>? OnVoiceMessageSent;
        public event Action<string>? OnError;
        
        #region Unity Lifecycle
        
        private void Start()
        {
            Initialize();
        }
        
        private void Update()
        {
            HandleKeyboardInput();
        }
        
        private void OnDestroy()
        {
            if (_isRecording)
            {
                StopVoiceRecording();
            }
            
            if (_audioRecorder != null)
            {
                _audioRecorder.OnRecordingStarted -= OnRecordingStarted;
                _audioRecorder.OnRecordingStopped -= OnRecordingStopped;
                _audioRecorder.OnRecordingCompleted -= OnRecordingCompleted;
                _audioRecorder.OnError -= OnRecordingError;
            }
        }
        
        #endregion
        
        #region Public Methods
        
        private void Initialize()
        {
            SetupComponents();
            SetupEventHandlers();
            UpdateVoiceButtonState(false);
            SetupChatManager();
        }
        
        public void SetChatManager(ChatSystemManager chatManager)
        {
            _chatManager = chatManager;
        }
        
        public async void SendVoiceMessage(byte[] audioData)
        {
            if (audioData == null || audioData.Length == 0)
            {
                OnError?.Invoke("음성 데이터가 비어있습니다.");
                return;
            }
                
            try
            {
                string transcribedText = await ConvertSpeechToText(audioData);
                
                if (!string.IsNullOrWhiteSpace(transcribedText))
                {
                    _chatManager?.SendUserMessage(transcribedText);
                    OnVoiceMessageSent?.Invoke(transcribedText);
                }
                else
                {
                    OnError?.Invoke("음성을 텍스트로 변환할 수 없습니다.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VoiceInputView] 음성 메시지 전송 실패: {ex.Message}");
                OnError?.Invoke($"음성 메시지 전송 실패: {ex.Message}");
            }
            finally
            {
            }
        }
        
        public void StartVoiceRecording()
        {
            if (_isRecording)
                return;
                
            if (_audioRecorder == null)
            {
                OnError?.Invoke("AudioRecorder가 없습니다.");
                return;
            }
                
            try
            {
                _isRecording = true;
                _recordingStartTime = Time.time;
                UpdateVoiceButtonState(true);
                
                bool success = _audioRecorder.StartRecording();
                if (!success)
                {
                    StopVoiceRecording();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VoiceInputView] 음성 녹음 시작 실패: {ex.Message}");
                OnError?.Invoke($"음성 녹음 시작 실패: {ex.Message}");
                StopVoiceRecording();
            }
        }
        
        public void StopVoiceRecording()
        {
            if (!_isRecording)
                return;
                
            // 키보드 녹음 상태도 초기화
            _isKeyboardRecording = false;
                
            if (_audioRecorder == null)
            {
                Debug.LogError("[VoiceInputView] AudioRecorder가 없습니다.");
                return;
            }
                
            try
            {
                _isRecording = false;
                UpdateVoiceButtonState(false);
                
                AudioClip? recordedClip = _audioRecorder.StopRecording();
                if (recordedClip != null)
                {
                    byte[] audioData = _audioRecorder.AudioClipToWavBytes(recordedClip);
                    if (audioData.Length > 0)
                    {
                        SendVoiceMessage(audioData);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VoiceInputView] 음성 녹음 중지 실패: {ex.Message}");
                OnError?.Invoke($"음성 녹음 중지 실패: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private void SetupComponents()
        {
            if (_btnVoiceRecord == null)
            {
                _btnVoiceRecord = transform.Find("BtnVoice")?.GetComponent<Button>();
                if (_btnVoiceRecord == null)
                {
                    Debug.LogWarning("[VoiceInputView] BtnVoice 버튼을 찾을 수 없습니다.");
                }
            }
                
            if (_txtVoiceStatus == null)
            {
                _txtVoiceStatus = _btnVoiceRecord?.GetComponentInChildren<TextMeshProUGUI>();
                if (_txtVoiceStatus == null)
                {
                    Debug.LogWarning("[VoiceInputView] 음성 상태 텍스트를 찾을 수 없습니다.");
                }
            }
                

                
            if (_audioRecorder == null)
            {
                _audioRecorder = AudioRecorder.Instance;
                if (_audioRecorder == null)
                {
                    _audioRecorder = gameObject.AddComponent<AudioRecorder>();
                }
            }
                
            if (_sttService == null)
            {
                _sttService = new STTService();
                if (_sttService == null)
                {
                    Debug.LogError("[VoiceInputView] STTService를 생성할 수 없습니다.");
                }
            }
        }
        
        private void SetupEventHandlers()
        {
                
            if (_audioRecorder != null)
            {
                _audioRecorder.OnRecordingStarted += OnRecordingStarted;
                _audioRecorder.OnRecordingStopped += OnRecordingStopped;
                _audioRecorder.OnRecordingCompleted += OnRecordingCompleted;
                _audioRecorder.OnError += OnRecordingError;
            }
        }
        
        private void SetupChatManager()
        {
            if (_chatManager == null)
            {
                _chatManager = FindAnyObjectByType<ChatSystemManager>();
                if (_chatManager == null)
                {
                    Debug.LogWarning("[VoiceInputView] ChatManager를 찾을 수 없습니다. 수동으로 SetChatManager를 호출해주세요.");
                }
            }
        }
        
        private void UpdateVoiceButtonState(bool isRecording)
        {
            if (_btnVoiceRecord != null)
            {
                _btnVoiceRecord.interactable = true;
                
                // 버튼 크기 변경으로 시각적 피드백 제공
                var rectTransform = _btnVoiceRecord.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    float scale = isRecording ? 1.1f : 1.0f;
                    rectTransform.localScale = Vector3.one * scale;
                }
                
                // 버튼 색상 변경
                var buttonColors = _btnVoiceRecord.colors;
                buttonColors.normalColor = isRecording ? Color.red : Color.white;
                buttonColors.highlightedColor = isRecording ? new Color(1f, 0.5f, 0.5f) : new Color(0.9f, 0.9f, 0.9f);
                _btnVoiceRecord.colors = buttonColors;
                
                if (_txtVoiceStatus != null)
                {
                    _txtVoiceStatus.text = isRecording ? "녹음 중... (버튼을 떼거나 T키를 놓으세요)" : "음성 입력 (버튼을 누르거나 T키 유지)";
                    _txtVoiceStatus.color = isRecording ? Color.white : new Color(0.8f, 0.8f, 0.8f);
                }
            }
        }
        

        
        private async System.Threading.Tasks.Task<string> ConvertSpeechToText(byte[] audioData)
        {
            if (_sttService == null)
            {
                Debug.LogError("[VoiceInputView] STT 서비스가 없습니다.");
                return string.Empty;
            }
            
            try
            {
                string transcribedText = await _sttService.ConvertSpeechToTextAsync(audioData);
                return transcribedText;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VoiceInputView] STT 변환 실패: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 더미 음성으로 STT 서버 테스트
        /// </summary>
        [ContextMenu("Test STT with Dummy Audio")]
        public async void TestSTTWithDummyAudio()
        {
            if (_sttService == null)
            {
                Debug.LogError("[VoiceInputView] STT 서비스가 없습니다.");
                return;
            }
            
            try
            {
                byte[] dummyAudio = _sttService.GenerateTestAudioData();
                string result = await _sttService.ConvertSpeechToTextAsync(dummyAudio);
                Debug.Log($"[VoiceInputView] STT 테스트 결과: '{result}'");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VoiceInputView] STT 테스트 실패: {ex.Message}");
            }
        }
        
        #region Press & Hold UI Events
        
        /// <summary>
        /// 마우스/터치 버튼을 누를 때 녹음 시작
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                StartVoiceRecording();
            }
        }
        
        /// <summary>
        /// 마우스/터치 버튼을 뗄 때 녹음 종료
        /// </summary>
        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                StopVoiceRecording();
            }
        }
        
        #endregion
        
        #region Keyboard Input
        
        /// <summary>
        /// T키 Press & Hold 입력 처리
        /// </summary>
        private void HandleKeyboardInput()
        {
            // UI 포커스 상태 확인 - InputField 등이 포커스를 가지고 있으면 키보드 입력 무시
            if (IsUIInputFieldFocused())
                return;
                
            bool tKeyPressed = Input.GetKeyDown(KeyCode.T);
            bool tKeyReleased = Input.GetKeyUp(KeyCode.T);
            
            if (tKeyPressed && !_isKeyboardRecording)
            {
                _isKeyboardRecording = true;
                StartVoiceRecording();
            }
            else if (tKeyReleased && _isKeyboardRecording)
            {
                _isKeyboardRecording = false;
                StopVoiceRecording();
            }
        }
        
        /// <summary>
        /// 현재 InputField가 포커스를 가지고 있는지 확인
        /// 텍스트 입력 중일 때 T키 입력을 무시하기 위함
        /// </summary>
        private bool IsUIInputFieldFocused()
        {
            // EventSystem을 통해 현재 선택된 오브젝트가 InputField인지 확인
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
            {
                var inputField = EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>();
                if (inputField != null)
                {
                    return inputField.isFocused;
                }
            }
            return false;
        }
        
        #endregion
        
        private void OnRecordingStarted()
        {
            // AudioRecorder에서 로그 출력
        }
        
        private void OnRecordingStopped()
        {
            // AudioRecorder에서 로그 출력
        }
        
        private void OnRecordingCompleted(AudioClip audioClip)
        {
            // AudioRecorder에서 로그 출력
        }
        

        
        private void OnRecordingError(string error)
        {
            OnError?.Invoke(error);
        }
        
        #endregion
    }
} 