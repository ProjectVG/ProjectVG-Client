using UnityEngine;
using ProjectVG.Infrastructure.Auth.Core;

namespace ProjectVG.Infrastructure.Auth.WebGL
{
    public class BFFToggle : MonoBehaviour
    {
        [Header("BFF Configuration")]
        [SerializeField] private bool enableBFFOnStart = true;
        [SerializeField] private bool autoDetectBFFSupport = true;
        
        [Header("BFF Detection Settings")]
        [SerializeField] private string bffHealthCheckEndpoint = "/auth/health";
        [SerializeField] private float detectionTimeoutSeconds = 5.0f;
        
        private IWebGLCookieBridge _cookieBridge;
        private bool _bffModeDetected = false;
        
        public bool IsBFFModeEnabled => _cookieBridge?.IsBFFModeEnabled ?? false;
        public bool IsBFFSupported => _bffModeDetected;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeBFFToggle();
        }
        
        private async void Start()
        {
            if (autoDetectBFFSupport)
            {
                await DetectBFFSupportAsync();
            }
            
            if (enableBFFOnStart && _bffModeDetected)
            {
                EnableBFFMode(true);
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// BFF 모드 활성화/비활성화
        /// </summary>
        public void EnableBFFMode(bool enabled)
        {
            if (_cookieBridge == null)
            {
                Debug.LogError("[BFFToggle] WebGL 쿠키 브리지를 찾을 수 없습니다.");
                return;
            }
            
            if (enabled && !_bffModeDetected)
            {
                Debug.LogWarning("[BFFToggle] BFF 지원이 감지되지 않았지만 BFF 모드를 활성화합니다.");
            }
            
            _cookieBridge.EnableBFFMode(enabled);
            
            Debug.Log($"[BFFToggle] BFF 모드 {(enabled ? "활성화" : "비활성화")}");
        }
        
        /// <summary>
        /// BFF 지원 여부를 수동으로 감지
        /// </summary>
        public async System.Threading.Tasks.Task<bool> DetectBFFSupportAsync()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[BFFToggle] 플레이 모드에서만 BFF 감지가 가능합니다.");
                return false;
            }
            
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                var apiClient = ProjectVG.Infrastructure.Network.Http.HttpApiClient.Instance;
                if (apiClient == null)
                {
                    Debug.LogWarning("[BFFToggle] HttpApiClient를 사용할 수 없습니다.");
                    return false;
                }
                
                var timeout = System.TimeSpan.FromSeconds(detectionTimeoutSeconds);
                using var cts = new System.Threading.CancellationTokenSource(timeout);
                
                Debug.Log($"[BFFToggle] BFF 지원 감지 중... ({bffHealthCheckEndpoint})");
                
                var response = await apiClient.GetAsync<dynamic>(bffHealthCheckEndpoint, cancellationToken: cts.Token);
                
                _bffModeDetected = response != null;
                
                if (_bffModeDetected)
                {
                    Debug.Log("[BFFToggle] BFF 지원이 감지되었습니다.");
                }
                else
                {
                    Debug.Log("[BFFToggle] BFF 지원이 감지되지 않았습니다.");
                }
                
                return _bffModeDetected;
            }
            catch (System.OperationCanceledException)
            {
                Debug.LogWarning($"[BFFToggle] BFF 감지 시간 초과 ({detectionTimeoutSeconds}초)");
                _bffModeDetected = false;
                return false;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BFFToggle] BFF 감지 실패: {ex.Message}");
                _bffModeDetected = false;
                return false;
            }
#else
            await System.Threading.Tasks.Task.Delay(100);
            Debug.Log("[BFFToggle] 에디터에서는 BFF 감지를 수행하지 않습니다.");
            _bffModeDetected = false;
            return false;
#endif
        }
        
        /// <summary>
        /// BFF 모드 상태 토글
        /// </summary>
        public void ToggleBFFMode()
        {
            if (_cookieBridge != null)
            {
                EnableBFFMode(!_cookieBridge.IsBFFModeEnabled);
            }
        }
        
        /// <summary>
        /// BFF 설정 정보 출력
        /// </summary>
        public void LogBFFStatus()
        {
            Debug.Log($"[BFFToggle] BFF 상태 정보:");
            Debug.Log($"  - BFF 지원 감지: {_bffModeDetected}");
            Debug.Log($"  - BFF 모드 활성화: {IsBFFModeEnabled}");
            Debug.Log($"  - 자동 감지 설정: {autoDetectBFFSupport}");
            Debug.Log($"  - 시작 시 활성화: {enableBFFOnStart}");
            Debug.Log($"  - Health Check URL: {bffHealthCheckEndpoint}");
        }
        
        #endregion
        
        #region Private Methods
        
        private void InitializeBFFToggle()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            _cookieBridge = new WebGLCookieBridge();
            
            if (!_cookieBridge.IsAvailable)
            {
                Debug.LogError("[BFFToggle] WebGL 쿠키 브리지를 초기화할 수 없습니다.");
                return;
            }
            
            _cookieBridge.OnCookieError += OnCookieError;
            
            Debug.Log("[BFFToggle] BFF 토글 초기화 완료");
#else
            Debug.LogWarning("[BFFToggle] WebGL 플랫폼이 아닙니다. BFF 기능을 사용할 수 없습니다.");
#endif
        }
        
        private void OnCookieError(string errorMessage)
        {
            Debug.LogError($"[BFFToggle] 쿠키 오류: {errorMessage}");
        }
        
        private void OnDestroy()
        {
            if (_cookieBridge != null)
            {
                _cookieBridge.OnCookieError -= OnCookieError;
            }
        }
        
        #endregion
        
        #region Editor Methods
        
#if UNITY_EDITOR
        [ContextMenu("Test BFF Detection")]
        private void TestBFFDetection()
        {
            if (Application.isPlaying)
            {
                _ = DetectBFFSupportAsync();
            }
            else
            {
                Debug.LogWarning("[BFFToggle] 플레이 모드에서만 테스트할 수 있습니다.");
            }
        }
        
        [ContextMenu("Log BFF Status")]
        private void TestLogBFFStatus()
        {
            LogBFFStatus();
        }
        
        [ContextMenu("Toggle BFF Mode")]
        private void TestToggleBFFMode()
        {
            if (Application.isPlaying)
            {
                ToggleBFFMode();
            }
            else
            {
                Debug.LogWarning("[BFFToggle] 플레이 모드에서만 토글할 수 있습니다.");
            }
        }
#endif
        
        #endregion
    }
}
