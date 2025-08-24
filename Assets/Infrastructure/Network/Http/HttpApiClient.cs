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

        public void Initialize(SessionManager sessionManager)
        {
            if (sessionManager == null) {
                throw new ArgumentNullException(nameof(sessionManager), "[HttpApiClient] SessionManager는 null일 수 없습니다.");
            }

            _sessionManager = sessionManager;
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = new CancellationTokenSource();
            SetupDefaultHeaders();
            IsInitialized = true;
        }

        public void AddDefaultHeader(string key, string value)
        {
            defaultHeaders[key] = value;
        }

        public void SetAuthToken(string token)
        {
            AddDefaultHeader(AUTHORIZATION_HEADER, $"{BEARER_PREFIX}{token}");
        }

        #endregion

        #region HTTP Methods

        public async UniTask<T> GetAsync<T>(string endpoint, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
        {
            ThrowIfNotInitialized();
            var url = IsFullUrl(endpoint) ? endpoint : GetFullUrl(endpoint);
            return await SendJsonRequestAsync<T>(url, UnityWebRequest.kHttpVerbGET, null, headers, cancellationToken);
        }

        public async UniTask<(T Data, Dictionary<string, string> Headers)> GetWithHeadersAsync<T>(string endpoint, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
        {
            ThrowIfNotInitialized();
            var url = IsFullUrl(endpoint) ? endpoint : GetFullUrl(endpoint);
            return await SendJsonRequestWithHeadersAsync<T>(url, UnityWebRequest.kHttpVerbGET, null, headers, cancellationToken);
        }

        public async UniTask<T> PostAsync<T>(string endpoint, object data = null, Dictionary<string, string> headers = null, bool requiresSession = false, CancellationToken cancellationToken = default)
        {
            ThrowIfNotInitialized();
            var url = GetFullUrl(endpoint);
            var jsonData = SerializeData(data, requiresSession);
            return await SendJsonRequestAsync<T>(url, UnityWebRequest.kHttpVerbPOST, jsonData, headers, cancellationToken);
        }

        public async UniTask<T> PutAsync<T>(string endpoint, object data = null, Dictionary<string, string> headers = null, bool requiresSession = false, CancellationToken cancellationToken = default)
        {
            ThrowIfNotInitialized();
            var url = GetFullUrl(endpoint);
            var jsonData = SerializeData(data, requiresSession);
            return await SendJsonRequestAsync<T>(url, UnityWebRequest.kHttpVerbPUT, jsonData, headers, cancellationToken);
        }

        public async UniTask<T> DeleteAsync<T>(string endpoint, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
        {
            ThrowIfNotInitialized();
            var url = GetFullUrl(endpoint);
            return await SendJsonRequestAsync<T>(url, UnityWebRequest.kHttpVerbDELETE, null, headers, cancellationToken);
        }

        public async UniTask<T> UploadFileAsync<T>(string endpoint, byte[] fileData, string fileName, string fieldName = "file", Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
        {
            ThrowIfNotInitialized();
            var url = GetFullUrl(endpoint);
            var formData = new Dictionary<string, object> { { fieldName, fileData } };
            var fileNames = new Dictionary<string, string> { { fieldName, fileName } };
            return await SendFormDataRequestAsync<T>(url, formData, fileNames, headers, cancellationToken);
        }

        public async UniTask<T> PostFormDataAsync<T>(string endpoint, Dictionary<string, object> formData, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
        {
            ThrowIfNotInitialized();
            var url = IsFullUrl(endpoint) ? endpoint : GetFullUrl(endpoint);
            return await SendFormDataRequestAsync<T>(url, formData, null, headers, cancellationToken);
        }

        public async UniTask<T> PostFormDataAsync<T>(string endpoint, Dictionary<string, object> formData, Dictionary<string, string> fileNames, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
        {
            ThrowIfNotInitialized();
            var url = IsFullUrl(endpoint) ? endpoint : GetFullUrl(endpoint);
            ValidateFileSizes(formData);
            return await SendFormDataRequestAsync<T>(url, formData, fileNames, headers, cancellationToken);
        }

        public void Shutdown()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            IsInitialized = false;
        }
        #endregion

        #region Private Methods

        private void ThrowIfNotInitialized()
        {
            if (!IsInitialized) {
                throw new InvalidOperationException("[HttpApiClient] HttpApiClient가 초기화되지 않았습니다. Initialize(SessionManager) 메서드를 먼저 호출하세요.");
            }
        }

        private void ValidateFileSizes(Dictionary<string, object> formData)
        {
            if (!NetworkConfig.EnableFileSizeCheck) return;

            foreach (var kvp in formData) {
                if (kvp.Value is byte[] byteData && byteData.Length > NetworkConfig.MaxFileSize) {
                    var fileSizeMB = byteData.Length / 1024.0 / 1024.0;
                    var maxSizeMB = NetworkConfig.MaxFileSize / 1024.0 / 1024.0;
                    throw new FileSizeExceededException(fileSizeMB, maxSizeMB);
                }
            }
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

            if (requiresSession && data is ChatRequest chatRequest && string.IsNullOrEmpty(chatRequest.sessionId)) {
                var sessionId = _sessionManager.SessionId;
                if (!string.IsNullOrEmpty(sessionId)) {
                    var jsonObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonData);
                    jsonObject["session_id"] = sessionId;
                    jsonData = JsonConvert.SerializeObject(jsonObject);
                }
                else {
                    Debug.LogWarning("[HttpApiClient] 세션 연결이 필요한 요청이지만 세션 ID를 획득할 수 없습니다.");
                }
            }

            return jsonData;
        }

        private async UniTask<T> SendJsonRequestAsync<T>(string url, string method, string jsonData, Dictionary<string, string> headers, CancellationToken cancellationToken)
        {
            LogRequestInfo(url, method, jsonData, headers);
            return await SendJsonRequestCoreAsync<T>(url, method, jsonData, headers, cancellationToken, false);
        }

        private async UniTask<(T Data, Dictionary<string, string> Headers)> SendJsonRequestWithHeadersAsync<T>(string url, string method, string jsonData, Dictionary<string, string> headers, CancellationToken cancellationToken)
        {
            return await SendJsonRequestCoreAsync<(T Data, Dictionary<string, string> Headers)>(url, method, jsonData, headers, cancellationToken, true);
        }

        private async UniTask<T> SendJsonRequestCoreAsync<T>(string url, string method, string jsonData, Dictionary<string, string> headers, CancellationToken cancellationToken, bool includeHeaders)
        {
            var combinedCancellationToken = CreateCombinedCancellationToken(cancellationToken);

            for (int attempt = 0; attempt <= NetworkConfig.MaxRetryCount; attempt++) {
                try {
                    using var request = CreateJsonRequest(url, method, jsonData, headers);
                    var operation = request.SendWebRequest();
                    await operation.WithCancellation(combinedCancellationToken);

                    if (request.result == UnityWebRequest.Result.Success) {
                        if (includeHeaders) {
                            var data = ParseResponse<T>(request);
                            var responseHeaders = ExtractResponseHeaders(request);
                            return (T)(object)(data, responseHeaders);
                        }
                        else {
                            return ParseResponse<T>(request);
                        }
                    }
                    else {
                        Debug.LogWarning($"[HttpApiClient] request.result != Success: {request.result}, StatusCode: {request.responseCode}");
                        throw new ApiException(request.error, request.responseCode, request.downloadHandler?.text);
                    }
                }
                catch (OperationCanceledException) {
                    throw;
                }
                catch (Exception ex) when (ex is not ApiException) {
                    await HandleRequestException(ex, attempt, combinedCancellationToken);
                }
            }

            throw new ApiException($"{NetworkConfig.MaxRetryCount + 1}번 시도 후 요청 실패", 0, "최대 재시도 횟수 초과");
        }

        private async UniTask<T> SendFormDataRequestAsync<T>(string url, Dictionary<string, object> formData, Dictionary<string, string> fileNames, Dictionary<string, string> headers, CancellationToken cancellationToken)
        {
            fileNames = fileNames ?? new Dictionary<string, string>();
            var combinedCancellationToken = CreateCombinedCancellationToken(cancellationToken);

            for (int attempt = 0; attempt <= NetworkConfig.MaxRetryCount; attempt++) {
                try {
                    var form = new WWWForm();

                    foreach (var kvp in formData) {
                        if (kvp.Value is byte[] byteData) {
                            string fileName = fileNames.ContainsKey(kvp.Key) ? fileNames[kvp.Key] : "file.wav";
                            form.AddBinaryData(kvp.Key, byteData, fileName);
                        }
                        else {
                            form.AddField(kvp.Key, kvp.Value.ToString());
                        }
                    }

                    using var request = UnityWebRequest.Post(url, form);
                    SetupRequest(request, headers);
                    request.timeout = (int)NetworkConfig.UploadTimeout;

                    var operation = request.SendWebRequest();
                    await operation.WithCancellation(combinedCancellationToken);

                    if (request.result == UnityWebRequest.Result.Success) {
                        return ParseResponse<T>(request);
                    }
                    else {
                        await HandleFileUploadFailure(request, attempt, combinedCancellationToken);
                    }
                }
                catch (OperationCanceledException) {
                    throw;
                }
                catch (Exception ex) when (ex is not ApiException) {
                    await HandleFileUploadException(ex, attempt, combinedCancellationToken);
                }
            }

            throw new ApiException($"{NetworkConfig.MaxRetryCount + 1}번 시도 후 파일 업로드 실패", 0, "최대 재시도 횟수 초과");
        }

        private CancellationToken CreateCombinedCancellationToken(CancellationToken cancellationToken)
        {
            if (cancellationTokenSource == null) {
                Debug.LogError("[HttpApiClient] cancellationTokenSource가 null입니다. HttpApiClient가 초기화되지 않았습니다.");
                return cancellationToken;
            }

            try {
                return CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancellationTokenSource.Token).Token;
            }
            catch (Exception ex) {
                Debug.LogError($"[HttpApiClient] CreateCombinedCancellationToken 실패: {ex.Message}");
                return cancellationToken;
            }
        }

        private async UniTask HandleRequestException(Exception ex, int attempt, CancellationToken cancellationToken)
        {
            if (ex is UnityWebRequestException webRequestEx) {
                var statusCode = webRequestEx.ResponseCode;

                if (ShouldRetry(statusCode) && attempt < NetworkConfig.MaxRetryCount) {
                    Debug.LogWarning($"[HttpApiClient] HTTP 에러 ({statusCode}) - 재시도 (시도 {attempt + 1}/{NetworkConfig.MaxRetryCount + 1})");
                    await DelayWithBackoff(attempt, cancellationToken);
                    return;
                }
                else {
                    Debug.LogWarning($"[HttpApiClient] HTTP 에러 ({statusCode}) - 재시도하지 않음");
                    throw new ApiException(webRequestEx.Message, statusCode, webRequestEx.Text);
                }
            }

            await HandleGenericException(ex, attempt, cancellationToken, "API 요청");
        }

        private async UniTask HandleFileUploadFailure(UnityWebRequest request, int attempt, CancellationToken cancellationToken)
        {
            var error = new ApiException(request.error, request.responseCode, request.downloadHandler?.text);

            if (ShouldRetry(request.responseCode) && attempt < NetworkConfig.MaxRetryCount) {
                Debug.LogWarning($"[HttpApiClient] 파일 업로드 실패 (시도 {attempt + 1}/{NetworkConfig.MaxRetryCount + 1}): {error.Message}");
                await DelayWithBackoff(attempt, cancellationToken);
                return;
            }

            throw error;
        }

        private async UniTask HandleFileUploadException(Exception ex, int attempt, CancellationToken cancellationToken)
        {
            await HandleGenericException(ex, attempt, cancellationToken, "파일 업로드");
        }

        private async UniTask HandleGenericException(Exception ex, int attempt, CancellationToken cancellationToken, string operationType)
        {
            if (attempt < NetworkConfig.MaxRetryCount) {
                Debug.LogWarning($"[HttpApiClient] {operationType} 예외 발생 (시도 {attempt + 1}/{NetworkConfig.MaxRetryCount + 1}): {ex.Message}");
                await DelayWithBackoff(attempt, cancellationToken);
                return;
            }
            throw new ApiException($"{NetworkConfig.MaxRetryCount + 1}번 시도 후 {operationType} 실패", 0, ex.Message);
        }

        private async UniTask DelayWithBackoff(int attempt, CancellationToken cancellationToken)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(NetworkConfig.RetryDelay * (attempt + 1)), cancellationToken: cancellationToken);
        }

        private UnityWebRequest CreateJsonRequest(string url, string method, string jsonData, Dictionary<string, string> headers)
        {
            var request = new UnityWebRequest(url, method);

            if (!string.IsNullOrEmpty(jsonData)) {
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
            bool isFileUpload = request.method == UnityWebRequest.kHttpVerbPOST && request.uploadHandler != null;

            foreach (var header in defaultHeaders) {
                if (isFileUpload && header.Key.ToLower() == "content-type") {
                    continue;
                }

                request.SetRequestHeader(header.Key, header.Value);
            }

            if (headers != null) {
                foreach (var header in headers) {
                    request.SetRequestHeader(header.Key, header.Value);
                }
            }
        }

        private void LogRequestInfo(string url, string method, string jsonData, Dictionary<string, string> headers)
        {
            Debug.Log($"[HttpApiClient] HTTP 요청 - URL: {url}, Method: {method}, Data: {jsonData}, Headers: {JsonConvert.SerializeObject(headers)}");
        }

        private T ParseResponse<T>(UnityWebRequest request)
        {
            var responseText = request.downloadHandler?.text;

            if (string.IsNullOrEmpty(responseText)) {
                Debug.LogWarning("[HttpApiClient] 응답 텍스트가 비어있습니다.");
                return default(T);
            }

            try {
                return JsonConvert.DeserializeObject<T>(responseText);
            }
            catch (Exception ex) {
                Debug.LogError($"[HttpApiClient] JSON 파싱 실패: {ex.Message}");

                if (IsErrorResponse(responseText)) {
                    Debug.LogWarning("[HttpApiClient] 서버에서 에러 응답을 받았습니다.");
                    throw new ApiException("서버 에러 응답", request.responseCode, responseText);
                }

                return TryFallbackParse<T>(responseText, request.responseCode, ex);
            }
        }

        private bool IsErrorResponse(string responseText)
        {
            try {
                var jsonObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseText);
                return jsonObject.ContainsKey("errorCode") && jsonObject.ContainsKey("message");
            }
            catch {
                return false;
            }
        }

        private T TryFallbackParse<T>(string responseText, long responseCode, Exception originalException)
        {
            try {
                return JsonUtility.FromJson<T>(responseText);
            }
            catch (Exception fallbackEx) {
                throw new ApiException($"응답 파싱 실패: {originalException.Message} (폴백도 실패: {fallbackEx.Message})", responseCode, responseText);
            }
        }

        private Dictionary<string, string> ExtractResponseHeaders(UnityWebRequest request)
        {
            var headers = new Dictionary<string, string>();
            var headerNames = new[] { "X-Access-Token", "X-Refresh-Token", "X-Expires-In", "X-User-Id", "Content-Type", "Authorization" };

            foreach (var headerName in headerNames) {
                var value = request.GetResponseHeader(headerName);
                if (!string.IsNullOrEmpty(value)) {
                    headers[headerName] = value;
                }
            }

            return headers;
        }

        private bool ShouldRetry(long responseCode)
        {
            return responseCode >= 500 || responseCode == 429;
        }
        #endregion
    }

    #region Exceptions

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
    #endregion
}