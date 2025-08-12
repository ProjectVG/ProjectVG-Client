using UnityEngine;
using System;
using System.Collections.Generic;
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
            InitializeHttpApiClient();
            InitializeSessionManager();
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
            Debug.Log($"Managers Ready: {(AreManagersReady() ? "Yes" : "No")}, Session: {(IsSessionConnected() ? "Connected" : "Disconnected")}");
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
            if (_webSocketManager == null)
            {
                throw new InvalidOperationException("WebSocketManager를 초기화할 수 없습니다.");
            }
            _managers.Add(_webSocketManager);
        }
        
        private void InitializeSessionManager()
        {
            if (_sessionManager == null && _createManagersIfNotExist)
            {
                var sessionObj = new GameObject("SessionManager");
                sessionObj.transform.SetParent(transform);
                _sessionManager = sessionObj.AddComponent<SessionManager>();
            }
            if (_sessionManager == null)
            {
                throw new InvalidOperationException("SessionManager를 초기화할 수 없습니다.");
            }
            _managers.Add(_sessionManager);
        }
        
        private void InitializeHttpApiClient()
        {
            if (_httpApiClient == null && _createManagersIfNotExist)
            {
                var httpObj = new GameObject("HttpApiClient");
                httpObj.transform.SetParent(transform);
                _httpApiClient = httpObj.AddComponent<HttpApiClient>();
            }
            if (_httpApiClient == null)
            {
                throw new InvalidOperationException("HttpApiClient를 초기화할 수 없습니다.");
            }
            _managers.Add(_httpApiClient);
        }
        
        #endregion
    }
}
