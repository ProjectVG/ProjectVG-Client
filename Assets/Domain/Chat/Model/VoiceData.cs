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
                
            try
            {
                // JavaScript 방식과 동일한 Base64 디코딩
                byte[] audioBytes = Convert.FromBase64String(base64Data);
                
                Debug.Log($"Base64 디코딩 완료: {audioBytes.Length} 바이트");
                
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
                // 형식 정규화 (audio/wav -> wav)
                string normalizedFormat = NormalizeAudioFormat(format);
                
                if (normalizedFormat == "wav")
                {
                    return ConvertWavBytesToAudioClip(audioBytes);
                }
                
                Debug.LogWarning($"지원하지 않는 오디오 형식: {format} (정규화됨: {normalizedFormat})");
                return null;
            }
            catch (Exception ex)
            {
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
            
            // MIME 타입 처리
            if (lowerFormat.StartsWith("audio/"))
            {
                string extension = lowerFormat.Substring(6); // "audio/" 제거
                return extension;
            }
            
            // 일반 확장자 처리
            switch (lowerFormat)
            {
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
                Debug.Log($"WAV 변환 시작: {wavBytes.Length} 바이트");
                
                // 원본 데이터의 처음 20바이트 출력 (헤더 확인용)
                string headerHex = BitConverter.ToString(wavBytes, 0, Math.Min(20, wavBytes.Length));
                Debug.Log($"원본 데이터 헤더 (처음 20바이트): {headerHex}");
                
                // WAV 헤더 구조 정확히 파싱
                byte[] audioData = null;
                int sampleRate = 44100;
                int channels = 1;
                
                // WAV 헤더 확인 (RIFF 시그니처)
                if (wavBytes.Length >= 12 && 
                    wavBytes[0] == 'R' && wavBytes[1] == 'I' && 
                    wavBytes[2] == 'F' && wavBytes[3] == 'F')
                {
                    Debug.Log("WAV 헤더 감지됨 - 정확한 헤더 파싱 시작");
                    
                    // WAV 파일 구조 파싱
                    int riffSize = BitConverter.ToInt32(wavBytes, 4);
                    Debug.Log($"RIFF 크기: {riffSize} 바이트");
                    
                    // WAVE 시그니처 확인
                    if (wavBytes[8] == 'W' && wavBytes[9] == 'A' && 
                        wavBytes[10] == 'V' && wavBytes[11] == 'E')
                    {
                        Debug.Log("WAVE 시그니처 확인됨");
                        
                        // 청크들을 순회하며 실제 오디오 데이터 찾기
                        int offset = 12;
                        bool foundDataChunk = false;
                        
                        while (offset < wavBytes.Length - 8)
                        {
                            string chunkId = System.Text.Encoding.ASCII.GetString(wavBytes, offset, 4);
                            int chunkSize = BitConverter.ToInt32(wavBytes, offset + 4);
                            
                            Debug.Log($"청크 발견: {chunkId}, 크기: {chunkSize} 바이트, 오프셋: {offset}");
                            
                            if (chunkId == "fmt ")
                            {
                                // fmt 청크에서 샘플레이트와 채널 정보 읽기
                                sampleRate = BitConverter.ToInt32(wavBytes, offset + 12);
                                channels = BitConverter.ToInt16(wavBytes, offset + 10);
                                Debug.Log($"오디오 정보: 샘플레이트={sampleRate}, 채널={channels}");
                            }
                            else if (chunkId == "data")
                            {
                                // 실제 오디오 데이터 발견
                                Debug.Log($"오디오 데이터 청크 발견: 크기={chunkSize}, 오프셋={offset + 8}");
                                audioData = new byte[chunkSize];
                                Array.Copy(wavBytes, offset + 8, audioData, 0, chunkSize);
                                foundDataChunk = true;
                                break;
                            }
                            
                            // 다음 청크로 이동 (청크 크기 + 8바이트 헤더)
                            offset += 8 + chunkSize;
                            
                            // 청크 크기가 홀수인 경우 패딩 바이트 추가
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
                    Debug.Log("WAV 헤더 없음 - raw PCM 데이터로 처리");
                    audioData = wavBytes;
                }
                
                // audioData가 null인지 확인
                if (audioData == null)
                {
                    Debug.LogError("오디오 데이터를 추출할 수 없습니다.");
                    return null;
                }
                
                // 오디오 데이터 헤더 확인
                string audioHeaderHex = BitConverter.ToString(audioData, 0, Math.Min(20, audioData.Length));
                Debug.Log($"오디오 데이터 (처음 20바이트): {audioHeaderHex}");
                
                // 16비트 PCM 데이터로 가정
                float[] samples = new float[audioData.Length / 2];
                
                // JavaScript 방식과 동일한 바이트 처리
                for (int i = 0; i < samples.Length; i++)
                {
                    // Little-endian 순서로 읽기 (JavaScript와 동일)
                    short sample = (short)((audioData[i * 2] & 0xFF) | (audioData[i * 2 + 1] << 8));
                    
                    // JavaScript와 동일한 정규화 (32768로 나누기)
                    float normalizedSample = sample / 32768f;
                    samples[i] = normalizedSample;
                }
                
                Debug.Log($"샘플 변환 완료: {samples.Length} 샘플");
                
                // 첫 0.1초 (4410 샘플)의 데이터 출력
                int samplesToCheck = Math.Min(4410, samples.Length);
                Debug.Log($"첫 0.1초 ({samplesToCheck} 샘플) 데이터 분석:");
                for (int i = 0; i < Math.Min(10, samplesToCheck); i++)
                {
                    Debug.Log($"샘플[{i}]: {samples[i]:F6}");
                }
                
                // 최대값과 최소값 확인
                float maxSample = float.MinValue;
                float minSample = float.MaxValue;
                for (int i = 0; i < samplesToCheck; i++)
                {
                    maxSample = Mathf.Max(maxSample, samples[i]);
                    minSample = Mathf.Min(minSample, samples[i]);
                }
                Debug.Log($"첫 0.1초 범위: {minSample:F6} ~ {maxSample:F6}");
                
                // 첫 번째 샘플이 최대값이면 시작 지점 문제일 수 있음
                if (Math.Abs(samples[0]) > 0.9f)
                {
                    Debug.LogWarning("첫 번째 샘플이 최대값입니다. 시작 지점을 조정해보겠습니다.");
                    
                    // 시작 지점을 1초 후로 조정 (44100 샘플)
                    int startOffset = Math.Min(44100, samples.Length / 2);
                    float[] adjustedSamples = new float[samples.Length - startOffset];
                    Array.Copy(samples, startOffset, adjustedSamples, 0, adjustedSamples.Length);
                    samples = adjustedSamples;
                    
                    Debug.Log($"시작 지점 조정 후: {samples.Length} 샘플");
                }
                
                // JavaScript에서는 DC 오프셋 제거나 페이드 인/아웃을 하지 않음
                // 단순하게 처리
                
                AudioClip audioClip = AudioClip.Create("Voice", samples.Length, channels, sampleRate, false);
                audioClip.SetData(samples, 0);
                
                Debug.Log($"AudioClip 생성 완료: {audioClip.length}초");
                
                // 임시로 Resources에 저장 (디버깅용)
                SaveAudioClipTemporarily(audioClip, wavBytes.Length);
                
                return audioClip;
            }
            catch (Exception ex)
            {
                Debug.LogError($"WAV 변환 실패: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// AudioClip을 임시로 Resources에 저장 (디버깅용)
        /// </summary>
        /// <param name="audioClip">저장할 AudioClip</param>
        /// <param name="originalSize">원본 데이터 크기</param>
        private static void SaveAudioClipTemporarily(AudioClip audioClip, int originalSize)
        {
            try
            {
                // 임시 폴더 생성
                string tempFolderPath = "Assets/Resources/TempAudio";
                if (!System.IO.Directory.Exists(tempFolderPath))
                {
                    System.IO.Directory.CreateDirectory(tempFolderPath);
                }
                
                // 고유한 파일명 생성
                string fileName = $"temp_audio_{DateTime.Now:yyyyMMdd_HHmmss}_{originalSize}.wav";
                string filePath = $"{tempFolderPath}/{fileName}";
                
                // WAV 파일로 저장
                SaveAudioClipAsWav(audioClip, filePath);
                
                Debug.Log($"AudioClip 임시 저장 완료: {filePath}");
                Debug.Log($"저장된 파일 크기: {originalSize} 바이트, AudioClip 길이: {audioClip.length}초");
                
                // Unity 에디터에서 에셋 새로고침
                #if UNITY_EDITOR
                UnityEditor.AssetDatabase.Refresh();
                #endif
                
            }
            catch (Exception ex)
            {
                Debug.LogError($"AudioClip 임시 저장 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// AudioClip을 WAV 파일로 저장
        /// </summary>
        /// <param name="audioClip">저장할 AudioClip</param>
        /// <param name="filePath">저장할 파일 경로</param>
        private static void SaveAudioClipAsWav(AudioClip audioClip, string filePath)
        {
            try
            {
                // WAV 헤더 생성
                byte[] header = CreateWavHeader(audioClip);
                
                // 오디오 데이터 추출
                float[] samples = new float[audioClip.samples];
                audioClip.GetData(samples, 0);
                
                // 16비트 PCM으로 변환
                byte[] audioData = new byte[samples.Length * 2];
                for (int i = 0; i < samples.Length; i++)
                {
                    short sample = (short)(samples[i] * 32767f);
                    audioData[i * 2] = (byte)(sample & 0xFF);
                    audioData[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
                }
                
                // 파일로 저장
                byte[] wavFile = new byte[header.Length + audioData.Length];
                Array.Copy(header, 0, wavFile, 0, header.Length);
                Array.Copy(audioData, 0, wavFile, header.Length, audioData.Length);
                
                System.IO.File.WriteAllBytes(filePath, wavFile);
                
                Debug.Log($"WAV 파일 저장 완료: {filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"WAV 파일 저장 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// WAV 헤더 생성
        /// </summary>
        /// <param name="audioClip">AudioClip</param>
        /// <returns>WAV 헤더 바이트 배열</returns>
        private static byte[] CreateWavHeader(AudioClip audioClip)
        {
            byte[] header = new byte[44];
            int offset = 0;
            
            // RIFF 헤더
            WriteString(header, ref offset, "RIFF");
            WriteInt32(header, ref offset, 36 + audioClip.samples * 2); // 파일 크기
            WriteString(header, ref offset, "WAVE");
            
            // fmt 청크
            WriteString(header, ref offset, "fmt ");
            WriteInt32(header, ref offset, 16); // fmt 청크 크기
            WriteInt16(header, ref offset, 1); // PCM
            WriteInt16(header, ref offset, audioClip.channels);
            WriteInt32(header, ref offset, audioClip.frequency);
            WriteInt32(header, ref offset, audioClip.frequency * audioClip.channels * 2); // 바이트 레이트
            WriteInt16(header, ref offset, audioClip.channels * 2); // 블록 얼라인
            WriteInt16(header, ref offset, 16); // 비트 깊이
            
            // data 청크
            WriteString(header, ref offset, "data");
            WriteInt32(header, ref offset, audioClip.samples * 2); // 데이터 크기
            
            return header;
        }
        
        private static void WriteString(byte[] buffer, ref int offset, string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                buffer[offset + i] = (byte)value[i];
            }
            offset += value.Length;
        }
        
        private static void WriteInt16(byte[] buffer, ref int offset, int value)
        {
            buffer[offset] = (byte)(value & 0xFF);
            buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
            offset += 2;
        }
        
        private static void WriteInt32(byte[] buffer, ref int offset, int value)
        {
            buffer[offset] = (byte)(value & 0xFF);
            buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 2] = (byte)((value >> 16) & 0xFF);
            buffer[offset + 3] = (byte)((value >> 24) & 0xFF);
            offset += 4;
        }
        
        public bool HasAudioClip() => AudioClip != null;
        
        public bool IsPlayable() => HasAudioClip() && Length > 0;
    }
} 