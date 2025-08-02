#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectVG.Domain.Chat.Service;
using ProjectVG.Infrastructure.Network.Services;
using ProjectVG.Core.Audio;

namespace ProjectVG.Domain.Chat.View
{
    /// <summary>
    /// 채팅 입력 UI 컴포넌트
    /// 텍스트 입력과 음성 입력을 지원합니다.
    /// </summary>
    public class ChatInputView : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private Button _btnSend;
        [SerializeField] private Button _btnVoice;
        [SerializeField] private Button _btnVoiceStop;
        [SerializeField] private TextMeshProUGUI _txtVoiceStatus;
        
        [Header("Voice Settings")]
        [SerializeField] private float _maxRecordingTime = 30f;
        [SerializeField] private string _voiceStatusRecording = "녹음 중...";
        [SerializeField] private string _voiceStatusProcessing = "음성을 텍스트로 변환 중...";
        
        private ChatManager _chatManager;
        private AudioRecorder _audioRecorder;
        private ISTTService _sttService;
        private bool _isRecording = false;
        private float _recordingStartTime;
        
        public event Action<string>? OnTextMessageSent;
        public event Action<string>? OnVoiceMessageSent;
        public event Action<string>? OnError;
        
        private void Start()
        {
            Initialize();
        }
        
        /// <summary>
        /// ChatInputView 초기화
        /// </summary>
        public void Initialize()
        {
            SetupComponents();
            SetupEventHandlers();
            UpdateVoiceButtonState(false);
        }
        
        /// <summary>
        /// 컴포넌트 설정
        /// </summary>
        private void SetupComponents()
        {
            if (_inputField == null)
                _inputField = GetComponentInChildren<TMP_InputField>();
                
            if (_btnSend == null)
                _btnSend = transform.Find("BtnSend")?.GetComponent<Button>();
                
            if (_btnVoice == null)
                _btnVoice = transform.Find("BtnVoice")?.GetComponent<Button>();
                
            if (_btnVoiceStop == null)
                _btnVoiceStop = transform.Find("BtnVoiceStop")?.GetComponent<Button>();
                
            if (_txtVoiceStatus == null)
                _txtVoiceStatus = transform.Find("TxtVoiceStatus")?.GetComponent<TextMeshProUGUI>();
                
            // AudioRecorder는 싱글톤으로 가져오기
            if (_audioRecorder == null)
                _audioRecorder = AudioRecorder.Instance;
                
            // STT 서비스 초기화
            if (_sttService == null)
                _sttService = new STTService();
        }
        
        /// <summary>
        /// 이벤트 핸들러 설정
        /// </summary>
        private void SetupEventHandlers()
        {
            if (_btnSend != null)
                _btnSend.onClick.AddListener(OnSendButtonClicked);
                
            if (_btnVoice != null)
                _btnVoice.onClick.AddListener(OnVoiceButtonClicked);
                
            if (_btnVoiceStop != null)
                _btnVoiceStop.onClick.AddListener(OnVoiceStopButtonClicked);
                
            if (_inputField != null)
                _inputField.onSubmit.AddListener(OnInputFieldSubmitted);
                
            // AudioRecorder 이벤트 구독
            if (_audioRecorder != null)
            {
                _audioRecorder.OnRecordingStarted += OnRecordingStarted;
                _audioRecorder.OnRecordingStopped += OnRecordingStopped;
                _audioRecorder.OnRecordingCompleted += OnRecordingCompleted;
                _audioRecorder.OnError += OnRecordingError;
            }
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
        /// 음성 메시지 전송
        /// </summary>
        /// <param name="audioData">음성 데이터</param>
        public async void SendVoiceMessage(byte[] audioData)
        {
            if (audioData == null || audioData.Length == 0)
                return;
                
            try
            {
                UpdateVoiceStatus(_voiceStatusProcessing);
                
                // STT 서비스를 통해 음성을 텍스트로 변환
                string transcribedText = await ConvertSpeechToText(audioData);
                
                if (!string.IsNullOrWhiteSpace(transcribedText))
                {
                    SendTextMessage(transcribedText);
                    OnVoiceMessageSent?.Invoke(transcribedText);
                }
                else
                {
                    OnError?.Invoke("음성을 텍스트로 변환할 수 없습니다.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"음성 메시지 전송 실패: {ex.Message}");
                OnError?.Invoke($"음성 메시지 전송 실패: {ex.Message}");
            }
            finally
            {
                UpdateVoiceStatus(string.Empty);
            }
        }
        
        /// <summary>
        /// 음성 녹음 시작
        /// </summary>
        public void StartVoiceRecording()
        {
            if (_isRecording)
                return;
                
            if (_audioRecorder == null)
            {
                Debug.LogError("AudioRecorder가 없습니다.");
                OnError?.Invoke("AudioRecorder가 없습니다.");
                return;
            }
                
            try
            {
                _isRecording = true;
                _recordingStartTime = Time.time;
                UpdateVoiceButtonState(true);
                UpdateVoiceStatus(_voiceStatusRecording);
                
                // AudioRecorder를 통해 음성 녹음 시작
                bool success = _audioRecorder.StartRecording();
                if (!success)
                {
                    _isRecording = false;
                    UpdateVoiceButtonState(false);
                    UpdateVoiceStatus(string.Empty);
                }
                
                Debug.Log("음성 녹음 시작");
            }
            catch (Exception ex)
            {
                Debug.LogError($"음성 녹음 시작 실패: {ex.Message}");
                OnError?.Invoke($"음성 녹음 시작 실패: {ex.Message}");
                StopVoiceRecording();
            }
        }
        
        /// <summary>
        /// 음성 녹음 중지
        /// </summary>
        public void StopVoiceRecording()
        {
            if (!_isRecording)
                return;
                
            if (_audioRecorder == null)
            {
                Debug.LogError("AudioRecorder가 없습니다.");
                return;
            }
                
            try
            {
                _isRecording = false;
                UpdateVoiceButtonState(false);
                UpdateVoiceStatus(string.Empty);
                
                // AudioRecorder를 통해 음성 녹음 중지
                AudioClip recordedClip = _audioRecorder.StopRecording();
                if (recordedClip != null)
                {
                    byte[] audioData = _audioRecorder.AudioClipToBytes(recordedClip);
                    if (audioData.Length > 0)
                    {
                        SendVoiceMessage(audioData);
                    }
                }
                
                Debug.Log("음성 녹음 중지");
            }
            catch (Exception ex)
            {
                Debug.LogError($"음성 녹음 중지 실패: {ex.Message}");
                OnError?.Invoke($"음성 녹음 중지 실패: {ex.Message}");
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
        
        /// <summary>
        /// 음성 버튼 상태 업데이트
        /// </summary>
        /// <param name="isRecording">녹음 중 여부</param>
        private void UpdateVoiceButtonState(bool isRecording)
        {
            if (_btnVoice != null)
                _btnVoice.gameObject.SetActive(!isRecording);
                
            if (_btnVoiceStop != null)
                _btnVoiceStop.gameObject.SetActive(isRecording);
        }
        
        /// <summary>
        /// 음성 상태 텍스트 업데이트
        /// </summary>
        /// <param name="status">상태 텍스트</param>
        private void UpdateVoiceStatus(string status)
        {
            if (_txtVoiceStatus != null)
            {
                _txtVoiceStatus.text = status;
                _txtVoiceStatus.gameObject.SetActive(!string.IsNullOrEmpty(status));
            }
        }
        
        /// <summary>
        /// 음성을 텍스트로 변환 (STT 서비스 호출)
        /// </summary>
        /// <param name="audioData">음성 데이터</param>
        /// <returns>변환된 텍스트</returns>
        private async System.Threading.Tasks.Task<string> ConvertSpeechToText(byte[] audioData)
        {
            if (_sttService == null)
            {
                Debug.LogError("STT 서비스가 없습니다.");
                return string.Empty;
            }
            
            try
            {
                // STT 서비스 초기화 (필요한 경우)
                if (!_sttService.IsAvailable)
                {
                    await _sttService.InitializeAsync();
                }
                
                // 음성을 텍스트로 변환
                string transcribedText = await _sttService.ConvertSpeechToTextAsync(audioData);
                return transcribedText;
            }
            catch (Exception ex)
            {
                Debug.LogError($"STT 변환 실패: {ex.Message}");
                throw;
            }
        }
        
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
        /// 음성 버튼 클릭 처리
        /// </summary>
        private void OnVoiceButtonClicked()
        {
            StartVoiceRecording();
        }
        
        /// <summary>
        /// 음성 중지 버튼 클릭 처리
        /// </summary>
        private void OnVoiceStopButtonClicked()
        {
            StopVoiceRecording();
        }
        
        /// <summary>
        /// 입력 필드 제출 처리
        /// </summary>
        /// <param name="text">입력된 텍스트</param>
        private void OnInputFieldSubmitted(string text)
        {
            SendTextMessage(text);
        }
        
        /// <summary>
        /// 녹음 시작 이벤트 처리
        /// </summary>
        private void OnRecordingStarted()
        {
            Debug.Log("녹음 시작됨");
        }
        
        /// <summary>
        /// 녹음 중지 이벤트 처리
        /// </summary>
        private void OnRecordingStopped()
        {
            Debug.Log("녹음 중지됨");
        }
        
        /// <summary>
        /// 녹음 완료 이벤트 처리
        /// </summary>
        /// <param name="audioClip">녹음된 AudioClip</param>
        private void OnRecordingCompleted(AudioClip audioClip)
        {
            Debug.Log($"녹음 완료: {audioClip.length}초");
        }
        
        /// <summary>
        /// 녹음 오류 이벤트 처리
        /// </summary>
        /// <param name="error">오류 메시지</param>
        private void OnRecordingError(string error)
        {
            Debug.LogError($"녹음 오류: {error}");
            OnError?.Invoke(error);
        }
        
        private void Update()
        {
            // 녹음 시간 제한 체크
            if (_isRecording && Time.time - _recordingStartTime > _maxRecordingTime)
            {
                StopVoiceRecording();
            }
        }
        
        private void OnDestroy()
        {
            if (_isRecording)
            {
                StopVoiceRecording();
            }
            
            // 이벤트 구독 해제
            if (_audioRecorder != null)
            {
                _audioRecorder.OnRecordingStarted -= OnRecordingStarted;
                _audioRecorder.OnRecordingStopped -= OnRecordingStopped;
                _audioRecorder.OnRecordingCompleted -= OnRecordingCompleted;
                _audioRecorder.OnError -= OnRecordingError;
            }
        }
    }
} 