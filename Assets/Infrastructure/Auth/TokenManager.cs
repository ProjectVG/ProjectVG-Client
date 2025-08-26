using System;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using ProjectVG.Infrastructure.Auth.Models;
using Newtonsoft.Json;
using ProjectVG.Infrastructure.Auth.Models;

namespace ProjectVG.Infrastructure.Auth
{
    /// <summary>
    /// OAuth2 토큰 관리자
    /// Access 토큰과 Refresh 토큰을 안전하게 저장하고 관리
    /// </summary>
    public class TokenManager : MonoBehaviour
    {
        // AccessToken은 메모리에만 저장하므로 ACCESS_TOKEN_KEY, TOKEN_EXPIRY_KEY 는 제거
        private const string REFRESH_TOKEN_KEY = "refresh_token";
        private const string USER_ID_KEY = "user_id";
        private const string ENCRYPTION_KEY = "ProjectVG_OAuth2_Secure_Key_2024";
        
        private static TokenManager _instance;
        public static TokenManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("TokenManager");
                    _instance = go.AddComponent<TokenManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        private AccessToken _currentAccessToken;
        private RefreshToken _currentRefreshToken;
        private string _currentUserId;
        
        public event Action<TokenSet> OnTokensUpdated;
        public event Action OnTokensExpired;
        public event Action OnTokensCleared;
        
        public bool HasValidTokens => _currentAccessToken != null && !_currentAccessToken.IsExpired();
        public bool HasRefreshToken => _currentRefreshToken != null;
        public string CurrentUserId => _currentUserId;
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                LoadTokensFromStorage();
                
                // 앱 시작 시 RefreshToken이 있으면 자동으로 AccessToken 복구 시도
                if (HasRefreshToken && !IsRefreshTokenExpired())
                {
                    Debug.Log("[TokenManager] RefreshToken 발견 - 자동 AccessToken 복구 시작");
                    StartCoroutine(AutoRecoverAccessTokenCoroutine());
                }
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// 토큰 세트 저장
        /// </summary>
        public void SaveTokens(TokenSet tokenSet)
        {
            if (tokenSet?.AccessToken == null)
            {
                Debug.LogWarning("[TokenManager] 저장할 Access Token이 없습니다.");
                return;
            }
            
            try
            {
                // AccessToken은 메모리에만 저장 (영속적 저장 안함)
                _currentAccessToken = tokenSet.AccessToken;
                _currentRefreshToken = tokenSet.RefreshToken;
                _currentUserId = tokenSet.RefreshToken?.DeviceId;
                
                // Refresh Token 저장 (강력한 암호화)
                string encryptedRefreshToken = null;
                if (_currentRefreshToken != null)
                {
                    var refreshTokenData = new TokenStorageData
                    {
                        Token = _currentRefreshToken.Token,
                        ExpiresAt = _currentRefreshToken.ExpiresAt,
                        TokenType = "Refresh",
                        Scope = "oauth2",
                        UserId = _currentRefreshToken.DeviceId
                    };
                    encryptedRefreshToken = EncryptData(JsonConvert.SerializeObject(refreshTokenData));
                    PlayerPrefs.SetString(REFRESH_TOKEN_KEY, encryptedRefreshToken);
                }
                else
                {
                    PlayerPrefs.DeleteKey(REFRESH_TOKEN_KEY);
                }
                
                // AccessToken 만료 시간은 메모리에만 보관 (PlayerPrefs 저장 안함)
                
                // User ID 저장
                if (!string.IsNullOrEmpty(_currentUserId))
                {
                    PlayerPrefs.SetString(USER_ID_KEY, _currentUserId);
                }
                
                PlayerPrefs.Save();
                
                Debug.Log("=== 🔐 TokenManager 토큰 저장 완료 ===");
                Debug.Log($"[TokenManager] Access Token 저장: 메모리 전용 (영속적 저장 안함)");
                Debug.Log($"[TokenManager] Access Token 만료: {_currentAccessToken.ExpiresAt}");
                
                if (_currentRefreshToken != null)
                {
                    Debug.Log($"[TokenManager] Refresh Token 저장 위치: PlayerPrefs['{REFRESH_TOKEN_KEY}']");
                    Debug.Log($"[TokenManager] Refresh Token 만료: {_currentRefreshToken.ExpiresAt}");
                    Debug.Log($"[TokenManager] Refresh Token 암호화: {!string.IsNullOrEmpty(encryptedRefreshToken)}");
                }
                else
                {
                    Debug.Log("[TokenManager] Refresh Token: 없음");
                }
                
                Debug.Log($"[TokenManager] User ID 저장 위치: PlayerPrefs['{USER_ID_KEY}'] = {_currentUserId}");
                Debug.Log("=== 토큰 저장 완료 ===");
                
                OnTokensUpdated?.Invoke(tokenSet);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TokenManager] 토큰 저장 실패: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 저장된 토큰 로드
        /// </summary>
        public TokenSet LoadTokens()
        {
            if (_currentAccessToken != null && _currentRefreshToken != null)
            {
                return new TokenSet(_currentAccessToken, _currentRefreshToken);
            }
            
            LoadTokensFromStorage();
            
            if (_currentAccessToken != null && _currentRefreshToken != null)
            {
                return new TokenSet(_currentAccessToken, _currentRefreshToken);
            }
            
            return null;
        }
        
        /// <summary>
        /// Access Token만 반환
        /// </summary>
        public string GetAccessToken()
        {
            if (_currentAccessToken != null && !_currentAccessToken.IsExpired())
            {
                return _currentAccessToken.Token;
            }
            
            // 만료된 경우 Refresh Token으로 갱신 시도
            if (_currentRefreshToken != null && !_currentRefreshToken.IsExpired())
            {
                Debug.Log("[TokenManager] Access Token이 만료되었습니다. Refresh Token으로 갱신을 시도하세요.");
                OnTokensExpired?.Invoke();
            }
            
            return null;
        }
        
        /// <summary>
        /// Refresh Token 반환
        /// </summary>
        public string GetRefreshToken()
        {
            return _currentRefreshToken?.Token;
        }
        
        /// <summary>
        /// 토큰 만료 여부 확인
        /// </summary>
        public bool IsAccessTokenExpired()
        {
            return _currentAccessToken?.IsExpired() ?? true;
        }
        
        /// <summary>
        /// Refresh Token 만료 여부 확인
        /// </summary>
        public bool IsRefreshTokenExpired()
        {
            return _currentRefreshToken?.IsExpired() ?? true;
        }
        
        /// <summary>
        /// 토큰 갱신
        /// </summary>
        public void UpdateAccessToken(string newAccessToken, int expiresInSeconds)
        {
            if (string.IsNullOrEmpty(newAccessToken))
            {
                Debug.LogError("[TokenManager] 새로운 Access Token이 비어있습니다.");
                return;
            }
            
            try
            {
                // AccessToken은 메모리에만 업데이트 (영속적 저장 안함)
                _currentAccessToken = new AccessToken(newAccessToken, expiresInSeconds, "Bearer", "oauth2");
                
                Debug.Log("[TokenManager] Access Token 갱신 완료 (메모리 전용)");
                Debug.Log($"[TokenManager] 새로운 만료 시간: {_currentAccessToken.ExpiresAt}");
                
                var tokenSet = new TokenSet(_currentAccessToken, _currentRefreshToken);
                OnTokensUpdated?.Invoke(tokenSet);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TokenManager] Access Token 갱신 실패: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 모든 토큰 삭제
        /// </summary>
        public void ClearTokens()
        {
            try
            {
                // 메모리에서 AccessToken 제거
                _currentAccessToken = null;
                _currentRefreshToken = null;
                _currentUserId = null;
                
                // RefreshToken만 PlayerPrefs에서 삭제 (기존 ACCESS_TOKEN_KEY, TOKEN_EXPIRY_KEY 삭제 로직 제거)
                PlayerPrefs.DeleteKey(REFRESH_TOKEN_KEY);
                PlayerPrefs.DeleteKey(USER_ID_KEY);
                PlayerPrefs.Save();
                
                Debug.Log("[TokenManager] 모든 토큰 삭제 완료");
                OnTokensCleared?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TokenManager] 토큰 삭제 실패: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 저장소에서 토큰 로드
        /// </summary>
        private void LoadTokensFromStorage()
        {
            try
            {
                // AccessToken은 저장소에서 로드하지 않음 (메모리 전용)
                // 앱 시작 시 AccessToken은 null 상태로 시작
                _currentAccessToken = null;
                
                // Refresh Token 로드
                if (PlayerPrefs.HasKey(REFRESH_TOKEN_KEY))
                {
                    var encryptedRefreshToken = PlayerPrefs.GetString(REFRESH_TOKEN_KEY);
                    var decryptedRefreshToken = DecryptData(encryptedRefreshToken);
                    var refreshTokenData = JsonConvert.DeserializeObject<TokenStorageData>(decryptedRefreshToken);
                    
                    _currentRefreshToken = new RefreshToken(
                        refreshTokenData.Token,
                        refreshTokenData.ExpiresAt,
                        refreshTokenData.UserId  // UserId 필드에 DeviceId가 저장되어 있음
                    );
                }
                
                // User ID 로드
                if (PlayerPrefs.HasKey(USER_ID_KEY))
                {
                    _currentUserId = PlayerPrefs.GetString(USER_ID_KEY);
                }
                
                Debug.Log("=== 🔍 TokenManager 저장소에서 토큰 로드 완료 ===");
                Debug.Log($"[TokenManager] Access Token: 메모리 전용 (영속적 로드 안함)");
                Debug.Log($"[TokenManager] Access Token 상태: null (앱 시작 시 기본값)");
                
                Debug.Log($"[TokenManager] Refresh Token 로드 위치: PlayerPrefs['{REFRESH_TOKEN_KEY}']");
                Debug.Log($"[TokenManager] Refresh Token 존재: {_currentRefreshToken != null}");
                if (_currentRefreshToken != null)
                {
                    Debug.Log($"[TokenManager] Refresh Token 만료: {_currentRefreshToken.ExpiresAt}");
                    Debug.Log($"[TokenManager] Refresh Token 유효: {!_currentRefreshToken.IsExpired()}");
                }
                
                Debug.Log($"[TokenManager] User ID 로드 위치: PlayerPrefs['{USER_ID_KEY}'] = {_currentUserId}");
                Debug.Log("=== 토큰 로드 완료 ===");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TokenManager] 토큰 로드 실패: {ex.Message}");
                // 로드 실패 시 모든 토큰 삭제
                ClearTokens();
            }
        }
        
        /// <summary>
        /// 데이터 암호화
        /// </summary>
        private string EncryptData(string data)
        {
            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(ENCRYPTION_KEY.PadRight(32, '0').Substring(0, 32));
                    aes.IV = new byte[16];
                    
                    using (var encryptor = aes.CreateEncryptor())
                    using (var ms = new System.IO.MemoryStream())
                    {
                        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        using (var sw = new System.IO.StreamWriter(cs))
                        {
                            sw.Write(data);
                        }
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TokenManager] 암호화 실패: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 데이터 복호화
        /// </summary>
        private string DecryptData(string encryptedData)
        {
            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(ENCRYPTION_KEY.PadRight(32, '0').Substring(0, 32));
                    aes.IV = new byte[16];
                    
                    using (var decryptor = aes.CreateDecryptor())
                    using (var ms = new System.IO.MemoryStream(Convert.FromBase64String(encryptedData)))
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var sr = new System.IO.StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TokenManager] 복호화 실패: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 디버그 정보 출력
        /// </summary>
        public string GetDebugInfo()
        {
            var info = "TokenManager Debug Info:\n";
            info += $"=== 저장 위치 정보 ===\n";
            info += $"Access Token 저장: 메모리 전용 (영속적 저장 안함)\n";
            info += $"Refresh Token 저장: PlayerPrefs['{REFRESH_TOKEN_KEY}']\n";
            info += $"User ID 저장: PlayerPrefs['{USER_ID_KEY}']\n";
            info += $"=== 토큰 상태 ===\n";
            info += $"Has Valid Tokens: {HasValidTokens}\n";
            info += $"Has Refresh Token: {HasRefreshToken}\n";
            info += $"Access Token Expired: {IsAccessTokenExpired()}\n";
            info += $"Refresh Token Expired: {IsRefreshTokenExpired()}\n";
            info += $"User ID: {_currentUserId}\n";
            
            if (_currentAccessToken != null)
            {
                info += $"Access Token Expires: {_currentAccessToken.ExpiresAt}\n";
                info += $"Access Token Type: {_currentAccessToken.TokenType}\n";
                info += $"Access Token 유효: {!_currentAccessToken.IsExpired()}\n";
            }
            
            if (_currentRefreshToken != null)
            {
                info += $"Refresh Token Expires: {_currentRefreshToken.ExpiresAt}\n";
                info += $"Refresh Token Device ID: {_currentRefreshToken.DeviceId}\n";
                info += $"Refresh Token 유효: {!_currentRefreshToken.IsExpired()}\n";
            }
            
            return info;
        }
        
        /// <summary>
        /// 앱 시작 시 RefreshToken으로 AccessToken 자동 복구
        /// </summary>
        private IEnumerator AutoRecoverAccessTokenCoroutine()
        {
            // TokenRefreshService가 초기화될 때까지 대기
            yield return new WaitForSeconds(0.5f);
            
            if (TokenRefreshService.Instance != null)
            {
                Debug.Log("[TokenManager] TokenRefreshService를 통해 AccessToken 복구 시도");
                
                // 비동기 호출을 코루틴에서 처리
                StartCoroutine(TryRefreshTokenCoroutine());
            }
            else
            {
                Debug.LogWarning("[TokenManager] TokenRefreshService가 아직 초기화되지 않았습니다.");
            }
        }
        
        /// <summary>
        /// RefreshToken으로 AccessToken 복구 코루틴
        /// </summary>
        private IEnumerator TryRefreshTokenCoroutine()
        {
            var refreshTask = TokenRefreshService.Instance.RefreshAccessTokenAsync();
            
            // UniTask를 코루틴에서 기다림
            yield return new WaitUntil(() => refreshTask.Status != Cysharp.Threading.Tasks.UniTaskStatus.Pending);
            
            if (refreshTask.Status == Cysharp.Threading.Tasks.UniTaskStatus.Succeeded)
            {
                bool success = refreshTask.GetAwaiter().GetResult();
                if (success)
                {
                    Debug.Log("[TokenManager] 앱 시작 시 AccessToken 자동 복구 성공");
                }
                else
                {
                    Debug.LogWarning("[TokenManager] 앱 시작 시 AccessToken 자동 복구 실패");
                }
            }
            else
            {
                Debug.LogError("[TokenManager] AccessToken 복구 중 오류 발생");
            }
        }
    }
    
    /// <summary>
    /// 토큰 저장용 데이터 구조
    /// </summary>
    [Serializable]
    public class TokenStorageData
    {
        public string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string TokenType { get; set; }
        public string Scope { get; set; }
        public string UserId { get; set; }
    }
}
