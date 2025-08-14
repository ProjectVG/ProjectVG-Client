# 매니저 시스템 가이드 (소규모 팀용)

간단/소규모 기준: 인스펙터 연결 + Initialize 패턴

## 규칙(요약)
| 항목 | 규칙 | 예시 |
|---|---|---|
| 의존성 연결 | SerializedField 인스펙터 참조 | [SerializeField] private AudioManager _audio; |
| 동적 생성 | 비활성 Instantiate → 값 세팅 → Initialize → 활성화 | go.SetActive(false); comp.Initialize(...); go.SetActive(true); |
| 수명주기 | Awake/OnEnable에서 외부 의존 사용 금지, Initialize 이후 사용 | Initialize 호출 순서 보장 |
| 구성 루트 | Bootstrapper 1개, DontDestroyOnLoad | GameBootstrapper |
| 설정 자산 | ScriptableObject 참조 | AppEnvironmentConfig |

## 패턴 1) 정적 의존성(인스펙터 연결)
```csharp
using UnityEngine;

public sealed class GameBootstrapper : MonoBehaviour
{
    [SerializeField] private AudioManager _audioManager;
    [SerializeField] private UIManager _uiManager;

    private void Awake()
    {
        _audioManager.Initialize();
        _uiManager.Initialize();
    }
}

public sealed class UIManager : MonoBehaviour
{
    [SerializeField] private AudioManager _audioManager;

    /// <summary>
    /// 매니저 초기화
    /// </summary>
    public void Initialize() { }
}
```

포인트
- SerializedField로 동일 씬/프리팹 내 의존 연결
- Awake에서 Initialize 순차 호출(간단, 명확)

## 패턴 2) 동적 생성(Initialize 세팅)
```csharp
using UnityEngine;

public sealed class EnemyFactory
{
    public Enemy Spawn(Enemy prefab, Vector3 pos, EnemyStats stats)
    {
        var go = Object.Instantiate(prefab.gameObject, pos, Quaternion.identity);
        go.SetActive(false);
        var enemy = go.GetComponent<Enemy>();
        enemy.Initialize(stats);
        go.SetActive(true);
        return enemy;
    }
}

public sealed class Enemy : MonoBehaviour
{
    /// <summary>
    /// 런타임 파라미터 주입
    /// </summary>
    public void Initialize(EnemyStats stats) { }
}
```

포인트
- Awake/OnEnable 이전 값 주입 보장
- 프리팹 바리언트로 기본값, Initialize로 런타임 값만 주입

## 패턴 3) 경량 서비스 조회(선택)
```csharp
public static class ServiceLocator
{
    private static readonly Dictionary<System.Type, object> _map = new();
    public static void Register<T>(T svc) => _map[typeof(T)] = svc;
    public static T Get<T>() => (T)_map[typeof(T)];
}

public sealed class GameBootstrapper : MonoBehaviour
{
    [SerializeField] private AudioManager _audio;
    private void Awake()
    {
        ServiceLocator.Register(_audio);
        _audio.Initialize();
    }
}
```

포인트
- 작은 규모에서만 사용, 과도한 DI 지양
- 1회 조회 후 캐싱 권장

## 실전 체크리스트
- [ ] 인스펙터 참조 누락 없음(씬/프리팹)
- [ ] Awake에서 Initialize 순서 정의(상위→하위)
- [ ] 동적 객체는 비활성 Instantiate → Initialize 후 활성화
- [ ] ScriptableObject로 설정/리소스 참조 외부화
- [ ] FindObjectOfType 매 프레임 호출 금지(초기 1회 캐싱만)

## 안티 패턴
- 글로벌 싱글톤 남용(하드 의존)
- Awake에서 외부 매니저 즉시 호출(주입 순서 깨짐)
- Resources 남용(필요 최소만)

