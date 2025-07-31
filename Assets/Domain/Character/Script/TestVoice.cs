using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class TestVoice : MonoBehaviour
{
    [SerializeField] private AudioClip audioClip;
    private AudioSource _audioSource;

    void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.clip = audioClip;
    }
}
