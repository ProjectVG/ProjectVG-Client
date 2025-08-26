#nullable enable
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using ProjectVG.Infrastructure.Network.Http;
using ProjectVG.Infrastructure.Network.DTOs.Chat;
using Newtonsoft.Json;
using Cysharp.Threading.Tasks;

namespace ProjectVG.Infrastructure.Network.Services
{
    /// <summary>
    /// Speech-to-Text 서비스 구현체
    /// HTTP API를 통해 음성을 텍스트로 변환합니다.
    /// </summary>
    public class STTService
    {
        private readonly HttpApiClient? _httpClient;
        
        public bool IsConnected => true;
        public bool IsAvailable => _httpClient != null;
        
        public STTService()
        {
            _httpClient = HttpApiClient.Instance;
            if (_httpClient == null)
            {
                Debug.LogError("[STTService] HttpApiClient.Instance가 null입니다. HttpApiClient가 생성되지 않았습니다.");
            }
        }
        
        /// <summary>
        /// 음성 데이터를 텍스트로 변환
        /// </summary>
        /// <param name="audioData">음성 데이터</param>
        /// <param name="audioFormat">음성 포맷</param>
        /// <param name="language">언어 코드</param>
        /// <returns>변환된 텍스트</returns>
        public async UniTask<string> ConvertSpeechToTextAsync(byte[] audioData, string audioFormat = "wav", string language = "ko", CancellationToken cancellationToken = default)
        {
            if (_httpClient == null)
            {
                Debug.LogError("[STTService] HttpApiClient가 null입니다. 초기화를 확인해주세요.");
                throw new InvalidOperationException("HttpApiClient가 초기화되지 않았습니다.");
            }

            if (audioData == null || audioData.Length == 0)
            {
                throw new ArgumentException("음성 데이터가 비어있습니다.");
            }
            
            try
            {
                var formData = new Dictionary<string, object>
                {
                    { "file", audioData }
                };

                var fileNames = new Dictionary<string, string>
                {
                    { "file", "recording.wav" }
                };
                
                // 서버 API에 맞게 language 파라미터만 사용
                string forcedLanguage = "ko";
                string endpoint = $"/api/v1/stt/transcribe?language={forcedLanguage}";
                
                var response = await _httpClient.PostFormDataAsync<STTResponse>(endpoint, formData, fileNames, null, requiresAuth: false, cancellationToken: cancellationToken);
                
                if (response != null && !string.IsNullOrEmpty(response.Text))
                {
                    return response.Text;
                }
                else
                {
                    Debug.LogError($"[STTService] STT 변환 실패: 응답이 비어있습니다. Text: '{response?.Text}'");
                    throw new Exception("음성 변환 실패: 응답이 비어있습니다.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[STTService] STT 변환 중 오류 발생: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 테스트용 더미 음성 데이터 생성 (1초, 22050Hz, 사인파)
        /// </summary>
        public byte[] GenerateTestAudioData()
        {
            int sampleRate = 22050;
            int duration = 1; // 1초
            int samples = sampleRate * duration;
            
            // 440Hz 사인파 생성
            float frequency = 440f;
            float[] audioData = new float[samples];
            
            for (int i = 0; i < samples; i++)
            {
                audioData[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * 0.5f;
            }
            
            // WAV로 변환
            byte[] pcm16 = new byte[samples * 2];
            int pcmIndex = 0;
            for (int i = 0; i < samples; i++)
            {
                float clamped = Mathf.Clamp(audioData[i], -1f, 1f);
                short s = (short)Mathf.RoundToInt(clamped * short.MaxValue);
                pcm16[pcmIndex++] = (byte)(s & 0xFF);
                pcm16[pcmIndex++] = (byte)((s >> 8) & 0xFF);
            }
            
            // WAV 헤더 추가
            return ProjectVG.Infrastructure.Audio.WavEncoder.WrapPcm16ToWav(pcm16, 1, sampleRate);
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