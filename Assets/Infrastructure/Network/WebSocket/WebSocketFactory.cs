using UnityEngine;
using ProjectVG.Infrastructure.Network.WebSocket.Platforms;

namespace ProjectVG.Infrastructure.Network.WebSocket
{
    /// <summary>
    /// 플랫폼별 WebSocket 구현을 생성하는 팩토리
    /// </summary>
    public static class WebSocketFactory
    {
        /// <summary>
        /// 현재 플랫폼에 맞는 WebSocket 구현을 생성합니다.
        /// </summary>
        public static INativeWebSocket Create()
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
                return new WebGLWebSocket();
            #elif UNITY_IOS || UNITY_ANDROID
                return new MobileWebSocket();
            #else
                return new DesktopWebSocket();
            #endif
        }
    }
} 