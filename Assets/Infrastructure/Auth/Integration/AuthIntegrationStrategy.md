# HttpApiClient 인증 통합 전략

## 개요
이 문서는 기존 HttpApiClient와 새로운 JWT 인증 시스템을 통합하는 전략을 설명합니다.

## 통합 방법

### 1. 확장 메서드 방식 (권장)
`HttpClientAuthExtension`을 통해 기존 HttpApiClient에 인증 기능을 추가합니다.

#### 장점
- 기존 HttpApiClient 코드 수정 불필요
- 선택적 인증 적용 가능
- 기존 API 호출과 인증 API 호출을 명확히 구분

#### 사용법
```csharp
// 기존 방식 (인증 없음)
var response = await httpClient.GetAsync<UserData>("/public/info");

// 새로운 방식 (인증 있음)
var response = await httpClient.GetAuthenticatedAsync<UserData>("/user/profile");
```

### 2. 인터셉터 방식 (선택적)
`HttpClientAuthInterceptor`를 통해 모든 요청을 자동으로 가로채서 인증 처리합니다.

#### 장점
- 투명한 인증 처리
- 기존 코드 수정 최소화
- 401 응답 자동 처리

#### 단점
- 모든 요청에 대한 오버헤드
- 디버깅 복잡성 증가

## 구현 세부사항

### 공개 엔드포인트 관리
인증이 필요하지 않은 엔드포인트들을 사전에 정의합니다:

```csharp
private static readonly HashSet<string> PublicEndpoints = new HashSet<string>
{
    "/auth/login",
    "/auth/callback", 
    "/auth/refresh",
    "/auth/health",
    "/public/"
};
```

### 401 응답 처리 플로우
1. 401 Unauthorized 응답 감지
2. AuthManager.RefreshTokenAsync() 호출
3. 갱신 성공 시: 새 토큰으로 원본 요청 재시도
4. 갱신 실패 시: AuthManager.LogoutAsync() 호출

### 토큰 주입 방식
```csharp
var authHeaders = new Dictionary<string, string>
{
    { "Authorization", $"Bearer {accessToken}" }
};
```

## 초기화 순서

### 1. 시스템 시작 시
```csharp
// 1. AuthManager 초기화
await AuthManager.Instance.InitializeAsync();

// 2. HttpClient 인증 확장 초기화  
HttpClientAuthExtension.Initialize(AuthManager.Instance);

// 3. (선택적) 인터셉터 초기화
authInterceptor.Initialize(HttpApiClient.Instance, AuthManager.Instance);
```

### 2. 로그인 후
```csharp
// AuthManager가 자동으로 HttpApiClient에 토큰 설정
// 이후 모든 인증 요청에서 자동으로 토큰 사용
```

### 3. 로그아웃 시
```csharp
// AuthManager가 자동으로 토큰 정리
// HttpApiClient에서 Authorization 헤더 제거
```

## 에러 처리 전략

### 인증 관련 예외
- `UnauthorizedAccessException`: 인증되지 않은 상태
- `ApiException` (401): 토큰 만료/무효화
- `InvalidOperationException`: 초기화 오류

### 재시도 로직
```csharp
// 최대 2번 시도 (초기 + 토큰 갱신 후)
for (int attempt = 0; attempt < 2; attempt++)
{
    try
    {
        return await apiCall(authHeaders);
    }
    catch (ApiException ex) when (ex.StatusCode == 401 && attempt == 0)
    {
        await Handle401ResponseAsync();
        // 다음 루프에서 재시도
    }
}
```

## 보안 고려사항

### 토큰 전송
- HTTPS 필수
- Authorization 헤더 사용
- 쿠키 기반 전송 (WebGL BFF 모드)

### 토큰 저장
- Access 토큰: 메모리에만 저장
- Refresh 토큰: 플랫폼별 보안 저장소
- 민감 정보 로깅 금지

### 타이밍 공격 방지
- 토큰 만료 시간을 정확히 노출하지 않음
- 일정한 응답 시간 유지

## 플랫폼별 고려사항

### WebGL
- BFF 모드 지원
- HttpOnly 쿠키 사용
- CSRF 토큰 검증

### 모바일/데스크톱
- 네이티브 보안 저장소 사용
- 앱 재시작 시 자동 인증 복원
- 백그라운드에서 토큰 갱신

## 테스트 시나리오

### 기본 인증 플로우
1. 로그인 → 토큰 발급
2. 인증 API 호출 → 성공
3. 토큰 만료 → 자동 갱신
4. 갱신 실패 → 로그아웃

### 에러 시나리오
1. 네트워크 오류 시 처리
2. 서버 오류 (5xx) 시 처리
3. 토큰 탈취 감지 시 처리
4. 동시 요청 시 토큰 경쟁 상태 처리

## 성능 최적화

### 토큰 캐싱
- 메모리에 Access 토큰 캐시
- 만료 전까지 재사용
- 만료 임박 시 사전 갱신

### 요청 최적화
- 불필요한 헤더 추가 방지
- 공개 엔드포인트 빠른 판별
- 인증 오버헤드 최소화

## 마이그레이션 가이드

### 기존 코드 수정
```csharp
// 기존
var user = await httpClient.GetAsync<User>("/user/profile");

// 수정 후
var user = await httpClient.GetAuthenticatedAsync<User>("/user/profile");
```

### 점진적 적용
1. 새로운 API 호출부터 인증 메서드 사용
2. 기존 API 호출 점진적 마이그레이션  
3. 공개 API는 기존 메서드 유지

### 호환성 유지
- 기존 HttpApiClient 인터페이스 보존
- 확장 메서드로 새 기능 추가
- 선택적 인터셉터 적용
