#nullable enable
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using ProjectVG.Infrastructure.Network.Http;
using ProjectVG.Infrastructure.Network.DTOs.Chat;
using Newtonsoft.Json;

namespace ProjectVG.Infrastructure.Network.Services
{
    /// <summary>
    /// Speech-to-Text 서비스 구현체
    /// HTTP API를 통해 음성을 텍스트로 변환합니다.
    /// </summary>
    public class STTService : ISTTService
    {
        private readonly string _baseUrl;
        private bool _isInitialized = false;
        private bool _isConnected = false;
        
        public bool IsConnected => _isConnected;
        public bool IsAvailable => _isInitialized && _isConnected;
        
        public STTService(string baseUrl = "http://localhost:7920")
        {
            _baseUrl = baseUrl;
        }
        
        /// <summary>
        /// STT 서비스 초기화
        /// </summary>
        /// <returns>초기화 성공 여부</returns>
        public async Task<bool> InitializeAsync()
        {
            try
            {
                // 서버 상태 확인 (전체 URL 사용)
                var healthResponse = await HttpApiClient.Instance.GetAsync<STTHealthResponse>($"{_baseUrl}/api/v1/health");
                if (healthResponse != null)
                {
                    _isConnected = healthResponse.Status == "healthy" && healthResponse.ModelLoaded == true;
                    _isInitialized = true;
                    
                    Debug.Log($"STT 서비스 초기화 완료: {_isConnected} (모델 로딩: {healthResponse.ModelLoaded})");
                    return _isConnected;
                }
                else
                {
                    Debug.LogError("STT 서버 상태 확인 실패");
                    _isConnected = false;
                    _isInitialized = false;
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"STT 서비스 초기화 실패: {ex.Message}");
                _isConnected = false;
                _isInitialized = false;
                return false;
            }
        }
        
        /// <summary>
        /// 음성 데이터를 텍스트로 변환
        /// </summary>
        /// <param name="audioData">음성 데이터</param>
        /// <param name="audioFormat">음성 포맷</param>
        /// <param name="language">언어 코드</param>
        /// <returns>변환된 텍스트</returns>
        public async Task<string> ConvertSpeechToTextAsync(byte[] audioData, string audioFormat = "wav", string language = "ko")
        {
            if (!IsAvailable)
            {
                throw new InvalidOperationException("STT 서비스가 사용 불가능합니다.");
            }
            
            if (audioData == null || audioData.Length == 0)
            {
                throw new ArgumentException("음성 데이터가 비어있습니다.");
            }
            
            try
            {
                // multipart/form-data로 파일 업로드
                var formData = new Dictionary<string, object>
                {
                    { "file", audioData }
                };
                
                // 쿼리 파라미터 추가
                string url = $"{_baseUrl}/api/v1/transcribe";
                if (!string.IsNullOrEmpty(language))
                {
                    url += $"?language={language}";
                }
                
                // HTTP POST 요청 (전체 URL 사용)
                var response = await HttpApiClient.Instance.PostFormDataAsync<STTResponse>(url, formData);
                
                if (response != null && !string.IsNullOrEmpty(response.Text))
                {
                    Debug.Log($"STT 변환 성공: {response.Text} (언어: {response.Language}, 확률: {response.LanguageProbability}, 처리시간: {response.ProcessingTime}초)");
                    return response.Text;
                }
                else
                {
                    Debug.LogError("STT 변환 실패: 응답이 비어있습니다.");
                    throw new Exception("음성 변환 실패: 응답이 비어있습니다.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"STT 변환 중 오류 발생: {ex.Message}");
                throw;
            }
        }
    }
    
    /// <summary>
    /// STT 요청 데이터 구조
    /// </summary>
    [Serializable]
    public class STTRequest
    {
        [JsonProperty("file")]
        public byte[] AudioData { get; set; } = new byte[0];
        
        [JsonProperty("filename")]
        public string Filename { get; set; } = "recording.wav";
        
        [JsonProperty("content_type")]
        public string ContentType { get; set; } = "audio/wav";
        
        [JsonProperty("language")]
        public string Language { get; set; } = "ko";
    }
    
    /// <summary>
    /// STT 응답 데이터 구조
    /// </summary>
    [Serializable]
    public class STTResponse
    {
        [JsonProperty("text")]
        public string? Text { get; set; }
        
        [JsonProperty("language")]
        public string? Language { get; set; }
        
        [JsonProperty("language_probability")]
        public float? LanguageProbability { get; set; }
        
        [JsonProperty("segments_count")]
        public int? SegmentsCount { get; set; }
        
        [JsonProperty("processing_time")]
        public float? ProcessingTime { get; set; }
        
        [JsonProperty("file_info")]
        public STTFileInfo? FileInfo { get; set; }
    }
    
    /// <summary>
    /// STT 파일 정보 구조
    /// </summary>
    [Serializable]
    public class STTFileInfo
    {
        [JsonProperty("filename")]
        public string? Filename { get; set; }
        
        [JsonProperty("content_type")]
        public string? ContentType { get; set; }
        
        [JsonProperty("size")]
        public long? Size { get; set; }
    }
    
    /// <summary>
    /// STT 서버 상태 응답 구조
    /// </summary>
    [Serializable]
    public class STTHealthResponse
    {
        [JsonProperty("status")]
        public string? Status { get; set; }
        
        [JsonProperty("model_loaded")]
        public bool? ModelLoaded { get; set; }
        
        [JsonProperty("service")]
        public string? Service { get; set; }
        
        [JsonProperty("timestamp")]
        public string? Timestamp { get; set; }
        
        [JsonProperty("uptime")]
        public float? Uptime { get; set; }
    }
} 