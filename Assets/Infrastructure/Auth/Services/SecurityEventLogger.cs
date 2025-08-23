using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ProjectVG.Infrastructure.Auth.Models;
using Newtonsoft.Json;

namespace ProjectVG.Infrastructure.Auth.Services
{
    /// <summary>
    /// 보안 이벤트 로깅 서비스
    /// </summary>
    public class SecurityEventLogger
    {
        private const string LOG_FILE_NAME = "security_events.log";
        private const int MAX_LOG_ENTRIES = 1000;
        private const int LOG_CLEANUP_THRESHOLD = 1200;
        
        private readonly string _logFilePath;
        private readonly Queue<SecurityEvent> _eventQueue = new Queue<SecurityEvent>();
        private readonly object _lockObject = new object();
        
        private bool _isInitialized = false;
        private DateTime _lastCleanup = DateTime.MinValue;
        
        public event Action<SecurityEvent> OnSecurityEventLogged;
        
        public SecurityEventLogger()
        {
            _logFilePath = GetLogFilePath();
            Initialize();
        }
        
        #region Public Methods
        
        /// <summary>
        /// 보안 이벤트 로깅
        /// </summary>
        public void LogSecurityEvent(SecurityEvent securityEvent)
        {
            if (securityEvent == null) return;
            
            try
            {
                // 기본 정보 자동 설정
                if (string.IsNullOrEmpty(securityEvent.DeviceId))
                {
                    securityEvent.DeviceId = UnityEngine.SystemInfo.deviceUniqueIdentifier;
                }
                
                if (securityEvent.Timestamp == default)
                {
                    securityEvent.Timestamp = DateTime.UtcNow;
                }
                
                if (string.IsNullOrEmpty(securityEvent.UserAgent))
                {
                    securityEvent.UserAgent = GenerateUserAgent();
                }
                
                lock (_lockObject)
                {
                    _eventQueue.Enqueue(securityEvent);
                    
                    // 큐 크기 제한
                    while (_eventQueue.Count > MAX_LOG_ENTRIES)
                    {
                        _eventQueue.Dequeue();
                    }
                }
                
                // 파일에 즉시 기록
                _ = WriteToFileAsync(securityEvent);
                
                // 이벤트 발행
                OnSecurityEventLogged?.Invoke(securityEvent);
                
                // 심각한 이벤트는 즉시 서버 전송
                if (securityEvent.Severity >= SecurityEventSeverity.High)
                {
                    _ = SendToServerAsync(securityEvent);
                }
                
                Debug.Log($"[SecurityEventLogger] 보안 이벤트 기록: {securityEvent.EventType} ({securityEvent.Severity})");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SecurityEventLogger] 보안 이벤트 로깅 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 토큰 재사용 감지 이벤트 로깅
        /// </summary>
        public void LogTokenReuseDetected(string tokenId, DateTime originalUse, DateTime reuseTime, string deviceId = null)
        {
            var securityEvent = new SecurityEvent
            {
                EventType = "TOKEN_REUSE_DETECTED",
                DeviceId = deviceId ?? UnityEngine.SystemInfo.deviceUniqueIdentifier,
                Timestamp = DateTime.UtcNow,
                Severity = SecurityEventSeverity.Critical,
                Details = new
                {
                    token_id = tokenId,
                    original_use_time = originalUse,
                    reuse_detection_time = reuseTime,
                    time_difference_seconds = (reuseTime - originalUse).TotalSeconds,
                    action_taken = "all_tokens_revoked"
                }
            };
            
            LogSecurityEvent(securityEvent);
        }
        
        /// <summary>
        /// 디바이스 불일치 이벤트 로깅
        /// </summary>
        public void LogDeviceMismatch(string expectedDevice, string receivedDevice, string tokenId = null)
        {
            var securityEvent = new SecurityEvent
            {
                EventType = "DEVICE_MISMATCH",
                DeviceId = receivedDevice,
                Timestamp = DateTime.UtcNow,
                Severity = SecurityEventSeverity.High,
                Details = new
                {
                    expected_device_id = expectedDevice,
                    received_device_id = receivedDevice,
                    token_id = tokenId,
                    action_taken = "token_rejected"
                }
            };
            
            LogSecurityEvent(securityEvent);
        }
        
        /// <summary>
        /// 의심스러운 로케이션 이벤트 로깅
        /// </summary>
        public void LogSuspiciousLocation(string ipAddress, string location, double distanceKm, string previousLocation = null)
        {
            var securityEvent = new SecurityEvent
            {
                EventType = "SUSPICIOUS_LOCATION",
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow,
                Severity = distanceKm > 1000 ? SecurityEventSeverity.High : SecurityEventSeverity.Medium,
                Details = new
                {
                    current_location = location,
                    previous_location = previousLocation,
                    distance_km = distanceKm,
                    ip_address = ipAddress,
                    threshold_exceeded = distanceKm > 1000
                }
            };
            
            LogSecurityEvent(securityEvent);
        }
        
        /// <summary>
        /// 최근 보안 이벤트 조회
        /// </summary>
        public List<SecurityEvent> GetRecentEvents(int count = 50, SecurityEventSeverity? minSeverity = null)
        {
            var result = new List<SecurityEvent>();
            
            lock (_lockObject)
            {
                var events = _eventQueue.ToArray();
                Array.Reverse(events); // 최신 순으로 정렬
                
                foreach (var evt in events)
                {
                    if (minSeverity.HasValue && evt.Severity < minSeverity.Value)
                        continue;
                    
                    result.Add(evt);
                    
                    if (result.Count >= count)
                        break;
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 보안 이벤트 통계
        /// </summary>
        public SecurityEventStats GetEventStats(TimeSpan? timeRange = null)
        {
            var cutoffTime = timeRange.HasValue ? DateTime.UtcNow - timeRange.Value : DateTime.MinValue;
            var stats = new SecurityEventStats();
            
            lock (_lockObject)
            {
                foreach (var evt in _eventQueue)
                {
                    if (evt.Timestamp < cutoffTime)
                        continue;
                    
                    stats.TotalEvents++;
                    
                    switch (evt.Severity)
                    {
                        case SecurityEventSeverity.Low:
                            stats.LowSeverityCount++;
                            break;
                        case SecurityEventSeverity.Medium:
                            stats.MediumSeverityCount++;
                            break;
                        case SecurityEventSeverity.High:
                            stats.HighSeverityCount++;
                            break;
                        case SecurityEventSeverity.Critical:
                            stats.CriticalSeverityCount++;
                            break;
                    }
                    
                    if (!stats.EventTypeCounts.ContainsKey(evt.EventType))
                    {
                        stats.EventTypeCounts[evt.EventType] = 0;
                    }
                    stats.EventTypeCounts[evt.EventType]++;
                }
            }
            
            return stats;
        }
        
        /// <summary>
        /// 로그 파일 정리
        /// </summary>
        public async UniTask CleanupLogsAsync()
        {
            if (DateTime.UtcNow - _lastCleanup < TimeSpan.FromHours(6))
            {
                return; // 6시간마다만 정리
            }
            
            try
            {
                await PerformLogCleanupAsync();
                _lastCleanup = DateTime.UtcNow;
                
                Debug.Log("[SecurityEventLogger] 로그 파일 정리 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SecurityEventLogger] 로그 정리 실패: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private void Initialize()
        {
            try
            {
                EnsureLogDirectoryExists();
                LoadExistingLogs();
                _isInitialized = true;
                
                Debug.Log($"[SecurityEventLogger] 초기화 완료 - 로그 파일: {_logFilePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SecurityEventLogger] 초기화 실패: {ex.Message}");
            }
        }
        
        private string GetLogFilePath()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL에서는 로컬 파일 시스템 접근 불가
            return null;
#else
            var logDirectory = Path.Combine(Application.persistentDataPath, "Logs", "Security");
            return Path.Combine(logDirectory, LOG_FILE_NAME);
#endif
        }
        
        private void EnsureLogDirectoryExists()
        {
            if (string.IsNullOrEmpty(_logFilePath)) return;
            
            var directory = Path.GetDirectoryName(_logFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
        
        private void LoadExistingLogs()
        {
            if (string.IsNullOrEmpty(_logFilePath) || !File.Exists(_logFilePath)) return;
            
            try
            {
                var lines = File.ReadAllLines(_logFilePath);
                var recentLines = lines.Length > MAX_LOG_ENTRIES 
                    ? lines[(lines.Length - MAX_LOG_ENTRIES)..]
                    : lines;
                
                foreach (var line in recentLines)
                {
                    try
                    {
                        var evt = JsonConvert.DeserializeObject<SecurityEvent>(line);
                        if (evt != null)
                        {
                            _eventQueue.Enqueue(evt);
                        }
                    }
                    catch
                    {
                        // 잘못된 JSON 라인 무시
                    }
                }
                
                Debug.Log($"[SecurityEventLogger] {_eventQueue.Count}개의 기존 로그 로드");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SecurityEventLogger] 기존 로그 로드 실패: {ex.Message}");
            }
        }
        
        private async UniTask WriteToFileAsync(SecurityEvent securityEvent)
        {
            if (string.IsNullOrEmpty(_logFilePath)) return;
            
            try
            {
                var json = JsonConvert.SerializeObject(securityEvent, Formatting.None);
                await File.AppendAllTextAsync(_logFilePath, json + Environment.NewLine);
                
                // 파일 크기 체크 및 정리
                if (_eventQueue.Count > LOG_CLEANUP_THRESHOLD)
                {
                    _ = CleanupLogsAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SecurityEventLogger] 파일 쓰기 실패: {ex.Message}");
            }
        }
        
        private async UniTask SendToServerAsync(SecurityEvent securityEvent)
        {
            try
            {
                var httpClient = ProjectVG.Infrastructure.Network.Http.HttpApiClient.Instance;
                if (httpClient == null) return;
                
                await httpClient.PostAsync<object>("/auth/security-events", securityEvent);
                
                Debug.Log($"[SecurityEventLogger] 보안 이벤트 서버 전송 완료: {securityEvent.EventType}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SecurityEventLogger] 서버 전송 실패: {ex.Message}");
            }
        }
        
        private async UniTask PerformLogCleanupAsync()
        {
            if (string.IsNullOrEmpty(_logFilePath)) return;
            
            try
            {
                // 메모리 큐에서 오래된 이벤트 제거
                lock (_lockObject)
                {
                    var cutoffTime = DateTime.UtcNow.AddDays(-7); // 7일 이전 이벤트 제거
                    var tempQueue = new Queue<SecurityEvent>();
                    
                    while (_eventQueue.Count > 0)
                    {
                        var evt = _eventQueue.Dequeue();
                        if (evt.Timestamp > cutoffTime)
                        {
                            tempQueue.Enqueue(evt);
                        }
                    }
                    
                    _eventQueue.Clear();
                    while (tempQueue.Count > 0)
                    {
                        _eventQueue.Enqueue(tempQueue.Dequeue());
                    }
                }
                
                // 파일 재작성
                if (File.Exists(_logFilePath))
                {
                    var tempFile = _logFilePath + ".tmp";
                    
                    using (var writer = new StreamWriter(tempFile))
                    {
                        lock (_lockObject)
                        {
                            foreach (var evt in _eventQueue)
                            {
                                var json = JsonConvert.SerializeObject(evt, Formatting.None);
                                await writer.WriteLineAsync(json);
                            }
                        }
                    }
                    
                    File.Replace(tempFile, _logFilePath, null);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SecurityEventLogger] 로그 정리 실행 실패: {ex.Message}");
            }
        }
        
        private string GenerateUserAgent()
        {
            return $"Unity/{Application.unityVersion} ({UnityEngine.SystemInfo.operatingSystem})";
        }
        
        #endregion
    }
    
    /// <summary>
    /// 보안 이벤트 통계
    /// </summary>
    [Serializable]
    public class SecurityEventStats
    {
        public int TotalEvents { get; set; }
        public int LowSeverityCount { get; set; }
        public int MediumSeverityCount { get; set; }
        public int HighSeverityCount { get; set; }
        public int CriticalSeverityCount { get; set; }
        public Dictionary<string, int> EventTypeCounts { get; set; } = new Dictionary<string, int>();
        
        public double CriticalPercentage => TotalEvents > 0 ? (double)CriticalSeverityCount / TotalEvents * 100 : 0;
        public double HighRiskPercentage => TotalEvents > 0 ? (double)(HighSeverityCount + CriticalSeverityCount) / TotalEvents * 100 : 0;
    }
}
