using System;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace ProjectVG.Infrastructure.Auth.OAuth2
{
    public class WindowsCallbackHandler : IOAuth2CallbackHandler
    {
        private bool _isListening = false;
        private string _expectedState = null;
        private HttpListener _httpListener = null;
        private CancellationTokenSource _cancellationTokenSource = null;
        private int _localPort = 8080;
        
        public bool IsHandlingCallback => _isListening;
        public string PlatformName => "Windows";
        
        public event Action<string, string> OnAuthorizationCodeReceived;
        public event Action<string> OnCallbackError;
        public event Action OnCallbackCancelled;
        
        #region Public Methods
        
        public async UniTask<bool> StartListeningForCallbackAsync(string expectedState, TimeSpan timeout)
        {
            if (_isListening)
            {
                Debug.LogWarning("[WindowsCallbackHandler] 이미 콜백을 수신 중입니다.");
                return false;
            }
            
            try
            {
                _expectedState = expectedState;
                _isListening = true;
                _cancellationTokenSource = new CancellationTokenSource(timeout);
                
                Debug.Log($"[WindowsCallbackHandler] 콜백 수신 시작 - 포트: {_localPort}, 타임아웃: {timeout.TotalSeconds}초");
                
#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && !UNITY_WEBGL
                await StartWindowsCallbackListener();
#else
                // 다른 플랫폼에서는 시뮬레이션
                _ = SimulateCallbackInEditor();
#endif
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WindowsCallbackHandler] 콜백 수신 시작 실패: {ex.Message}");
                StopListening();
                OnCallbackError?.Invoke($"시작 실패: {ex.Message}");
                return false;
            }
        }
        
        public void StopListening()
        {
            if (!_isListening)
            {
                return;
            }
            
            _isListening = false;
            _expectedState = null;
            
            try
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                
                if (_httpListener != null && _httpListener.IsListening)
                {
                    _httpListener.Stop();
                    _httpListener.Close();
                }
                _httpListener = null;
                
                Debug.Log("[WindowsCallbackHandler] 콜백 수신 중지");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WindowsCallbackHandler] 콜백 수신 중지 실패: {ex.Message}");
            }
        }
        
        public bool ValidateCallback(string code, string state, string expectedState)
        {
            if (string.IsNullOrEmpty(code))
            {
                Debug.LogError("[WindowsCallbackHandler] Authorization Code가 비어있습니다.");
                return false;
            }
            
            if (string.IsNullOrEmpty(state) || string.IsNullOrEmpty(expectedState))
            {
                Debug.LogError("[WindowsCallbackHandler] State 파라미터가 비어있습니다.");
                return false;
            }
            
            if (state != expectedState)
            {
                Debug.LogError("[WindowsCallbackHandler] State 파라미터가 일치하지 않습니다.");
                return false;
            }
            
            Debug.Log("[WindowsCallbackHandler] 콜백 검증 성공");
            return true;
        }
        
        public string GetCallbackUrl()
        {
            return $"http://127.0.0.1:{_localPort}/callback";
        }
        
        #endregion
        
        #region Private Methods
        
        private async UniTask StartWindowsCallbackListener()
        {
#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && !UNITY_WEBGL
            try
            {
                // 사용 가능한 포트 찾기
                _localPort = FindAvailablePort(8080, 8090);
                
                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add($"http://127.0.0.1:{_localPort}/");
                _httpListener.Start();
                
                Debug.Log($"[WindowsCallbackHandler] HTTP 리스너 시작: http://127.0.0.1:{_localPort}/");
                
                // 비동기로 요청 처리
                _ = ProcessHttpRequestsAsync(_cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WindowsCallbackHandler] HTTP 리스너 시작 실패: {ex.Message}");
                throw;
            }
#else
            await UniTask.CompletedTask;
#endif
        }
        
        private async UniTask ProcessHttpRequestsAsync(CancellationToken cancellationToken)
        {
#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && !UNITY_WEBGL
            while (_isListening && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var contextTask = _httpListener.GetContextAsync();
                    var context = await contextTask.WithCancellation(cancellationToken);
                    
                    _ = ProcessSingleRequestAsync(context);
                }
                catch (OperationCanceledException)
                {
                    Debug.Log("[WindowsCallbackHandler] HTTP 리스너 취소됨");
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WindowsCallbackHandler] HTTP 요청 처리 실패: {ex.Message}");
                    
                    if (_isListening)
                    {
                        OnCallbackError?.Invoke($"HTTP 요청 처리 실패: {ex.Message}");
                        StopListening();
                    }
                    break;
                }
            }
#else
            await UniTask.CompletedTask;
#endif
        }
        
        private async UniTask ProcessSingleRequestAsync(HttpListenerContext context)
        {
#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && !UNITY_WEBGL
            try
            {
                var request = context.Request;
                var response = context.Response;
                
                Debug.Log($"[WindowsCallbackHandler] HTTP 요청 수신: {request.Url}");
                
                // URL에서 파라미터 파싱
                var query = request.QueryString;
                var code = query["code"];
                var state = query["state"];
                var error = query["error"];
                var errorDescription = query["error_description"];
                
                string responseText;
                
                if (!string.IsNullOrEmpty(error))
                {
                    var errorMsg = $"OAuth2 오류: {error}";
                    if (!string.IsNullOrEmpty(errorDescription))
                    {
                        errorMsg += $" - {errorDescription}";
                    }
                    
                    responseText = CreateErrorHtml(errorMsg);
                    Debug.LogError($"[WindowsCallbackHandler] {errorMsg}");
                    
                    await SendHttpResponseAsync(response, responseText, 400);
                    
                    StopListening();
                    OnCallbackError?.Invoke(errorMsg);
                    return;
                }
                
                if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(state))
                {
                    if (ValidateCallback(code, state, _expectedState))
                    {
                        responseText = CreateSuccessHtml();
                        await SendHttpResponseAsync(response, responseText, 200);
                        
                        Debug.Log("[WindowsCallbackHandler] Authorization Code 수신 성공");
                        StopListening();
                        OnAuthorizationCodeReceived?.Invoke(code, state);
                    }
                    else
                    {
                        responseText = CreateErrorHtml("콜백 검증 실패");
                        await SendHttpResponseAsync(response, responseText, 400);
                        
                        Debug.LogError("[WindowsCallbackHandler] 콜백 검증 실패");
                        StopListening();
                        OnCallbackError?.Invoke("콜백 검증 실패");
                    }
                }
                else
                {
                    responseText = CreateErrorHtml("필수 파라미터 누락");
                    await SendHttpResponseAsync(response, responseText, 400);
                    
                    Debug.LogError("[WindowsCallbackHandler] 필수 파라미터 누락");
                    StopListening();
                    OnCallbackError?.Invoke("필수 파라미터 누락");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WindowsCallbackHandler] HTTP 응답 처리 실패: {ex.Message}");
            }
#else
            await UniTask.CompletedTask;
#endif
        }
        
        private async UniTask SendHttpResponseAsync(HttpListenerResponse response, string content, int statusCode)
        {
#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && !UNITY_WEBGL
            try
            {
                var buffer = Encoding.UTF8.GetBytes(content);
                
                response.StatusCode = statusCode;
                response.ContentType = "text/html; charset=utf-8";
                response.ContentLength64 = buffer.Length;
                
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WindowsCallbackHandler] HTTP 응답 전송 실패: {ex.Message}");
            }
#else
            await UniTask.CompletedTask;
#endif
        }
        
        private string CreateSuccessHtml()
        {
            return @"
<!DOCTYPE html>
<html>
<head>
    <title>인증 성공</title>
    <meta charset='utf-8'>
    <style>
        body { font-family: Arial, sans-serif; text-align: center; margin-top: 50px; }
        .success { color: #28a745; font-size: 24px; margin-bottom: 20px; }
        .message { color: #666; font-size: 16px; }
    </style>
</head>
<body>
    <div class='success'>✓ 인증이 완료되었습니다</div>
    <div class='message'>이 창을 닫고 게임으로 돌아가세요.</div>
    <script>
        setTimeout(() => { window.close(); }, 3000);
    </script>
</body>
</html>";
        }
        
        private string CreateErrorHtml(string errorMessage)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <title>인증 실패</title>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; text-align: center; margin-top: 50px; }}
        .error {{ color: #dc3545; font-size: 24px; margin-bottom: 20px; }}
        .message {{ color: #666; font-size: 16px; }}
    </style>
</head>
<body>
    <div class='error'>✗ 인증에 실패했습니다</div>
    <div class='message'>{errorMessage}</div>
    <div class='message' style='margin-top: 20px;'>이 창을 닫고 다시 시도해주세요.</div>
    <script>
        setTimeout(() => {{ window.close(); }}, 5000);
    </script>
</body>
</html>";
        }
        
        private int FindAvailablePort(int startPort, int endPort)
        {
#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && !UNITY_WEBGL
            for (int port = startPort; port <= endPort; port++)
            {
                try
                {
                    var listener = new HttpListener();
                    listener.Prefixes.Add($"http://127.0.0.1:{port}/");
                    listener.Start();
                    listener.Stop();
                    return port;
                }
                catch
                {
                    // 포트가 사용 중이면 다음 포트 시도
                    continue;
                }
            }
            
            throw new InvalidOperationException($"사용 가능한 포트를 찾을 수 없습니다 ({startPort}-{endPort})");
#else
            return startPort;
#endif
        }
        
        private async UniTask SimulateCallbackInEditor()
        {
            await UniTask.Delay(2000); // 2초 후 시뮬레이션
            
            if (_isListening)
            {
                Debug.Log("[WindowsCallbackHandler] 에디터에서 콜백 시뮬레이션");
                var simulatedCode = "simulated_auth_code_windows_12345";
                var simulatedState = _expectedState;
                
                StopListening();
                OnAuthorizationCodeReceived?.Invoke(simulatedCode, simulatedState);
            }
        }
        
        #endregion
    }
}
