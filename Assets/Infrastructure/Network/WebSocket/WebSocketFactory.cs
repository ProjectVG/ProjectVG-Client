using UnityEngine;
using ProjectVG.Infrastructure.Network.WebSocket.Platforms;

namespace ProjectVG.Infrastructure.Network.WebSocket
{
    /**
     * Unity 6 플랫폼별 WebSocket 구현체 팩토리
     * 
     * 현재 플랫폼에 맞는 최적의 WebSocket 구현체를 생성합니다.
     */
    public static class WebSocketFactory
    {
        /**
         * 현재 플랫폼에 맞는 WebSocket 구현체 생성
         */
        public static INativeWebSocket CreateWebSocket()
        {
            if (Application.isEditor)
            {
                Debug.Log("[WebSocketFactory] 에디터 환경: DesktopWebSocket 사용");
                return new DesktopWebSocket();
            }
            
            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                case RuntimePlatform.IPhonePlayer:
                    Debug.Log($"[WebSocketFactory] 모바일 플랫폼 ({Application.platform}): MobileWebSocket 사용");
                    return new MobileWebSocket();
                    
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.LinuxPlayer:
                    Debug.Log($"[WebSocketFactory] 데스크톱 플랫폼 ({Application.platform}): DesktopWebSocket 사용");
                    return new DesktopWebSocket();
                    
                case RuntimePlatform.WebGLPlayer:
                    Debug.Log($"[WebSocketFactory] WebGL 플랫폼: WebGLWebSocket 사용");
                    return new WebGLWebSocket();
                    
                default:
                    Debug.LogWarning($"[WebSocketFactory] 지원되지 않는 플랫폼 ({Application.platform}): DesktopWebSocket 사용");
                    return new DesktopWebSocket();
            }
        }
        
        /**
         * 특정 구현체 강제 생성 (테스트용)
         */
        public static INativeWebSocket CreateWebSocket(WebSocketType type)
        {
            switch (type)
            {
                case WebSocketType.Desktop:
                    return new DesktopWebSocket();
                case WebSocketType.Mobile:
                    return new MobileWebSocket();
                case WebSocketType.WebGL:
                    return new WebGLWebSocket();
                default:
                    return CreateWebSocket();
            }
        }
        
        /**
         * WebSocket 구현체 타입
         */
        public enum WebSocketType
        {
            Auto,
            Desktop,
            Mobile,
            WebGL
        }
    }
} 