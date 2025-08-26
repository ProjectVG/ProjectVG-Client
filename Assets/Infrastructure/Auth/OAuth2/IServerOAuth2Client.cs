using System;
using System.Threading.Tasks;
using ProjectVG.Infrastructure.Auth.Models;
using ProjectVG.Infrastructure.Auth.OAuth2.Models;

namespace ProjectVG.Infrastructure.Auth.OAuth2
{
    /// <summary>
    /// 서버 OAuth2 클라이언트 인터페이스
    /// </summary>
    public interface IServerOAuth2Client
    {
        /// <summary>
        /// OAuth2 설정이 유효한지 확인
        /// </summary>
        bool IsConfigured { get; }
        
        /// <summary>
        /// PKCE 파라미터 생성 (Code Verifier, Code Challenge, State)
        /// </summary>
        Task<PKCEParameters> GeneratePKCEAsync();
        
        /// <summary>
        /// 서버 OAuth2 인증 시작
        /// </summary>
        Task<string> StartServerOAuth2Async(PKCEParameters pkce);
        
        /// <summary>
        /// OAuth2 콜백 처리
        /// </summary>
        Task<(bool success, string state)> HandleOAuth2CallbackAsync(string callbackUrl);
        
        /// <summary>
        /// 서버에서 토큰 요청
        /// </summary>
        Task<TokenSet> RequestTokenAsync(string state);
        
        /// <summary>
        /// 전체 OAuth2 로그인 플로우 (편의 메서드)
        /// </summary>
        Task<TokenSet> LoginWithServerOAuth2Async();
    }
}
