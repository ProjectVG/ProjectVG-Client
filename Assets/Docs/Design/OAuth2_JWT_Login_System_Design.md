# OAuth2 + JWT 로그인 시스템 설계서

## 목표
유니티 JWT+OAuth2 로그인 로직 개발
- 지원 환경: PC + Mobile + WebGL
- JWT 발급 및 인증은 서버에서 처리
- 완료 시 서버에서 JWT 토큰 발급

## 권장 아키텍처 요약
- **권장 아키텍처**: Authorization Code + PKCE, Access 토큰은 메모리 보관, Refresh 토큰은 플랫폼별로 보안 저장소(모바일/PC) 혹은 HttpOnly 쿠키(WebGL).
- **토큰 수명**: Access 5–15분, Refresh 7–30일, Refresh 토큰 로테이션 및 재사용 감지 활성화.
- **서버 책임**: 토큰 발급/회전/무효화 전담, WebGL은 가능하면 BFF(Backend For Frontend)로 토큰 비노출.

## 1. 유니티에서 JWT 토큰 저장 방법 (보안/플랫폼별)

### 공통 원칙
- **Access 토큰**: 절대 디스크에 저장하지 않고 앱 **메모리 내 보관**. 앱 재시작 시 Refresh로 재발급.
- **Refresh 토큰**: 재발급 권한이므로 반드시 **보안 저장소**에 보관. 재사용 감지/로테이션 사용.
- **PlayerPrefs 사용 금지**. 로컬 파일/SQLite 평문 저장 금지.

### 모바일(iOS/Android)
- **iOS**: Keychain 사용.
- **Android**: Android Keystore 기반 보안 저장(EncryptedSharedPreferences 등 래핑 라이브러리).
- Unity에서는 플랫폼별 네이티브 플러그인 또는 검증된 **Secure Storage** 패키지 사용.

### PC(Windows/macOS)
- **Windows**: DPAPI(ProtectedData) 기반 보관 또는 OS 계정 보호 스토어 사용.
- **macOS**: Keychain 사용.
- 동일하게 보안 저장 패키지 또는 네이티브 브리지를 통한 안전 저장.

### WebGL(브라우저)
- **Access 토큰**: 브라우저 저장소(localStorage/sessionStorage/cookie)에 저장 금지. **메모리만**.
- **Refresh 토큰**: **Secure, HttpOnly, SameSite(Lax/Strict), Secure** 쿠키 권장. 클라이언트 JS/Unity 스크립트에서 접근 불가.
- 가능하면 **BFF** 패턴으로 액세스 토큰을 클라이언트에 주지 않고 서버-서버로만 사용.

## 2. JWT Refresh/Access 토큰 방법의 적합성 검토

### 게임에 적합성
매우 보편적이며 적합. 세션 유지/재인증 최소화, 멀티플랫폼 일관성 확보, 서버 무상태 확장성 좋음.

### 주의사항
- **세션 길이**가 긴 게임 특성상 Access는 짧게, **자동 갱신 루틴**을 안정적으로 구현.
- **토큰 회전/재사용 감지**로 탈취 대응.
- **네트워크 불안정/오프라인**을 고려한 재시도/백오프.
- **디바이스 분실/탈옥·루팅 환경**을 고려해 민감 데이터 최소화 및 서버 측 위험 탐지.

## 3. 유니티 OAuth2 + JWT 로그인 흐름 (모바일/PC/WebGL)

### 공통: Authorization Code + PKCE
1. 클라이언트가 **인증 페이지**를 시스템 브라우저(모바일: ASWebAuthenticationSession/Custom Tabs, PC: 시스템 브라우저)로 오픈.
2. 사용자 로그인/동의 → **Authorization Code** 수신(리다이렉트 URI: 앱 스킴 혹은 loopback).
3. 클라이언트가 **PKCE code_verifier**와 함께 토큰 엔드포인트에 **코드 교환**.
4. 서버에서 **Access/Refresh** 발급.
5. 클라이언트:
   - Access: 메모리 보관.
   - Refresh: 플랫폼별 보안 저장소(모바일/PC) 또는 WebGL은 HttpOnly 쿠키(서버 `Set-Cookie`).
6. 만료 전/요청 실패 시 **Refresh로 갱신**, 실패 시 재로그인.

### 모바일
- 시스템 브라우저 + 앱 스킴/Universal Link로 콜백 수신.
- Refresh는 Keychain/Keystore에 저장, Access는 메모리.
- 앱 재시작 시 Refresh 로드 → 사전 갱신(silent sign-in).

### PC
- 시스템 브라우저 + 로컬 loopback(127.0.0.1:port) 또는 커스텀 URL 스킴.
- Refresh는 OS 보안 스토어, Access는 메모리.

### WebGL
- 브라우저 내 표준 OAuth2 Code+PKCE.
- 권장 1: **BFF**로 토큰을 브라우저에 노출하지 않고 서버 세션(HttpOnly 쿠키)만 사용.
- 권장 2: BFF가 어렵다면 Refresh는 **HttpOnly 쿠키**, Access는 메모리, API 호출은 `Authorization` 헤더에 Access.
- **CSRF 방지**: SameSite=Lax/Strict, 필요 시 CSRF 토큰(Double Submit/State), OAuth state 검증.

## 4. "Access는 내부 저장, Refresh는 쿠키 저장"의 보안 효과/과잉 여부

### WebGL
효과적. Refresh를 HttpOnly 쿠키로 두면 XSS에 안전. Access는 메모리에만 존재해 XSS 리스크를 축소.

### 모바일/PC
쿠키는 브라우저 영역이라 앱과 수명/관리 분리. 네이티브 앱에서는 **쿠키 대신 보안 저장소**가 표준적이고 더 간단·안전.

### 결론
멀티플랫폼을 모두 만족하려면:
- WebGL: Refresh = HttpOnly 쿠키, Access = 메모리.
- 모바일/PC: Refresh = 보안 저장소, Access = 메모리.

네이티브에서 Refresh를 쿠키로 관리하려는 것은 **과도/비표준**. 유지보수와 DX가 나빠짐.

## 운영 상 세부 권장사항

### 토큰 정책
- Access: 5–15분, 스코프 최소화.
- Refresh: 7–30일, **매 사용 시 회전**, 이전 토큰 재사용 시 전 계정 위험 플래그.
- 디바이스 바인딩(디바이스 식별자/서명) 및 위치/UA 이상 탐지.

### 네트워크/보안
- 전 구간 **TLS**.
- WebGL: **CSP, XSS 방어**, SameSite 쿠키, 서브도메인 정책 정합.
- 모바일/PC: 루팅/탈옥 탐지 시 민감 기능 제한.

### 클라이언트 구현 포인트
- `UnityWebRequest`의 `CookieHandler` 활용(WebGL은 브라우저 쿠키 자동 연동, 네이티브는 명시 핸들러로 세션 유지 가능).
- 전송 시 `Authorization: Bearer <access>`만 사용. 갱신은 별도 엔드포인트.
- 만료 전 **선제 갱신 스케줄러**(예: 만료 T-60초) + 지수 백오프.
- 실패 시 단일 로그아웃/무효화 경로.

## 최소 체크리스트
- [ ] Access 메모리 보관, Refresh는 플랫폼별 안전 저장.
- [ ] Code+PKCE, Refresh 로테이션/재사용 감지.
- [ ] WebGL은 HttpOnly 쿠키(+가능하면 BFF), CSRF 방어.
- [ ] 모바일/PC는 네이티브 보안 스토어 사용.
- [ ] 만료 전 자동 갱신, 실패 시 재로그인 처리.

## 요약
- Access는 항상 메모리 보관. Refresh는 WebGL은 HttpOnly 쿠키, 모바일/PC는 OS 보안 저장소가 최선.
- 게임에서도 Refresh/Access 방식은 적합하며, PKCE+로테이션+CSRF/XSS 방어로 보강하면 안전하고 확장성 높음.
- 네이티브에서 Refresh를 쿠키로 관리하는 것은 비권장이며 과도. WebGL에서만 쿠키 전략이 실효적.
