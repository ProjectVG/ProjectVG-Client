using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using System.Threading;
using ProjectVG.Infrastructure.Network.Configs;
using ProjectVG.Infrastructure.Network.DTOs.Chat;
using ProjectVG.Infrastructure.Network.Services;
using Newtonsoft.Json;

namespace ProjectVG.Infrastructure.Network.Http
{
    public class HttpApiClient : Singleton<HttpApiClient>
    {
        [Header("API Configuration")]

        private const string ACCEPT_HEADER = "application/json";
        private const string AUTHORIZATION_HEADER = "Authorization";
        private const string BEARER_PREFIX = "Bearer ";

        private readonly Dictionary<string, string> defaultHeaders = new Dictionary<string, string>();
        private CancellationTokenSource cancellationTokenSource;
        private SessionManager _sessionManager;
        public bool IsInitialized { get; private set; }
        
        #region Unity Lifecycle
        
        protected override void Awake()
        {
            base.Awake();
        }

        private void OnDestroy()
        {
            Shutdown();
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// 초기화 실행
        /// </summary>
        public void Initialize(SessionManager sessionManager)
        {
            _sessionManager = sessionManager;
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = new CancellationTokenSource();
            ApplyNetworkConfig();
            SetupDefaultHeaders();
            IsInitialized = true;
        }
        
        /// <summary>
        /// 기본 헤더 추가
        /// </summary>
        public void AddDefaultHeader(string key, string value)
        {
            defaultHeaders[key] = value;
        }

        /// <summary>
        /// 인증 토큰 설정
        /// </summary>
        public void SetAuthToken(string token)
        {
            AddDefaultHeader(AUTHORIZATION_HEADER, $"{BEARER_PREFIX}{token}");
        }

        /// <summary>
        /// TokenManager에서 자동으로 토큰 설정
        /// </summary>
        public void SetAuthTokenFromManager()
        {
            try
            {
                var tokenManager = ProjectVG.Infrastructure.Auth.TokenManager.Instance;
                var accessToken = tokenManager.GetAccessToken();
                
                if (!string.IsNullOrEmpty(accessToken))
                {
                    SetAuthToken(accessToken);
                    Debug.Log("[HttpApiClient] TokenManager에서 Access Token 설정 완료");
                }
                else
                {
                    Debug.LogWarning("[HttpApiClient] TokenManager에서 유효한 Access Token을 찾을 수 없습니다.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpApiClient] TokenManager에서 토큰 설정 실패: {ex.Message}");
            }
        }

        public async UniTask<T> GetAsync<T>(string endpoint, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
        {
            // 자동 초기화
            if (!IsInitialized)
            {
                Debug.LogWarning("[HttpApiClient] 자동 초기화 수행");
                Initialize(null);
            }
            
            var url = IsFullUrl(endpoint) ? endpoint : GetFullUrl(endpoint);
            Debug.Log($"[HttpApiClient] GET 요청 시작: {url}");
            Debug.Log($"[HttpApiClient] 헤더: {JsonConvert.SerializeObject(headers)}");
            return await SendJsonRequestAsync<T>(url, UnityWebRequest.kHttpVerbGET, null, headers, cancellationToken);
        }

        /// <summary>
        /// GET 요청 (HTTP 헤더 포함 응답)
        /// </summary>
        public async UniTask<(T Data, Dictionary<string, string> Headers)> GetWithHeadersAsync<T>(string endpoint, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
        {
            var url = IsFullUrl(endpoint) ? endpoint : GetFullUrl(endpoint);
            return await SendJsonRequestWithHeadersAsync<T>(url, UnityWebRequest.kHttpVerbGET, null, headers, cancellationToken);
        }

        public async UniTask<T> PostAsync<T>(string endpoint, object data = null, Dictionary<string, string> headers = null, bool requiresSession = false, CancellationToken cancellationToken = default)
        {
            var url = GetFullUrl(endpoint);
            var jsonData = SerializeData(data, requiresSession);
            return await SendJsonRequestAsync<T>(url, UnityWebRequest.kHttpVerbPOST, jsonData, headers, cancellationToken);
        }

        public async UniTask<T> PutAsync<T>(string endpoint, object data = null, Dictionary<string, string> headers = null, bool requiresSession = false, CancellationToken cancellationToken = default)
        {
            var url = GetFullUrl(endpoint);
            var jsonData = SerializeData(data, requiresSession);
            return await SendJsonRequestAsync<T>(url, UnityWebRequest.kHttpVerbPUT, jsonData, headers, cancellationToken);
        }

        public async UniTask<T> DeleteAsync<T>(string endpoint, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
        {
            var url = GetFullUrl(endpoint);
            return await SendJsonRequestAsync<T>(url, UnityWebRequest.kHttpVerbDELETE, null, headers, cancellationToken);
        }

        public async UniTask<T> UploadFileAsync<T>(string endpoint, byte[] fileData, string fileName, string fieldName = "file", Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
        {
            var url = GetFullUrl(endpoint);
            var formData = new Dictionary<string, object>
            {
                { fieldName, fileData }
            };
            var fileNames = new Dictionary<string, string>
            {
                { fieldName, fileName }
            };
            return await SendFormDataRequestAsync<T>(url, formData, fileNames, headers, cancellationToken);
        }
        
        public async UniTask<T> PostFormDataAsync<T>(string endpoint, Dictionary<string, object> formData, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
        {
            var url = IsFullUrl(endpoint) ? endpoint : GetFullUrl(endpoint);
            return await SendFormDataRequestAsync<T>(url, formData, null, headers, cancellationToken);
        }

        public async UniTask<T> PostFormDataAsync<T>(string endpoint, Dictionary<string, object> formData, Dictionary<string, string> fileNames, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
        {
            var url = IsFullUrl(endpoint) ? endpoint : GetFullUrl(endpoint);
            
            // 파일 크기 검사
            if (NetworkConfig.EnableFileSizeCheck)
            {
                foreach (var kvp in formData)
                {
                    if (kvp.Value is byte[] byteData)
                    {
                        if (byteData.Length > NetworkConfig.MaxFileSize)
                        {
                            var fileSizeMB = byteData.Length / 1024.0 / 1024.0;
                            var maxSizeMB = NetworkConfig.MaxFileSize / 1024.0 / 1024.0;
                            throw new FileSizeExceededException(fileSizeMB, maxSizeMB);
                        }
                    }
                }
            }
            
            return await SendFormDataRequestAsync<T>(url, formData, fileNames, headers, cancellationToken);
        }

        /// <summary>
        /// 종료 처리
        /// </summary>
        public void Shutdown()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            IsInitialized = false;
        }
        
        #endregion
        
        #region Private Methods

        private void ApplyNetworkConfig()
        {
        }

        private void SetupDefaultHeaders()
        {
            defaultHeaders.Clear();
            defaultHeaders["Content-Type"] = NetworkConfig.ContentType;
            defaultHeaders["User-Agent"] = NetworkConfig.UserAgent;
            defaultHeaders["Accept"] = ACCEPT_HEADER;
        }

        private string GetFullUrl(string endpoint)
        {
            return NetworkConfig.GetFullApiUrl(endpoint);
        }
        
        private bool IsFullUrl(string url)
        {
            return url.StartsWith("http://") || url.StartsWith("https://");
        }

        private string SerializeData(object data, bool requiresSession = false)
        {
            if (data == null) return null;
            
            var jsonData = JsonConvert.SerializeObject(data);
            
            if (requiresSession && data is ChatRequest chatRequest && string.IsNullOrEmpty(chatRequest.sessionId))
            {
                var sessionId = _sessionManager?.SessionId ?? "";
                if (!string.IsNullOrEmpty(sessionId))
                {
                    var jsonObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonData);
                    jsonObject["session_id"] = sessionId;
                    jsonData = JsonConvert.SerializeObject(jsonObject);
                }
                else
                {
                    Debug.LogWarning("[HttpApiClient] 세션 연결이 필요한 요청이지만 세션 ID를 획득할 수 없습니다.");
                }
            }
            
            return jsonData;
        }

		

        private async UniTask<T> SendJsonRequestAsync<T>(string url, string method, string jsonData, Dictionary<string, string> headers, CancellationToken cancellationToken)
        {
            Debug.Log($"[HttpApiClient] SendJsonRequestAsync 시작");
            Debug.Log($"[HttpApiClient] URL: {url}");
            Debug.Log($"[HttpApiClient] Method: {method}");
            Debug.Log($"[HttpApiClient] JSON Data: {jsonData}");
            Debug.Log($"[HttpApiClient] Headers: {JsonConvert.SerializeObject(headers)}");
            Debug.Log($"[HttpApiClient] IsInitialized: {IsInitialized}");
            Debug.Log($"[HttpApiClient] cancellationTokenSource null: {cancellationTokenSource == null}");
            
            var combinedCancellationToken = CreateCombinedCancellationToken(cancellationToken);

            for (int attempt = 0; attempt <= NetworkConfig.MaxRetryCount; attempt++)
            {
                try
                {
                    using var request = CreateJsonRequest(url, method, jsonData, headers);
                    
                    var operation = request.SendWebRequest();
                    await operation.WithCancellation(combinedCancellationToken);

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        return ParseResponse<T>(request);
                    }
                    else
                    {
                        await HandleRequestFailure(request, attempt, combinedCancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex) when (ex is not ApiException)
                {
                    await HandleRequestException(ex, attempt, combinedCancellationToken);
                }
            }

            throw new ApiException($"{NetworkConfig.MaxRetryCount + 1}번 시도 후 요청 실패", 0, "최대 재시도 횟수 초과");
        }

        private async UniTask<(T Data, Dictionary<string, string> Headers)> SendJsonRequestWithHeadersAsync<T>(string url, string method, string jsonData, Dictionary<string, string> headers, CancellationToken cancellationToken)
        {
            var combinedCancellationToken = CreateCombinedCancellationToken(cancellationToken);

            for (int attempt = 0; attempt <= NetworkConfig.MaxRetryCount; attempt++)
            {
                try
                {
                    using var request = CreateJsonRequest(url, method, jsonData, headers);
                    
                    var operation = request.SendWebRequest();
                    await operation.WithCancellation(combinedCancellationToken);

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var data = ParseResponse<T>(request);
                        var responseHeaders = ExtractResponseHeaders(request);
                        return (data, responseHeaders);
                    }
                    else
                    {
                        await HandleRequestFailure(request, attempt, combinedCancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex) when (ex is not ApiException)
                {
                    await HandleRequestException(ex, attempt, combinedCancellationToken);
                }
            }

            throw new ApiException($"{NetworkConfig.MaxRetryCount + 1}번 시도 후 요청 실패", 0, "최대 재시도 횟수 초과");
        }

		

		
        


        private async UniTask<T> SendFormDataRequestAsync<T>(string url, Dictionary<string, object> formData, Dictionary<string, string> fileNames, Dictionary<string, string> headers, CancellationToken cancellationToken)
        {
            fileNames = fileNames ?? new Dictionary<string, string>();
            var combinedCancellationToken = CreateCombinedCancellationToken(cancellationToken);

            for (int attempt = 0; attempt <= NetworkConfig.MaxRetryCount; attempt++)
            {
                try
                {
                    var form = new WWWForm();
                    Debug.Log($"[HttpApiClient] 폼 데이터 전송 시작 - URL: {url}");
                    Debug.Log($"[HttpApiClient] 실제 전송 URL: {url}");
                    
                    foreach (var kvp in formData)
                    {
                        if (kvp.Value is byte[] byteData)
                        {
                            string fileName = fileNames.ContainsKey(kvp.Key) ? fileNames[kvp.Key] : "file.wav";
                            form.AddBinaryData(kvp.Key, byteData, fileName);
                            Debug.Log($"[HttpApiClient] 바이너리 데이터 추가 - 필드: {kvp.Key}, 파일명: {fileName}, 크기: {byteData.Length} bytes");
                        }
                        else
                        {
                            form.AddField(kvp.Key, kvp.Value.ToString());
                            Debug.Log($"[HttpApiClient] 필드 추가 - {kvp.Key}: {kvp.Value}");
                        }
                    }
                    
                    using var request = UnityWebRequest.Post(url, form);
                    // 파일 업로드 시 Content-Type은 UnityWebRequest가 자동으로 설정하도록 함
                    SetupRequest(request, headers);
                    request.timeout = (int)NetworkConfig.UploadTimeout; // Use UploadTimeout
                    
                    var operation = request.SendWebRequest();
                    await operation.WithCancellation(combinedCancellationToken);

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        return ParseResponse<T>(request);
                    }
                    else
                    {
                        await HandleFileUploadFailure(request, attempt, combinedCancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex) when (ex is not ApiException)
                {
                    await HandleFileUploadException(ex, attempt, combinedCancellationToken);
                }
            }

            throw new ApiException($"{NetworkConfig.MaxRetryCount + 1}번 시도 후 파일 업로드 실패", 0, "최대 재시도 횟수 초과");
        }

        private CancellationToken CreateCombinedCancellationToken(CancellationToken cancellationToken)
        {
            if (cancellationTokenSource == null)
            {
                Debug.LogError("[HttpApiClient] cancellationTokenSource가 null입니다. HttpApiClient가 초기화되지 않았습니다.");
                return cancellationToken;
            }
            
            try
            {
                return CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancellationTokenSource.Token).Token;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpApiClient] CreateCombinedCancellationToken 실패: {ex.Message}");
                return cancellationToken;
            }
        }

        private async UniTask HandleRequestFailure(UnityWebRequest request, int attempt, CancellationToken cancellationToken)
        {
            var error = new ApiException(request.error, request.responseCode, request.downloadHandler?.text);
            
            if (ShouldRetry(request.responseCode) && attempt < NetworkConfig.MaxRetryCount)
            {
                Debug.LogWarning($"[HttpApiClient] API 요청 실패 (시도 {attempt + 1}/{NetworkConfig.MaxRetryCount + 1}): {error.Message}");
                await UniTask.Delay(TimeSpan.FromSeconds(NetworkConfig.RetryDelay * (attempt + 1)), cancellationToken: cancellationToken);
                return;
            }
            
            throw error;
        }

        private async UniTask HandleRequestException(Exception ex, int attempt, CancellationToken cancellationToken)
        {
            if (attempt < NetworkConfig.MaxRetryCount)
            {
                Debug.LogWarning($"[HttpApiClient] API 요청 예외 발생 (시도 {attempt + 1}/{NetworkConfig.MaxRetryCount + 1}): {ex.Message}");
                await UniTask.Delay(TimeSpan.FromSeconds(NetworkConfig.RetryDelay * (attempt + 1)), cancellationToken: cancellationToken);
                return;
            }
            throw new ApiException($"{NetworkConfig.MaxRetryCount + 1}번 시도 후 요청 실패", 0, ex.Message);
        }

        private async UniTask HandleFileUploadFailure(UnityWebRequest request, int attempt, CancellationToken cancellationToken)
        {
            var error = new ApiException(request.error, request.responseCode, request.downloadHandler?.text);
            
            if (ShouldRetry(request.responseCode) && attempt < NetworkConfig.MaxRetryCount)
            {
                Debug.LogWarning($"[HttpApiClient] 파일 업로드 실패 (시도 {attempt + 1}/{NetworkConfig.MaxRetryCount + 1}): {error.Message}");
                await UniTask.Delay(TimeSpan.FromSeconds(NetworkConfig.RetryDelay * (attempt + 1)), cancellationToken: cancellationToken);
                return;
            }
            
            throw error;
        }

        private async UniTask HandleFileUploadException(Exception ex, int attempt, CancellationToken cancellationToken)
        {
            if (attempt < NetworkConfig.MaxRetryCount)
            {
                Debug.LogWarning($"[HttpApiClient] 파일 업로드 예외 발생 (시도 {attempt + 1}/{NetworkConfig.MaxRetryCount + 1}): {ex.Message}");
                await UniTask.Delay(TimeSpan.FromSeconds(NetworkConfig.RetryDelay * (attempt + 1)), cancellationToken: cancellationToken);
                return;
            }
            throw new ApiException($"{NetworkConfig.MaxRetryCount + 1}번 시도 후 파일 업로드 실패", 0, ex.Message);
        }

		

        private UnityWebRequest CreateJsonRequest(string url, string method, string jsonData, Dictionary<string, string> headers)
        {
            var request = new UnityWebRequest(url, method);
            
            if (!string.IsNullOrEmpty(jsonData))
            {
                var bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.SetRequestHeader("Content-Type", "application/json");
            }
            
            request.downloadHandler = new DownloadHandlerBuffer();
            SetupRequest(request, headers);
            request.timeout = (int)NetworkConfig.HttpTimeout;
            
            return request;
        }

        private void SetupRequest(UnityWebRequest request, Dictionary<string, string> headers)
        {
            // UnityWebRequest.Post로 생성된 요청은 무조건 파일 업로드로 처리
            bool isFileUpload = request.method == UnityWebRequest.kHttpVerbPOST && 
                               request.uploadHandler != null;
            
            foreach (var header in defaultHeaders)
            {
                // 파일 업로드 시에는 Content-Type 헤더를 제외 (UnityWebRequest가 자동 설정)
                if (isFileUpload && header.Key.ToLower() == "content-type")
                {
                    continue;
                }
                
                request.SetRequestHeader(header.Key, header.Value);
            }

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.SetRequestHeader(header.Key, header.Value);
                }
            }
            
            // 디버깅: Content-Type 헤더 확인
            string contentType = request.GetRequestHeader("Content-Type");
            Debug.Log($"[HttpApiClient] 요청 헤더 설정 완료 - Content-Type: {contentType}");
        }

        private T ParseResponse<T>(UnityWebRequest request)
        {
            var responseText = request.downloadHandler?.text;
            
            Debug.Log($"[HttpApiClient] 응답 파싱 - Status: {request.responseCode}, Content-Length: {request.downloadHandler?.data?.Length ?? 0}");
            Debug.Log($"[HttpApiClient] 응답 텍스트: '{responseText}'");
            
            if (string.IsNullOrEmpty(responseText))
            {
                Debug.LogWarning("[HttpApiClient] 응답 텍스트가 비어있습니다.");
                return default(T);
            }

            try
            {
                return JsonConvert.DeserializeObject<T>(responseText);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpApiClient] JSON 파싱 실패: {ex.Message}");
                return TryFallbackParse<T>(responseText, request.responseCode, ex);
            }
        }

        private T TryFallbackParse<T>(string responseText, long responseCode, Exception originalException)
        {
            try
            {
                return JsonUtility.FromJson<T>(responseText);
            }
            catch (Exception fallbackEx)
            {
                throw new ApiException($"응답 파싱 실패: {originalException.Message} (폴백도 실패: {fallbackEx.Message})", responseCode, responseText);
            }
        }

        /// <summary>
        /// HTTP 응답 헤더 추출
        /// </summary>
        private Dictionary<string, string> ExtractResponseHeaders(UnityWebRequest request)
        {
            var headers = new Dictionary<string, string>();
            
            // UnityWebRequest에서 사용 가능한 헤더들
            var responseHeaders = request.GetResponseHeader("X-Access-Token");
            if (!string.IsNullOrEmpty(responseHeaders))
                headers["X-Access-Token"] = responseHeaders;
                
            responseHeaders = request.GetResponseHeader("X-Refresh-Token");
            if (!string.IsNullOrEmpty(responseHeaders))
                headers["X-Refresh-Token"] = responseHeaders;
                
            responseHeaders = request.GetResponseHeader("X-Expires-In");
            if (!string.IsNullOrEmpty(responseHeaders))
                headers["X-Expires-In"] = responseHeaders;
                
            responseHeaders = request.GetResponseHeader("X-User-Id");
            if (!string.IsNullOrEmpty(responseHeaders))
                headers["X-User-Id"] = responseHeaders;
                
            // 기타 헤더들도 추가 가능
            responseHeaders = request.GetResponseHeader("Content-Type");
            if (!string.IsNullOrEmpty(responseHeaders))
                headers["Content-Type"] = responseHeaders;
                
            responseHeaders = request.GetResponseHeader("Authorization");
            if (!string.IsNullOrEmpty(responseHeaders))
                headers["Authorization"] = responseHeaders;
            
            Debug.Log($"[HttpApiClient] 응답 헤더 추출: {headers.Count}개 헤더");
            foreach (var header in headers)
            {
                Debug.Log($"[HttpApiClient] 헤더: {header.Key} = {header.Value}");
            }
            
            return headers;
        }

        private bool ShouldRetry(long responseCode)
        {
            return responseCode >= 500 || responseCode == 429;
        }
        
        #endregion
    }

    public class ApiException : Exception
    {
        public long StatusCode { get; }
        public string ResponseBody { get; }

        public ApiException(string message, long statusCode, string responseBody) 
            : base(message)
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }
    }

    public class FileSizeExceededException : ApiException
    {
        public FileSizeExceededException(double fileSizeMB, double maxSizeMB) 
            : base($"File size exceeds limit: {fileSizeMB:F2}MB (limit: {maxSizeMB:F2}MB)", 413, null)
        {
            FileSizeMB = fileSizeMB;
            MaxSizeMB = maxSizeMB;
        }
        
        public double FileSizeMB { get; }
        public double MaxSizeMB { get; }
    }
} 