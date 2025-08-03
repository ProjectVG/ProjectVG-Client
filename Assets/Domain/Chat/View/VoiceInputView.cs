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
    /// 음성 입력 UI 컴포넌트
    /// </summary>
    public class VoiceInputView : MonoBehaviour
    {
        [Header("UI Components")]
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
        
        public event Action<string>? OnVoiceMessageSent;
        public event Action<string>? OnError;
        
        private void Start()
        {
            Initialize();
        }
        
        /// <summary>
        /// VoiceInputView 초기화
        /// </summary>
        public void Initialize()
        {
            SetupComponents();
            SetupEventHandlers();
            UpdateVoiceButtonState(false);
            SetupChatManager();
        }
        
        #region 초기화 설정
        
        /// <summary>
        /// 컴포넌트 설정
        /// </summary>
        private void SetupComponents()
        {
            if (_btnVoice == null)
            {
                _btnVoice = transform.Find("BtnVoice")?.GetComponent<Button>();
                if (_btnVoice == null)
                {
                    Debug.LogWarning("VoiceInputView: BtnVoice 버튼을 찾을 수 없습니다.");
                }
            }
                
            if (_btnVoiceStop == null)
            {
                _btnVoiceStop = transform.Find("BtnVoiceStop")?.GetComponent<Button>();
                if (_btnVoiceStop == null)
                {
                    Debug.LogWarning("VoiceInputView: BtnVoiceStop 버튼을 찾을 수 없습니다.");
                }
            }
                
            if (_txtVoiceStatus == null)
            {
                _txtVoiceStatus = transform.Find("TxtVoiceStatus")?.GetComponent<TextMeshProUGUI>();
                if (_txtVoiceStatus == null)
                {
                    Debug.LogWarning("VoiceInputView: TxtVoiceStatus 텍스트를 찾을 수 없습니다.");
                }
            }
                
            if (_audioRecorder == null)
            {
                _audioRecorder = AudioRecorder.Instance;
                if (_audioRecorder == null)
                {
                    _audioRecorder = gameObject.AddComponent<AudioRecorder>();
                    Debug.Log("VoiceInputView: AudioRecorder 컴포넌트를 자동으로 추가했습니다.");
                }
            }
                
            if (_sttService == null)
            {
                _sttService = new STTService();
                if (_sttService == null)
                {
                    Debug.LogError("VoiceInputView: STTService를 생성할 수 없습니다.");
                }
            }
        }
        
        /// <summary>
        /// 이벤트 핸들러 설정
        /// </summary>
        private void SetupEventHandlers()
        {
            if (_btnVoice != null)
                _btnVoice.onClick.AddListener(OnVoiceButtonClicked);
                
            if (_btnVoiceStop != null)
                _btnVoiceStop.onClick.AddListener(OnVoiceStopButtonClicked);
                
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
        /// ChatManager 자동 설정 (null인 경우 자동 생성)
        /// </summary>
        public void SetupChatManager()
        {
            if (_chatManager == null)
            {
                _chatManager = FindObjectOfType<ChatManager>();
                if (_chatManager == null)
                {
                    Debug.LogWarning("VoiceInputView: ChatManager를 찾을 수 없습니다. 수동으로 SetChatManager를 호출해주세요.");
                }
                else
                {
                    Debug.Log("VoiceInputView: ChatManager를 자동으로 찾아서 설정했습니다.");
                }
            }
        }
        
        #endregion
        
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
                
                string transcribedText = await ConvertSpeechToText(audioData);
                
                if (!string.IsNullOrWhiteSpace(transcribedText))
                {
                    if (_chatManager != null)
                    {
                        _chatManager.SendUserMessage(transcribedText);
                    }
                    
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
        
        #region UI 업데이트
        
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
        
        #endregion
        
        #region 음성 처리
        
        /// <summary>
        /// 음성을 텍스트로 변환
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
                if (!_sttService.IsAvailable)
                {
                    await _sttService.InitializeAsync();
                }
                
                string transcribedText = await _sttService.ConvertSpeechToTextAsync(audioData);
                return transcribedText;
            }
            catch (Exception ex)
            {
                Debug.LogError($"STT 변환 실패: {ex.Message}");
                throw;
            }
        }
        
        #endregion
        
        #region 이벤트 핸들러
        
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
        
        #endregion
        
        private void Update()
        {
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