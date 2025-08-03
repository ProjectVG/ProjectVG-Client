#nullable enable
using System;
using System.Threading.Tasks;

namespace ProjectVG.Infrastructure.Network.Services
{
    /// <summary>
    /// Speech-to-Text 서비스 인터페이스
    /// 음성 데이터를 텍스트로 변환하는 기능을 제공합니다.
    /// </summary>
    public interface ISTTService
    {
        /// <summary>
        /// 음성 데이터를 텍스트로 변환
        /// </summary>
        /// <param name="audioData">음성 데이터</param>
        /// <param name="audioFormat">음성 포맷 (wav, mp3 등)</param>
        /// <param name="language">언어 코드 (ko-KR, en-US 등)</param>
        /// <returns>변환된 텍스트</returns>
        Task<string> ConvertSpeechToTextAsync(byte[] audioData, string audioFormat = "wav", string language = "ko-KR");
        
        /// <summary>
        /// 서비스 초기화
        /// </summary>
        /// <returns>초기화 성공 여부</returns>
        Task<bool> InitializeAsync();
        
        /// <summary>
        /// 서비스 연결 상태 확인
        /// </summary>
        /// <returns>연결 상태</returns>
        bool IsConnected { get; }
        
        /// <summary>
        /// 서비스 사용 가능 여부
        /// </summary>
        /// <returns>사용 가능 여부</returns>
        bool IsAvailable { get; }
    }
} 