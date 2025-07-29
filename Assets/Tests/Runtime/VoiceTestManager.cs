using UnityEngine;
using ProjectVG.Core.Audio;
using ProjectVG.Domain.Chat.Model;

namespace ProjectVG.Tests.Runtime
{
    public class VoiceTestManager : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private VoiceManager _voiceManager;
        [SerializeField] private string _testBase64Data = "";
        [SerializeField] private string _testFormat = "wav";
        
        [Header("Test Results")]
        [SerializeField] private bool _isPlaying = false;
        [SerializeField] private float _volume = 1f;
        
        private VoiceData? _testVoiceData;
        
        private void Start()
        {
            if (_voiceManager == null)
            {
                _voiceManager = FindFirstObjectByType<VoiceManager>();
            }
            
            if (_voiceManager != null)
            {
                _voiceManager.OnVoiceStarted += OnVoiceStarted;
                _voiceManager.OnVoiceStopped += OnVoiceStopped;
                _voiceManager.OnVoiceFinished += OnVoiceFinished;
            }
        }
        
        private void Update()
        {
            if (_voiceManager != null)
            {
                _isPlaying = _voiceManager.IsPlaying;
                _volume = _voiceManager.Volume;
            }
        }
        
        [ContextMenu("1. 테스트 VoiceData 생성")]
        public void CreateTestVoiceData()
        {
            if (string.IsNullOrEmpty(_testBase64Data))
            {
                Debug.LogWarning("테스트용 Base64 데이터가 없습니다.");
                return;
            }
            
            _testVoiceData = VoiceData.FromBase64(_testBase64Data, _testFormat);
            
            if (_testVoiceData != null)
            {
                Debug.Log($"테스트 VoiceData 생성 성공: {_testVoiceData.Format}, 길이: {_testVoiceData.Length:F2}초");
            }
            else
            {
                Debug.LogError("테스트 VoiceData 생성 실패");
            }
        }
        
        [ContextMenu("2. 음성 재생")]
        public void PlayVoice()
        {
            if (_voiceManager == null)
            {
                Debug.LogError("VoiceManager가 없습니다.");
                return;
            }
            
            if (_testVoiceData == null)
            {
                Debug.LogWarning("테스트 VoiceData가 없습니다. 먼저 생성하세요.");
                return;
            }
            
            _voiceManager.PlayVoice(_testVoiceData);
        }
        
        [ContextMenu("3. 음성 중지")]
        public void StopVoice()
        {
            if (_voiceManager != null)
            {
                _voiceManager.StopVoice();
            }
        }
        
        [ContextMenu("4. 음성 일시정지")]
        public void PauseVoice()
        {
            if (_voiceManager != null)
            {
                _voiceManager.PauseVoice();
            }
        }
        
        [ContextMenu("5. 음성 재개")]
        public void ResumeVoice()
        {
            if (_voiceManager != null)
            {
                _voiceManager.ResumeVoice();
            }
        }
        
        [ContextMenu("6. 볼륨 설정 (0.5)")]
        public void SetVolumeHalf()
        {
            if (_voiceManager != null)
            {
                _voiceManager.SetVolume(0.5f);
            }
        }
        
        [ContextMenu("7. 볼륨 설정 (1.0)")]
        public void SetVolumeFull()
        {
            if (_voiceManager != null)
            {
                _voiceManager.SetVolume(1.0f);
            }
        }
        
        [ContextMenu("8. 자동 재생 토글")]
        public void ToggleAutoPlay()
        {
            if (_voiceManager != null)
            {
                bool currentAutoPlay = _voiceManager.IsPlaying;
                _voiceManager.SetAutoPlay(!currentAutoPlay);
                Debug.Log($"자동 재생: {!currentAutoPlay}");
            }
        }
        
        private void OnVoiceStarted(VoiceData voiceData)
        {
            Debug.Log($"음성 재생 시작됨: {voiceData.Format}, 길이: {voiceData.Length:F2}초");
        }
        
        private void OnVoiceStopped()
        {
            Debug.Log("음성 재생 중지됨");
        }
        
        private void OnVoiceFinished()
        {
            Debug.Log("음성 재생 완료됨");
        }
        
        private void OnDestroy()
        {
            if (_voiceManager != null)
            {
                _voiceManager.OnVoiceStarted -= OnVoiceStarted;
                _voiceManager.OnVoiceStopped -= OnVoiceStopped;
                _voiceManager.OnVoiceFinished -= OnVoiceFinished;
            }
        }
    }
} 