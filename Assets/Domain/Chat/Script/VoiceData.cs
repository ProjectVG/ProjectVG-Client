using System;
using UnityEngine;

namespace ProjectVG.Domain.Chat
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
                
            try
            {
                byte[] audioBytes = Convert.FromBase64String(base64Data);
                AudioClip audioClip = ConvertBytesToAudioClip(audioBytes, format);
                
                if (audioClip != null)
                {
                    return new VoiceData(audioClip, audioClip.length, format);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Base64에서 AudioClip 변환 실패: {ex.Message}");
            }
            
            return null;
        }
        
        private static AudioClip ConvertBytesToAudioClip(byte[] audioBytes, string format)
        {
            if (audioBytes == null || audioBytes.Length == 0)
                return null;
                
            try
            {
                if (format.ToLower() == "wav")
                {
                    return ConvertWavBytesToAudioClip(audioBytes);
                }
                
                Debug.LogWarning($"지원하지 않는 오디오 형식: {format}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"AudioClip 변환 실패: {ex.Message}");
                return null;
            }
        }
        
        private static AudioClip ConvertWavBytesToAudioClip(byte[] wavBytes)
        {
            try
            {
                int sampleRate = 44100;
                int channels = 1;
                
                int headerSize = 44;
                if (wavBytes.Length <= headerSize)
                    return null;
                    
                byte[] audioData = new byte[wavBytes.Length - headerSize];
                Array.Copy(wavBytes, headerSize, audioData, 0, audioData.Length);
                
                float[] samples = new float[audioData.Length / 2];
                for (int i = 0; i < samples.Length; i++)
                {
                    short sample = (short)((audioData[i * 2] & 0xFF) | (audioData[i * 2 + 1] << 8));
                    samples[i] = sample / 32768f;
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