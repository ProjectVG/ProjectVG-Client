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