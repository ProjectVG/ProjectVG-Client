#nullable enable
using System;
using UnityEngine;
using System.Collections.Generic;
using ProjectVG.Infrastructure.Audio;

namespace ProjectVG.Core.Audio
{
    /// <summary>
    /// 정확한 시간 기반 음성 녹음 시스템
    /// 녹음 시작/중지 시간을 기반으로 정확한 길이의 오디오를 생성합니다.
    /// </summary>
    public class AudioRecorder : Singleton<AudioRecorder>
    {
        [Header("Recording Settings")]
        [SerializeField] private int _sampleRate = 44100;
        [SerializeField] private int _channels = 1;
        [SerializeField] private int _maxRecordingLength = 30; // 최대 녹음 시간 (초)
        
        [Header("Audio Processing")]
        [SerializeField] private bool _enableNoiseReduction = false; // 노이즈 제거 비활성화
        [SerializeField] private float _silenceThreshold = 0.001f; // 무음 임계값 낮춤
        
        private AudioClip? _recordingClip;
        private bool _isRecording = false;
        private float _recordingStartTime;
        private float _recordingEndTime;
        private string _currentDevice = null;
        
        // 이벤트
        public event Action? OnRecordingStarted;
        public event Action? OnRecordingStopped;
        public event Action<AudioClip>? OnRecordingCompleted;
        public event Action<string>? OnError;
        public event Action<float>? OnRecordingProgress; // 녹음 진행률 (0-1)
        
        // 프로퍼티
        public bool IsRecording => _isRecording;
        public float RecordingDuration => _isRecording ? Time.time - _recordingStartTime : 0f;
        public bool IsRecordingAvailable => Microphone.devices.Length > 0;
        public float RecordingProgress => _isRecording ? Mathf.Clamp01(RecordingDuration / _maxRecordingLength) : 0f;
        
        #region Unity Lifecycle
        
        protected override void Awake()
        {
            base.Awake();
            InitializeMicrophone();
        }
        
        private void Update()
        {
            if (_isRecording)
            {
                // 녹음 진행률 이벤트 발생
                OnRecordingProgress?.Invoke(RecordingProgress);
                
                // 최대 녹음 시간 체크
                if (RecordingDuration >= _maxRecordingLength)
                {
                    StopRecording();
                }
            }
        }
        
        private void OnDestroy()
        {
            if (_isRecording)
            {
                StopRecording();
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// 음성 녹음 시작
        /// </summary>
        /// <returns>녹음 시작 성공 여부</returns>
        public bool StartRecording()
        {
            if (_isRecording)
            {
                Debug.LogWarning("[AudioRecorder] 이미 녹음 중입니다.");
                return false;
            }
            
            if (!IsRecordingAvailable)
            {
                Debug.LogError("[AudioRecorder] 마이크가 사용 불가능합니다.");
                OnError?.Invoke("마이크가 사용 불가능합니다.");
                return false;
            }
            
            try
            {
                _isRecording = true;
                _recordingStartTime = Time.time;
                
                // 최대 녹음 시간만큼 버퍼 할당
                _recordingClip = Microphone.Start(_currentDevice, false, _maxRecordingLength, _sampleRate);
                
                Debug.Log($"[AudioRecorder] 녹음 시작 - 최대 시간: {_maxRecordingLength}초, 샘플레이트: {_sampleRate}Hz");
                OnRecordingStarted?.Invoke();
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AudioRecorder] 녹음 시작 실패: {ex.Message}");
                OnError?.Invoke($"녹음 시작 실패: {ex.Message}");
                _isRecording = false;
                return false;
            }
        }
        
        /// <summary>
        /// 음성 녹음 중지
        /// </summary>
        /// <returns>처리된 AudioClip</returns>
        public AudioClip? StopRecording()
        {
            if (!_isRecording)
            {
                Debug.LogWarning("[AudioRecorder] 녹음 중이 아닙니다.");
                return null;
            }
            
            try
            {
                _isRecording = false;
                _recordingEndTime = Time.time;
                float actualRecordingDuration = _recordingEndTime - _recordingStartTime;
                
                Microphone.End(_currentDevice);
                
                if (_recordingClip != null)
                {
                    AudioClip processedClip = ProcessRecordingClip(actualRecordingDuration);
                    if (processedClip != null)
                    {
                        Debug.Log($"[AudioRecorder] 녹음 완료 - 실제 녹음 시간: {actualRecordingDuration:F2}초, 샘플: {processedClip.samples}");
                        OnRecordingCompleted?.Invoke(processedClip);
                    }
                }
                
                OnRecordingStopped?.Invoke();
                return _recordingClip;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AudioRecorder] 녹음 중지 실패: {ex.Message}");
                OnError?.Invoke($"녹음 중지 실패: {ex.Message}");
                _isRecording = false;
                return null;
            }
        }
        
        /// <summary>
        /// AudioClip을 WAV 바이트 배열로 변환
        /// </summary>
        public byte[] AudioClipToWavBytes(AudioClip audioClip)
        {
            if (audioClip == null)
                return Array.Empty<byte>();
            try
            {
                return WavEncoder.FromAudioClip(audioClip);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AudioRecorder] WAV 변환 실패: {ex.Message}");
                return Array.Empty<byte>();
            }
        }
        
        /// <summary>
        /// 녹음 파일 저장 (디버깅용)
        /// </summary>
        public bool SaveRecordingToFile(AudioClip audioClip, string fileName = "recording")
        {
            if (audioClip == null)
            {
                Debug.LogError("[AudioRecorder] 저장할 AudioClip이 null입니다.");
                return false;
            }

            try
            {
                byte[] wavData = AudioClipToWavBytes(audioClip);
                if (wavData.Length == 0)
                {
                    Debug.LogError("[AudioRecorder] WAV 데이터 변환 실패");
                    return false;
                }

                string filePath = System.IO.Path.Combine(Application.persistentDataPath, $"{fileName}.wav");
                System.IO.File.WriteAllBytes(filePath, wavData);
                
                Debug.Log($"[AudioRecorder] 녹음 파일 저장 완료: {filePath}");
                Debug.Log($"[AudioRecorder] 파일 크기: {wavData.Length} bytes");
                Debug.Log($"[AudioRecorder] AudioClip 정보 - 샘플: {audioClip.samples}, 채널: {audioClip.channels}, 주파수: {audioClip.frequency}");
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AudioRecorder] 파일 저장 실패: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 사용 가능한 마이크 목록 반환
        /// </summary>
        public string[] GetAvailableMicrophones()
        {
            return Microphone.devices;
        }
        
        /// <summary>
        /// 기본 마이크 반환
        /// </summary>
        public string GetDefaultMicrophone()
        {
            string[] devices = Microphone.devices;
            return devices.Length > 0 ? devices[0] : string.Empty;
        }
        
        /// <summary>
        /// 현재 마이크 설정
        /// </summary>
        public void SetMicrophone(string deviceName)
        {
            if (Array.Exists(Microphone.devices, device => device == deviceName))
            {
                _currentDevice = deviceName;
                Debug.Log($"[AudioRecorder] 마이크 설정 변경: {deviceName}");
            }
            else
            {
                Debug.LogWarning($"[AudioRecorder] 존재하지 않는 마이크: {deviceName}");
            }
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// 마이크 초기화
        /// </summary>
        private void InitializeMicrophone()
        {
            string[] devices = Microphone.devices;
            if (devices.Length > 0)
            {
                _currentDevice = devices[0];
                Debug.Log($"[AudioRecorder] 기본 마이크 설정: {_currentDevice}");
            }
            else
            {
                Debug.LogError("[AudioRecorder] 사용 가능한 마이크가 없습니다.");
            }
        }
        
        /// <summary>
        /// 녹음된 AudioClip 처리
        /// </summary>
        private AudioClip? ProcessRecordingClip(float actualDuration)
        {
            if (_recordingClip == null)
                return null;
                
            // 실제 녹음 시간을 기반으로 샘플 수 계산
            int actualSamples = Mathf.RoundToInt(actualDuration * _sampleRate);
            
            // 최대 샘플 수 제한 (버퍼 크기)
            int maxSamples = _recordingClip.samples;
            actualSamples = Mathf.Min(actualSamples, maxSamples);
            
            Debug.Log($"[AudioRecorder] 실제 녹음 길이: {actualSamples} 샘플, 전체 버퍼: {_recordingClip.samples} 샘플, 실제 시간: {actualDuration:F2}초");
            
            if (actualSamples <= 0)
            {
                Debug.LogWarning("[AudioRecorder] 녹음된 데이터가 없습니다.");
                return null;
            }
            
            // 실제 녹음된 길이만큼만 새로운 AudioClip 생성
            AudioClip processedClip = AudioClip.Create(
                "RecordedAudio",
                actualSamples,
                _recordingClip.channels,
                _recordingClip.frequency,
                false
            );
            
            float[] samples = new float[actualSamples * _recordingClip.channels];
            _recordingClip.GetData(samples, 0);
            
            // 노이즈 리덕션 적용
            if (_enableNoiseReduction)
            {
                ApplyNoiseReduction(samples);
            }
            
            processedClip.SetData(samples, 0);
            _recordingClip = processedClip;
            
            Debug.Log($"[AudioRecorder] 처리된 AudioClip - 샘플: {_recordingClip.samples}, 채널: {_recordingClip.channels}, 주파수: {_recordingClip.frequency}");
            
            return _recordingClip;
        }
        
        /// <summary>
        /// 노이즈 리덕션 적용
        /// </summary>
        private void ApplyNoiseReduction(float[] audioData)
        {
            for (int i = 0; i < audioData.Length; i++)
            {
                if (Mathf.Abs(audioData[i]) < _silenceThreshold)
                {
                    audioData[i] = 0f;
                }
            }
        }
        
        #endregion
    }
} 