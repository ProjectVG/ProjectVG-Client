#nullable enable
using System;
using UnityEngine;
using UnityEngine.Audio;
using ProjectVG.Domain.Chat.Model;
using Cysharp.Threading.Tasks;

namespace ProjectVG.Core.Audio
{
    public class VoiceController : AudioControllerCore, IAudioController
    {
        [Header("Voice Settings")]
        [SerializeField] private bool _autoPlay = true;
        
        private VoiceData? _currentVoice;
        private bool _isPlaying = false;
        
        public string GetControllerName() => "Voice";
        public VoiceData? CurrentVoice => _currentVoice;
        public bool AutoPlay => _autoPlay;
        
        public event Action? OnVoiceFinished;
        public event Action<VoiceData>? OnVoiceStarted;
        public event Action? OnVoiceStopped;
        
        #region Unity Lifecycle
        
        private void Update()
        {
            if (_isPlaying && !_audioSource.isPlaying && _audioSource.clip != null)
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
        
        #region IAudioController Implementation
        
        public void Initialize()
        {
            base.Initialize("VoiceVolume");
            SetVolume(_volume);
        }
        
        protected override AudioMixerGroup? GetAudioMixerGroupFromManager()
        {
            return AudioManager.Instance._voiceGroup;
        }
        
        public override void SetVolume(float volume)
        {
            base.SetVolume(volume);
            
            if (_audioSource != null)
            {
                _audioSource.volume = _volume;
            }
        }
        
        public override void Stop()
        {
            StopVoice();
        }
        
        public override bool IsPlaying()
        {
            return _isPlaying;
        }
        
        #endregion
        
        #region Public Methods
        
        public void PlayVoice(AudioClip? clip)
        {
            if (_audioSource == null)
            {
                Debug.LogWarning("[VoiceController] AudioSource가 설정되지 않았습니다!");
                return;
            }
            
            if (clip == null)
            {
                Debug.LogWarning("[VoiceController] Voice 클립이 null입니다!");
                return;
            }
            
            PrepareAudioSource();
            
            _audioSource.clip = clip;
            _audioSource.Play();
            _isPlaying = true;
            
            Debug.Log($"[VoiceController] Voice 재생 시작: {clip.name}");
        }
        
        public async void PlayVoice(VoiceData voiceData)
        {
            if (voiceData == null || !voiceData.IsPlayable())
            {
                Debug.LogWarning("[VoiceController] 재생할 수 있는 VoiceData가 없습니다.");
                return;
            }
            
            PrepareAudioSource();
            
            await UniTask.Delay(50);
            
            _currentVoice = voiceData;
            _audioSource.clip = voiceData.AudioClip;
            _audioSource.volume = _volume;
            
            if (_autoPlay)
            {
                _audioSource.Play();
                _isPlaying = true;
                OnVoiceStarted?.Invoke(voiceData);
            }
        }
        
        public async UniTask PlayVoiceAsync(VoiceData voiceData)
        {
            if (voiceData == null || !voiceData.IsPlayable())
            {
                Debug.LogWarning("[VoiceController] 재생할 수 있는 VoiceData가 없습니다.");
                return;
            }
            
            PrepareAudioSource();
            
            await UniTask.Delay(50);
            
            _currentVoice = voiceData;
            _audioSource.clip = voiceData.AudioClip;
            _audioSource.volume = _volume;
            
            if (_autoPlay)
            {
                _audioSource.Play();
                _isPlaying = true;
                OnVoiceStarted?.Invoke(voiceData);
                
                await UniTask.WaitUntil(() => !_isPlaying);
            }
        }
        
        public void StopVoice()
        {
            if (_audioSource != null && _audioSource.isPlaying)
            {
                _audioSource.Stop();
                _isPlaying = false;
                OnVoiceStopped?.Invoke();
            }
        }
        
        public void PauseVoice()
        {
            if (_audioSource != null && _audioSource.isPlaying)
            {
                _audioSource.Pause();
                _isPlaying = false;
            }
        }
        
        public void ResumeVoice()
        {
            if (_audioSource != null && _audioSource.clip != null && !_audioSource.isPlaying)
            {
                _audioSource.UnPause();
                _isPlaying = true;
            }
        }
        
        public void SetAutoPlay(bool autoPlay)
        {
            _autoPlay = autoPlay;
        }
        
        public AudioClip? GetCurrentClip()
        {
            return _audioSource?.clip;
        }
        
        #endregion
        
        #region Private Methods
        
        private void PrepareAudioSource()
        {
            if (_audioSource == null) return;
            
            if (_audioSource.isPlaying)
            {
                _audioSource.Stop();
            }
            
            _audioSource.volume = _volume;
            _audioSource.clip = null;
        }
        
        #endregion
    }
} 