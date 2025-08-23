using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ProjectVG.Infrastructure.Network.Http;
using ProjectVG.Infrastructure.Auth.Core;

namespace ProjectVG.Infrastructure.Auth.Integration
{
    /// <summary>
    /// HttpApiClient에 인증 기능을 추가하는 확장 클래스
    /// </summary>
    public static class HttpClientAuthExtension
    {
        private static IAuthManager _authManager;
        private static bool _isInitialized = false;
        private static readonly object _lockObject = new object();
        
        // 인증이 필요 없는 엔드포인트들
        private static readonly HashSet<string> PublicEndpoints = new HashSet<string>
        {
            "/auth/login",
            "/auth/callback",
            "/auth/refresh",
            "/auth/health",
            "/public/"
        };
        
        /// <summary>
        /// 인증 확장 기능 초기화
        /// </summary>
        public static void Initialize(IAuthManager authManager)
        {
            lock (_lockObject)
            {
                if (_isInitialized)
                {
                    Debug.LogWarning("[HttpClientAuthExtension] 이미 초기화되었습니다.");
                    return;
                }
                
                _authManager = authManager ?? throw new ArgumentNullException(nameof(authManager));
                _isInitialized = true;
                
                Debug.Log("[HttpClientAuthExtension] 인증 확장 기능 초기화 완료");
            }
        }
        
        /// <summary>
        /// 인증이 필요한 GET 요청
        /// </summary>
        public static async UniTask<T> GetAuthenticatedAsync<T>(this HttpApiClient client, string endpoint, 
            Dictionary<string, string> headers = null, System.Threading.CancellationToken cancellationToken = default)
        {
            return await ExecuteWithAuthAsync(async (authHeaders) =>
            {
                var combinedHeaders = CombineHeaders(headers, authHeaders);
                return await client.GetAsync<T>(endpoint, combinedHeaders, cancellationToken);
            }, endpoint);
        }
        
        /// <summary>
        /// 인증이 필요한 POST 요청
        /// </summary>
        public static async UniTask<T> PostAuthenticatedAsync<T>(this HttpApiClient client, string endpoint, 
            object data = null, Dictionary<string, string> headers = null, bool requiresSession = false, 
            System.Threading.CancellationToken cancellationToken = default)
        {
            return await ExecuteWithAuthAsync(async (authHeaders) =>
            {
                var combinedHeaders = CombineHeaders(headers, authHeaders);
                return await client.PostAsync<T>(endpoint, data, combinedHeaders, requiresSession, cancellationToken);
            }, endpoint);
        }
        
        /// <summary>
        /// 인증이 필요한 PUT 요청
        /// </summary>
        public static async UniTask<T> PutAuthenticatedAsync<T>(this HttpApiClient client, string endpoint, 
            object data = null, Dictionary<string, string> headers = null, bool requiresSession = false, 
            System.Threading.CancellationToken cancellationToken = default)
        {
            return await ExecuteWithAuthAsync(async (authHeaders) =>
            {
                var combinedHeaders = CombineHeaders(headers, authHeaders);
                return await client.PutAsync<T>(endpoint, data, combinedHeaders, requiresSession, cancellationToken);
            }, endpoint);
        }
        
        /// <summary>
        /// 인증이 필요한 DELETE 요청
        /// </summary>
        public static async UniTask<T> DeleteAuthenticatedAsync<T>(this HttpApiClient client, string endpoint, 
            Dictionary<string, string> headers = null, System.Threading.CancellationToken cancellationToken = default)
        {
            return await ExecuteWithAuthAsync(async (authHeaders) =>
            {
                var combinedHeaders = CombineHeaders(headers, authHeaders);
                return await client.DeleteAsync<T>(endpoint, combinedHeaders, cancellationToken);
            }, endpoint);
        }
        
        /// <summary>
        /// 인증이 필요한 파일 업로드 요청
        /// </summary>
        public static async UniTask<T> UploadFileAuthenticatedAsync<T>(this HttpApiClient client, string endpoint, 
            byte[] fileData, string fileName, string fieldName = "file", Dictionary<string, string> headers = null, 
            System.Threading.CancellationToken cancellationToken = default)
        {
            return await ExecuteWithAuthAsync(async (authHeaders) =>
            {
                var combinedHeaders = CombineHeaders(headers, authHeaders);
                return await client.UploadFileAsync<T>(endpoint, fileData, fileName, fieldName, combinedHeaders, cancellationToken);
            }, endpoint);
        }
        
        /// <summary>
        /// 엔드포인트가 인증을 필요로 하는지 확인
        /// </summary>
        public static bool RequiresAuthentication(string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint))
                return false;
            
            foreach (var publicEndpoint in PublicEndpoints)
            {
                if (endpoint.StartsWith(publicEndpoint, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 공개 엔드포인트 추가
        /// </summary>
        public static void AddPublicEndpoint(string endpoint)
        {
            if (!string.IsNullOrEmpty(endpoint))
            {
                PublicEndpoints.Add(endpoint);
                Debug.Log($"[HttpClientAuthExtension] 공개 엔드포인트 추가: {endpoint}");
            }
        }
        
        /// <summary>
        /// 공개 엔드포인트 제거
        /// </summary>
        public static void RemovePublicEndpoint(string endpoint)
        {
            if (PublicEndpoints.Remove(endpoint))
            {
                Debug.Log($"[HttpClientAuthExtension] 공개 엔드포인트 제거: {endpoint}");
            }
        }
        
        /// <summary>
        /// 401 응답 처리 (토큰 갱신 시도)
        /// </summary>
        public static async UniTask<bool> Handle401ResponseAsync()
        {
            if (!_isInitialized || _authManager == null)
            {
                Debug.LogError("[HttpClientAuthExtension] 초기화되지 않았습니다.");
                return false;
            }
            
            try
            {
                Debug.Log("[HttpClientAuthExtension] 401 응답 감지, 토큰 갱신 시도");
                
                var refreshResult = await _authManager.RefreshTokenAsync();
                
                if (refreshResult)
                {
                    Debug.Log("[HttpClientAuthExtension] 토큰 갱신 성공");
                    return true;
                }
                else
                {
                    Debug.LogWarning("[HttpClientAuthExtension] 토큰 갱신 실패, 로그아웃 처리");
                    await _authManager.LogoutAsync();
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpClientAuthExtension] 401 응답 처리 중 오류: {ex.Message}");
                await _authManager.LogoutAsync();
                return false;
            }
        }
        
        /// <summary>
        /// 현재 인증 상태 확인
        /// </summary>
        public static bool IsAuthenticated()
        {
            return _isInitialized && _authManager?.IsAuthenticated == true;
        }
        
        /// <summary>
        /// 종료 처리
        /// </summary>
        public static void Shutdown()
        {
            lock (_lockObject)
            {
                _authManager = null;
                _isInitialized = false;
                
                Debug.Log("[HttpClientAuthExtension] 종료 처리 완료");
            }
        }
        
        #region Private Methods
        
        private static async UniTask<T> ExecuteWithAuthAsync<T>(
            Func<Dictionary<string, string>, UniTask<T>> apiCall, 
            string endpoint)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("HttpClientAuthExtension이 초기화되지 않았습니다.");
            }
            
            if (!RequiresAuthentication(endpoint))
            {
                // 인증이 필요 없는 엔드포인트는 그대로 호출
                return await apiCall(null);
            }
            
            if (!_authManager.IsAuthenticated)
            {
                throw new UnauthorizedAccessException("인증되지 않은 상태입니다.");
            }
            
            // 최대 2번 시도 (초기 시도 + 토큰 갱신 후 재시도)
            for (int attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    var accessToken = await _authManager.GetValidAccessTokenAsync();
                    if (string.IsNullOrEmpty(accessToken))
                    {
                        throw new UnauthorizedAccessException("유효한 Access 토큰을 얻을 수 없습니다.");
                    }
                    
                    var authHeaders = new Dictionary<string, string>
                    {
                        { "Authorization", $"Bearer {accessToken}" }
                    };
                    
                    return await apiCall(authHeaders);
                }
                catch (ApiException ex) when (ex.StatusCode == 401 && attempt == 0)
                {
                    Debug.LogWarning("[HttpClientAuthExtension] 401 응답 받음, 토큰 갱신 후 재시도");
                    
                    var refreshSuccess = await Handle401ResponseAsync();
                    if (!refreshSuccess)
                    {
                        throw new UnauthorizedAccessException("토큰 갱신 실패로 인한 인증 오류");
                    }
                    
                    // 다음 루프에서 재시도
                    continue;
                }
            }
            
            throw new InvalidOperationException("예상치 못한 상황: 루프를 벗어났습니다.");
        }
        
        private static Dictionary<string, string> CombineHeaders(
            Dictionary<string, string> userHeaders, 
            Dictionary<string, string> authHeaders)
        {
            var combined = new Dictionary<string, string>();
            
            // 인증 헤더 먼저 추가
            if (authHeaders != null)
            {
                foreach (var kvp in authHeaders)
                {
                    combined[kvp.Key] = kvp.Value;
                }
            }
            
            // 사용자 헤더 추가 (인증 헤더 덮어쓰기 방지)
            if (userHeaders != null)
            {
                foreach (var kvp in userHeaders)
                {
                    if (!combined.ContainsKey(kvp.Key))
                    {
                        combined[kvp.Key] = kvp.Value;
                    }
                    else
                    {
                        Debug.LogWarning($"[HttpClientAuthExtension] 사용자 헤더가 인증 헤더와 충돌합니다: {kvp.Key}");
                    }
                }
            }
            
            return combined;
        }
        
        #endregion
    }
}
