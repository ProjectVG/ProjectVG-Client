# 초기 로딩 시스템 설계 (이벤트 기반)

## 개요

앱 실행 시 모든 컴포넌트가 준비될 때까지 로딩 상태를 유지하고, 준비 완료 후 다음 씬으로 전환하는 시스템의 설계 문서입니다.

## 시스템 요구사항

### 1. 초기화 단계별 요구사항

1. **싱글톤 컴포넌트 초기화**
   - 모든 Manager 및 Controller 오브젝트 로드
   - 각 컴포넌트의 초기화 완료 확인

2. **서버 연결 및 인증**
   - 서버와의 WebSocket 연결 수립
   - 기존 로그인 상태 확인 및 자동 로그인
   - 세션 연결 상태 확인

3. **기타 준비 작업**
   - 리소스 로딩
   - 설정 파일 로드
   - 필수 데이터 초기화

### 2. UI/UX 요구사항

- 로딩 중 상태 표시
- 각 단계별 진행률 표시
- 준비 완료 시 "게임 시작" 버튼 활성화
- 페이드 인/아웃 효과로 씬 전환

## 아키텍처 설계

### 선택된 접근 방식: 이벤트 기반 시스템 (GameManager 중심)

**선택 이유:**
- GameManager가 모든 씬에서 지속되는 싱글톤
- StartupManager는 Start 씬에서만 존재
- 이벤트 구독을 통한 효율적인 상태 관리
- 기존 GameManager 시스템과의 완벽한 통합

### 시스템 구성요소

```
GameManager (DontDestroyOnLoad)
├── InitializationPhase 이벤트 발생
├── Progress 이벤트 발생
└── 완료/에러 이벤트 발생
    ↓ (이벤트 구독)
LoadingManager (Start 씬 전용)
├── GameManager 이벤트 구독
├── LoadingUI 제어
└── SceneTransitionManager 호출
    ↓
LoadingUI ← LoadingManager
SceneTransitionManager (싱글톤)
```

### 이벤트 기반 동작 흐름

1. **GameManager**: 초기화 진행하며 이벤트 발생
2. **LoadingManager**: GameManager 이벤트 구독하여 UI 업데이트
3. **LoadingUI**: 진행 상황 표시 및 사용자 상호작용
4. **SceneTransitionManager**: 씬 전환 관리

## 구현된 클래스

### 1. GameManager (확장됨)

```csharp
public enum InitializationPhase
{
    NotStarted,
    InitializingManagers,
    ConnectingToServer,
    LoadingResources,
    Completed
}

public class GameManager : Singleton<GameManager>
{
    // 이벤트
    public event Action<InitializationPhase> OnPhaseChanged;
    public event Action<float> OnProgressChanged;
    public event Action OnGameInitialized;
    public event Action<string> OnInitializationError;
    
    // 비동기 초기화
    public async UniTask InitializeGameAsync();
    
    // 상태 조회
    public InitializationStatus GetInitializationStatus();
}
```

**주요 기능:**
- 단계별 초기화 진행
- 실시간 진행률 계산
- 각 단계마다 이벤트 발생
- 에러 처리 및 알림

### 2. LoadingManager (새로 구현)

```csharp
public class LoadingManager : MonoBehaviour
{
    // GameManager 이벤트 구독
    private void SubscribeToGameManager();
    
    // 초기화 시작
    public async void StartInitialization();
    
    // 게임 시작 (씬 전환)
    public async void StartGame();
    
    // 이벤트 핸들러
    private void OnPhaseChanged(InitializationPhase phase);
    private void OnProgressChanged(float progress);
    private void OnGameInitialized();
}
```

**주요 기능:**
- GameManager 이벤트 구독 관리
- LoadingUI와 연동
- 초기화 완료 시 씬 전환 처리

### 3. LoadingUI (새로 구현)

```csharp
public class LoadingUI : MonoBehaviour
{
    // UI 업데이트
    public void UpdatePhase(InitializationPhase phase);
    public void UpdateProgress(float progress);
    public void ShowStartButton();
    public void ShowError(string errorMessage);
    
    // 애니메이션
    public async UniTask FadeOut();
    private IEnumerator AnimateProgress(float targetProgress);
}
```

**주요 기능:**
- 실시간 진행률 표시
- 단계별 상태 텍스트 업데이트
- 부드러운 진행률 애니메이션
- 에러 상황 표시
- 페이드 아웃 효과

### 4. SceneTransitionManager (새로 구현)

```csharp
public class SceneTransitionManager : Singleton<SceneTransitionManager>
{
    // 씬 전환
    public async UniTask TransitionToScene(string sceneName);
    public async UniTask TransitionToSceneWithProgress(string sceneName, IProgress<float> progress);
    
    // 페이드 효과
    private async UniTask FadeOut();
    private async UniTask FadeIn();
}
```

**주요 기능:**
- 페이드 인/아웃 효과
- 비동기 씬 로딩
- 진행률 피드백 (선택사항)
- DontDestroyOnLoad 관리

## 동작 시퀀스

### 1. 앱 시작 시퀀스

```
1. StartScene 로드
2. LoadingManager 생성 및 GameManager 이벤트 구독
3. LoadingManager.StartInitialization() 호출
4. GameManager.InitializeGameAsync() 실행
   ├── Phase: InitializingManagers (0% → 40%)
   ├── Phase: ConnectingToServer (40% → 80%)
   └── Phase: LoadingResources (80% → 100%)
5. 완료 이벤트 발생 → StartButton 활성화
6. 사용자 버튼 클릭 → SceneTransition 실행
```

### 2. 이벤트 흐름

```
GameManager                    LoadingManager               LoadingUI
    |                              |                        |
    |──OnPhaseChanged──────────────▶|                        |
    |                              |──UpdatePhase──────────▶|
    |                              |                        |
    |──OnProgressChanged───────────▶|                        |
    |                              |──UpdateProgress───────▶|
    |                              |                        |
    |──OnGameInitialized───────────▶|                        |
    |                              |──ShowStartButton──────▶|
```

## 주요 개선사항

### 1. 이벤트 기반 아키텍처
- **분리된 관심사**: GameManager는 초기화, LoadingManager는 UI 관리
- **유연한 확장**: 새로운 구독자 추가 용이
- **생명주기 독립성**: Start 씬 전용 컴포넌트와 전역 싱글톤 분리

### 2. 실시간 피드백
- **진행률 표시**: 0-100% 실시간 업데이트
- **단계별 상태**: 사용자에게 현재 작업 내용 알림
- **부드러운 애니메이션**: 진행률 변화 시 자연스러운 전환

### 3. 에러 처리
- **상세한 에러 정보**: 초기화 실패 시 구체적인 오류 내용 표시
- **복구 가능성**: 부분적 실패 상황에서의 대응 방안
- **사용자 피드백**: 에러 UI를 통한 명확한 상황 전달

## 파일 구조

```
Assets/
├── Core/
│   ├── Managers/
│   │   └── GameManager.cs (확장됨)
│   └── Loading/
│       ├── LoadingManager.cs
│       ├── LoadingUI.cs
│       └── SceneTransitionManager.cs
├── App/
│   └── Scenes/
│       └── StartScene.unity
└── Docs/
    └── Initial_Startup_System_Design.md
```

## 사용법

### 1. Start 씬 설정

1. StartScene에 LoadingManager 컴포넌트 추가
2. LoadingUI 컴포넌트도 같은 오브젝트에 추가
3. UI 요소들 (ProgressBar, StatusText, StartButton 등) 연결
4. 다음 씬 이름 설정

### 2. GameManager 설정

- 기존 GameManager 설정 그대로 사용
- 자동 초기화 옵션 유지 또는 LoadingManager에서 수동 호출

### 3. 씬 전환 설정

- LoadingManager에서 다음 씬 이름 지정
- SceneTransitionManager는 자동으로 싱글톤 생성

## 고려사항

### 1. 성능 최적화
- GameManager 이벤트는 Start 씬에서만 구독
- 메모리 누수 방지를 위한 적절한 구독 해제
- 불필요한 업데이트 최소화

### 2. 확장성
- 새로운 초기화 단계 추가 시 enum만 수정
- 추가 이벤트 리스너 등록 가능
- 플랫폼별 초기화 로직 분리 가능

### 3. 에러 복구
- 네트워크 연결 실패 시 재시도 로직
- 부분적 초기화 실패 상황 처리
- 사용자 선택지 제공 (재시도/오프라인 모드 등)

## 결론

이벤트 기반 아키텍처를 통해 GameManager의 전역적 특성과 LoadingManager의 씬 전용 특성을 효과적으로 분리했습니다. 이를 통해 유지보수성과 확장성을 높이면서도 사용자에게 명확한 초기화 진행 상황을 제공할 수 있습니다.
