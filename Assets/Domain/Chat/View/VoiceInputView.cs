#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectVG.Domain.Chat.Service;
using ProjectVG.Infrastructure.Network.Services;
using ProjectVG.Core.Audio;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProjectVG.Domain.Chat.View
{
    public class VoiceInputView : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Button? _btnVoice;
        [SerializeField] private Button? _btnVoiceStop;
        [SerializeField] private TextMeshProUGUI? _txtVoiceStatus;
        [SerializeField] private Slider? _progressBar; // 녹음 진행률 표시
        
        [Header("Voice Settings")]
        [SerializeField] private float _maxRecordingTime = 30f;
        [SerializeField] private string _voiceStatusRecording = "Recording..."; // "녹음 중..."에서 변경
        [SerializeField] private string _voiceStatusProcessing = "Converting speech to text..."; // "음성을 텍스트로 변환 중..."에서 변경
        
        [Header("Debug Settings")]
        [SerializeField] private bool _saveRecordingToFile = true;
        
        private ChatManager? _chatManager;
        private AudioRecorder? _audioRecorder;
        private STTService? _sttService;
        private bool _isRecording = false;
        private float _recordingStartTime;
        
        public event Action<string>? OnVoiceMessageSent;
        public event Action<string>? OnError;
        
        #region Unity Lifecycle
        
        private void Start()
        {
            Initialize();
        }
        
        private void Update()
        {
            // 새로운 AudioRecorder는 자체적으로 최대 시간을 관리하므로 제거
            // if (_isRecording && Time.time - _recordingStartTime > _maxRecordingTime)
            // {
            //     StopVoiceRecording();
            // }
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
                _audioRecorder.OnRecordingProgress -= OnRecordingProgress;
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
        
        public void SetChatManager(ChatManager chatManager)
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
                UpdateVoiceStatus(_voiceStatusProcessing);
                
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
                UpdateVoiceStatus(string.Empty);
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
                UpdateVoiceStatus(_voiceStatusRecording);
                UpdateProgressBar(0f);
                
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
                
            if (_audioRecorder == null)
            {
                Debug.LogError("[VoiceInputView] AudioRecorder가 없습니다.");
                return;
            }
                
            try
            {
                _isRecording = false;
                UpdateVoiceButtonState(false);
                UpdateVoiceStatus(string.Empty);
                UpdateProgressBar(0f);
                
                AudioClip? recordedClip = _audioRecorder.StopRecording();
                if (recordedClip != null)
                {
                    // 디버깅을 위한 파일 저장
                    if (_saveRecordingToFile)
                    {
                        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        _audioRecorder.SaveRecordingToFile(recordedClip, $"voice_recording_{timestamp}");
                    }
                    
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
            if (_btnVoice == null)
            {
                _btnVoice = transform.Find("BtnVoice")?.GetComponent<Button>();
                if (_btnVoice == null)
                {
                    Debug.LogWarning("[VoiceInputView] BtnVoice 버튼을 찾을 수 없습니다.");
                }
            }
                
            if (_btnVoiceStop == null)
            {
                _btnVoiceStop = transform.Find("BtnVoiceStop")?.GetComponent<Button>();
                if (_btnVoiceStop == null)
                {
                    Debug.LogWarning("[VoiceInputView] BtnVoiceStop 버튼을 찾을 수 없습니다.");
                }
            }
                
            if (_txtVoiceStatus == null)
            {
                _txtVoiceStatus = transform.Find("TxtVoiceStatus")?.GetComponent<TextMeshProUGUI>();
                if (_txtVoiceStatus == null)
                {
                    Debug.LogWarning("[VoiceInputView] TxtVoiceStatus 텍스트를 찾을 수 없습니다.");
                }
            }
            
            if (_progressBar == null)
            {
                _progressBar = transform.Find("ProgressBar")?.GetComponent<Slider>();
                if (_progressBar == null)
                {
                    Debug.LogWarning("[VoiceInputView] ProgressBar 슬라이더를 찾을 수 없습니다.");
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
            if (_btnVoice != null)
                _btnVoice.onClick.AddListener(OnVoiceButtonClicked);
                
            if (_btnVoiceStop != null)
                _btnVoiceStop.onClick.AddListener(OnVoiceStopButtonClicked);
                
            if (_audioRecorder != null)
            {
                _audioRecorder.OnRecordingStarted += OnRecordingStarted;
                _audioRecorder.OnRecordingStopped += OnRecordingStopped;
                _audioRecorder.OnRecordingCompleted += OnRecordingCompleted;
                _audioRecorder.OnRecordingProgress += OnRecordingProgress;
                _audioRecorder.OnError += OnRecordingError;
            }
        }
        
        private void SetupChatManager()
        {
            if (_chatManager == null)
            {
                _chatManager = FindAnyObjectByType<ChatManager>();
                if (_chatManager == null)
                {
                    Debug.LogWarning("[VoiceInputView] ChatManager를 찾을 수 없습니다. 수동으로 SetChatManager를 호출해주세요.");
                }
            }
        }
        
        private void UpdateVoiceButtonState(bool isRecording)
        {
            if (_btnVoice != null)
                _btnVoice.gameObject.SetActive(!isRecording);
                
            if (_btnVoiceStop != null)
                _btnVoiceStop.gameObject.SetActive(isRecording);
        }
        
        private void UpdateVoiceStatus(string status)
        {
            if (_txtVoiceStatus != null)
            {
                _txtVoiceStatus.text = status;
                _txtVoiceStatus.gameObject.SetActive(!string.IsNullOrEmpty(status));
            }
        }
        
        private void UpdateProgressBar(float progress)
        {
            if (_progressBar != null)
            {
                _progressBar.value = progress;
                _progressBar.gameObject.SetActive(progress > 0f);
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
                Debug.Log("[VoiceInputView] 더미 음성으로 STT 서버 테스트 시작");
                byte[] dummyAudio = _sttService.GenerateTestAudioData();
                string result = await _sttService.ConvertSpeechToTextAsync(dummyAudio);
                Debug.Log($"[VoiceInputView] 더미 음성 테스트 결과: '{result}'");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VoiceInputView] 더미 음성 테스트 실패: {ex.Message}");
            }
        }
        
        private void OnVoiceButtonClicked()
        {
            StartVoiceRecording();
        }
        
        private void OnVoiceStopButtonClicked()
        {
            StopVoiceRecording();
        }
        
        private void OnRecordingStarted()
        {
            Debug.Log("[VoiceInputView] 녹음 시작됨");
        }
        
        private void OnRecordingStopped()
        {
            Debug.Log("[VoiceInputView] 녹음 중지됨");
        }
        
        private void OnRecordingCompleted(AudioClip audioClip)
        {
            Debug.Log($"[VoiceInputView] 녹음 완료 - 샘플: {audioClip.samples}, 길이: {audioClip.length:F2}초");
        }
        
        private void OnRecordingProgress(float progress)
        {
            UpdateProgressBar(progress);
            // Debug.Log($"[VoiceInputView] 녹음 진행률: {progress:P0}"); // 디버그 메시지 제거
        }
        
        private void OnRecordingError(string error)
        {
            Debug.LogError($"[VoiceInputView] 녹음 오류: {error}");
            OnError?.Invoke(error);
        }
        
        #endregion
    }
} 