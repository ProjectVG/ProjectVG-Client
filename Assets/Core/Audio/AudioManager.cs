using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;

public class AudioManager : Singleton<AudioManager>
{
    [SerializeField] private AudioSource voiceSource;
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    public void Initialize() { }
}
