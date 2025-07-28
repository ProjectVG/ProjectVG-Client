using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using System.Threading;
using ProjectVG.Infrastructure.Network.Configs;

namespace ProjectVG.Infrastructure.Network.Http
{
    /// <summary>
    /// HTTP API 클라이언트
    /// UnityWebRequest를 사용하여 서버와 통신하며, UniTask 기반 비동기 처리를 지원합니다.
    /// </summary>
    public class HttpApiClient : MonoBehaviour
    {
        [Header("API Configuration")]
        [SerializeField] private ApiConfig apiConfig;
        [SerializeField] private float timeout = 30f;
        [SerializeField] private int maxRetryCount = 3;
        [SerializeField] private float retryDelay = 1f;

        [Header("Headers")]
        [SerializeField] private string contentType = "application/json";
        [SerializeField] private string userAgent = "ProjectVG-Client/1.0";

        private readonly Dictionary<string, string> defaultHeaders = new Dictionary<string, string>();
        private CancellationTokenSource cancellationTokenSource;

        public static HttpApiClient Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeClient();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
        }

        private void InitializeClient()
        {
            cancellationTokenSource = new CancellationTokenSource();
            
            if (apiConfig == null)
            {
                Debug.LogWarning("ApiConfig가 설정되지 않았습니다. 기본값을 사용합니다.");
                SetupDefaultHeaders();
                return;
            }
            
            timeout = apiConfig.Timeout;
            maxRetryCount = apiConfig.MaxRetryCount;
            retryDelay = apiConfig.RetryDelay;
            contentType = apiConfig.ContentType;
            userAgent = apiConfig.UserAgent;
            
            SetupDefaultHeaders();
        }

        private void SetupDefaultHeaders()
        {
            defaultHeaders.Clear();
            defaultHeaders["Content-Type"] = contentType;
            defaultHeaders["User-Agent"] = userAgent;
            defaultHeaders["Accept"] = "application/json";
        }

        /// <summary>
        /// ApiConfig 설정 (런타임에서 동적으로 변경 가능)
        /// </summary>
        public void SetApiConfig(ApiConfig config)
        {
            apiConfig = config;
            InitializeClient();
        }

        public void AddDefaultHeader(string key, string value)
        {
            defaultHeaders[key] = value;
        }

        public void SetAuthToken(string token)
        {
            AddDefaultHeader("Authorization", $"Bearer {token}");
        }

        private string GetFullUrl(string endpoint)
        {
            if (apiConfig != null)
            {
                return apiConfig.GetFullUrl(endpoint);
            }
            
            return $"http://122.153.130.223:7900/api/v1/{endpoint.TrimStart('/')}";
        }

        public async UniTask<T> GetAsync<T>(string endpoint, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
        {
            var url = GetFullUrl(endpoint);
            return await SendRequestAsync<T>(url, UnityWebRequest.kHttpVerbGET, null, headers, cancellationToken);
        }

        public async UniTask<T> PostAsync<T>(string endpoint, object data = null, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
        {
            var url = GetFullUrl(endpoint);
            var jsonData = data != null ? JsonUtility.ToJson(data) : null;
            return await SendRequestAsync<T>(url, UnityWebRequest.kHttpVerbPOST, jsonData, headers, cancellationToken);
        }

        public async UniTask<T> PutAsync<T>(string endpoint, object data = null, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
        {
            var url = GetFullUrl(endpoint);
            var jsonData = data != null ? JsonUtility.ToJson(data) : null;
            return await SendRequestAsync<T>(url, UnityWebRequest.kHttpVerbPUT, jsonData, headers, cancellationToken);
        }

        public async UniTask<T> DeleteAsync<T>(string endpoint, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
        {
            var url = GetFullUrl(endpoint);
            return await SendRequestAsync<T>(url, UnityWebRequest.kHttpVerbDELETE, null, headers, cancellationToken);
        }

        public async UniTask<T> UploadFileAsync<T>(string endpoint, byte[] fileData, string fileName, string fieldName = "file", Dictionary<string, string> headers = null, CancellationToken cancellationToken = default)
        {
            var url = GetFullUrl(endpoint);
            return await SendFileRequestAsync<T>(url, fileData, fileName, fieldName, headers, cancellationToken);
        }

        private async UniTask<T> SendRequestAsync<T>(string url, string method, string jsonData, Dictionary<string, string> headers, CancellationToken cancellationToken)
        {
            var combinedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancellationTokenSource.Token).Token;

            for (int attempt = 0; attempt <= maxRetryCount; attempt++)
            {
                try
                {
                    using var request = CreateRequest(url, method, jsonData, headers);
                    
                    var operation = request.SendWebRequest();
                    await operation.WithCancellation(combinedCancellationToken);

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        return ParseResponse<T>(request);
                    }
                    else
                    {
                        var error = new ApiException(request.error, request.responseCode, request.downloadHandler?.text);
                        
                        if (ShouldRetry(request.responseCode) && attempt < maxRetryCount)
                        {
                            Debug.LogWarning($"API 요청 실패 (시도 {attempt + 1}/{maxRetryCount + 1}): {error.Message}");
                            await UniTask.Delay(TimeSpan.FromSeconds(retryDelay * (attempt + 1)), cancellationToken: combinedCancellationToken);
                            continue;
                        }
                        
                        throw error;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex) when (ex is not ApiException)
                {
                    if (attempt < maxRetryCount)
                    {
                        Debug.LogWarning($"API 요청 예외 발생 (시도 {attempt + 1}/{maxRetryCount + 1}): {ex.Message}");
                        await UniTask.Delay(TimeSpan.FromSeconds(retryDelay * (attempt + 1)), cancellationToken: combinedCancellationToken);
                        continue;
                    }
                    throw new ApiException($"{maxRetryCount + 1}번 시도 후 요청 실패", 0, ex.Message);
                }
            }

            throw new ApiException($"{maxRetryCount + 1}번 시도 후 요청 실패", 0, "최대 재시도 횟수 초과");
        }

        private async UniTask<T> SendFileRequestAsync<T>(string url, byte[] fileData, string fileName, string fieldName, Dictionary<string, string> headers, CancellationToken cancellationToken)
        {
            var combinedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancellationTokenSource.Token).Token;

            for (int attempt = 0; attempt <= maxRetryCount; attempt++)
            {
                try
                {
                    var form = new WWWForm();
                    form.AddBinaryData(fieldName, fileData, fileName);
                    
                    using var request = UnityWebRequest.Post(url, form);
                    SetupRequest(request, headers);
                    request.timeout = (int)timeout;

                    var operation = request.SendWebRequest();
                    await operation.WithCancellation(combinedCancellationToken);

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        return ParseResponse<T>(request);
                    }
                    else
                    {
                        var error = new ApiException(request.error, request.responseCode, request.downloadHandler?.text);
                        
                        if (ShouldRetry(request.responseCode) && attempt < maxRetryCount)
                        {
                            Debug.LogWarning($"파일 업로드 실패 (시도 {attempt + 1}/{maxRetryCount + 1}): {error.Message}");
                            await UniTask.Delay(TimeSpan.FromSeconds(retryDelay * (attempt + 1)), cancellationToken: combinedCancellationToken);
                            continue;
                        }
                        
                        throw error;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex) when (ex is not ApiException)
                {
                    if (attempt < maxRetryCount)
                    {
                        Debug.LogWarning($"파일 업로드 예외 발생 (시도 {attempt + 1}/{maxRetryCount + 1}): {ex.Message}");
                        await UniTask.Delay(TimeSpan.FromSeconds(retryDelay * (attempt + 1)), cancellationToken: combinedCancellationToken);
                        continue;
                    }
                    throw new ApiException($"{maxRetryCount + 1}번 시도 후 파일 업로드 실패", 0, ex.Message);
                }
            }

            throw new ApiException($"{maxRetryCount + 1}번 시도 후 파일 업로드 실패", 0, "최대 재시도 횟수 초과");
        }

        private UnityWebRequest CreateRequest(string url, string method, string jsonData, Dictionary<string, string> headers)
        {
            var request = new UnityWebRequest(url, method);
            
            if (!string.IsNullOrEmpty(jsonData))
            {
                var bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            }
            
            request.downloadHandler = new DownloadHandlerBuffer();
            SetupRequest(request, headers);
            request.timeout = (int)timeout;
            
            return request;
        }

        private void SetupRequest(UnityWebRequest request, Dictionary<string, string> headers)
        {
            foreach (var header in defaultHeaders)
            {
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
            {
                return default(T);
            }

            try
            {
                // Newtonsoft.Json 사용 (snake_case 지원)
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(responseText);
            }
            catch (Exception ex)
            {
                // Newtonsoft.Json 실패 시 Unity JsonUtility로 폴백
                try
                {
                    return JsonUtility.FromJson<T>(responseText);
                }
                catch (Exception fallbackEx)
                {
                    throw new ApiException($"응답 파싱 실패: {ex.Message} (폴백도 실패: {fallbackEx.Message})", request.responseCode, responseText);
                }
            }
        }

        private bool ShouldRetry(long responseCode)
        {
            return responseCode >= 500 || responseCode == 429;
        }

        public void Shutdown()
        {
            cancellationTokenSource?.Cancel();
        }
    }

    /// <summary>
    /// API 예외 클래스
    /// </summary>
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
} 