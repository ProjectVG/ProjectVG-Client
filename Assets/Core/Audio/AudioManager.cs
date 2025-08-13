#nullable enable
using System;
using UnityEngine;
using UnityEngine.Audio;
using Cysharp.Threading.Tasks;
using ProjectVG.Domain.Chat.Model;
using ProjectVG.Core.Utils;

namespace ProjectVG.Core.Audio
{
    public class AudioManager : Singleton<AudioManager>
    {
        [Header("Audio Controllers")]
        [SerializeField] private BGMController? _bgmController;
        [SerializeField] private VoiceController? _voiceController;
        [SerializeField] private SFXController? _sfxController;
        [SerializeField] private UIController? _uiController;
        
        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer? _audioMixer;
        
        [Header("Audio Mixer Groups")]
        [SerializeField] public AudioMixerGroup? _masterGroup;
        [SerializeField] public AudioMixerGroup? _bgmGroup;
        [SerializeField] public AudioMixerGroup? _sfxGroup;
        [SerializeField] public AudioMixerGroup? _uiGroup;
        [SerializeField] public AudioMixerGroup? _voiceGroup;
        
        [Header("Volume Settings")]
        [SerializeField] private float _masterVolume = 1f;
        
        private bool _isInitialized = false;
        private bool _isLoadingSettings = false;
        
        public bool IsInitialized => _isInitialized;
        public float MasterVolume => _masterVolume;
        public float BgmVolume => _bgmController?.GetVolume() ?? 1f;
        public float SfxVolume => _sfxController?.GetVolume() ?? 1f;
        public float UiVolume => _uiController?.GetVolume() ?? 1f;
        public float VoiceVolume => _voiceController?.GetVolume() ?? 1f;
        
        public event Action<float>? OnMasterVolumeChanged;
        public event Action<float>? OnBgmVolumeChanged;
        public event Action<float>? OnSfxVolumeChanged;
        public event Action<float>? OnUiVolumeChanged;
        public event Action<float>? OnVoiceVolumeChanged;
        
        #region Unity Lifecycle
        
        protected override void Awake()
        {
            base.Awake();
        }
        
        private void Start()
        {
            Initialize();
        }
        
        private void OnDestroy()
        {
            Cleanup();
        }
        
        #endregion
        
        #region Public Methods
        
        public void Initialize()
        {
            if (_isInitialized) return;
            
            try
            {
                InitializeControllers();
                LoadVolumeSettings();
                
                _isInitialized = true;
                Debug.Log("[AudioManager] 초기화 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AudioManager] 초기화 실패: {ex.Message}");
            }
        }
        
        public void PlayBGM(AudioClip? clip, bool loop = true)
        {
            _bgmController?.PlayBGM(clip, loop);
        }
        
        public void StopBGM()
        {
            _bgmController?.StopBGM();
        }
        
        public void PlayVoice(AudioClip? clip)
        {
            _voiceController?.PlayVoice(clip);
        }
        
        public void PlayVoice(VoiceData voiceData)
        {
            _voiceController?.PlayVoice(voiceData);
        }
        
        public async UniTask PlayVoiceAsync(VoiceData voiceData)
        {
            if (_voiceController != null)
            {
                await _voiceController.PlayVoiceAsync(voiceData);
            }
        }
        
        public void StopVoice()
        {
            _voiceController?.StopVoice();
        }
        
        public void PauseVoice()
        {
            _voiceController?.PauseVoice();
        }
        
        public void ResumeVoice()
        {
            _voiceController?.ResumeVoice();
        }
        
        public void PlaySFX(AudioClip? clip, Vector3? position = null)
        {
            _sfxController?.PlaySFX(clip, position);
        }
        
        public void PlayUI(AudioClip? clip)
        {
            _uiController?.PlayUI(clip);
        }
        
        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
            
            if (_audioMixer != null)
            {
                float dbValue = _masterVolume > 0 ? 20f * Mathf.Log10(_masterVolume) : -80f;
                _audioMixer.SetFloat("MasterVolume", dbValue);
            }
            
            OnMasterVolumeChanged?.Invoke(_masterVolume);
            SaveVolumeSettings();
        }
        
        public void SetBGMVolume(float volume)
        {
            _bgmController?.SetVolume(volume);
            OnBgmVolumeChanged?.Invoke(volume);
            SaveVolumeSettings();
        }
        
        public void SetSFXVolume(float volume)
        {
            _sfxController?.SetVolume(volume);
            OnSfxVolumeChanged?.Invoke(volume);
            SaveVolumeSettings();
        }
        
        public void SetUIVolume(float volume)
        {
            _uiController?.SetVolume(volume);
            OnUiVolumeChanged?.Invoke(volume);
            SaveVolumeSettings();
        }
        
        public void SetVoiceVolume(float volume)
        {
            _voiceController?.SetVolume(volume);
            OnVoiceVolumeChanged?.Invoke(volume);
            SaveVolumeSettings();
        }
        
        public void SaveVolumeSettings()
        {
            if (_isLoadingSettings) return;
            
            PlayerPrefs.SetFloat("Audio_MasterVolume", _masterVolume);
            PlayerPrefs.SetFloat("Audio_BGMVolume", BgmVolume);
            PlayerPrefs.SetFloat("Audio_SFXVolume", SfxVolume);
            PlayerPrefs.SetFloat("Audio_UIVolume", UiVolume);
            PlayerPrefs.SetFloat("Audio_VoiceVolume", VoiceVolume);
            PlayerPrefs.Save();
        }
        
        public void LoadVolumeSettings()
        {
            _isLoadingSettings = true;
            
            _masterVolume = PlayerPrefs.GetFloat("Audio_MasterVolume", 1f);
            float bgmVolume = PlayerPrefs.GetFloat("Audio_BGMVolume", 1f);
            float sfxVolume = PlayerPrefs.GetFloat("Audio_SFXVolume", 1f);
            float uiVolume = PlayerPrefs.GetFloat("Audio_UIVolume", 1f);
            float voiceVolume = PlayerPrefs.GetFloat("Audio_VoiceVolume", 1f);
            
            SetMasterVolume(_masterVolume);
            SetBGMVolume(bgmVolume);
            SetSFXVolume(sfxVolume);
            SetUIVolume(uiVolume);
            SetVoiceVolume(voiceVolume);
            
            _isLoadingSettings = false;
        }
        
        public void ResetVolumeSettings()
        {
            SetMasterVolume(1f);
            SetBGMVolume(1f);
            SetSFXVolume(1f);
            SetUIVolume(1f);
            SetVoiceVolume(1f);
        }
        
        public void StopAllAudio()
        {
            _bgmController?.Stop();
            _voiceController?.Stop();
            _sfxController?.Stop();
            _uiController?.Stop();
        }
        
        public bool IsBGMPlaying()
        {
            return _bgmController?.IsPlaying() ?? false;
        }
        
        public bool IsVoicePlaying()
        {
            return _voiceController?.IsPlaying() ?? false;
        }
        
        public int GetActiveSFXCount()
        {
            // SFXController에서 활성 소스 개수 반환 메서드 추가 필요
            return 0;
        }
        
        public int GetActiveUICount()
        {
            // UIController에서 활성 소스 개수 반환 메서드 추가 필요
            return 0;
        }
        
        #endregion
        
        #region Private Methods
        
        private void InitializeControllers()
        {
            SetupAudioMixerGroups();
            
            _bgmController?.Initialize();
            _voiceController?.Initialize();
            _sfxController?.Initialize();
            _uiController?.Initialize();
        }
        
        private void SetupAudioMixerGroups()
        {
            if (_audioMixer == null)
            {
                Debug.LogWarning("[AudioManager] AudioMixer가 설정되지 않았습니다!");
                return;
            }
            
            // 그룹이 설정되지 않은 경우 동적으로 생성
            if (_masterGroup == null)
                _masterGroup = CreateOrFindGroup("Master");
            if (_bgmGroup == null)
                _bgmGroup = CreateOrFindGroup("BGM");
            if (_sfxGroup == null)
                _sfxGroup = CreateOrFindGroup("SFX");
            if (_uiGroup == null)
                _uiGroup = CreateOrFindGroup("UI");
            if (_voiceGroup == null)
                _voiceGroup = CreateOrFindGroup("Voice");
            
            // 각 컨트롤러에 그룹 할당
            AssignGroupToController(_bgmController, _bgmGroup);
            AssignGroupToController(_voiceController, _voiceGroup);
            AssignGroupToController(_sfxController, _sfxGroup);
            AssignGroupToController(_uiController, _uiGroup);
        }
        
        private AudioMixerGroup? CreateOrFindGroup(string groupName)
        {
            if (_audioMixer == null) return null;
            
            // 기존 그룹 찾기
            AudioMixerGroup[] groups = _audioMixer.FindMatchingGroups(groupName);
            if (groups.Length > 0)
            {
                Debug.Log($"[AudioManager] 기존 그룹 사용: {groupName}");
                return groups[0];
            }
            
            // 그룹이 없으면 생성 (Unity 에디터에서만 가능)
            Debug.LogWarning($"[AudioManager] 그룹 '{groupName}'을 찾을 수 없습니다. AudioMixer에서 수동으로 생성해주세요.");
            return null;
        }
        
        private void AssignGroupToController(AudioControllerCore? controller, AudioMixerGroup? group)
        {
            if (controller == null || group == null) return;
            
            // AudioControllerCore의 _audioMixerGroup 필드에 할당
            var audioSource = controller.GetAudioSource();
            if (audioSource != null)
            {
                audioSource.outputAudioMixerGroup = group;
                Debug.Log($"[AudioManager] {controller.GetType().Name}에 {group.name} 그룹 할당");
            }
        }
        
        private void Cleanup()
        {
            _bgmController?.Stop();
            _voiceController?.Stop();
            _sfxController?.Stop();
            _uiController?.Stop();
            
            Debug.Log("[AudioManager] 정리 완료");
        }
        
        #endregion
    }
}
