#nullable enable
using System;
using UnityEngine;
using ProjectVG.Domain.Chat.Model;
using Cysharp.Threading.Tasks;

namespace ProjectVG.Core.Audio
{
    public class VoiceManager : Singleton<VoiceManager>
    {
        [Header("Voice Audio Source")]
        [SerializeField] private AudioSource _voiceSource;
        
        [Header("Voice Settings")]
        [SerializeField] private float _volume = 1.0f;
        [SerializeField] private bool _autoPlay = true;
        
        private VoiceData? _currentVoice;
        private bool _isPlaying = false;
        
        public bool IsPlaying => _isPlaying;
        public float Volume => _volume;
        public VoiceData? CurrentVoice => _currentVoice;
        
        public event Action? OnVoiceFinished;
        public event Action<VoiceData>? OnVoiceStarted;
        public event Action? OnVoiceStopped;
        
        #region Unity Lifecycle
        
        protected override void Awake()
        {
            base.Awake();
            Initialize();
        }
        
        private void Update()
        {
            if (_isPlaying && !_voiceSource.isPlaying && _voiceSource.clip != null)
            {
                _isPlaying = false;
                OnVoiceFinished?.Invoke();
            }
        }
        
        private void OnDestroy()
        {
            StopVoice();
        }
        
        #endregion
        
        #region Public Methods
        
        public async void PlayVoice(VoiceData voiceData)
        {
            if (voiceData == null || !voiceData.IsPlayable())
            {
                Debug.LogWarning("[VoiceManager] 재생할 수 있는 VoiceData가 없습니다.");
                return;
            }
            
            PrepareAudioSource();
            
            await UniTask.Delay(50);
            
            _currentVoice = voiceData;
            _voiceSource.clip = voiceData.AudioClip;
            _voiceSource.volume = _volume;
            
            if (_autoPlay)
            {
                _voiceSource.Play();
                _isPlaying = true;
                OnVoiceStarted?.Invoke(voiceData);
            }
        }
        
        public async UniTask PlayVoiceAsync(VoiceData voiceData)
        {
            if (voiceData == null || !voiceData.IsPlayable())
            {
                Debug.LogWarning("[VoiceManager] 재생할 수 있는 VoiceData가 없습니다.");
                return;
            }
            
            PrepareAudioSource();
            
            await UniTask.Delay(50);
            
            _currentVoice = voiceData;
            _voiceSource.clip = voiceData.AudioClip;
            _voiceSource.volume = _volume;
            
            if (_autoPlay)
            {
                _voiceSource.Play();
                _isPlaying = true;
                OnVoiceStarted?.Invoke(voiceData);
                
                await UniTask.WaitUntil(() => !_isPlaying);
            }
        }
        
        public void StopVoice()
        {
            if (_voiceSource.isPlaying)
            {
                _voiceSource.Stop();
                _isPlaying = false;
                OnVoiceStopped?.Invoke();
            }
        }
        
        public void PauseVoice()
        {
            if (_voiceSource.isPlaying)
            {
                _voiceSource.Pause();
                _isPlaying = false;
            }
        }
        
        public void ResumeVoice()
        {
            if (_voiceSource.clip != null && !_voiceSource.isPlaying)
            {
                _voiceSource.UnPause();
                _isPlaying = true;
            }
        }
        
        public void SetVolume(float volume)
        {
            _volume = Mathf.Clamp01(volume);
            
            if (_voiceSource != null)
            {
                _voiceSource.volume = _volume;
            }
        }
        
        public void SetAutoPlay(bool autoPlay)
        {
            _autoPlay = autoPlay;
        }
        
        #endregion
        
        #region Private Methods
        
        private void Initialize()
        {
            if (_voiceSource == null)
            {
                _voiceSource = gameObject.AddComponent<AudioSource>();
                _voiceSource.playOnAwake = false;
                _voiceSource.loop = false;
                _voiceSource.volume = _volume;
            }
            
            SetVolume(_volume);
        }
        
        private void PrepareAudioSource()
        {
            if (_voiceSource == null) return;
            
            if (_voiceSource.isPlaying)
            {
                _voiceSource.Stop();
            }
            
            _voiceSource.volume = _volume;
            _voiceSource.clip = null;
        }
        
        #endregion
    }
} 