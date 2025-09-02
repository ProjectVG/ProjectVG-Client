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
        private const string DEFAULT_FILE_NAME = "file.wav";

        private readonly Dictionary<string, string> defaultHeaders = new Dictionary<string, string>();
        private CancellationTokenSource cancellationTokenSource;
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
        public void Initialize()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = new CancellationTokenSource();
            ApplyNetworkConfig();
            SetupDefaultHeaders();
            IsInitialized = true;
        }
        
        public void AddDefaultHeader(string key, string value)
        {
            defaultHeaders[key] = value;
        }

        public void RemoveDefaultHeader(string key)
        {
            defaultHeaders.Remove(key);
        }

        public void SetAuthToken(string token)
        {
            AddDefaultHeader(AUTHORIZATION_HEADER, $"{BEARER_PREFIX}{token}");
        }

        private void EnsureAuthToken(bool requiresAuth)
        {
            if (!requiresAuth) return;
            
            try
            {
                var tokenManager = ProjectVG.Infrastructure.Auth.TokenManager.Instance;
                var accessToken = tokenManager.GetAccessToken();

                Debug.Log($"[HttpApiClient] Access Token: {accessToken}");

                if (!string.IsNullOrEmpty(accessToken))
                {
                    SetAuthToken(accessToken);
                }
                else
                {
                    Debug.LogWarning("[HttpApiClient] 유효한 Access Token을 찾을 수 없습니다.");
                    RemoveDefaultHeader(AUTHORIZATION_HEADER);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpApiClient] 토큰 설정 실패: {ex.Message}");
                RemoveDefaultHeader(AUTHORIZATION_HEADER);
            }
        }

        public async UniTask<T> GetAsync<T>(string endpoint, Dictionary<string, string> headers = null, bool requiresAuth = false, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            EnsureAuthToken(requiresAuth);
            
            var url = IsFullUrl(endpoint) ? endpoint : GetFullUrl(endpoint);
            return await SendJsonRequestAsync<T>(url, UnityWebRequest.kHttpVerbGET, null, headers, cancellationToken);
        }

        /// <summary>
        /// GET 요청 (HTTP 헤더 포함 응답)
        /// </summary>
        public async UniTask<(T Data, Dictionary<string, string> Headers)> GetWithHeadersAsync<T>(string endpoint, Dictionary<string, string> headers = null, bool requiresAuth = false, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            EnsureAuthToken(requiresAuth);
            
            var url = IsFullUrl(endpoint) ? endpoint : GetFullUrl(endpoint);
            return await SendJsonRequestWithHeadersAsync<T>(url, UnityWebRequest.kHttpVerbGET, null, headers, cancellationToken);
        }

        /// <summary>
        /// POST 요청 (HTTP 헤더 포함 응답)
        /// </summary>
        public async UniTask<(T Data, Dictionary<string, string> Headers)> PostWithHeadersAsync<T>(string endpoint, object data = null, Dictionary<string, string> headers = null, bool requiresAuth = false, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            EnsureAuthToken(requiresAuth);
            
            var url = GetFullUrl(endpoint);
            var jsonData = SerializeData(data);
            return await SendJsonRequestWithHeadersAsync<T>(url, UnityWebRequest.kHttpVerbPOST, jsonData, headers, cancellationToken);
        }

        public async UniTask<T> PostAsync<T>(string endpoint, object data = null, Dictionary<string, string> headers = null, bool requiresAuth = false, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            EnsureAuthToken(requiresAuth);
            
            var url = GetFullUrl(endpoint);
            var jsonData = SerializeData(data);
            return await SendJsonRequestAsync<T>(url, UnityWebRequest.kHttpVerbPOST, jsonData, headers, cancellationToken);
        }

        public async UniTask<T> PutAsync<T>(string endpoint, object data = null, Dictionary<string, string> headers = null, bool requiresAuth = false, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            EnsureAuthToken(requiresAuth);
            
            var url = GetFullUrl(endpoint);
            var jsonData = SerializeData(data);
            return await SendJsonRequestAsync<T>(url, UnityWebRequest.kHttpVerbPUT, jsonData, headers, cancellationToken);
        }

        public async UniTask<T> DeleteAsync<T>(string endpoint, Dictionary<string, string> headers = null, bool requiresAuth = false, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            EnsureAuthToken(requiresAuth);
            
            var url = GetFullUrl(endpoint);
            return await SendJsonRequestAsync<T>(url, UnityWebRequest.kHttpVerbDELETE, null, headers, cancellationToken);
        }

        public async UniTask<T> UploadFileAsync<T>(string endpoint, byte[] fileData, string fileName, string fieldName = "file", Dictionary<string, string> headers = null, bool requiresAuth = false, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            EnsureAuthToken(requiresAuth);
            
            var url = GetFullUrl(endpoint);
            var formData = new Dictionary<string, object> { { fieldName, fileData } };
            var fileNames = new Dictionary<string, string> { { fieldName, fileName } };
            
            return await SendFormDataRequestAsync<T>(url, formData, fileNames, headers, cancellationToken);
        }
        
        public async UniTask<T> PostFormDataAsync<T>(string endpoint, Dictionary<string, object> formData, Dictionary<string, string> headers, bool requiresAuth = false, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            EnsureAuthToken(requiresAuth);
            
            var url = IsFullUrl(endpoint) ? endpoint : GetFullUrl(endpoint);
            return await SendFormDataRequestAsync<T>(url, formData, null, headers, cancellationToken);
        }

        public async UniTask<T> PostFormDataAsync<T>(string endpoint, Dictionary<string, object> formData, Dictionary<string, string> fileNames, Dictionary<string, string> headers, bool requiresAuth = false, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            EnsureAuthToken(requiresAuth);
            ValidateFileSize(formData);
            
            var url = IsFullUrl(endpoint) ? endpoint : GetFullUrl(endpoint);
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

        private void EnsureInitialized()
        {
            if (!IsInitialized)
            {
                Initialize();
            }
        }
        
        private void ValidateFileSize(Dictionary<string, object> formData)
        {
            if (!NetworkConfig.EnableFileSizeCheck) return;
            
            foreach (var kvp in formData)
            {
                if (kvp.Value is byte[] byteData && byteData.Length > NetworkConfig.MaxFileSize)
                {
                    var fileSizeMB = byteData.Length / 1024.0 / 1024.0;
                    var maxSizeMB = NetworkConfig.MaxFileSize / 1024.0 / 1024.0;
                    throw new FileSizeExceededException(fileSizeMB, maxSizeMB);
                }
            }
        }
        
        private string SerializeData(object data)
        {
            return data == null ? null : JsonConvert.SerializeObject(data);
        }
        
        private void ApplyNetworkConfig()
        {
        }

        private void SetupDefaultHeaders()
        {
            defaultHeaders.Clear();
            defaultHeaders["Content-Type"] = NetworkConfig.ContentType;
            
#if !UNITY_WEBGL || UNITY_EDITOR
            defaultHeaders["User-Agent"] = NetworkConfig.UserAgent;
#else
            // WebGL에서는 커스텀 헤더로 클라이언트 식별
            defaultHeaders["X-Client-Type"] = "ProjectVG-WebGL";
            defaultHeaders["X-Client-Version"] = Application.version;
#endif
            
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
        private async UniTask<T> SendJsonRequestAsync<T>(string url, string method, string jsonData, Dictionary<string, string> headers, CancellationToken cancellationToken)
        {
            return await ExecuteRequestWithRetry<T>(async (attempt, token) =>
            {
                using var request = CreateJsonRequest(url, method, jsonData, headers);
                var operation = request.SendWebRequest();
                await operation.WithCancellation(token);
                
                if (request.result == UnityWebRequest.Result.Success)
                    return ParseResponse<T>(request);
                    
                await HandleRequestFailure(request, attempt, token);
                return default(T);
            }, cancellationToken);
        }

        private async UniTask<(T Data, Dictionary<string, string> Headers)> SendJsonRequestWithHeadersAsync<T>(string url, string method, string jsonData, Dictionary<string, string> headers, CancellationToken cancellationToken)
        {
            return await ExecuteRequestWithRetry<(T, Dictionary<string, string>)>(async (attempt, token) =>
            {
                using var request = CreateJsonRequest(url, method, jsonData, headers);
                var operation = request.SendWebRequest();
                await operation.WithCancellation(token);
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    var data = ParseResponse<T>(request);
                    var responseHeaders = ExtractResponseHeaders(request);
                    return (data, responseHeaders);
                }
                    
                await HandleRequestFailure(request, attempt, token);
                return default;
            }, cancellationToken);
        }

        private async UniTask<T> SendFormDataRequestAsync<T>(string url, Dictionary<string, object> formData, Dictionary<string, string> fileNames, Dictionary<string, string> headers, CancellationToken cancellationToken)
        {
            fileNames ??= new Dictionary<string, string>();
            
            return await ExecuteRequestWithRetry<T>(async (attempt, token) =>
            {
                var form = CreateFormData(formData, fileNames);
                using var request = UnityWebRequest.Post(url, form);
                SetupRequest(request, headers);
                request.timeout = (int)NetworkConfig.UploadTimeout;
                
                var operation = request.SendWebRequest();
                await operation.WithCancellation(token);
                
                if (request.result == UnityWebRequest.Result.Success)
                    return ParseResponse<T>(request);
                    
                await HandleRequestFailure(request, attempt, token, isFileUpload: true);
                return default(T);
            }, cancellationToken);
        }

        private CancellationToken CreateCombinedCancellationToken(CancellationToken cancellationToken)
        {
            if (cancellationTokenSource?.Token.CanBeCanceled == true)
            {
                try
                {
                    return CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancellationTokenSource.Token).Token;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[HttpApiClient] CancellationToken 생성 실패: {ex.Message}");
                }
            }
            
            return cancellationToken;
        }

        private async UniTask<T> ExecuteRequestWithRetry<T>(Func<int, CancellationToken, UniTask<T>> requestFunc, CancellationToken cancellationToken)
        {
            var combinedCancellationToken = CreateCombinedCancellationToken(cancellationToken);

            for (int attempt = 0; attempt <= NetworkConfig.MaxRetryCount; attempt++)
            {
                try
                {
                    return await requestFunc(attempt, combinedCancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (ApiException) when (attempt == NetworkConfig.MaxRetryCount)
                {
                    throw;
                }
                catch (Exception ex) when (ex is not ApiException && attempt < NetworkConfig.MaxRetryCount)
                {
                    await DelayForRetry(attempt, combinedCancellationToken);
                }
            }

            throw new ApiException($"{NetworkConfig.MaxRetryCount + 1}번 시도 후 요청 실패", 0, "최대 재시도 횟수 초과");
        }
        
        private async UniTask HandleRequestFailure(UnityWebRequest request, int attempt, CancellationToken cancellationToken, bool isFileUpload = false)
        {
            var error = new ApiException(request.error, request.responseCode, request.downloadHandler?.text);
            var requestType = isFileUpload ? "파일 업로드" : "API 요청";
            
            if (ShouldRetry(request.responseCode) && attempt < NetworkConfig.MaxRetryCount)
            {
                Debug.LogWarning($"[HttpApiClient] {requestType} 실패 (시도 {attempt + 1}/{NetworkConfig.MaxRetryCount + 1}): {error.Message}");
                await DelayForRetry(attempt, cancellationToken);
                return;
            }
            
            throw error;
        }
        
        private async UniTask DelayForRetry(int attempt, CancellationToken cancellationToken)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(NetworkConfig.RetryDelay * (attempt + 1)), cancellationToken: cancellationToken);
        }
        
        private WWWForm CreateFormData(Dictionary<string, object> formData, Dictionary<string, string> fileNames)
        {
            var form = new WWWForm();
            
            foreach (var kvp in formData)
            {
                if (kvp.Value is byte[] byteData)
                {
                    string fileName = fileNames.ContainsKey(kvp.Key) ? fileNames[kvp.Key] : DEFAULT_FILE_NAME;
                    form.AddBinaryData(kvp.Key, byteData, fileName);
                }
                else
                {
                    form.AddField(kvp.Key, kvp.Value?.ToString() ?? string.Empty);
                }
            }
            
            return form;
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
            
        }

        private T ParseResponse<T>(UnityWebRequest request)
        {
            var responseText = request.downloadHandler?.text;
            
            if (string.IsNullOrEmpty(responseText))
                return default(T);

            try
            {
                return JsonConvert.DeserializeObject<T>(responseText);
            }
            catch (Exception ex)
            {
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
                var errorMessage = $"JSON 파싱 실패: {originalException.Message} (Unity JsonUtility 폴백도 실패: {fallbackEx.Message})";
                Debug.LogError($"[HttpApiClient] {errorMessage}");
                throw new ApiException(errorMessage, responseCode, responseText);
            }
        }

        private Dictionary<string, string> ExtractResponseHeaders(UnityWebRequest request)
        {
            var headers = new Dictionary<string, string>();
            var headerNames = new[] 
            {
                "X-Access-Token", "X-Refresh-Token", "X-Expires-In", "X-User-Id",
                "Content-Type", "Authorization"
            };
            
            foreach (var headerName in headerNames)
            {
                var headerValue = request.GetResponseHeader(headerName);
                if (!string.IsNullOrEmpty(headerValue))
                    headers[headerName] = headerValue;
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