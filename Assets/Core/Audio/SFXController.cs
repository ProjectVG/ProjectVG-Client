#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace ProjectVG.Core.Audio
{
    public class SFXController : AudioControllerCore, IAudioController
    {
        [Header("SFX Settings")]
        [SerializeField] private int _poolSize = 10;
        
        private Queue<AudioSource> _pool = new Queue<AudioSource>();
        private List<AudioSource> _activeSources = new List<AudioSource>();
        
        public string GetControllerName() => "SFX";
        
        public void Initialize()
        {
            base.Initialize("SFXVolume");
            CreatePool();
        }
        
        protected override AudioMixerGroup? GetAudioMixerGroupFromManager()
        {
            return AudioManager.Instance._sfxGroup;
        }
        
        public void Stop()
        {
            foreach (var source in _activeSources.ToArray())
            {
                if (source != null)
                {
                    source.Stop();
                    ReturnToPool(source);
                }
            }
        }
        
        public bool IsPlaying()
        {
            return _activeSources.Count > 0;
        }
        
        public void PlaySFX(AudioClip? clip, Vector3? position = null)
        {
            if (clip == null)
            {
                Debug.LogWarning("[SFXController] SFX 클립이 null입니다!");
                return;
            }
            
            AudioSource? audioSource = GetPooledSource();
            if (audioSource == null)
            {
                Debug.LogWarning("[SFXController] 사용 가능한 AudioSource가 없습니다!");
                return;
            }
            
            audioSource.clip = clip;
            audioSource.loop = false;
            
            if (position.HasValue)
            {
                audioSource.transform.position = position.Value;
                audioSource.spatialBlend = 1f;
            }
            else
            {
                audioSource.spatialBlend = 0f;
            }
            
            audioSource.Play();
            
            StartCoroutine(ReturnToPoolWhenFinished(audioSource));
            
            Debug.Log($"[SFXController] SFX 재생: {clip.name}");
        }
        
        private void CreatePool()
        {
            for (int i = 0; i < _poolSize; i++)
            {
                GameObject poolObject = new GameObject($"PooledSFXSource_{i}");
                poolObject.transform.SetParent(transform);
                
                AudioSource audioSource = poolObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.outputAudioMixerGroup = GetAudioMixerGroupFromManager();
                
                _pool.Enqueue(audioSource);
            }
            
            Debug.Log($"[SFXController] SFX AudioSource 풀 생성 완료: {_poolSize}개");
        }
        
        private AudioSource? GetPooledSource()
        {
            if (_pool.Count > 0)
            {
                AudioSource audioSource = _pool.Dequeue();
                _activeSources.Add(audioSource);
                return audioSource;
            }
            
            return null;
        }
        
        private void ReturnToPool(AudioSource audioSource)
        {
            audioSource.Stop();
            audioSource.clip = null;
            audioSource.volume = 1f;
            audioSource.pitch = 1f;
            
            _activeSources.Remove(audioSource);
            _pool.Enqueue(audioSource);
        }
        
        private IEnumerator ReturnToPoolWhenFinished(AudioSource audioSource)
        {
            while (audioSource != null && audioSource.isPlaying)
            {
                yield return null;
            }
            
            if (audioSource != null)
            {
                ReturnToPool(audioSource);
            }
        }
    }
}
