# 새로운 매니저 추가 체크리스트

## 📋 필수 체크리스트

새로운 매니저를 추가할 때 다음 항목들을 순서대로 확인하세요:

### ✅ 1. 매니저 클래스 생성

- [ ] `IManager` 인터페이스 구현
- [ ] `Initialize()` 메서드 구현
- [ ] `Shutdown()` 메서드 구현
- [ ] 필요한 이벤트 정의
- [ ] 적절한 네임스페이스 사용 (`ProjectVG.Core.Managers`)

```csharp
public class YourManager : MonoBehaviour, IManager
{
    public void Initialize() { /* 구현 */ }
    public void Shutdown() { /* 구현 */ }
}
```

### ✅ 2. ManagerRegistry 수정

- [ ] Header에 SerializeField 변수 추가
- [ ] Public 프로퍼티 추가
- [ ] `InitializeAllManagers()`에 초기화 메서드 호출 추가
- [ ] Private 초기화 메서드 구현

```csharp
[SerializeField] private YourManager _yourManager;
public YourManager YourManager => _yourManager;

public void InitializeAllManagers()
{
    // 기존 초기화들...
    InitializeYourManager(); // 추가
}

private void InitializeYourManager() { /* 구현 */ }
```

### ✅ 3. GameManager 수정

- [ ] Public 접근자 프로퍼티 추가

```csharp
public YourManager YourManager => _managerRegistry?.YourManager;
```

### ✅ 4. (선택사항) 의존성 주입 설정

- [ ] `DependencyManager.RegisterServices()`에 등록 추가
- [ ] `DependencyManager.InjectDependencies()`에 주입 추가 (필요시)

### ✅ 5. 테스트 및 검증

- [ ] 컴파일 에러 없음 확인
- [ ] 런타임에서 매니저 생성 확인
- [ ] 초기화 로그 확인
- [ ] `GameManager.Instance.LogManagerStatus()` 실행하여 상태 확인

## 🚀 고급 옵션 체크리스트

### ✅ 커스텀 초기화 단계 (필요시)

- [ ] `InitializationPhase` enum에 새 단계 추가
- [ ] `InitializationManager.InitializeAsync()`에 새 단계 추가
- [ ] 진행률 계산 업데이트

### ✅ 매니저간 의존성 (필요시)

- [ ] `[Inject]` 어트리뷰트 사용
- [ ] `DependencyManager`에서 의존성 등록
- [ ] 초기화 순서 고려

### ✅ 이벤트 시스템 (추천)

- [ ] 매니저에서 적절한 이벤트 발생
- [ ] 이벤트 구독/해제 예시 작성
- [ ] 메모리 누수 방지 (이벤트 해제)

## 📝 실제 예시: AudioManager

```csharp
// 1. 매니저 클래스
public class AudioManager : MonoBehaviour, IManager
{
    public void Initialize() { /* 오디오 시스템 초기화 */ }
    public void Shutdown() { /* 리소스 정리 */ }
    public void PlaySound(string soundName) { /* 사운드 재생 */ }
}

// 2. ManagerRegistry 추가
[SerializeField] private AudioManager _audioManager;
public AudioManager AudioManager => _audioManager;

// 3. GameManager 접근자
public AudioManager AudioManager => _managerRegistry?.AudioManager;

// 4. 사용 예시
GameManager.Instance.AudioManager?.PlaySound("button_click");
```

## 🐛 일반적인 실수들

### ❌ 하지 말아야 할 것들

- **GameManager에 복잡한 로직 추가**
  ```csharp
  // 나쁜 예
  public void PlaySound(string name) { /* 복잡한 로직 */ }
  
  // 좋은 예  
  public AudioManager AudioManager => _managerRegistry?.AudioManager;
  ```

- **다른 매니저에 직접 의존**
  ```csharp
  // 나쁜 예
  public class UIManager : MonoBehaviour
  {
      private AudioManager _audioManager; // 직접 참조
  }
  
  // 좋은 예
  public class UIManager : MonoBehaviour
  {
      [Inject] private AudioManager _audioManager; // 의존성 주입
  }
  ```

- **초기화 순서 무시**
  ```csharp
  // 나쁜 예
  void Start()
  {
      GameManager.Instance.AudioManager.PlaySound("start"); // 초기화 전 호출
  }
  
  // 좋은 예
  void Start()
  {
      GameManager.Instance.OnGameInitialized += () => {
          GameManager.Instance.AudioManager.PlaySound("start");
      };
  }
  ```

## 🔍 디버깅 가이드

### 매니저가 null인 경우

1. **초기화 전에 접근했는지 확인**
   ```csharp
   GameManager.Instance.OnGameInitialized += () => {
       // 여기서 매니저 사용
   };
   ```

2. **ManagerRegistry에 제대로 등록했는지 확인**
   ```csharp
   // Inspector에서 매니저 참조가 설정되었는지 확인
   ```

3. **초기화 메서드가 호출되는지 확인**
   ```csharp
   // 로그를 통해 InitializeYourManager()가 호출되는지 확인
   ```

### 의존성 주입이 안 되는 경우

1. **서비스가 등록되었는지 확인**
   ```csharp
   // DependencyManager.RegisterServices()에서 등록 확인
   ```

2. **InjectDependencies가 호출되었는지 확인**
   ```csharp
   // DependencyManager.InjectDependencies()에서 주입 확인
   ```

## 📊 성능 고려사항

- **매니저 수 제한**: 너무 많은 매니저는 복잡성 증가
- **초기화 시간**: 복잡한 초기화는 LoadingResources 단계에서
- **메모리 사용량**: 불필요한 리소스 로딩 방지
- **이벤트 정리**: OnDestroy에서 이벤트 구독 해제

## 📚 참고 자료

- [Manager System Guide](./Manager_System_Guide.md)
- [Unity Singleton Pattern](./Unity_Singleton_Guide.md)
- [Dependency Injection Guide](./Dependency_Injection_Guide.md)

---

> 💡 **꿀팁**: 새로운 매니저를 추가하기 전에 기존 매니저로 해결할 수 있는지 검토해보세요. AudioManager에 UI 사운드 기능을 추가하는 것이 UISoundManager를 새로 만드는 것보다 나을 수 있습니다.
