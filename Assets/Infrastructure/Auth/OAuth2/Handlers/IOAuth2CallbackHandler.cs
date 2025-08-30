using System;
using System.Threading.Tasks;

namespace ProjectVG.Infrastructure.Auth.OAuth2.Handlers
{
    /// <summary>
    /// OAuth2 콜백 핸들러 인터페이스
    /// 플랫폼별 OAuth2 콜백 처리 방법을 정의
    /// </summary>
    public interface IOAuth2CallbackHandler
    {
        /// <summary>
        /// 콜백 핸들러 초기화
        /// </summary>
        /// <param name="expectedState">예상되는 State 값</param>
        /// <param name="timeoutSeconds">타임아웃 (초)</param>
        Task InitializeAsync(string expectedState, float timeoutSeconds);
        
        /// <summary>
        /// 콜백 대기
        /// </summary>
        /// <returns>콜백 URL (없으면 null)</returns>
        Task<string> WaitForCallbackAsync();
        
        /// <summary>
        /// 콜백 핸들러 정리
        /// </summary>
        void Cleanup();
        
        /// <summary>
        /// 현재 플랫폼 이름
        /// </summary>
        string PlatformName { get; }
        
        /// <summary>
        /// 지원 여부
        /// </summary>
        bool IsSupported { get; }
    }
}
