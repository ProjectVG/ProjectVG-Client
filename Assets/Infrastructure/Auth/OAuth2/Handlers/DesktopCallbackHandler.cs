using System;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace ProjectVG.Infrastructure.Auth.OAuth2.Handlers
{
    /// <summary>
    /// 데스크톱 OAuth2 콜백 핸들러
    /// 로컬 HTTP 서버를 통해 OAuth2 콜백을 처리
    /// </summary>
    public class DesktopCallbackHandler : IOAuth2CallbackHandler
    {
        private string _expectedState;
        private float _timeoutSeconds;
        private bool _isInitialized = false;
        private bool _isDisposed = false;
        private HttpListener _listener;
        private CancellationTokenSource _cancellationTokenSource;
        private string _callbackUrl;
        
        public string PlatformName => "Desktop";
        public bool IsSupported => true;
        
        public async Task InitializeAsync(string expectedState, float timeoutSeconds)
        {
            _expectedState = expectedState;
            _timeoutSeconds = timeoutSeconds;
            _isInitialized = true;
            _cancellationTokenSource = new CancellationTokenSource();
            
            // 로컬 HTTP 서버 시작
            await StartLocalServerAsync();
            
            Debug.Log($"[DesktopCallbackHandler] 초기화 완료 - State: {expectedState}, Timeout: {timeoutSeconds}초");
        }
        
        public async Task<string> WaitForCallbackAsync()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("데스크톱 콜백 핸들러가 초기화되지 않았습니다.");
            }
            
            Debug.Log("[DesktopCallbackHandler] OAuth2 콜백 대기 시작");
            
            var startTime = DateTime.UtcNow;
            var timeout = TimeSpan.FromSeconds(_timeoutSeconds);
            
            while (DateTime.UtcNow - startTime < timeout && !_isDisposed)
            {
                if (!string.IsNullOrEmpty(_callbackUrl))
                {
                    Debug.Log($"[DesktopCallbackHandler] OAuth2 콜백 수신: {_callbackUrl}");
                    return _callbackUrl;
                }
                
                // 100ms 대기
                await UniTask.Delay(100);
            }
            
            Debug.LogWarning("[DesktopCallbackHandler] OAuth2 콜백 타임아웃");
            return null;
        }
        
        public void Cleanup()
        {
            _isDisposed = true;
            _cancellationTokenSource?.Cancel();
            _listener?.Stop();
            _listener?.Close();
            Debug.Log("[DesktopCallbackHandler] 정리 완료");
        }
        
        /// <summary>
        /// 로컬 HTTP 서버 시작
        /// </summary>
        private async Task StartLocalServerAsync()
        {
            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add("http://localhost:3000/");
                _listener.Start();
                
                Debug.Log("[DesktopCallbackHandler] 로컬 서버 시작: http://localhost:3000/");
                
                // 서버 리스닝 시작
                _ = ListenForCallbackAsync();
                
                await UniTask.CompletedTask;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DesktopCallbackHandler] 로컬 서버 시작 실패: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 콜백 리스닝
        /// </summary>
        private async Task ListenForCallbackAsync()
        {
            try
            {
                while (!_isDisposed && _listener.IsListening)
                {
                    var context = await _listener.GetContextAsync();
                    _ = ProcessCallbackAsync(context);
                }
            }
            catch (Exception ex)
            {
                if (!_isDisposed)
                {
                    Debug.LogError($"[DesktopCallbackHandler] 콜백 리스닝 중 오류: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 콜백 처리
        /// </summary>
        private async Task ProcessCallbackAsync(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;
                
                Debug.Log($"[DesktopCallbackHandler] 요청 수신: {request.Url}");
                
                // OAuth2 콜백 URL인지 확인 (auth/callback 또는 auth/google/callback)
                if (request.Url.AbsolutePath.Contains("auth/callback") || request.Url.AbsolutePath.Contains("auth/google/callback"))
                {
                    var state = request.QueryString["state"];
                    var success = request.QueryString["success"];
                    
                    Debug.Log($"[DesktopCallbackHandler] OAuth2 콜백 파라미터 - State: {state}, Success: {success}");
                    
                    if (!string.IsNullOrEmpty(state) && state == _expectedState)
                    {
                        // 성공 응답
                        var successHtml = @"
                            <!DOCTYPE html>
                            <html>
                            <head>
                                <meta charset='UTF-8'>
                                <title>OAuth2 성공</title>
                                <style>
                                    body { font-family: 'Malgun Gothic', Arial, sans-serif; text-align: center; padding: 50px; }
                                    .success { color: green; font-size: 24px; }
                                    .message { margin: 20px 0; }
                                </style>
                            </head>
                            <body>
                                <div class='success'>✅ OAuth2 로그인 성공!</div>
                                <div class='message'>Unity 앱으로 돌아가세요.</div>
                                <script>
                                    setTimeout(function() { window.close(); }, 3000);
                                </script>
                            </body>
                            </html>";
                        
                        var buffer = System.Text.Encoding.UTF8.GetBytes(successHtml);
                        response.ContentType = "text/html";
                        response.ContentLength64 = buffer.Length;
                        response.OutputStream.Write(buffer, 0, buffer.Length);
                        response.Close();
                        
                        // 콜백 URL 저장
                        _callbackUrl = request.Url.ToString();
                    }
                    else
                    {
                        // 실패 응답
                        var errorHtml = @"
                            <!DOCTYPE html>
                            <html>
                            <head>
                                <meta charset='UTF-8'>
                                <title>OAuth2 실패</title>
                                <style>
                                    body { font-family: 'Malgun Gothic', Arial, sans-serif; text-align: center; padding: 50px; }
                                    .error { color: red; font-size: 24px; }
                                    .message { margin: 20px 0; }
                                </style>
                            </head>
                            <body>
                                <div class='error'>❌ OAuth2 로그인 실패</div>
                                <div class='message'>Unity 앱으로 돌아가세요.</div>
                                <script>
                                    setTimeout(function() { window.close(); }, 3000);
                                </script>
                            </body>
                            </html>";
                        
                        var buffer = System.Text.Encoding.UTF8.GetBytes(errorHtml);
                        response.ContentType = "text/html";
                        response.ContentLength64 = buffer.Length;
                        response.OutputStream.Write(buffer, 0, buffer.Length);
                        response.Close();
                    }
                }
                else
                {
                    // 기본 응답
                    var html = @"
                        <!DOCTYPE html>
                        <html>
                        <head>
                            <meta charset='UTF-8'>
                            <title>OAuth2 콜백 서버</title>
                            <style>
                                body { font-family: 'Malgun Gothic', Arial, sans-serif; text-align: center; padding: 50px; }
                            </style>
                        </head>
                        <body>
                            <h1>OAuth2 콜백 서버</h1>
                            <p>Unity OAuth2 콜백을 처리하는 서버입니다.</p>
                        </body>
                        </html>";
                    
                    var buffer = System.Text.Encoding.UTF8.GetBytes(html);
                    response.ContentType = "text/html";
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                    response.Close();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DesktopCallbackHandler] 콜백 처리 중 오류: {ex.Message}");
            }
        }
    }
}
