using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectVG.Core.Attributes;

namespace ProjectVG.Core.DI
{
    public class DIContainer : Singleton<DIContainer>
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        
        #region Unity Lifecycle
        
        protected override void Awake()
        {
            base.Awake();
        }
        
        #endregion
        
        #region Public Methods
        
        public void Register<T>(T service)
        {
            _services[typeof(T)] = service;
        }
        
        public void Unregister<T>()
        {
            _services.Remove(typeof(T));
        }
        
        public T Get<T>()
        {
            if (_services.TryGetValue(typeof(T), out var service))
            {
                return (T)service;
            }
            return default(T);
        }
        
        public void InjectDependencies(MonoBehaviour component)
        {
            var type = component.GetType();
            var fields = type.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Debug.Log($"[DIContainer] {component.GetType().Name}에 의존성 주입 시작");
            
            foreach (var field in fields)
            {
                var injectAttribute = field.GetCustomAttributes(typeof(InjectAttribute), true);
                if (injectAttribute.Length > 0)
                {
                    var serviceType = field.FieldType;
                    var service = GetService(serviceType);
                    if (service != null)
                    {
                        field.SetValue(component, service);
                        Debug.Log($"[DIContainer] 의존성 주입 완료: {component.GetType().Name}.{field.Name} <- {serviceType.Name}");
                        
                        // 주입 후 검증
                        var injectedValue = field.GetValue(component);
                        if (injectedValue != null)
                        {
                            Debug.Log($"[DIContainer] 주입 검증 성공: {field.Name}에 {serviceType.Name} 인스턴스가 정상적으로 설정됨");
                        }
                        else
                        {
                            Debug.LogError($"[DIContainer] 주입 검증 실패: {field.Name}이 여전히 null임");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[DIContainer] 의존성 주입 실패: {serviceType.Name} 서비스를 찾을 수 없습니다.");
                        Debug.LogWarning($"[DIContainer] 등록된 서비스 목록: {string.Join(", ", _services.Keys)}");
                    }
                }
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private object GetService(Type serviceType)
        {
            if (_services.TryGetValue(serviceType, out var service))
            {
                return service;
            }
            return null;
        }
        
        #endregion
    }
} 