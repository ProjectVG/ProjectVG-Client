using System;
using UnityEngine;
using ProjectVG.Domain.Chat.Model;
using Cysharp.Threading.Tasks;

namespace ProjectVG.Core.Audio
{
    /// <summary>
    /// Voice 재생을 전담하는 매니저
    /// VoiceData를 받아서 음성을 재생하고 제어합니다.
    /// </summary>
    public class VoiceManager : MonoBehaviour
    {
        [Header("Voice Audio Source")]
        [SerializeField] private AudioSource _voiceSource;
        
        [Header("Voice Settings")]
        [SerializeField] private float _volume = 1.0f;
        [SerializeField] private bool _autoPlay = true;
        
        private VoiceData? _currentVoice;
        private bool _isPlaying = false;
        
        public static VoiceManager Instance { get; private set; }
        
        public bool IsPlaying => _isPlaying;
        public float Volume => _volume;
        public VoiceData? CurrentVoice => _currentVoice;
        
        public event Action OnVoiceFinished;
        public event Action<VoiceData> OnVoiceStarted;
        public event Action OnVoiceStopped;
        
        private void Awake()
        {
            InitializeSingleton();
            InitializeVoiceSource();
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
        
        private void InitializeVoiceSource()
        {
            if (_voiceSource == null)
            {
                _voiceSource = gameObject.AddComponent<AudioSource>();
                _voiceSource.playOnAwake = false;
                _voiceSource.loop = false;
                _voiceSource.volume = _volume; // 초기 볼륨을 설정된 볼륨으로
            }
            
            SetVolume(_volume);
        }
        
        /// <summary>
        /// 재생 전 AudioSource 완전 정리
        /// </summary>
        private void PrepareAudioSource()
        {
            if (_voiceSource == null) return;
            
            // 이전 재생 완전 중지
            if (_voiceSource.isPlaying)
            {
                _voiceSource.Stop();
            }
            
            // 볼륨을 설정된 값으로 초기화
            _voiceSource.volume = _volume;
            
            // 클립 완전 정리
            _voiceSource.clip = null;
        }
        
        /// <summary>
        /// VoiceData를 사용하여 음성을 재생합니다.
        /// </summary>
        /// <param name="voiceData">재생할 VoiceData</param>
        public async void PlayVoice(VoiceData voiceData)
        {
            if (voiceData == null || !voiceData.IsPlayable())
            {
                Debug.LogWarning("재생할 수 있는 VoiceData가 없습니다.");
                return;
            }
            
            PrepareAudioSource();
            
            // 재생 전 짧은 대기 (터짐 방지)
            await UniTask.Delay(50); // 50ms 대기
            
            _currentVoice = voiceData;
            _voiceSource.clip = voiceData.AudioClip;
            _voiceSource.volume = _volume; // 고정 볼륨 설정
            
            if (_autoPlay)
            {
                _voiceSource.Play();
                _isPlaying = true;
                OnVoiceStarted?.Invoke(voiceData);
                
                Debug.Log($"음성 재생 시작: {voiceData.Format}, 길이: {voiceData.Length:F2}초");
            }
        }
        
        /// <summary>
        /// VoiceData를 재생하고 완료될 때까지 기다립니다.
        /// </summary>
        /// <param name="voiceData">재생할 VoiceData</param>
        /// <returns>재생 완료까지 기다리는 UniTask</returns>
        public async UniTask PlayVoiceAsync(VoiceData voiceData)
        {
            if (voiceData == null || !voiceData.IsPlayable())
            {
                Debug.LogWarning("재생할 수 있는 VoiceData가 없습니다.");
                return;
            }
            
            PrepareAudioSource();
            
            // 재생 전 짧은 대기 (터짐 방지)
            await UniTask.Delay(50); // 50ms 대기
            
            _currentVoice = voiceData;
            _voiceSource.clip = voiceData.AudioClip;
            _voiceSource.volume = _volume; // 고정 볼륨 설정
            
            if (_autoPlay)
            {
                _voiceSource.Play();
                _isPlaying = true;
                OnVoiceStarted?.Invoke(voiceData);
                
                Debug.Log($"음성 재생 시작: {voiceData.Format}, 길이: {voiceData.Length:F2}초");
                
                // 재생 완료까지 대기
                await UniTask.WaitUntil(() => !_isPlaying);
                
                Debug.Log("음성 재생 완료 (Async)");
            }
        }
        
        /// <summary>
        /// 현재 재생 중인 음성을 중지합니다.
        /// </summary>
        public void StopVoice()
        {
            if (_voiceSource.isPlaying)
            {
                _voiceSource.Stop();
                _isPlaying = false;
                OnVoiceStopped?.Invoke();
                
                Debug.Log("음성 재생 중지");
            }
        }
        
        /// <summary>
        /// 현재 재생 중인 음성을 일시정지합니다.
        /// </summary>
        public void PauseVoice()
        {
            if (_voiceSource.isPlaying)
            {
                _voiceSource.Pause();
                _isPlaying = false;
                
                Debug.Log("음성 재생 일시정지");
            }
        }
        
        /// <summary>
        /// 일시정지된 음성을 재개합니다.
        /// </summary>
        public void ResumeVoice()
        {
            if (_voiceSource.clip != null && !_voiceSource.isPlaying)
            {
                _voiceSource.UnPause();
                _isPlaying = true;
                
                Debug.Log("음성 재생 재개");
            }
        }
        
        /// <summary>
        /// 음성 볼륨을 설정합니다.
        /// </summary>
        /// <param name="volume">볼륨 값 (0.0 ~ 1.0)</param>
        public void SetVolume(float volume)
        {
            _volume = Mathf.Clamp01(volume);
            
            if (_voiceSource != null)
            {
                _voiceSource.volume = _volume;
            }
            
            Debug.Log($"음성 볼륨 설정: {_volume:F2}");
        }
        
        /// <summary>
        /// 자동 재생 기능을 설정합니다.
        /// </summary>
        /// <param name="autoPlay">자동 재생 여부</param>
        public void SetAutoPlay(bool autoPlay)
        {
            _autoPlay = autoPlay;
        }
        
        private void Update()
        {
            // 재생 완료 감지
            if (_isPlaying && !_voiceSource.isPlaying && _voiceSource.clip != null)
            {
                _isPlaying = false;
                OnVoiceFinished?.Invoke();
                
                Debug.Log("음성 재생 완료");
            }
        }
        
        private void OnDestroy()
        {
            StopVoice();
        }
    }
} 