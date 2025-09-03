using System;
using System.Threading.Tasks;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ProjectVG.Infrastructure.Auth.Utils;
using ProjectVG.Infrastructure.Auth.Models;
using ProjectVG.Infrastructure.Network.DTOs.Auth;
using ProjectVG.Infrastructure.Network.Http;

namespace ProjectVG.Infrastructure.Auth.Services
{
    /// <summary>
    /// Guest 인증 서비스
    /// 디바이스 고유 ID를 사용한 게스트 로그인 처리
    /// </summary>
    public class GuestAuthService : Singleton<GuestAuthService>
    {
        private HttpApiClient _httpClient;
        private TokenManager _tokenManager;

        public event Action<TokenSet> OnGuestLoginSuccess;
        public event Action<string> OnGuestLoginFailed;

        #region Unity Lifecycle Methods
        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _httpClient = HttpApiClient.Instance;
            _tokenManager = TokenManager.Instance;
            
            Debug.Log("[GuestAuthService] 초기화 완료");
        }
        private void OnDestroy()
        {
            // 이벤트 정리는 구독자가 담당
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Guest 로그인 수행
        /// </summary>
        /// <returns>로그인 성공 여부</returns>
        public async UniTask<bool> LoginAsGuestAsync()
        {
            try
            {
                Debug.Log("[GuestAuthService] Guest 로그인 시작");

                // 디바이스 고유 ID 생성
                string deviceId = DeviceIdProvider.GetDeviceId();
                Debug.Log($"[GuestAuthService] 디바이스 ID 생성: {MaskDeviceId(deviceId)}");

                // Guest 로그인 요청
                var request = new GuestLoginRequest(deviceId);
                
                Debug.Log($"[GuestAuthService] 서버 요청: {request.GetDebugInfo()}");

                // API 호출 - requiresAuth=false (로그인 전이므로)
                var response = await _httpClient.PostAsync<GuestLoginResponse>(
                    "/api/v1/auth/guest-login",
                    deviceId, // 서버는 [FromBody] string guestId를 받음
                    requiresAuth: false
                );

                if (response == null)
                {
                    throw new Exception("서버 응답이 null입니다.");
                }

                Debug.Log($"[GuestAuthService] 서버 응답: {response.GetDebugInfo()}");

                if (!response.Success)
                {
                    throw new Exception(response.Message ?? "Guest 로그인 실패");
                }

                // 토큰 검증
                if (response.Tokens == null || string.IsNullOrEmpty(response.Tokens.AccessToken))
                {
                    throw new Exception("서버에서 유효한 토큰을 받지 못했습니다.");
                }

                // TokenSet 생성 및 저장
                var tokenSet = response.ToTokenSet();
                _tokenManager.SaveTokens(tokenSet);

                Debug.Log("[GuestAuthService] Guest 로그인 성공");
                Debug.Log($"[GuestAuthService] 사용자 ID: {response.User?.UserId}");
                Debug.Log($"[GuestAuthService] AccessToken : {tokenSet.AccessToken.Token}");
                Debug.Log($"[GuestAuthService] AccessToken 만료: {tokenSet.AccessToken.ExpiresAt}");

                // 성공 이벤트 발생
                OnGuestLoginSuccess?.Invoke(tokenSet);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GuestAuthService] Guest 로그인 실패: {ex.Message}");
                
                // 실패 이벤트 발생
                OnGuestLoginFailed?.Invoke(ex.Message);
                
                return false;
            }
        }

        /// <summary>
        /// 현재 디바이스가 게스트 로그인 가능한지 확인
        /// </summary>
        /// <returns>게스트 로그인 가능 여부</returns>
        public bool CanLoginAsGuest()
        {
            try
            {
                string deviceId = DeviceIdProvider.GetDeviceId();
                return !string.IsNullOrEmpty(deviceId);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GuestAuthService] 디바이스 ID 생성 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 현재 디바이스 ID 반환
        /// </summary>
        /// <returns>디바이스 ID</returns>
        public string GetCurrentDeviceId()
        {
            return DeviceIdProvider.GetDeviceId();
        }

        /// <summary>
        /// 디바이스 ID 초기화 (테스트/디버깅용)
        /// </summary>
        public void ResetDeviceId()
        {
            DeviceIdProvider.ClearDeviceId();
            Debug.Log("[GuestAuthService] 디바이스 ID 초기화 완료");
        }

        /// <summary>
        /// Guest 로그인 가능 상태 확인
        /// </summary>
        /// <returns>현재 상태 정보</returns>
        public GuestLoginStatus GetGuestLoginStatus()
        {
            var status = new GuestLoginStatus
            {
                CanLoginAsGuest = CanLoginAsGuest(),
                DeviceId = GetCurrentDeviceId(),
                HasStoredTokens = _tokenManager.HasValidTokens || _tokenManager.HasRefreshToken,
                PlatformInfo = DeviceIdProvider.GetPlatformInfo()
            };

            return status;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 디바이스 ID 마스킹 (로깅용)
        /// </summary>
        private string MaskDeviceId(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId) || deviceId.Length < 8)
            {
                return "***";
            }

            return $"{deviceId.Substring(0, 4)}****{deviceId.Substring(deviceId.Length - 4)}";
        }

        /// <summary>
        /// 디버그 정보 출력
        /// </summary>
        public string GetDebugInfo()
        {
            var info = "GuestAuthService Debug Info:\n";
            info += $"Can Login As Guest: {CanLoginAsGuest()}\n";
            info += $"Current Device ID: {MaskDeviceId(GetCurrentDeviceId())}\n";
            info += $"Has Valid Tokens: {_tokenManager?.HasValidTokens ?? false}\n";
            info += $"Has Refresh Token: {_tokenManager?.HasRefreshToken ?? false}\n";
            info += $"{DeviceIdProvider.GetPlatformInfo()}\n";

            return info;
        }

        #endregion
    }

    /// <summary>
    /// Guest 로그인 상태 정보
    /// </summary>
    [Serializable]
    public class GuestLoginStatus
    {
        /// <summary>
        /// Guest 로그인 가능 여부
        /// </summary>
        public bool CanLoginAsGuest { get; set; }

        /// <summary>
        /// 현재 디바이스 ID
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// 저장된 토큰 존재 여부
        /// </summary>
        public bool HasStoredTokens { get; set; }

        /// <summary>
        /// 플랫폼 정보
        /// </summary>
        public string PlatformInfo { get; set; }

        /// <summary>
        /// 디버그 정보 출력
        /// </summary>
        public string GetDebugInfo()
        {
            return $"GuestLoginStatus: CanLogin={CanLoginAsGuest}, " +
                   $"HasTokens={HasStoredTokens}, " +
                   $"Platform={PlatformInfo}";
        }
    }
}