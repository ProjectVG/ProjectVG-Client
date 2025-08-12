using UnityEngine;
using UnityEngine.UI;
using ProjectVG.Domain.Character.Service;
using ProjectVG.Domain.Character.Live2D.Model;

namespace ProjectVG.Domain.Character.Test
{
    /// <summary>
    /// Live2DModelManager 테스트를 위한 컴포넌트
    /// </summary>
    public class Live2DModelTest : MonoBehaviour
    {
        [Header("테스트 설정")]
        [SerializeField] private string _testCharacterId = "test_character";
        
        [Header("UI 요소")]
        [SerializeField] private Button _loadCharacterBtn;
        [SerializeField] private Button _activateCharacterBtn;
        [SerializeField] private Button _unloadCharacterBtn;
        [SerializeField] private Button _unloadAllBtn;
        [SerializeField] private Button _changeVisibilityBtn;
        [SerializeField] private InputField _characterIdInput;
        
        [Header("테스트 결과")]
        [SerializeField] private Text _statusText;
        
        private bool _isCharacterVisible = true;

        private void Start()
        {
            SetupUI();
        }

        /// <summary>
        /// UI 요소들을 설정한다.
        /// </summary>
        private void SetupUI()
        {
            if (_loadCharacterBtn != null)
            {
                _loadCharacterBtn.onClick.RemoveAllListeners();
                _loadCharacterBtn.onClick.AddListener(OnLoadCharacterClicked);
            }
            
            if (_activateCharacterBtn != null)
            {
                _activateCharacterBtn.onClick.RemoveAllListeners();
                _activateCharacterBtn.onClick.AddListener(OnActivateCharacterClicked);
            }
            
            if (_unloadCharacterBtn != null)
            {
                _unloadCharacterBtn.onClick.RemoveAllListeners();
                _unloadCharacterBtn.onClick.AddListener(OnUnloadCharacterClicked);
            }
            
            if (_unloadAllBtn != null)
            {
                _unloadAllBtn.onClick.RemoveAllListeners();
                _unloadAllBtn.onClick.AddListener(OnUnloadAllClicked);
            }
            
            if (_changeVisibilityBtn != null)
            {
                _changeVisibilityBtn.onClick.RemoveAllListeners();
                _changeVisibilityBtn.onClick.AddListener(OnChangeVisibilityClicked);
            }
            
            if (_characterIdInput != null)
            {
                _characterIdInput.text = _testCharacterId;
                _characterIdInput.onValueChanged.AddListener(OnCharacterIdChanged);
            }
            
            UpdateStatus("테스트 준비 완료");
        }

        /// <summary>
        /// 캐릭터 로드 버튼 클릭 시 호출된다.
        /// </summary>
        private async void OnLoadCharacterClicked()
        {
            UpdateStatus($"캐릭터 로드 시작: {_testCharacterId}");
            
            var character = await Live2DModelManager.Instance.LoadCharacterAsync(_testCharacterId, activateImmediately: true);
            if (character != null)
            {
                UpdateStatus($"캐릭터 로드 및 활성화 완료: {_testCharacterId}");
            }
            else
            {
                UpdateStatus($"캐릭터 로드 실패: {_testCharacterId}");
            }
        }

        /// <summary>
        /// 캐릭터 활성화 버튼 클릭 시 호출된다.
        /// </summary>
        private void OnActivateCharacterClicked()
        {
            Live2DModelManager.Instance.ActivateCharacter(_testCharacterId);
            UpdateStatus($"캐릭터 활성화: {_testCharacterId}");
        }

        /// <summary>
        /// 캐릭터 언로드 버튼 클릭 시 호출된다.
        /// </summary>
        private void OnUnloadCharacterClicked()
        {
            Live2DModelManager.Instance.UnloadCharacter(_testCharacterId);
            UpdateStatus($"캐릭터 언로드 완료: {_testCharacterId}");
        }

        /// <summary>
        /// 모든 캐릭터 언로드 버튼 클릭 시 호출된다.
        /// </summary>
        private void OnUnloadAllClicked()
        {
            Live2DModelManager.Instance.UnloadAllCharacters();
            UpdateStatus("모든 캐릭터 언로드 완료");
        }

        /// <summary>
        /// 가시성 변경 버튼 클릭 시 호출된다.
        /// </summary>
        private void OnChangeVisibilityClicked()
        {
            _isCharacterVisible = !_isCharacterVisible;
            Live2DModelManager.Instance.SetCharacterVisibility(_isCharacterVisible);
            UpdateStatus($"캐릭터 가시성 변경: {(_isCharacterVisible ? "보임" : "숨김")}");
        }

        /// <summary>
        /// 캐릭터 ID 입력 변경 시 호출된다.
        /// </summary>
        private void OnCharacterIdChanged(string newId)
        {
            _testCharacterId = newId;
        }

        /// <summary>
        /// 상태 텍스트를 업데이트한다.
        /// </summary>
        private void UpdateStatus(string message)
        {
            Debug.Log($"[Live2DModelTest] {message}");
            
            if (_statusText != null)
            {
                _statusText.text = message;
            }
        }

        /// <summary>
        /// 현재 상태 정보를 출력한다.
        /// </summary>
        [ContextMenu("현재 상태 출력")]
        private void PrintCurrentStatus()
        {
            var activeCharacter = Live2DModelManager.Instance.GetActiveCharacter();
            var hasCharacter = Live2DModelManager.Instance.HasCharacter(_testCharacterId);
            
            Debug.Log($"[Live2DModelTest] 현재 상태:");
            Debug.Log($"  - 테스트 캐릭터 ID: {_testCharacterId}");
            Debug.Log($"  - 캐릭터 로드 여부: {hasCharacter}");
            Debug.Log($"  - 활성 캐릭터: {(activeCharacter != null ? activeCharacter.name : "없음")}");
        }
    }
}

