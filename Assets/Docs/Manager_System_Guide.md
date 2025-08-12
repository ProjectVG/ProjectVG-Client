# 매니저 시스템 사용 가이드

## 개요

리팩토링된 매니저 시스템은 **단일 책임 원칙**에 따라 GameManager의 복잡성을 분산시켜 관리하기 쉽게 만들었습니다.

## 🏗️ 시스템 구조

```
GameManager (퍼사드/조율자)
├── InitializationManager (초기화 전담)
├── ManagerRegistry (매니저 생성/관리)
└── DependencyManager (의존성 주입)
```

### 각 매니저의 역할

| 매니저 | 책임 | 주요 기능 |
|--------|------|-----------|
| **GameManager** | 전체 조율, 퍼사드 | 간단한 인터페이스 제공, 이벤트 중계 |
| **InitializationManager** | 초기화 과정 관리 | 단계별 초기화, 진행률 추적, 이벤트 발생 |
| **ManagerRegistry** | 매니저 생명주기 관리 | 매니저 생성/등록/해제, 상태 조회 |
| **DependencyManager** | 의존성 주입 | 서비스 등록, 의존성 해결 |

## 📖 기본 사용법

### 1. 기존 코드 - 변경 없음!

```csharp
// 여전히 이렇게 사용 가능
GameManager.Instance.InitializeGameAsync();
GameManager.Instance.OnGameInitialized += OnGameReady;

// 매니저 접근도 동일
var webSocket = GameManager.Instance.WebSocketManager;
var session = GameManager.Instance.SessionManager;
```

### 2. 새로운 기능들

```csharp
// 상세한 초기화 상태 조회
var status = GameManager.Instance.GetInitializationStatus();
Debug.Log($"현재 단계: {status.CurrentPhase}");
Debug.Log($"진행률: {status.Progress * 100}%");

// 매니저 준비 상태 확인
if (GameManager.Instance.AreManagersReady())
{
    // 모든 매니저가 준비됨
}
```

## 🔧 새로운 매니저 추가하기

### Step 1: 매니저 클래스 생성

```csharp
using UnityEngine;
using ProjectVG.Core.Managers;

public class AudioManager : MonoBehaviour, IManager
{
    [Header("Audio Settings")]
    [SerializeField] private float _masterVolume = 1.0f;
    
    public float MasterVolume => _masterVolume;
    
    public void Initialize()
    {
        Debug.Log("[AudioManager] 초기화 완료");
    }
    
    public void Shutdown()
    {
        Debug.Log("[AudioManager] 종료 처리");
    }
    
    public void PlaySound(string soundName)
    {
        // 사운드 재생 로직
    }
}
```

### Step 2: ManagerRegistry에 추가

```csharp
// ManagerRegistry.cs에 추가
[Header("Manager References")]
[SerializeField] private AudioManager _audioManager;  // 👈 추가

public AudioManager AudioManager => _audioManager;   // 👈 추가

public void InitializeAllManagers()
{
    InitializeWebSocketManager();
    InitializeSessionManager();
    InitializeHttpApiClient();
    InitializeAudioManager();  // 👈 추가
}

private void InitializeAudioManager()  // 👈 새 메서드
{
    if (_audioManager == null && _createManagersIfNotExist)
    {
        var audioObj = new GameObject("AudioManager");
        audioObj.transform.SetParent(transform);
        _audioManager = audioObj.AddComponent<AudioManager>();
    }
    
    if (_audioManager != null)
    {
        _managers.Add(_audioManager);
        _audioManager.Initialize();
        Debug.Log("[ManagerRegistry] AudioManager 초기화 완료");
    }
}
```

### Step 3: GameManager에 접근자 추가

```csharp
// GameManager.cs에 추가
public AudioManager AudioManager => _managerRegistry?.AudioManager;
```

### Step 4: (선택사항) 의존성 주입 설정

```csharp
// DependencyManager.cs에 추가 (필요한 경우)
private void RegisterServices(ManagerRegistry managerRegistry)
{
    // 기존 코드...
    
    if (managerRegistry.AudioManager != null)
    {
        _container.Register<AudioManager>(managerRegistry.AudioManager);
        Debug.Log("[DependencyManager] AudioManager 등록 완료");
    }
}
```

## 🎯 실제 사용 예시

### 새로운 AudioManager 사용

```csharp
public class GameController : MonoBehaviour
{
    private void Start()
    {
        // GameManager 초기화 완료 대기
        GameManager.Instance.OnGameInitialized += OnGameReady;
    }
    
    private void OnGameReady()
    {
        // AudioManager 사용
        var audioManager = GameManager.Instance.AudioManager;
        audioManager?.PlaySound("background_music");
        
        Debug.Log($"Audio Volume: {audioManager.MasterVolume}");
    }
}
```

## 🚀 고급 사용법

### 1. 커스텀 초기화 단계 추가

새로운 매니저가 특별한 초기화 과정이 필요한 경우:

```csharp
// InitializationPhase enum에 추가
public enum InitializationPhase
{
    NotStarted,
    InitializingManagers,
    ConnectingToServer,
    LoadingResources,
    InitializingAudio,  // 👈 새 단계 추가
    Completed
}

// InitializationManager.cs에서 단계 추가
private async UniTask InitializeAsync()
{
    // 기존 단계들...
    
    SetPhase(InitializationPhase.InitializingAudio);
    await InitializeAudioAsync();  // 새 단계
    UpdateProgress(0.9f);
    
    SetPhase(InitializationPhase.Completed);
    // ...
}
```

### 2. 매니저간 의존성 처리

```csharp
public class UIManager : MonoBehaviour, IManager
{
    [Inject] private AudioManager _audioManager;  // 의존성 주입
    
    public void ShowMenu()
    {
        _audioManager?.PlaySound("menu_open");
        // UI 표시 로직
    }
}
```

## 📋 체크리스트

새로운 매니저를 추가할 때 확인하세요:

- [ ] `IManager` 인터페이스 구현
- [ ] `ManagerRegistry`에 참조 추가
- [ ] `ManagerRegistry.InitializeAllManagers()`에 초기화 메서드 추가
- [ ] `GameManager`에 접근자 추가
- [ ] (필요시) `DependencyManager`에 의존성 등록
- [ ] (필요시) 새로운 초기화 단계 추가
- [ ] 매니저 상태 로깅에 추가

## 🔍 디버깅 팁

### 1. 매니저 상태 확인

```csharp
// Inspector에서 또는 코드에서 호출
GameManager.Instance.LogManagerStatus();
```

### 2. 초기화 진행 상황 모니터링

```csharp
GameManager.Instance.OnPhaseChanged += (phase) => {
    Debug.Log($"초기화 단계: {phase}");
};

GameManager.Instance.OnProgressChanged += (progress) => {
    Debug.Log($"진행률: {progress * 100:F1}%");
};
```

### 3. 일반적인 문제들

| 문제 | 원인 | 해결책 |
|------|------|--------|
| "Manager가 null" | 초기화 전에 접근 | `OnGameInitialized` 이벤트 대기 |
| "의존성 주입 실패" | 서비스 미등록 | `DependencyManager`에 등록 확인 |
| "초기화 실패" | 매니저 생성 실패 | `_createManagersIfNotExist` 설정 확인 |

## 🎨 베스트 프랙티스

### 1. 매니저 설계 원칙

✅ **해야 할 것**
- 단일 책임 원칙 준수
- `IManager` 인터페이스 구현
- 명확한 Initialize/Shutdown 구현
- 의존성 주입 활용

❌ **하지 말아야 할 것**
- 다른 매니저에 직접 의존
- GameManager에 복잡한 로직 추가
- Singleton 남용

### 2. 네이밍 컨벤션

```csharp
// 좋은 예
AudioManager, NetworkManager, UIManager

// 나쁜 예  
Manager, SoundSystem, AudioController
```

### 3. 이벤트 사용

```csharp
// 매니저에서 이벤트 발생
public class AudioManager : MonoBehaviour, IManager
{
    public event Action<string> OnSoundPlayed;
    
    public void PlaySound(string soundName)
    {
        // 재생 로직
        OnSoundPlayed?.Invoke(soundName);
    }
}
```

## 📚 추가 자료

- [Unity 싱글톤 패턴 가이드](./Unity_Singleton_Guide.md)
- [의존성 주입 사용법](./Dependency_Injection_Guide.md)
- [이벤트 시스템 가이드](./Event_System_Guide.md)

---

> 💡 **팁**: 새로운 매니저를 추가하기 전에 기존 매니저로 해결할 수 있는지 먼저 검토해보세요. 너무 많은 매니저는 오히려 복잡성을 증가시킬 수 있습니다.
