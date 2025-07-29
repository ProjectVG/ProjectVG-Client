namespace ProjectVG.Infrastructure.Network.Configs
{
    /// <summary>
    /// 서버 설정 정보 (현재 미사용)
    /// 서버의 메시지 형식과 지원 기능을 정의합니다.
    /// WebSocket뿐만 아니라 다른 서버 설정도 포함할 수 있습니다.
    /// </summary>
    [System.Serializable]
    public class ServerConfig
    {
        /// <summary>
        /// 메시지 타입 ("json" 또는 "binary")
        /// </summary>
        public string messageType;
        
        /// <summary>
        /// 서버 버전
        /// </summary>
        public string version;
        
        /// <summary>
        /// 오디오 지원 여부
        /// </summary>
        public bool supportsAudio;
        
        /// <summary>
        /// 바이너리 메시지 지원 여부
        /// </summary>
        public bool supportsBinary;
        
        /// <summary>
        /// 오디오 형식 (예: "wav", "mp3")
        /// </summary>
        public string audioFormat;
        
        /// <summary>
        /// 최대 메시지 크기 (바이트)
        /// </summary>
        public int maxMessageSize;
        
        /// <summary>
        /// JSON 형식인지 확인
        /// </summary>
        public bool IsJsonFormat => messageType?.ToLower() == "json";
        
        /// <summary>
        /// 바이너리 형식인지 확인
        /// </summary>
        public bool IsBinaryFormat => messageType?.ToLower() == "binary";
        
        /// <summary>
        /// 설정 정보를 문자열로 반환
        /// </summary>
        public override string ToString()
        {
            return $"ServerConfig[Type: {messageType}, Version: {version}, Audio: {supportsAudio}, Binary: {supportsBinary}, Format: {audioFormat}, MaxSize: {maxMessageSize} bytes]";
        }
    }
} 