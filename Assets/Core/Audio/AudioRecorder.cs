#nullable enable
using System;
using UnityEngine;
using System.Collections.Generic;

namespace ProjectVG.Core.Audio
{
    /// <summary>
    /// 음성 녹음 기능을 제공하는 클래스
    /// </summary>
    public class AudioRecorder : MonoBehaviour
    {
        [Header("Recording Settings")]
        [SerializeField] private int _sampleRate = 44100;
        [SerializeField] private int _channels = 1;
        [SerializeField] private int _maxRecordingLength = 30;
        
        private AudioClip _recordingClip;
        private bool _isRecording = false;
        private float _recordingStartTime;
        private List<float> _audioBuffer;
        
        public static AudioRecorder Instance { get; private set; }
        
        public bool IsRecording => _isRecording;
        public float RecordingDuration => _isRecording ? Time.time - _recordingStartTime : 0f;
        public bool IsRecordingAvailable => Microphone.devices.Length > 0;
        
        public event Action? OnRecordingStarted;
        public event Action? OnRecordingStopped;
        public event Action<AudioClip>? OnRecordingCompleted;
        public event Action<string>? OnError;
        
        private void Awake()
        {
            InitializeSingleton();
            _audioBuffer = new List<float>();
        }
        
        private void InitializeSingleton()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// 녹음 시작
        /// </summary>
        /// <returns>녹음 시작 성공 여부</returns>
        public bool StartRecording()
        {
            if (_isRecording)
            {
                Debug.LogWarning("이미 녹음 중입니다.");
                return false;
            }
            
            if (!IsRecordingAvailable)
            {
                Debug.LogError("마이크가 사용 불가능합니다.");
                OnError?.Invoke("마이크가 사용 불가능합니다.");
                return false;
            }
            
            try
            {
                _isRecording = true;
                _recordingStartTime = Time.time;
                _audioBuffer.Clear();
                
                // 마이크에서 녹음 시작
                _recordingClip = Microphone.Start(null, false, _maxRecordingLength, _sampleRate);
                
                OnRecordingStarted?.Invoke();
                Debug.Log("음성 녹음 시작");
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"녹음 시작 실패: {ex.Message}");
                OnError?.Invoke($"녹음 시작 실패: {ex.Message}");
                _isRecording = false;
                return false;
            }
        }
        
        /// <summary>
        /// 녹음 중지
        /// </summary>
        /// <returns>녹음된 AudioClip</returns>
        public AudioClip? StopRecording()
        {
            if (!_isRecording)
            {
                Debug.LogWarning("녹음 중이 아닙니다.");
                return null;
            }
            
            try
            {
                _isRecording = false;
                
                // 마이크 녹음 중지
                Microphone.End(null);
                
                // 녹음된 데이터 처리
                if (_recordingClip != null)
                {
                    ProcessRecordingClip();
                    OnRecordingCompleted?.Invoke(_recordingClip);
                }
                
                OnRecordingStopped?.Invoke();
                Debug.Log("음성 녹음 중지");
                
                return _recordingClip;
            }
            catch (Exception ex)
            {
                Debug.LogError($"녹음 중지 실패: {ex.Message}");
                OnError?.Invoke($"녹음 중지 실패: {ex.Message}");
                _isRecording = false;
                return null;
            }
        }
        
        /// <summary>
        /// 녹음된 AudioClip을 byte 배열로 변환
        /// </summary>
        /// <param name="audioClip">변환할 AudioClip</param>
        /// <returns>byte 배열</returns>
        public byte[] AudioClipToBytes(AudioClip audioClip)
        {
            if (audioClip == null)
                return new byte[0];
                
            try
            {
                // AudioClip의 샘플 데이터 가져오기
                float[] samples = new float[audioClip.samples * audioClip.channels];
                audioClip.GetData(samples, 0);
                
                // float 배열을 byte 배열로 변환 (16비트 PCM)
                byte[] audioBytes = new byte[samples.Length * 2];
                for (int i = 0; i < samples.Length; i++)
                {
                    short sample = (short)(samples[i] * short.MaxValue);
                    BitConverter.GetBytes(sample).CopyTo(audioBytes, i * 2);
                }
                
                return audioBytes;
            }
            catch (Exception ex)
            {
                Debug.LogError($"AudioClip을 byte 배열로 변환 실패: {ex.Message}");
                return new byte[0];
            }
        }
        
        /// <summary>
        /// 녹음된 AudioClip 처리
        /// </summary>
        private void ProcessRecordingClip()
        {
            if (_recordingClip == null)
                return;
                
            // 녹음된 실제 길이 계산
            int recordedLength = Microphone.GetPosition(null);
            if (recordedLength <= 0)
            {
                Debug.LogWarning("녹음된 데이터가 없습니다.");
                return;
            }
            
            // 새로운 AudioClip 생성 (실제 녹음된 길이만큼)
            AudioClip processedClip = AudioClip.Create(
                "RecordedAudio",
                recordedLength,
                _recordingClip.channels,
                _recordingClip.frequency,
                false
            );
            
            // 데이터 복사
            float[] samples = new float[recordedLength * _recordingClip.channels];
            _recordingClip.GetData(samples, 0);
            processedClip.SetData(samples, 0);
            
            _recordingClip = processedClip;
        }
        
        /// <summary>
        /// 녹음 시간 제한 체크
        /// </summary>
        private void Update()
        {
            if (_isRecording && RecordingDuration >= _maxRecordingLength)
            {
                StopRecording();
            }
        }
        
        /// <summary>
        /// 녹음 중지 (자동 정리)
        /// </summary>
        private void OnDestroy()
        {
            if (_isRecording)
            {
                StopRecording();
            }
        }
        
        /// <summary>
        /// 사용 가능한 마이크 목록 가져오기
        /// </summary>
        /// <returns>마이크 이름 배열</returns>
        public string[] GetAvailableMicrophones()
        {
            return Microphone.devices;
        }
        
        /// <summary>
        /// 기본 마이크 이름 가져오기
        /// </summary>
        /// <returns>기본 마이크 이름</returns>
        public string GetDefaultMicrophone()
        {
            string[] devices = Microphone.devices;
            return devices.Length > 0 ? devices[0] : string.Empty;
        }
    }
} 