using System;
using UnityEngine;
using UnityEngine.InputSystem;
using ProjectVG.Core.Utils;

namespace ProjectVG.Core.Input
{
    public class PlatformInputManager : Singleton<PlatformInputManager>
    {
        [Header("Input Settings")]
        [SerializeField] private bool _enableEscapeInput = true;
        [SerializeField] private bool _enableBackButtonInput = true;
        [SerializeField] private bool _enableWebGLInput = false;

        [Header("Input Actions")]
        [SerializeField] private InputActionAsset _inputActionAsset;

        private InputAction _escapeAction;
        private InputAction _backAction;
        private bool _isInitialized = false;

        // 이벤트
        public event Action OnEscapePressed;
        public event Action OnBackPressed;
        public event Action OnMenuTogglePressed;

        protected override void Awake()
        {
            base.Awake();
            InitializeInputSystem();
        }

        private void Start()
        {
            ConfigurePlatformSpecificSettings();
        }

        private void InitializeInputSystem()
        {
            if (_isInitialized) return;

            CreateInputActions();
            SetupInputBindings();
            SubscribeToInputEvents();

            _isInitialized = true;
            Debug.Log("[PlatformInputManager] 입력 시스템 초기화 완료");
        }

        private void CreateInputActions()
        {
            // 기존 InputActionAsset이 있으면 사용, 없으면 런타임에 생성
            if (_inputActionAsset != null)
            {
                _escapeAction = _inputActionAsset.FindAction("Escape");
                _backAction = _inputActionAsset.FindAction("Back");
            }

            // 액션이 없으면 런타임에 생성
            if (_escapeAction == null)
            {
                _escapeAction = new InputAction("Escape", InputActionType.Button);
            }

            if (_backAction == null)
            {
                _backAction = new InputAction("Back", InputActionType.Button);
            }
        }

        private void SetupInputBindings()
        {
            // ESC 키 바인딩 (PC/Mac)
            if (_enableEscapeInput)
            {
                _escapeAction.AddBinding("<Keyboard>/escape");
                
                // 추가 바인딩 (옵션)
                _escapeAction.AddBinding("<Gamepad>/start"); // 게임패드 스타트 버튼
            }

            // 뒤로가기 바인딩 (모바일/게임패드)
            if (_enableBackButtonInput)
            {
                // Android 뒤로가기 버튼
#if UNITY_ANDROID && !UNITY_EDITOR
                _backAction.AddBinding("<AndroidGamepad>/back");
#endif
                
                // 게임패드 B 버튼 (뒤로가기 의미)
                _backAction.AddBinding("<Gamepad>/buttonEast");
                
                // 추가 모바일 제스처 (필요시)
                // _backAction.AddBinding("<Touchscreen>/touch0/swipe");
            }
        }

        private void SubscribeToInputEvents()
        {
            if (_escapeAction != null)
            {
                _escapeAction.performed += OnEscapeActionPerformed;
            }

            if (_backAction != null)
            {
                _backAction.performed += OnBackActionPerformed;
            }
        }

        private void ConfigurePlatformSpecificSettings()
        {
            RuntimePlatform platform = Application.platform;

            switch (platform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.LinuxPlayer:
                case RuntimePlatform.LinuxEditor:
                    // PC 플랫폼: ESC 키 활성화
                    _enableEscapeInput = true;
                    _enableBackButtonInput = false;
                    break;

                case RuntimePlatform.Android:
                case RuntimePlatform.IPhonePlayer:
                    // 모바일 플랫폼: 뒤로가기 버튼 활성화
                    _enableEscapeInput = false;
                    _enableBackButtonInput = true;
                    break;

                case RuntimePlatform.WebGLPlayer:
                    // WebGL: 설정에 따라 결정
                    _enableEscapeInput = _enableWebGLInput;
                    _enableBackButtonInput = false;
                    break;

                default:
                    // 기타 플랫폼: 모든 입력 활성화
                    _enableEscapeInput = true;
                    _enableBackButtonInput = true;
                    break;
            }

            Debug.Log($"[PlatformInputManager] 플랫폼 '{platform}' 설정: ESC={_enableEscapeInput}, Back={_enableBackButtonInput}");
        }

        private void OnEscapeActionPerformed(InputAction.CallbackContext context)
        {
            if (!_enableEscapeInput) return;

            Debug.Log("[PlatformInputManager] ESC 키 입력 감지");
            
            OnEscapePressed?.Invoke();
            OnMenuTogglePressed?.Invoke();
        }

        private void OnBackActionPerformed(InputAction.CallbackContext context)
        {
            if (!_enableBackButtonInput) return;

            Debug.Log("[PlatformInputManager] 뒤로가기 버튼 입력 감지");
            
            OnBackPressed?.Invoke();
            OnMenuTogglePressed?.Invoke();
        }

        private void OnEnable()
        {
            EnableInputActions();
        }

        private void OnDisable()
        {
            DisableInputActions();
        }

        private void EnableInputActions()
        {
            if (!ShouldEnableInputForCurrentPlatform()) return;

            _escapeAction?.Enable();
            _backAction?.Enable();

            Debug.Log("[PlatformInputManager] 입력 액션 활성화");
        }

        private void DisableInputActions()
        {
            _escapeAction?.Disable();
            _backAction?.Disable();

            Debug.Log("[PlatformInputManager] 입력 액션 비활성화");
        }

        private bool ShouldEnableInputForCurrentPlatform()
        {
            RuntimePlatform platform = Application.platform;

            // WebGL에서는 설정에 따라 결정
            if (platform == RuntimePlatform.WebGLPlayer && !_enableWebGLInput)
            {
                return false;
            }

            return true;
        }

        // 외부에서 입력 활성화/비활성화 제어
        public void SetInputEnabled(bool enabled)
        {
            if (enabled)
            {
                EnableInputActions();
            }
            else
            {
                DisableInputActions();
            }
        }

        // 특정 입력 타입 활성화/비활성화
        public void SetEscapeInputEnabled(bool enabled)
        {
            _enableEscapeInput = enabled;
            
            if (enabled && ShouldEnableInputForCurrentPlatform())
            {
                _escapeAction?.Enable();
            }
            else
            {
                _escapeAction?.Disable();
            }
        }

        public void SetBackInputEnabled(bool enabled)
        {
            _enableBackButtonInput = enabled;
            
            if (enabled && ShouldEnableInputForCurrentPlatform())
            {
                _backAction?.Enable();
            }
            else
            {
                _backAction?.Disable();
            }
        }

        // 현재 플랫폼 정보 가져오기
        public bool IsDesktopPlatform()
        {
            RuntimePlatform platform = Application.platform;
            return platform == RuntimePlatform.WindowsPlayer ||
                   platform == RuntimePlatform.WindowsEditor ||
                   platform == RuntimePlatform.OSXPlayer ||
                   platform == RuntimePlatform.OSXEditor ||
                   platform == RuntimePlatform.LinuxPlayer ||
                   platform == RuntimePlatform.LinuxEditor;
        }

        public bool IsMobilePlatform()
        {
            RuntimePlatform platform = Application.platform;
            return platform == RuntimePlatform.Android ||
                   platform == RuntimePlatform.IPhonePlayer;
        }

        public bool IsWebGLPlatform()
        {
            return Application.platform == RuntimePlatform.WebGLPlayer;
        }

        // Android 뒤로가기 버튼 특별 처리 (추가 안전장치)
        private void Update()
        {
            // Android에서 Input.inputString을 통한 추가 뒤로가기 감지
            if (Application.platform == RuntimePlatform.Android && _enableBackButtonInput)
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                {
                    Debug.Log("[PlatformInputManager] Android ESC/Back 키 감지 (Update)");
                    OnBackPressed?.Invoke();
                    OnMenuTogglePressed?.Invoke();
                }
            }
        }

        private void OnDestroy()
        {
            // 이벤트 구독 해제
            if (_escapeAction != null)
            {
                _escapeAction.performed -= OnEscapeActionPerformed;
                _escapeAction?.Dispose();
            }

            if (_backAction != null)
            {
                _backAction.performed -= OnBackActionPerformed;
                _backAction?.Dispose();
            }

            Debug.Log("[PlatformInputManager] 정리 완료");
        }

        // 개발자용 디버그 메서드
        [ContextMenu("Test Escape Input")]
        private void TestEscapeInput()
        {
            OnEscapePressed?.Invoke();
            OnMenuTogglePressed?.Invoke();
        }

        [ContextMenu("Test Back Input")]
        private void TestBackInput()
        {
            OnBackPressed?.Invoke();
            OnMenuTogglePressed?.Invoke();
        }
    }
}