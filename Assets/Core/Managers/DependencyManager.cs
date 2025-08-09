using UnityEngine;
using ProjectVG.Core.DI;
using ProjectVG.Infrastructure.Network.Services;
using ProjectVG.Infrastructure.Network.WebSocket;
using ProjectVG.Infrastructure.Network.Http;

namespace ProjectVG.Core.Managers
{
    /// <summary>
    /// 의존성 주입을 전담하는 매니저
    /// </summary>
    public class DependencyManager : MonoBehaviour
    {
        private DIContainer _container;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            _container = DIContainer.Instance;
        }
        
        #endregion
        
        #region Public Methods
        
        public void SetupDependencies(ManagerRegistry managerRegistry)
        {
            if (managerRegistry == null)
            {
                Debug.LogError("[DependencyManager] ManagerRegistry가 null입니다.");
                return;
            }
            
            Debug.Log("[DependencyManager] 의존성 주입 시작");
            
            RegisterServices(managerRegistry);
            InjectDependencies(managerRegistry);
            
            Debug.Log("[DependencyManager] 의존성 주입 완료");
        }
        
        public void RegisterService<T>(T service)
        {
            _container.Register<T>(service);
            Debug.Log($"[DependencyManager] 서비스 등록: {typeof(T).Name}");
        }
        
        public T GetService<T>()
        {
            return _container.Get<T>();
        }
        
        #endregion
        
        #region Private Methods
        
        private void RegisterServices(ManagerRegistry managerRegistry)
        {
            if (managerRegistry.SessionManager != null)
            {
                _container.Register<SessionManager>(managerRegistry.SessionManager);
                Debug.Log("[DependencyManager] SessionManager 등록 완료");
            }
            
            if (managerRegistry.WebSocketManager != null)
            {
                _container.Register<WebSocketManager>(managerRegistry.WebSocketManager);
                Debug.Log("[DependencyManager] WebSocketManager 등록 완료");
            }
            
            if (managerRegistry.HttpApiClient != null)
            {
                _container.Register<HttpApiClient>(managerRegistry.HttpApiClient);
                Debug.Log("[DependencyManager] HttpApiClient 등록 완료");
            }
        }
        
        private void InjectDependencies(ManagerRegistry managerRegistry)
        {
            if (managerRegistry.HttpApiClient != null)
            {
                _container.InjectDependencies(managerRegistry.HttpApiClient);
                Debug.Log("[DependencyManager] HttpApiClient 의존성 주입 완료");
            }
            
            if (managerRegistry.WebSocketManager != null)
            {
                _container.InjectDependencies(managerRegistry.WebSocketManager);
                Debug.Log("[DependencyManager] WebSocketManager 의존성 주입 완료");
            }
        }
        
        #endregion
    }
}
