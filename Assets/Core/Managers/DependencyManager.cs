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
            
            private void Awake()
            {
                _container = DIContainer.Instance;
            }
            
            public void SetupDependencies(ManagerRegistry managerRegistry)
            {
                if (managerRegistry == null)
                {
                    Debug.LogError("[DependencyManager] ManagerRegistry가 null입니다.");
                    return;
                }
                
                RegisterServices(managerRegistry);
                InjectDependencies(managerRegistry);
                Debug.Log("[DependencyManager] DI 완료");
            }
            
            public void RegisterService<T>(T service)
            {
                _container.Register<T>(service);
            }
            
            public T GetService<T>()
            {
                return _container.Get<T>();
            }
            
            private void RegisterServices(ManagerRegistry managerRegistry)
            {
                if (managerRegistry.SessionManager != null)
                {
                    _container.Register<SessionManager>(managerRegistry.SessionManager);
                }
                
                if (managerRegistry.WebSocketManager != null)
                {
                    _container.Register<WebSocketManager>(managerRegistry.WebSocketManager);
                }
                
                if (managerRegistry.HttpApiClient != null)
                {
                    _container.Register<HttpApiClient>(managerRegistry.HttpApiClient);
                }
            }
            
            private void InjectDependencies(ManagerRegistry managerRegistry)
            {
                if (managerRegistry.SessionManager != null)
                {
                    _container.InjectDependencies(managerRegistry.SessionManager);
                }
                
                if (managerRegistry.HttpApiClient != null)
                {
                    _container.InjectDependencies(managerRegistry.HttpApiClient);
                }
            }
        }
    }
