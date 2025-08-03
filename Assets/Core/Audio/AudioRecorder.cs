#nullable enable
using System;
using UnityEngine;
using System.Collections.Generic;

namespace ProjectVG.Core.Audio
{
    public class AudioRecorder : Singleton<AudioRecorder>
    {
        [Header("Recording Settings")]
        [SerializeField] private int _sampleRate = 44100;
        [SerializeField] private int _channels = 1;
        [SerializeField] private int _maxRecordingLength = 30;
        
        private AudioClip _recordingClip;
        private bool _isRecording = false;
        private float _recordingStartTime;
        private List<float> _audioBuffer;
        
        public bool IsRecording => _isRecording;
        public float RecordingDuration => _isRecording ? Time.time - _recordingStartTime : 0f;
        public bool IsRecordingAvailable => Microphone.devices.Length > 0;
        
        public event Action? OnRecordingStarted;
        public event Action? OnRecordingStopped;
        public event Action<AudioClip>? OnRecordingCompleted;
        public event Action<string>? OnError;
        
        protected override void Awake()
        {
            base.Awake();
            _audioBuffer = new List<float>();
        }
        
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
                
                Microphone.End(null);
                
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
        
        public byte[] AudioClipToBytes(AudioClip audioClip)
        {
            if (audioClip == null)
                return new byte[0];
                
            try
            {
                float[] samples = new float[audioClip.samples * audioClip.channels];
                audioClip.GetData(samples, 0);
                
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
        
        private void ProcessRecordingClip()
        {
            if (_recordingClip == null)
                return;
                
            int recordedLength = Microphone.GetPosition(null);
            if (recordedLength <= 0)
            {
                Debug.LogWarning("녹음된 데이터가 없습니다.");
                return;
            }
            
            AudioClip processedClip = AudioClip.Create(
                "RecordedAudio",
                recordedLength,
                _recordingClip.channels,
                _recordingClip.frequency,
                false
            );
            
            float[] samples = new float[recordedLength * _recordingClip.channels];
            _recordingClip.GetData(samples, 0);
            processedClip.SetData(samples, 0);
            
            _recordingClip = processedClip;
        }
        
        private void Update()
        {
            if (_isRecording && RecordingDuration >= _maxRecordingLength)
            {
                StopRecording();
            }
        }
        
        private void OnDestroy()
        {
            if (_isRecording)
            {
                StopRecording();
            }
        }
        
        public string[] GetAvailableMicrophones()
        {
            return Microphone.devices;
        }
        
        public string GetDefaultMicrophone()
        {
            string[] devices = Microphone.devices;
            return devices.Length > 0 ? devices[0] : string.Empty;
        }
    }
} 