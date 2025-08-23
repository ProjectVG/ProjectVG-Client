# SessionManager-AuthManager 통합 가이드

## 개요
이 문서는 기존 SessionManager와 새로운 AuthManager를 통합하는 방법을 설명합니다.

## 통합 아키텍처

### 컴포넌트 관계
```
AuthManager ←→ SessionManagerExtension ←→ SessionManager
     ↓                    ↓                    ↓
  토큰 관리           연동 로직            WebSocket 세션
```

### 의존성 흐름
1. **AuthSystemInitializer** - 전체 시스템 초기화 조정
2. **AuthManager** - JWT 토큰 생명주기 관리
3. **SessionManagerExtension** - 두 시스템 간 브리지 역할
4. **SessionManager** - WebSocket 세션 관리

## 주요 기능

### 1. 인증된 세션 관리
```csharp
// 인증된 세션 요청
var sessionId = await sessionManager.GetAuthenticatedSessionAsync();

// 세션 인증 상태 확인
bool isAuthenticated = sessionManager.IsSessionAuthenticated();
```

### 2. 토큰-세션 연동
```csharp
// 세션에 인증 토큰 주입
await sessionManager.InjectAuthTokenToSessionAsync();

// 토큰 갱신 시 세션 재인증
await sessionManager.ReauthenticateSessionAsync();
```

### 3. 자동 상태 동기화
- **로그인 시**: 세션에 자동으로 토큰 주입
- **토큰 갱신 시**: 세션 자동 재인증
- **로그아웃 시**: 세션 인증 상태 자동 해제

## 초기화 순서

### 1. 기본 초기화
```csharp
// AuthSystemInitializer에서 자동 처리
await authSystemInitializer.InitializeAuthSystemAsync();
```

### 2. 수동 초기화 (고급)
```csharp
// 1. AuthManager 초기화
await AuthManager.Instance.InitializeAsync();

// 2. SessionManager 초기화 (기존)
sessionManager.Initialize(webSocketManager);

// 3. 연동 확장 초기화
SessionManagerExtension.Initialize(sessionManager, authManager);
```

## 이벤트 흐름

### 인증 상태 변경 시
```
AuthManager.OnAuthStateChanged
    ↓
SessionManagerExtension.OnAuthStateChanged
    ↓
세션 토큰 주입/해제
```

### 세션 연결 상태 변경 시
```
SessionManager.OnSessionStarted/Ended
    ↓
SessionManagerExtension.OnSessionStarted/Ended
    ↓
인증 상태 동기화
```

## WebSocket 메시지 구조

### 인증 토큰 전송
```json
{
    "type": "auth",
    "token": "Bearer eyJhbGciOiJIUzI1NiIs...",
    "timestamp": "2024-01-01T00:00:00Z"
}
```

### 인증 응답
```json
{
    "type": "auth_response", 
    "success": true,
    "session_id": "session_12345",
    "expires_at": "2024-01-01T01:00:00Z"
}
```

### 로그아웃 신호
```json
{
    "type": "logout",
    "reason": "user_logout"
}
```

## 상태 관리

### 인증 세션 상태
- `IsAuthenticatedSession`: 세션이 인증된 상태인지 확인
- `AuthenticatedSessionId`: 마지막 인증된 세션 ID
- `_lastAuthenticatedSessionId`: 내부 상태 추적

### 상태 전환
```
미인증 → 로그인 → 세션 연결 → 토큰 주입 → 인증된 세션
인증된 세션 → 토큰 갱신 → 재인증 → 인증된 세션
인증된 세션 → 로그아웃 → 세션 해제 → 미인증
```

## 에러 처리

### 토큰 주입 실패
```csharp
try 
{
    var success = await sessionManager.InjectAuthTokenToSessionAsync();
    if (!success) 
    {
        // 재시도 또는 로그아웃 처리
        await authManager.LogoutAsync();
    }
}
catch (Exception ex)
{
    Debug.LogError($"토큰 주입 실패: {ex.Message}");
}
```

### 세션 연결 실패
```csharp
sessionManager.OnSessionError += (error) => 
{
    Debug.LogError($"세션 오류: {error}");
    // 인증 상태 정리
    SessionManagerExtension.ClearSessionAuthenticationAsync();
};
```

## 보안 고려사항

### 토큰 전송 보안
- WebSocket 연결은 WSS(Secure WebSocket) 사용
- 토큰은 메모리에서만 관리, 로깅 금지
- 세션 타임아웃 설정

### 세션 보안
- 세션 ID 충돌 방지
- 동시 세션 제한
- 비정상 연결 해제 감지

## 플랫폼별 고려사항

### WebGL
- BFF 모드에서 토큰 서버 전송
- 쿠키 기반 세션 관리 연동
- CSRF 토큰 검증

### 모바일/데스크톱
- 앱 백그라운드 시 세션 유지
- 네트워크 재연결 시 자동 재인증
- 푸시 알림과 세션 상태 동기화

## 디버깅 가이드

### 로그 확인 포인트
1. `[AuthSystemInitializer]` - 초기화 과정
2. `[SessionManagerExtension]` - 연동 로직
3. `[AuthManager]` - 토큰 관리
4. `[SessionManager]` - 세션 상태

### 일반적인 문제

#### 세션은 연결되었지만 인증 안됨
```csharp
// 확인 사항
Debug.Log($"세션 연결: {sessionManager.IsSessionConnected}");
Debug.Log($"인증 상태: {authManager.IsAuthenticated}");
Debug.Log($"인증된 세션: {sessionManager.IsSessionAuthenticated()}");
```

#### 토큰 갱신 후 세션 인증 실패
```csharp
// 재인증 시도
await sessionManager.ReauthenticateSessionAsync();
```

## 마이그레이션 체크리스트

### 기존 SessionManager 사용 코드
- [ ] `sessionManager.GetSessionIdAsync()` → `sessionManager.GetAuthenticatedSessionAsync()`
- [ ] 세션 상태 확인 로직 추가
- [ ] 인증 실패 시 처리 로직 추가

### 새로운 기능 활용
- [ ] 자동 토큰 주입 기능 활용
- [ ] 세션 재인증 기능 활용
- [ ] 통합 이벤트 핸들링 구현

### 테스트 시나리오
- [ ] 로그인 → 세션 연결 → 인증 플로우
- [ ] 토큰 만료 → 자동 갱신 → 세션 재인증
- [ ] 네트워크 끊김 → 재연결 → 세션 복원
- [ ] 로그아웃 → 세션 정리 → 상태 초기화
