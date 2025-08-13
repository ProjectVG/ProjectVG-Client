#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace ProjectVG.Core.Audio
{
    public class UIController : AudioControllerCore, IAudioController
    {
        [Header("UI Settings")]
        [SerializeField] private int _poolSize = 5;
        
        private Queue<AudioSource> _pool = new Queue<AudioSource>();
        private List<AudioSource> _activeSources = new List<AudioSource>();
        
        public string GetControllerName() => "UI";
        
        public void Initialize()
        {
            base.Initialize("UIVolume");
            CreatePool();
        }
        
        protected override AudioMixerGroup? GetAudioMixerGroupFromManager()
        {
            return AudioManager.Instance._uiGroup;
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
        
        public void PlayUI(AudioClip? clip)
        {
            if (clip == null)
            {
                Debug.LogWarning("[UIController] UI 클립이 null입니다!");
                return;
            }
            
            AudioSource? audioSource = GetPooledSource();
            if (audioSource == null)
            {
                Debug.LogWarning("[UIController] 사용 가능한 AudioSource가 없습니다!");
                return;
            }
            
            audioSource.clip = clip;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
            
            audioSource.Play();
            
            StartCoroutine(ReturnToPoolWhenFinished(audioSource));
            
            Debug.Log($"[UIController] UI 효과음 재생: {clip.name}");
        }
        
        private void CreatePool()
        {
            for (int i = 0; i < _poolSize; i++)
            {
                GameObject poolObject = new GameObject($"PooledUISource_{i}");
                poolObject.transform.SetParent(transform);
                
                AudioSource audioSource = poolObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.outputAudioMixerGroup = GetAudioMixerGroupFromManager();
                
                _pool.Enqueue(audioSource);
            }
            
            Debug.Log($"[UIController] UI AudioSource 풀 생성 완료: {_poolSize}개");
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
            while (audioSource.isPlaying)
            {
                yield return null;
            }
            
            ReturnToPool(audioSource);
        }
    }
}
