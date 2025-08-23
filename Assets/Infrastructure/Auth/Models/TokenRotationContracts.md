# 토큰 로테이션 및 재사용 감지 서버 계약서

## 개요
JWT Refresh 토큰의 로테이션과 재사용 감지를 통한 보안 강화 방안을 정의합니다.

## 토큰 로테이션 정책

### 기본 원칙
- **Refresh 토큰은 매 사용 시 로테이션** (새 토큰 발급)
- **이전 토큰은 즉시 무효화** (단, Grace Period 존재)
- **재사용 감지 시 모든 관련 토큰 무효화**
- **디바이스/세션 바인딩**으로 토큰 범위 제한

### 토큰 수명 정책
- **Access Token**: 5-15분 (기본 10분)
- **Refresh Token**: 7-30일 (기본 14일)
- **Grace Period**: 30초 (네트워크 지연 고려)
- **Rotation Window**: 토큰 발급 후 24시간 내 최대 로테이션 횟수 제한

## 서버 API 계약

### 1. 토큰 갱신 API

#### Endpoint
```
POST /auth/refresh
Content-Type: application/json
```

#### Request Body
```json
{
  "refresh_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "device_id": "550e8400-e29b-41d4-a716-446655440000",
  "client_info": {
    "platform": "Unity",
    "version": "1.0.0",
    "os": "Windows 10",
    "device_fingerprint": "hash_of_device_characteristics"
  },
  "rotation_id": "rot_12345678901234567890"
}
```

#### Success Response (200)
```json
{
  "success": true,
  "data": {
    "access_token": {
      "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
      "expires_in": 600,
      "expires_at": "2024-01-01T12:10:00Z",
      "token_type": "Bearer",
      "scope": "read write"
    },
    "refresh_token": {
      "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
      "expires_in": 1209600,
      "expires_at": "2024-01-15T12:00:00Z",
      "rotation_id": "rot_12345678901234567891",
      "device_id": "550e8400-e29b-41d4-a716-446655440000"
    },
    "rotation_info": {
      "previous_token_invalidated": true,
      "grace_period_seconds": 30,
      "max_rotations_per_day": 100,
      "current_rotation_count": 5
    }
  },
  "timestamp": "2024-01-01T12:00:00Z"
}
```

#### Error Responses

##### 토큰 만료 (401)
```json
{
  "success": false,
  "error": {
    "code": "TOKEN_EXPIRED",
    "message": "Refresh token has expired",
    "details": {
      "expired_at": "2024-01-01T11:00:00Z",
      "requires_reauth": true
    }
  },
  "timestamp": "2024-01-01T12:00:00Z"
}
```

##### 토큰 재사용 감지 (403)
```json
{
  "success": false,
  "error": {
    "code": "TOKEN_REUSE_DETECTED",
    "message": "Refresh token reuse detected - all tokens invalidated",
    "details": {
      "reused_token_id": "rot_12345678901234567890",
      "original_use_time": "2024-01-01T11:55:00Z",
      "reuse_detection_time": "2024-01-01T12:00:00Z",
      "all_tokens_invalidated": true,
      "security_event_logged": true,
      "requires_reauth": true
    }
  },
  "timestamp": "2024-01-01T12:00:00Z"
}
```

##### 디바이스 불일치 (403)
```json
{
  "success": false,
  "error": {
    "code": "DEVICE_MISMATCH",
    "message": "Token issued for different device",
    "details": {
      "expected_device_id": "550e8400-e29b-41d4-a716-446655440000",
      "received_device_id": "550e8400-e29b-41d4-a716-446655440001",
      "requires_reauth": true
    }
  },
  "timestamp": "2024-01-01T12:00:00Z"
}
```

##### 로테이션 한도 초과 (429)
```json
{
  "success": false,
  "error": {
    "code": "ROTATION_LIMIT_EXCEEDED",
    "message": "Too many token rotations in 24 hours",
    "details": {
      "max_rotations_per_day": 100,
      "current_count": 100,
      "reset_time": "2024-01-02T00:00:00Z",
      "retry_after_seconds": 3600
    }
  },
  "timestamp": "2024-01-01T12:00:00Z"
}
```

### 2. 토큰 무효화 API

#### Endpoint
```
POST /auth/revoke
Content-Type: application/json
```

#### Request Body
```json
{
  "refresh_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "revoke_all_tokens": false,
  "reason": "user_logout"
}
```

#### Success Response (200)
```json
{
  "success": true,
  "data": {
    "tokens_revoked": 1,
    "revocation_time": "2024-01-01T12:00:00Z"
  }
}
```

### 3. 토큰 상태 조회 API

#### Endpoint
```
GET /auth/token/status?token_id=rot_12345678901234567890
Authorization: Bearer {access_token}
```

#### Success Response (200)
```json
{
  "success": true,
  "data": {
    "token_id": "rot_12345678901234567890",
    "status": "active",
    "issued_at": "2024-01-01T10:00:00Z",
    "expires_at": "2024-01-15T10:00:00Z",
    "last_used": "2024-01-01T11:30:00Z",
    "rotation_count": 5,
    "device_id": "550e8400-e29b-41d4-a716-446655440000",
    "is_in_grace_period": false
  }
}
```

## 재사용 감지 메커니즘

### 감지 시나리오
1. **동일 토큰 2회 이상 사용**: 첫 사용 후 무효화된 토큰 재사용
2. **Grace Period 초과**: 30초 이내 동일 토큰 사용은 허용, 초과 시 재사용 감지
3. **디바이스 불일치**: 다른 디바이스에서 동일 토큰 사용
4. **지리적 이상**: 짧은 시간 내 지리적으로 불가능한 위치에서 사용

### 감지 시 조치
1. **즉시 무효화**: 해당 토큰과 연관된 모든 토큰 무효화
2. **보안 이벤트 로그**: 상세한 보안 이벤트 기록
3. **사용자 알림**: 보안 위험 알림 (선택적)
4. **추가 검증**: 다음 로그인 시 추가 인증 요구

### Grace Period 처리
```json
{
  "grace_period_policy": {
    "duration_seconds": 30,
    "allow_same_device": true,
    "allow_same_ip": true,
    "max_grace_uses": 1,
    "description": "네트워크 지연으로 인한 중복 요청 허용"
  }
}
```

## 클라이언트 구현 가이드

### 토큰 로테이션 처리
```csharp
// 1. 기존 토큰으로 갱신 요청
var refreshRequest = new RefreshTokenRequest 
{
    RefreshToken = oldRefreshToken.Token,
    DeviceId = DeviceInfo.GetDeviceId(),
    RotationId = oldRefreshToken.RotationId
};

// 2. 새 토큰 셋 수신 및 저장
var newTokenSet = await httpClient.PostAsync<TokenSet>("/auth/refresh", refreshRequest);

// 3. 이전 토큰 즉시 삭제 (메모리에서)
oldRefreshToken = null;

// 4. 새 토큰 저장
await tokenStorage.StoreRefreshTokenAsync(newTokenSet.RefreshToken);
tokenStorage.StoreAccessTokenInMemory(newTokenSet.AccessToken);
```

### 재사용 감지 에러 처리
```csharp
try 
{
    var result = await RefreshTokenAsync();
}
catch (ApiException ex) when (ex.StatusCode == 403)
{
    if (ex.ErrorCode == "TOKEN_REUSE_DETECTED")
    {
        // 모든 토큰 삭제 및 재로그인 요구
        await ClearAllTokensAsync();
        await TriggerReauthenticationAsync();
        LogSecurityEvent("Token reuse detected", ex.Details);
    }
}
```

## 보안 강화 옵션

### 디바이스 바인딩
```json
{
  "device_binding": {
    "device_id": "550e8400-e29b-41d4-a716-446655440000",
    "device_fingerprint": "sha256_hash_of_device_characteristics",
    "platform_info": {
      "os": "Windows 10",
      "browser": "Unity WebGL",
      "screen_resolution": "1920x1080",
      "timezone": "Asia/Seoul"
    }
  }
}
```

### IP 주소 검증
```json
{
  "ip_validation": {
    "enforce_ip_binding": false,
    "allow_ip_change": true,
    "max_ip_changes_per_day": 10,
    "geolocation_check": true,
    "suspicious_location_threshold_km": 1000
  }
}
```

### 세션 제한
```json
{
  "session_limits": {
    "max_concurrent_sessions": 3,
    "max_devices_per_user": 5,
    "force_logout_on_new_device": false,
    "notify_on_new_session": true
  }
}
```

## 모니터링 및 알림

### 보안 이벤트 로그
```json
{
  "event_type": "TOKEN_REUSE_DETECTED",
  "user_id": "user_12345",
  "device_id": "550e8400-e29b-41d4-a716-446655440000",
  "ip_address": "192.168.1.100",
  "user_agent": "Unity/2023.1.0 (Windows)",
  "timestamp": "2024-01-01T12:00:00Z",
  "details": {
    "original_token_id": "rot_12345678901234567890",
    "reuse_attempt_count": 2,
    "time_since_invalidation": "45s",
    "geographic_distance_km": 0,
    "action_taken": "all_tokens_revoked"
  }
}
```

### 알림 정책
- **즉시 알림**: 토큰 재사용 감지
- **일별 요약**: 의심스러운 활동 요약
- **주간 보고서**: 보안 상태 전반 리뷰

## 성능 최적화

### 캐싱 전략
- **토큰 상태 캐시**: Redis 기반 빠른 조회
- **디바이스 정보 캐시**: 반복 검증 최소화
- **지리적 위치 캐시**: IP 기반 위치 정보 캐싱

### 데이터베이스 인덱스
```sql
-- 토큰 조회 최적화
CREATE INDEX idx_refresh_tokens_device_id ON refresh_tokens(device_id);
CREATE INDEX idx_refresh_tokens_rotation_id ON refresh_tokens(rotation_id);
CREATE INDEX idx_refresh_tokens_expires_at ON refresh_tokens(expires_at);

-- 보안 이벤트 조회 최적화  
CREATE INDEX idx_security_events_user_device ON security_events(user_id, device_id);
CREATE INDEX idx_security_events_timestamp ON security_events(timestamp);
```

## 테스트 시나리오

### 정상 플로우
1. Access 토큰 만료 → Refresh 요청 → 새 토큰 셋 발급
2. 연속 갱신 → 각각 새로운 Refresh 토큰 발급
3. Grace Period 내 중복 요청 → 허용

### 비정상 플로우  
1. 만료된 Refresh 토큰 사용 → 401 에러
2. 무효화된 토큰 재사용 → 403 에러 + 전체 무효화
3. 다른 디바이스에서 토큰 사용 → 403 에러
4. 로테이션 한도 초과 → 429 에러

### 경계 조건
1. Grace Period 경계 시점 테스트
2. 네트워크 지연 시뮬레이션
3. 동시 갱신 요청 처리
4. 디바이스 변경 시나리오
