using System;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using ProjectVG.Infrastructure.Network.Configs;

namespace ProjectVG.Infrastructure.Network.Configs
{
    /// <summary>
    /// 서버 설정 로더 (현재 미사용)
    /// 서버에서 메시지 타입 등의 설정을 동적으로 로드합니다.
    /// WebSocket뿐만 아니라 다른 서버 설정도 로드할 수 있습니다.
    /// </summary>
    public static class ServerConfigLoader
    {
        /// <summary>
        /// 서버 설정 로드
        /// </summary>
        /// <param name="configEndpoint">설정 엔드포인트 (기본값: "config")</param>
        /// <returns>서버 설정 또는 null</returns>
        public static async UniTask<ServerConfig> LoadServerConfigAsync(string configEndpoint = "config")
        {
            try
            {
                Debug.Log($"서버 설정 로드 중... (엔드포인트: {configEndpoint})");
                
                var configUrl = NetworkConfig.GetFullApiUrl(configEndpoint);
                using (var request = UnityWebRequest.Get(configUrl))
                {
                    request.timeout = 10;
                    await request.SendWebRequest();
                    
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var jsonResponse = request.downloadHandler.text;
                        var serverConfig = JsonUtility.FromJson<ServerConfig>(jsonResponse);
                        
                        Debug.Log($"서버 설정 로드 완료: {serverConfig.messageType}");
                        return serverConfig;
                    }
                    else
                    {
                        Debug.LogWarning($"서버 설정 로드 실패: {request.error}");
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"서버 설정 로드 중 오류: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 서버 설정 유효성 검사
        /// </summary>
        /// <param name="config">서버 설정</param>
        /// <returns>유효한지 여부</returns>
        public static bool ValidateServerConfig(ServerConfig config)
        {
            if (config == null)
            {
                Debug.LogWarning("서버 설정이 null입니다.");
                return false;
            }
            
            if (string.IsNullOrEmpty(config.messageType))
            {
                Debug.LogWarning("서버 설정에 메시지 타입이 없습니다.");
                return false;
            }
            
            var messageType = config.messageType.ToLower();
            if (messageType != "json" && messageType != "binary")
            {
                Debug.LogWarning($"지원하지 않는 메시지 타입: {config.messageType}");
                return false;
            }
            
            Debug.Log($"서버 설정 유효성 검사 통과: {config.messageType}");
            return true;
        }
        
        /// <summary>
        /// 서버 설정과 NetworkConfig 비교
        /// </summary>
        /// <param name="serverConfig">서버 설정</param>
        /// <returns>일치하는지 여부</returns>
        public static bool CompareWithNetworkConfig(ServerConfig serverConfig)
        {
            if (serverConfig == null) return false;
            
            var networkConfigType = NetworkConfig.WebSocketMessageType.ToLower();
            var serverConfigType = serverConfig.messageType.ToLower();
            
            var isMatch = networkConfigType == serverConfigType;
            
            if (!isMatch)
            {
                Debug.LogWarning($"NetworkConfig({networkConfigType})와 서버 설정({serverConfigType})이 일치하지 않습니다.");
            }
            else
            {
                Debug.Log($"NetworkConfig와 서버 설정이 일치합니다: {networkConfigType}");
            }
            
            return isMatch;
        }
    }
} 