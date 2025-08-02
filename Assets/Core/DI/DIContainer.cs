using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectVG.Core.Attributes;

namespace ProjectVG.Core.DI
{
    /// <summary>
    /// 간단한 의존성 주입 컨테이너
    /// </summary>
    public class DIContainer : MonoBehaviour
    {
        private static DIContainer _instance;
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        
        public static DIContainer Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("DIContainer");
                    _instance = go.AddComponent<DIContainer>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// 서비스 등록
        /// </summary>
        public void Register<T>(T service)
        {
            _services[typeof(T)] = service;
        }
        
        /// <summary>
        /// 서비스 해제
        /// </summary>
        public void Unregister<T>()
        {
            _services.Remove(typeof(T));
        }
        
        /// <summary>
        /// 서비스 가져오기
        /// </summary>
        public T Get<T>()
        {
            if (_services.TryGetValue(typeof(T), out var service))
            {
                return (T)service;
            }
            return default(T);
        }
        
        /// <summary>
        /// 컴포넌트에 의존성 주입
        /// </summary>
        public void InjectDependencies(MonoBehaviour component)
        {
            var type = component.GetType();
            var fields = type.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
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
                        Debug.Log($"의존성 주입 완료: {component.GetType().Name}.{field.Name} <- {serviceType.Name}");
                    }
                    else
                    {
                        Debug.LogWarning($"의존성 주입 실패: {serviceType.Name} 서비스를 찾을 수 없습니다.");
                    }
                }
            }
        }
        
        private object GetService(Type serviceType)
        {
            if (_services.TryGetValue(serviceType, out var service))
            {
                return service;
            }
            return null;
        }
    }
} 