#nullable enable
using UnityEngine;
using UnityEngine.Audio;

namespace ProjectVG.Core.Audio
{
    public class AudioControllerCore : MonoBehaviour
    {
        [SerializeField] protected AudioSource? _audioSource;
        [SerializeField] protected AudioMixerGroup? _audioMixerGroup;
        
        protected float _volume = 1f;
        protected string _volumeParameterName = "";
        
        public virtual void Initialize(string volumeParameterName)
        {
            _volumeParameterName = volumeParameterName;
            
            // AudioSource가 없으면 자동 생성
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.loop = false;
                _audioSource.volume = _volume;
            }
            
            // AudioMixerGroup이 설정되지 않은 경우 AudioManager에서 자동 할당
            if (_audioMixerGroup == null)
            {
                var group = GetAudioMixerGroupFromManager();
                if (group != null)
                {
                    _audioMixerGroup = group;
                }
            }
            
            if (_audioSource != null && _audioMixerGroup != null)
            {
                _audioSource.outputAudioMixerGroup = _audioMixerGroup;
            }
        }
        
        protected virtual AudioMixerGroup? GetAudioMixerGroupFromManager()
        {
            // 하위 클래스에서 오버라이드하여 적절한 그룹 반환
            return null;
        }
        
        public virtual void SetVolume(float volume)
        {
            _volume = Mathf.Clamp01(volume);
            
            if (_audioMixerGroup != null && !string.IsNullOrEmpty(_volumeParameterName))
            {
                float dbValue = _volume > 0 ? 20f * Mathf.Log10(_volume) : -80f;
                _audioMixerGroup.audioMixer.SetFloat(_volumeParameterName, dbValue);
            }
        }
        
        public virtual void Stop()
        {
            if (_audioSource != null)
            {
                _audioSource.Stop();
            }
        }
        
        public virtual bool IsPlaying()
        {
            return _audioSource != null && _audioSource.isPlaying;
        }
        
        public virtual float GetVolume()
        {
            return _volume;
        }
        
        public AudioSource? GetAudioSource() => _audioSource;
        public AudioMixerGroup? GetAudioMixerGroup() => _audioMixerGroup;
    }
}
