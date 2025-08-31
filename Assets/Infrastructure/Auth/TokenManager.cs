using System;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using ProjectVG.Infrastructure.Auth.Models;
using Newtonsoft.Json;

namespace ProjectVG.Infrastructure.Auth
{
    public class TokenManager : MonoBehaviour
    {
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
                
                if (HasRefreshToken && !IsRefreshTokenExpired())
                {
                    StartCoroutine(AutoRecoverAccessTokenCoroutine());
                }
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        public void SaveTokens(TokenSet tokenSet)
        {
            if (tokenSet?.AccessToken == null)
            {
                Debug.LogWarning("[TokenManager] 저장할 Access Token이 없습니다.");
                return;
            }
            
            try
            {
                _currentAccessToken = tokenSet.AccessToken;
                _currentRefreshToken = tokenSet.RefreshToken;
                _currentUserId = tokenSet.RefreshToken?.DeviceId;
                
                if (_currentRefreshToken != null)
                {
                    var refreshTokenData = new TokenStorageData
                    {
                        Token = _currentRefreshToken.Token,
                        ExpiresAt = _currentRefreshToken.ExpiresAt,
                        UserId = _currentRefreshToken.DeviceId
                    };
                    var encryptedRefreshToken = EncryptData(JsonConvert.SerializeObject(refreshTokenData));
                    PlayerPrefs.SetString(REFRESH_TOKEN_KEY, encryptedRefreshToken);
                }
                else
                {
                    PlayerPrefs.DeleteKey(REFRESH_TOKEN_KEY);
                }
                
                if (!string.IsNullOrEmpty(_currentUserId))
                {
                    PlayerPrefs.SetString(USER_ID_KEY, _currentUserId);
                }
                
                PlayerPrefs.Save();
                Debug.Log("[TokenManager] 토큰 저장 완료");
                
                OnTokensUpdated?.Invoke(tokenSet);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TokenManager] 토큰 저장 실패: {ex.Message}");
                throw;
            }
        }
        
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
        
        public string GetAccessToken()
        {
            if (_currentAccessToken != null && !_currentAccessToken.IsExpired())
            {
                return _currentAccessToken.Token;
            }
            
            if (_currentRefreshToken != null && !_currentRefreshToken.IsExpired())
            {
                OnTokensExpired?.Invoke();
            }
            
            return null;
        }
        
        public string GetRefreshToken()
        {
            return _currentRefreshToken?.Token;
        }
        
        public bool IsAccessTokenExpired()
        {
            return _currentAccessToken?.IsExpired() ?? true;
        }
        
        public bool IsRefreshTokenExpired()
        {
            return _currentRefreshToken?.IsExpired() ?? true;
        }
        
        public void UpdateAccessToken(string newAccessToken)
        {
            if (string.IsNullOrEmpty(newAccessToken))
            {
                Debug.LogError("[TokenManager] 새로운 Access Token이 비어있습니다.");
                return;
            }
            
            try
            {
                _currentAccessToken = new AccessToken(newAccessToken);
                
                var tokenSet = new TokenSet(_currentAccessToken, _currentRefreshToken);
                OnTokensUpdated?.Invoke(tokenSet);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TokenManager] Access Token 갱신 실패: {ex.Message}");
                throw;
            }
        }
        
        public void ClearTokens()
        {
            try
            {
                _currentAccessToken = null;
                _currentRefreshToken = null;
                _currentUserId = null;
                
                PlayerPrefs.DeleteKey(REFRESH_TOKEN_KEY);
                PlayerPrefs.DeleteKey(USER_ID_KEY);
                PlayerPrefs.Save();
                
                OnTokensCleared?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TokenManager] 토큰 삭제 실패: {ex.Message}");
                throw;
            }
        }
        
        private void LoadTokensFromStorage()
        {
            try
            {
                _currentAccessToken = null;
                
                if (PlayerPrefs.HasKey(REFRESH_TOKEN_KEY))
                {
                    var encryptedRefreshToken = PlayerPrefs.GetString(REFRESH_TOKEN_KEY);
                    var decryptedRefreshToken = DecryptData(encryptedRefreshToken);
                    var refreshTokenData = JsonConvert.DeserializeObject<TokenStorageData>(decryptedRefreshToken);
                    
                    _currentRefreshToken = new RefreshToken(
                        refreshTokenData.Token,
                        refreshTokenData.ExpiresAt,
                        refreshTokenData.UserId
                    );
                }
                
                if (PlayerPrefs.HasKey(USER_ID_KEY))
                {
                    _currentUserId = PlayerPrefs.GetString(USER_ID_KEY);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TokenManager] 토큰 로드 실패: {ex.Message}");
                ClearTokens();
            }
        }
        
        private string EncryptData(string data)
        {
            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(ENCRYPTION_KEY.PadRight(32, '0').Substring(0, 32));
                    aes.GenerateIV(); // 랜덤 IV 생성
                    
                    using (var encryptor = aes.CreateEncryptor())
                    using (var ms = new System.IO.MemoryStream())
                    {
                        // 먼저 IV를 기록
                        ms.Write(aes.IV, 0, aes.IV.Length);
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
        
        private string DecryptData(string encryptedData)
        {
            try
            {
                // 새로운 방식 (랜덤 IV) 시도
                return DecryptDataWithRandomIV(encryptedData);
            }
            catch (Exception)
            {
                // 실패 시 기존 방식 (고정 IV) 시도
                try
                {
                    Debug.LogWarning("[TokenManager] 새로운 방식 복호화 실패, 기존 방식으로 시도합니다.");
                    return DecryptDataWithFixedIV(encryptedData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[TokenManager] 모든 복호화 방식 실패: {ex.Message}");
                    throw;
                }
            }
        }
        
        private string DecryptDataWithRandomIV(string encryptedData)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(ENCRYPTION_KEY.PadRight(32, '0').Substring(0, 32));
                
                var allBytes = Convert.FromBase64String(encryptedData);
                if (allBytes.Length < 16) throw new InvalidOperationException("암호문 길이가 유효하지 않습니다.");
                
                // IV 추출
                var iv = new byte[16];
                Buffer.BlockCopy(allBytes, 0, iv, 0, iv.Length);
                
                // 암호문 추출
                var cipher = new byte[allBytes.Length - iv.Length];
                Buffer.BlockCopy(allBytes, iv.Length, cipher, 0, cipher.Length);
                
                aes.IV = iv;
                
                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new System.IO.MemoryStream(cipher))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var sr = new System.IO.StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }
        
        private string DecryptDataWithFixedIV(string encryptedData)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(ENCRYPTION_KEY.PadRight(32, '0').Substring(0, 32));
                aes.IV = new byte[16]; // 기존 고정 IV
                
                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new System.IO.MemoryStream(Convert.FromBase64String(encryptedData)))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var sr = new System.IO.StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }
        
        private IEnumerator AutoRecoverAccessTokenCoroutine()
        {
            yield return new WaitForSeconds(0.5f);
            
            if (TokenRefreshService.Instance != null)
            {
                StartCoroutine(TryRefreshTokenCoroutine());
            }
        }
        
        private IEnumerator TryRefreshTokenCoroutine()
        {
            var refreshTask = TokenRefreshService.Instance.RefreshAccessTokenAsync();
            yield return new WaitUntil(() => refreshTask.Status != Cysharp.Threading.Tasks.UniTaskStatus.Pending);
            
            if (refreshTask.Status == Cysharp.Threading.Tasks.UniTaskStatus.Succeeded)
            {
                bool success = refreshTask.GetAwaiter().GetResult();
                if (!success)
                {
                    Debug.LogWarning("[TokenManager] 앱 시작 시 AccessToken 자동 복구 실패");
                }
            }
        }
    }
    
    [Serializable]
    public class TokenStorageData
    {
        public string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string UserId { get; set; }
    }
}