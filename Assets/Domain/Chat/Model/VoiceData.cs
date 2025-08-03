using System;
using UnityEngine;

namespace ProjectVG.Domain.Chat.Model
{
    [Serializable]
    public class VoiceData
    {
        public AudioClip AudioClip { get; set; }
        public float Length { get; set; }
        public string Format { get; set; } = "wav";
        
        public VoiceData(AudioClip audioClip, float length, string format = "wav")
        {
            AudioClip = audioClip;
            Length = length;
            Format = format;
        }
        
        public VoiceData()
        {
        }
        
        public static VoiceData FromBase64(string base64Data, string format = "wav")
        {
            if (string.IsNullOrEmpty(base64Data))
                return null;
                
            try {
                byte[] audioBytes = Convert.FromBase64String(base64Data);
                
                
                AudioClip audioClip = ConvertBytesToAudioClip(audioBytes, format);
                
                if (audioClip != null){
                    return new VoiceData(audioClip, audioClip.length, format);
                }
            }
            catch (Exception ex) {
                Debug.LogError($"Base64에서 AudioClip 변환 실패: {ex.Message}");
            }
            
            return null;
        }
        
        private static AudioClip ConvertBytesToAudioClip(byte[] audioBytes, string format)
        {
            if (audioBytes == null || audioBytes.Length == 0)
                return null;
                
            try {
                string normalizedFormat = NormalizeAudioFormat(format);
                
                if (normalizedFormat == "wav") {
                    return ConvertWavBytesToAudioClip(audioBytes);
                }
                
                Debug.LogWarning($"지원하지 않는 오디오 형식: {format} (정규화됨: {normalizedFormat})");
                return null;
            }
            catch (Exception ex) {
                Debug.LogError($"AudioClip 변환 실패: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 오디오 형식을 정규화합니다.
        /// </summary>
        /// <param name="format">원본 형식</param>
        /// <returns>정규화된 형식</returns>
        private static string NormalizeAudioFormat(string format)
        {
            if (string.IsNullOrEmpty(format))
                return "wav";
                
            string lowerFormat = format.ToLower().Trim();
            
            if (lowerFormat.StartsWith("audio/")){
                string extension = lowerFormat.Substring(6);
                return extension;
            }
            switch (lowerFormat){
                case "wav":
                case ".wav":
                    return "wav";
                case "mp3":
                case ".mp3":
                    return "mp3";
                case "ogg":
                case ".ogg":
                    return "ogg";
                default:
                    return lowerFormat;
            }
        }
        
        private static AudioClip ConvertWavBytesToAudioClip(byte[] wavBytes)
        {
            try
            {   
                byte[] audioData = null;
                int sampleRate = 44100;
                int channels = 1;
                if (wavBytes.Length >= 12 && 
                    wavBytes[0] == 'R' && wavBytes[1] == 'I' && 
                    wavBytes[2] == 'F' && wavBytes[3] == 'F')
                {
                    if (wavBytes[8] == 'W' && wavBytes[9] == 'A' && 
                        wavBytes[10] == 'V' && wavBytes[11] == 'E')
                    {

                        int offset = 12;
                        bool foundDataChunk = false;
                        
                        while (offset < wavBytes.Length - 8)
                        {
                            string chunkId = System.Text.Encoding.ASCII.GetString(wavBytes, offset, 4);
                            int chunkSize = BitConverter.ToInt32(wavBytes, offset + 4);

                            if (chunkId == "fmt "){
                                sampleRate = BitConverter.ToInt32(wavBytes, offset + 12);
                                channels = BitConverter.ToInt16(wavBytes, offset + 10);
                            }
                            else if (chunkId == "data")
                            {
                                audioData = new byte[chunkSize];
                                Array.Copy(wavBytes, offset + 8, audioData, 0, chunkSize);
                                foundDataChunk = true;
                                break;
                            }
                            
                            offset += 8 + chunkSize;
                            
                            if (chunkSize % 2 != 0)
                                offset += 1;
                        }
                        
                        if (!foundDataChunk)
                        {
                            Debug.LogError("WAV 파일에서 data 청크를 찾을 수 없습니다.");
                            return null;
                        }
                    }
                    else
                    {
                        Debug.LogError("WAVE 시그니처를 찾을 수 없습니다.");
                        return null;
                    }
                }
                else
                {
                    audioData = wavBytes;
                }
                
                if (audioData == null)
                {
                    Debug.LogError("오디오 데이터를 추출할 수 없습니다.");
                    return null;
                }
                

                
                float[] samples = new float[audioData.Length / 2];
                
                for (int i = 0; i < samples.Length; i++)
                {
                    short sample = (short)((audioData[i * 2] & 0xFF) | (audioData[i * 2 + 1] << 8));
                    float normalizedSample = sample / 32768f;
                    samples[i] = normalizedSample;
                }
                

                

                
                AudioClip audioClip = AudioClip.Create("Voice", samples.Length, channels, sampleRate, false);
                audioClip.SetData(samples, 0);
                

                
                return audioClip;
            }
            catch (Exception ex)
            {
                Debug.LogError($"WAV 변환 실패: {ex.Message}");
                return null;
            }
        }
        

        
        public bool HasAudioClip() => AudioClip != null;
        
        public bool IsPlayable() => HasAudioClip() && Length > 0;
    }
} 