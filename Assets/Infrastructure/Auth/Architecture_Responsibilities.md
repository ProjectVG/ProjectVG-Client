# 인증 모듈 아키텍처 및 컴포넌트 책임 정의

## 폴더 구조
```
Assets/Infrastructure/Auth/
├── Core/
│   ├── IAuthManager.cs           # 인증 매니저 인터페이스
│   ├── AuthManager.cs            # 중앙 인증 관리자 (Singleton)
│   ├── ITokenStorage.cs          # 토큰 저장소 추상화
│   ├── IRefreshScheduler.cs      # 토큰 갱신 스케줄러 인터페이스
│   └── RefreshScheduler.cs       # 자동 토큰 갱신 관리
├── Models/
│   ├── AccessToken.cs            # Access 토큰 모델
│   ├── RefreshToken.cs           # Refresh 토큰 모델
│   ├── TokenSet.cs              # 토큰 셋 (Access + Refresh)
│   └── AuthState.cs             # 인증 상태 모델
├── OAuth2/
│   ├── IOAuth2Client.cs         # OAuth2 클라이언트 인터페이스
│   ├── OAuth2Client.cs          # OAuth2 PKCE 구현
│   ├── OAuth2Config.cs          # OAuth2 설정
│   └── PKCEHelper.cs            # PKCE 헬퍼
├── Storage/
│   ├── ISecureStorage.cs        # 보안 저장소 추상화
│   ├── Platforms/
│   │   ├── AndroidSecureStorage.cs  # Android Keystore
│   │   ├── IOSSecureStorage.cs      # iOS Keychain
│   │   ├── WindowsSecureStorage.cs  # Windows DPAPI
│   │   ├── MacOSSecureStorage.cs    # macOS Keychain
│   │   └── WebGLCookieStorage.cs    # WebGL 쿠키 저장
│   └── SecureStorageFactory.cs  # 플랫폼별 저장소 팩토리
├── WebGL/
│   ├── IWebGLCookieBridge.cs    # WebGL 쿠키 브리지 인터페이스
│   ├── WebGLCookieBridge.cs     # WebGL 쿠키 처리
│   └── BFFToggle.cs             # BFF 모드 토글
└── Integration/
    ├── HttpClientAuthExtension.cs  # HttpApiClient 인증 확장
    └── SessionManagerExtension.cs  # SessionManager 인증 연동
```

## 컴포넌트 책임 정의

### 1. AuthManager (중앙 인증 관리자)
**책임:**
- 전체 인증 플로우 조정
- 로그인/로그아웃 상태 관리
- HttpApiClient와 토큰 연동
- 인증 이벤트 발행

**메서드:**
- `InitializeAsync()` - 초기화 및 저장된 토큰 복원
- `LoginAsync()` - OAuth2 로그인 시작
- `LogoutAsync()` - 로그아웃 및 토큰 무효화
- `IsAuthenticated` - 인증 상태 확인
- `GetValidAccessTokenAsync()` - 유효한 Access 토큰 반환 (자동 갱신 포함)

### 2. ITokenStorage (토큰 저장소)
**책임:**
- 플랫폼별 토큰 보안 저장/복원
- Access 토큰은 메모리만, Refresh 토큰은 보안 저장소

**메서드:**
- `StoreRefreshTokenAsync(RefreshToken token)` - Refresh 토큰 저장
- `LoadRefreshTokenAsync()` - Refresh 토큰 로드
- `ClearRefreshTokenAsync()` - Refresh 토큰 삭제
- `StoreAccessTokenInMemory(AccessToken token)` - Access 토큰 메모리 저장
- `GetAccessTokenFromMemory()` - 메모리에서 Access 토큰 반환

### 3. IOAuth2Client (OAuth2 클라이언트)
**책임:**
- OAuth2 PKCE 플로우 처리
- 인증 URL 생성 및 브라우저 오픈
- Authorization Code 교환

**메서드:**
- `StartAuthorizationAsync()` - 인증 시작 (브라우저 오픈)
- `ExchangeCodeForTokensAsync(string code, string state)` - 코드를 토큰으로 교환
- `RefreshTokenAsync(RefreshToken refreshToken)` - 토큰 갱신

### 4. IRefreshScheduler (토큰 갱신 스케줄러)
**책임:**
- Access 토큰 만료 전 자동 갱신
- 갱신 실패 시 재시도 및 백오프
- 갱신 불가 시 로그아웃 트리거

**메서드:**
- `ScheduleRefresh(AccessToken token)` - 갱신 스케줄링
- `CancelRefresh()` - 스케줄 취소
- `ForceRefreshAsync()` - 즉시 갱신

### 5. ISecureStorage (보안 저장소)
**책임:**
- 플랫폼별 보안 저장소 추상화
- 암호화된 토큰 저장/복원

**메서드:**
- `StoreAsync(string key, string value)` - 보안 저장
- `LoadAsync(string key)` - 보안 로드
- `DeleteAsync(string key)` - 보안 삭제
- `IsAvailable` - 저장소 사용 가능 여부

### 6. IWebGLCookieBridge (WebGL 쿠키 브리지)
**책임:**
- WebGL에서 HttpOnly 쿠키 처리
- BFF 모드 지원
- CSRF 토큰 관리

**메서드:**
- `SetRefreshTokenCookie(RefreshToken token)` - Refresh 토큰 쿠키 설정
- `GetRefreshTokenFromCookie()` - 쿠키에서 Refresh 토큰 읽기
- `ClearRefreshTokenCookie()` - Refresh 토큰 쿠키 삭제
- `GetCSRFToken()` - CSRF 토큰 반환

## 통합 지점

### HttpApiClient 연동
- AuthManager가 HttpApiClient.SetAuthToken() 호출
- 401 응답 시 AuthManager.RefreshTokenAsync() 자동 호출
- 갱신 실패 시 AuthManager.LogoutAsync() 호출

### SessionManager 연동
- AuthManager 초기화 시 SessionManager와 연결
- 인증 상태 변경 시 SessionManager에 알림
- WebSocket 연결 시 Access 토큰 전달

## 보안 원칙

### Access 토큰
- **메모리에만 저장** (디스크 저장 금지)
- 앱 재시작 시 Refresh 토큰으로 재발급
- 5-15분 수명

### Refresh 토큰
- **플랫폼별 보안 저장소**에만 저장
- 매 사용 시 로테이션 (서버에서 새 토큰 발급)
- 7-30일 수명
- 재사용 감지 시 모든 토큰 무효화

### WebGL 특수 처리
- Refresh 토큰은 HttpOnly 쿠키
- Access 토큰은 메모리만
- BFF 모드 시 토큰을 클라이언트에 노출하지 않음
- CSRF 방지 적용
