# Unity/C# 네이밍 컨벤션 (ProjectVG)

## C# 식별자
| 대상 | 규칙 | 예시 |
|---|---|---|
| 클래스/구조체/열거형/속성/메서드 | PascalCase | ChatManager, MessageType, LoadConfig |
| 인터페이스 | IPascalCase | IChatService |
| 상수(const) | UPPER_SNAKE_CASE | DEFAULT_TIMEOUT_MS |
| 비공개 필드 | _camelCase | _sessionId |
| 직렬화 비공개 필드 | _camelCase | _gain |
| 매개변수/지역변수 | camelCase | messageText |
| 제네릭 매개변수 | TName | TItem, TResponse |

## C# 패턴
| 항목 | 규칙 | 예시 |
|---|---|---|
| 불리언 | Is/Has/Can/Should 접두사 | IsConnected, HasData |
| 이벤트 이름 | OnXxx / XxxChanged | OnMessageReceived, VolumeChanged |
| 비동기 메서드 | Async 접미사, ct 매개변수 | LoadAsync(CancellationToken ct) |
| 네임스페이스 | 폴더 구조 반영, 루트 ProjectVG | ProjectVG.Domain.Chat |
| 파일명 | 공개 루트 타입명과 동일 | ChatManager.cs |

## Unity 클래스
| 범주 | 접미/접두 | 예시 | 용도 |
|---|---|---|---|
| MonoBehaviour | Controller | PlayerController | 기능 제어 |
|  | Manager | AudioManager | 전역 수명/상태 |
|  | Service | ChatService | 외부/비즈 로직 |
|  | View/Presenter | ChatView | UI 표시/중개 |
|  | Bootstrapper/Installer | GameBootstrapper | 초기화/조립 |
| ScriptableObject | Config/Settings/Profile/Definition/Registry/Database | Live2DModelConfig | 데이터/설정 |
| 기타 | Handler/Factory/Pool | WebSocketHandler | 책임 명확화 |

공개 API: 명령형 동사 사용(Initialize/Apply/Load 등)

## Unity 자산
| 타입 | 규칙 | 예시 |
|---|---|---|
| 씬 | PascalCase, 역할 접미 허용(Main/Boot/Loading/Sample) | MainScene, LoadingScene |
| 프리팹 | PascalCase, UI 루트 접두사 Panel/Dialog/HUD | PanelChat, DialogConfirm |
| SO 에셋 | 타입명 기반 + 키 | NetworkConfig_Prod |
| Addressables | Domain/Category/Name | UI/Panels/PanelChat |
| 폴더 | PascalCase 단수형 | Domain/Character/Model |

## UI 위젯
| 위치 | 규칙 | 예시 |
|---|---|---|
| Hierarchy 이름 | 접두사 사용 Panel/Btn/Img/Txt/Input/Scroll/Toggle/Slider/Dropdown | PanelChat, BtnSend |
| 코드 필드명 | 의미 중심 camelCase | sendButton, titleText |

## 예시(요약)
- MonoBehaviour: ScreenTapManager, Live2DModelManagerFacade
- ScriptableObject: Live2DModelConfig, AppEnvironmentConfig
- Prefab: PanelChat, ChatBubbleUI, AudioInputView
- Scene: MainScene, Live2DScene

## 금지/주의
- 범용어 남용: Data/Util/Helper/Manager(무의미 사용)
- 약어 조합: Cfg/Ctrllr/Svc
- 파일명 ≠ 타입명 불일치
- 부정형 이벤트/플래그: NotReady → 긍정형 ShouldWait/IsReady

