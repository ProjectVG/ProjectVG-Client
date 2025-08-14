# C# 코드 스타일

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

공개 API는 명령형 동사를 사용합니다(Initialize/Apply/Load 등).
