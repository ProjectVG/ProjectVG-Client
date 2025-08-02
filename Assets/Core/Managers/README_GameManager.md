# GameManager 패턴 사용법

## 개요
GameManager는 Unity에서 가장 권장되는 매니저 초기화 패턴입니다. 게임의 핵심 매니저들(WebSocketManager, SessionManager, HttpApiClient)의 생명주기를 관리합니다.

## Unity에서 가장 권장되는 이유

### 1. **명확한 책임 분리**
- GameManager: 매니저들의 생명주기 관리
- 각 매니저: 자신의 도메인 로직만 담당

### 2. **의존성 관리**
- 매니저들 간의 의존성을 명확하게 관리
- 초기화 순서 보장 (WebSocketManager → SessionManager → HttpApiClient)

### 3. **확장성**
- 새로운 매니저 추가가 용이
- 설정 변경이 간단

### 4. **디버깅 용이성**
- Inspector에서 매니저 상태 확인 가능
- ContextMenu를 통한 상태 로그 출력

## 설정 방법

### 1. GameManager 프리팹 생성
1. 빈 GameObject 생성
2. GameManager 컴포넌트 추가
3. 프리팹으로 저장

### 2. 씬에 배치
- 첫 번째 씬에 GameManager 프리팹 배치
- DontDestroyOnLoad로 설정되어 씬 전환 시에도 유지

### 3. 설정 옵션
GameManager Inspector에서 설정 가능:
- **Auto Initialize On Start**: Start() 시 자동 초기화 여부
- **Create Managers If Not Exist**: 매니저가 없을 때 자동 생성 여부
- **Manager References**: 각 매니저의 참조 (선택사항)

## 사용법

### 자동 초기화 (기본)
```csharp
// GameManager가 Start()에서 자동으로 초기화
// 별도 코드 작성 불필요
```

### 수동 초기화
```csharp
// GameManager 참조
var gameManager = GameManager.Instance;

// 수동 초기화
gameManager.InitializeGame();

// 초기화 완료 이벤트 구독
gameManager.OnGameInitialized += () => {
    Debug.Log("게임 초기화 완료!");
};

// 초기화 에러 이벤트 구독
gameManager.OnInitializationError += (error) => {
    Debug.LogError($"초기화 실패: {error}");
};
```

### 매니저 접근
```csharp
// GameManager를 통한 접근
var webSocket = GameManager.Instance.WebSocketManager;
var session = GameManager.Instance.SessionManager;
var httpClient = GameManager.Instance.HttpApiClient;

// 또는 직접 접근 (싱글톤)
var webSocket = WebSocketManager.Instance;
var session = SessionManager.Instance;
var httpClient = HttpApiClient.Instance;
```

### 상태 확인
```csharp
// 에디터에서
// GameManager 우클릭 → Log Manager Status

// 코드에서
if (GameManager.Instance.AreManagersReady())
{
    // 모든 매니저가 준비됨
}
```

## 장점

### 1. **Unity 커뮤니티 표준**
- 대부분의 Unity 프로젝트에서 사용
- 개발자들이 익숙한 패턴

### 2. **Inspector 지원**
- 매니저 참조를 Inspector에서 설정 가능
- 실시간 상태 확인 가능

### 3. **명확한 생명주기**
- 초기화 순서 보장
- 종료 시 역순으로 정리

### 4. **에러 처리**
- 초기화 실패 시 명확한 에러 메시지
- 이벤트를 통한 에러 처리

## 다른 방법들과의 비교

| 방법 | 장점 | 단점 | 권장도 |
|------|------|------|--------|
| **GameManager 패턴** | 표준, 명확, 확장성 | 약간의 보일러플레이트 | ⭐⭐⭐⭐⭐ |
| ScriptableObject 초기화 | 자동화, 설정 관리 | 복잡, Resources 의존 | ⭐⭐⭐ |
| 별도 컴포넌트 | 간단, 직관적 | 실수 가능성, 관리 어려움 | ⭐⭐ |

## 결론

**GameManager 패턴**이 Unity에서 가장 권장되는 방식입니다. 이유:

1. **Unity 커뮤니티 표준**: 대부분의 프로덕션 프로젝트에서 사용
2. **명확한 책임 분리**: 각 매니저의 역할이 명확
3. **확장성**: 새로운 매니저 추가가 용이
4. **디버깅 용이성**: Inspector에서 상태 확인 가능
5. **에러 처리**: 명확한 에러 메시지와 처리

이 패턴을 사용하면 프로젝트의 유지보수성과 확장성이 크게 향상됩니다. 