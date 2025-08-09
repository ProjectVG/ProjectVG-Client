using UnityEngine;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using ProjectVG.Infrastructure.Network.WebSocket;
using ProjectVG.Infrastructure.Network.Services;
using ProjectVG.Infrastructure.Network.Http;

namespace ProjectVG.Core.Managers
{
    /** 
     * 매니저들의 등록, 생성, 생명주기를 관리하는 매니저
     */
    public class ManagerRegistry : MonoBehaviour
    {
        [Header("Manager References")]
        [SerializeField] private WebSocketManager _webSocketManager;
        [SerializeField] private SessionManager _sessionManager;
        [SerializeField] private HttpApiClient _httpApiClient;
        [SerializeField] private AudioManager _audioManager;
        
        [Header("Settings")]
        [SerializeField] private bool _createManagersIfNotExist = true;
        
        private readonly List<IManager> _managers = new List<IManager>();
        
        public WebSocketManager WebSocketManager => _webSocketManager;
        public SessionManager SessionManager => _sessionManager;
        public HttpApiClient HttpApiClient => _httpApiClient;
        public AudioManager AudioManager => _audioManager;
        
        #region Public Methods
        
        public void InitializeAllManagers()
        {
            InitializeWebSocketManager();
            InitializeSessionManager();
            InitializeHttpApiClient();
        }
        
        public async UniTask<bool> TryConnectSessionAsync()
        {
            if (_sessionManager == null)
            {
                Debug.LogError("[ManagerRegistry] SessionManager가 초기화되지 않았습니다.");
                return false;
            }
            
            try
            {
                Debug.Log("[ManagerRegistry] 세션 연결 시도");
                
                if (_webSocketManager != null && !_webSocketManager.IsConnected)
                {
                    bool webSocketConnected = await _webSocketManager.ConnectAsync();
                    if (!webSocketConnected)
                    {
                        Debug.LogError("[ManagerRegistry] WebSocket 연결 실패");
                        return false;
                    }
                }
                
                Debug.Log("[ManagerRegistry] WebSocket 연결 완료");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ManagerRegistry] 세션 연결 오류: {ex.Message}");
                return false;
            }
        }
        
        public bool AreManagersReady()
        {
            return _webSocketManager != null && 
                   _sessionManager != null && 
                   _httpApiClient != null;
        }
        
        public bool IsSessionConnected()
        {
            return _sessionManager != null && _sessionManager.IsSessionConnected;
        }
        
        public void ShutdownAllManagers()
        {
            Debug.Log("[ManagerRegistry] 모든 매니저 종료 시작");
            
            for (int i = _managers.Count - 1; i >= 0; i--)
            {
                try
                {
                    _managers[i]?.Shutdown();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ManagerRegistry] 매니저 종료 오류: {ex.Message}");
                }
            }
            
            _managers.Clear();
            Debug.Log("[ManagerRegistry] 모든 매니저 종료 완료");
        }
        
        public void LogManagerStatus()
        {
            Debug.Log("[ManagerRegistry] === 매니저 상태 ===");
            Debug.Log($"[ManagerRegistry] WebSocketManager: {(_webSocketManager != null ? "준비됨" : "없음")}");
            Debug.Log($"[ManagerRegistry] SessionManager: {(_sessionManager != null ? "준비됨" : "없음")}");
            Debug.Log($"[ManagerRegistry] HttpApiClient: {(_httpApiClient != null ? "준비됨" : "없음")}");
            Debug.Log($"[ManagerRegistry] 전체 준비: {(AreManagersReady() ? "완료" : "미완료")}");
            Debug.Log($"[ManagerRegistry] 세션 연결: {(IsSessionConnected() ? "연결됨" : "미연결")}");
        }
        
        #endregion
        
        #region Private Methods
        
        private void InitializeWebSocketManager()
        {
            if (_webSocketManager == null && _createManagersIfNotExist)
            {
                var webSocketObj = new GameObject("WebSocketManager");
                webSocketObj.transform.SetParent(transform);
                _webSocketManager = webSocketObj.AddComponent<WebSocketManager>();
            }
            
            if (_webSocketManager != null)
            {
                _managers.Add(_webSocketManager);
                Debug.Log("[ManagerRegistry] WebSocketManager 초기화 완료");
            }
            else
            {
                throw new InvalidOperationException("WebSocketManager를 초기화할 수 없습니다.");
            }
        }
        
        private void InitializeSessionManager()
        {
            if (_sessionManager == null && _createManagersIfNotExist)
            {
                var sessionObj = new GameObject("SessionManager");
                sessionObj.transform.SetParent(transform);
                _sessionManager = sessionObj.AddComponent<SessionManager>();
            }
            
            if (_sessionManager != null)
            {
                _managers.Add(_sessionManager);
                Debug.Log("[ManagerRegistry] SessionManager 초기화 완료");
            }
            else
            {
                throw new InvalidOperationException("SessionManager를 초기화할 수 없습니다.");
            }
        }
        
        private void InitializeHttpApiClient()
        {
            if (_httpApiClient == null && _createManagersIfNotExist)
            {
                var httpObj = new GameObject("HttpApiClient");
                httpObj.transform.SetParent(transform);
                _httpApiClient = httpObj.AddComponent<HttpApiClient>();
            }
            
            if (_httpApiClient != null)
            {
                _managers.Add(_httpApiClient);
                Debug.Log("[ManagerRegistry] HttpApiClient 초기화 완료");
            }
            else
            {
                throw new InvalidOperationException("HttpApiClient를 초기화할 수 없습니다.");
            }
        }
        
        #endregion
    }
}
