#nullable enable
using UnityEngine;
using UnityEngine.Audio;

namespace ProjectVG.Core.Audio
{
    public class BGMController : AudioControllerCore, IAudioController
    {
        public string GetControllerName() => "BGM";
        
        public void Initialize()
        {
            base.Initialize("BGMVolume");
        }
        
        protected override AudioMixerGroup? GetAudioMixerGroupFromManager()
        {
            return AudioManager.Instance._bgmGroup;
        }
        
        public void PlayBGM(AudioClip? clip, bool loop = true)
        {
            if (_audioSource == null)
            {
                Debug.LogWarning("[BGMController] AudioSource가 설정되지 않았습니다!");
                return;
            }
            
            if (clip == null)
            {
                Debug.LogWarning("[BGMController] BGM 클립이 null입니다!");
                return;
            }
            
            _audioSource.clip = clip;
            _audioSource.loop = loop;
            _audioSource.Play();
            
            Debug.Log($"[BGMController] BGM 재생 시작: {clip.name}");
        }
        
        public void StopBGM()
        {
            Stop();
            Debug.Log("[BGMController] BGM 재생 중지");
        }
        
        public AudioClip? GetCurrentClip()
        {
            return _audioSource?.clip;
        }
    }
}
