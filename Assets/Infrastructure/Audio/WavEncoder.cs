using System;
using UnityEngine;

namespace ProjectVG.Infrastructure.Audio
{
    /// <summary>
    /// AudioClip을 WAV 포맷의 바이트 배열로 인코딩합니다.
    /// </summary>
    public static class WavEncoder
    {
        /// <summary>
        /// AudioClip을 16-bit PCM WAV 바이트 배열로 변환합니다.
        /// </summary>
        public static byte[] FromAudioClip(AudioClip audioClip)
        {
            if (audioClip == null)
            {
                return Array.Empty<byte>();
            }

            int channels = audioClip.channels;
            int sampleRate = audioClip.frequency;
            int sampleCount = audioClip.samples * channels;

            float[] samples = new float[sampleCount];
            audioClip.GetData(samples, 0);

            byte[] pcm16 = new byte[sampleCount * 2];
            int pcmIndex = 0;
            for (int i = 0; i < sampleCount; i++)
            {
                float clamped = Mathf.Clamp(samples[i], -1f, 1f);
                short s = (short)Mathf.RoundToInt(clamped * short.MaxValue);
                pcm16[pcmIndex++] = (byte)(s & 0xFF);
                pcm16[pcmIndex++] = (byte)((s >> 8) & 0xFF);
            }

            return WrapPcm16ToWav(pcm16, channels, sampleRate);
        }

        /// <summary>
        /// 16-bit PCM 샘플 데이터를 WAV 컨테이너로 포장합니다.
        /// </summary>
        public static byte[] WrapPcm16ToWav(byte[] pcm16Data, int channels, int sampleRate)
        {
            if (pcm16Data == null || pcm16Data.Length == 0)
            {
                return Array.Empty<byte>();
            }

            int bitsPerSample = 16;
            int subchunk2Size = pcm16Data.Length;
            int byteRate = sampleRate * channels * (bitsPerSample / 8);
            short blockAlign = (short)(channels * (bitsPerSample / 8));
            int chunkSize = 36 + subchunk2Size;

            byte[] wav = new byte[44 + subchunk2Size];
            int i = 0;

            wav[i++] = (byte)'R'; wav[i++] = (byte)'I'; wav[i++] = (byte)'F'; wav[i++] = (byte)'F';
            wav[i++] = (byte)(chunkSize & 0xFF);
            wav[i++] = (byte)((chunkSize >> 8) & 0xFF);
            wav[i++] = (byte)((chunkSize >> 16) & 0xFF);
            wav[i++] = (byte)((chunkSize >> 24) & 0xFF);
            wav[i++] = (byte)'W'; wav[i++] = (byte)'A'; wav[i++] = (byte)'V'; wav[i++] = (byte)'E';
            wav[i++] = (byte)'f'; wav[i++] = (byte)'m'; wav[i++] = (byte)'t'; wav[i++] = (byte)' ';
            wav[i++] = 16; wav[i++] = 0; wav[i++] = 0; wav[i++] = 0;
            wav[i++] = 1; wav[i++] = 0;
            wav[i++] = (byte)(channels & 0xFF);
            wav[i++] = (byte)((channels >> 8) & 0xFF);
            wav[i++] = (byte)(sampleRate & 0xFF);
            wav[i++] = (byte)((sampleRate >> 8) & 0xFF);
            wav[i++] = (byte)((sampleRate >> 16) & 0xFF);
            wav[i++] = (byte)((sampleRate >> 24) & 0xFF);
            wav[i++] = (byte)(byteRate & 0xFF);
            wav[i++] = (byte)((byteRate >> 8) & 0xFF);
            wav[i++] = (byte)((byteRate >> 16) & 0xFF);
            wav[i++] = (byte)((byteRate >> 24) & 0xFF);
            wav[i++] = (byte)(blockAlign & 0xFF);
            wav[i++] = (byte)((blockAlign >> 8) & 0xFF);
            wav[i++] = (byte)(bitsPerSample & 0xFF);
            wav[i++] = (byte)((bitsPerSample >> 8) & 0xFF);
            wav[i++] = (byte)'d'; wav[i++] = (byte)'a'; wav[i++] = (byte)'t'; wav[i++] = (byte)'a';
            wav[i++] = (byte)(subchunk2Size & 0xFF);
            wav[i++] = (byte)((subchunk2Size >> 8) & 0xFF);
            wav[i++] = (byte)((subchunk2Size >> 16) & 0xFF);
            wav[i++] = (byte)((subchunk2Size >> 24) & 0xFF);

            Buffer.BlockCopy(pcm16Data, 0, wav, 44, subchunk2Size);
            return wav;
        }
    }
}


