# 인게임 디버그 콘솔 사용 가이드

## 개요
인게임에서 Debug.Log 메시지를 실시간으로 확인할 수 있는 관리자 콘솔입니다.

## 주요 기능
- **실시간 로그 표시**: 모든 Debug.Log 메시지를 실시간으로 확인
- **백그라운드 로깅**: 콘솔이 숨겨져 있어도 로그 수집 (설정 가능)
- **키워드 필터링**: 특정 키워드가 포함된 로그만 표시
- **오브젝트 풀링**: 성능 최적화를 위한 로그 엔트리 오브젝트 재사용
- **자동 로그 정리**: 설정된 시간이 지난 로그 자동 삭제
- **설정 옵션**: 타임스탬프, 로그 타입, 자동 스크롤 등 설정 가능

## UI 구성 방법

### 1. 기본 UI 구조
```
Canvas (Screen Space - Overlay)
└── ConsolePanel (GameObject)
    ├── Background (Image)
    ├── ScrollView (ScrollRect)
    │   ├── Viewport
    │   │   └── Content (LogContentParent)
    │   │       └── [LogEntry Objects - 동적 생성]
    │   └── Scrollbar
    └── ControlPanel (HorizontalLayoutGroup)
        ├── ClearButton (Button)
        ├── ToggleButton (Button)
        └── FilterInput (InputField)
```

### 2. Log Entry Prefab 구조
```
LogEntryPrefab (GameObject)
└── LogText (TextMeshProUGUI)
    - Font: Consolas 또는 Monospace
    - Font Size: 12
    - Color: 흰색 (기본)
    - Alignment: Top-Left
    - Word Wrap: 활성화
```

### 3. 컴포넌트 설정
1. **InGameDebugConsole** 스크립트를 ConsolePanel에 추가
2. **LogEntryPrefab** 스크립트를 로그 엔트리 프리팹에 추가
3. **DebugConsoleSettings** ScriptableObject 생성:
   - Project 창에서 우클릭 → Create → ProjectVG → Debug Console Settings
   - 설정값들을 원하는 대로 조정
   - **Pooling Settings**: 오브젝트 풀링 설정 (Pool Size, Use Object Pooling)
4. 인스펙터에서 UI 컴포넌트들을 연결:
   - `_consolePanel`: ConsolePanel GameObject
   - `_scrollRect`: ScrollRect 컴포넌트
   - `_logContentParent`: Content Transform (로그 엔트리들이 생성될 부모)
   - `_logEntryPrefab`: LogEntryPrefab GameObject
   - `_clearButton`: Clear 버튼
   - `_toggleButton`: Toggle 버튼
   - `_filterInput`: Filter InputField
   - `_settings`: 생성한 DebugConsoleSettings ScriptableObject

### 4. 권장 UI 설정
- **ConsolePanel**: 
  - Anchor: Top-Stretch
  - Size: Width 100%, Height 300
  - Background: 반투명 검은색
- **LogContentParent**: 
  - **VerticalLayoutGroup** (자동 설정됨)
  - Spacing: 2
  - Child Control Height: true
  - Child Force Expand Height: false
  - Padding: 5, 5, 5, 5
  - **ContentSizeFitter** (자동 설정됨)
    - Vertical Fit: Preferred Size
    - Horizontal Fit: Preferred Size
- **LogEntryPrefab**: 
  - Font: Consolas 또는 Monospace
  - Font Size: 12
  - Color: 흰색 (기본)
  - Alignment: Top-Left
  - Word Wrap: 활성화
  - **ContentSizeFitter** (자동 추가됨)
    - Vertical Fit: Preferred Size

## 사용 방법

### 입력 방법
- **PC**: F12 키로 콘솔 토글 (보이기/숨기기)
- **모바일**: 설정된 개수의 손가락 동시 터치로 콘솔 토글 (기본값: 3개)

### 기능
1. **로그 필터링**: FilterInput에 키워드 입력
2. **로그 지우기**: Clear 버튼 클릭
3. **콘솔 토글**: Toggle 버튼으로 콘솔 보이기/숨기기
4. **설정 관리**: DebugConsoleSettings ScriptableObject에서 모든 설정 관리

### 주요 설정 항목
- **Console Settings**: 기본 콘솔 동작 설정 (백그라운드 로깅 포함)
- **Input Settings**: 모바일 입력 설정 (터치 개수, 활성화 여부)
- **Pooling Settings**: 오브젝트 풀링 성능 최적화 설정
- **Filter Settings**: 로그 필터링 설정
- **UI Settings**: 폰트 크기, 색상 등 UI 관련 설정

## 코드 예시

```csharp
// 디버그 콘솔 참조
InGameDebugConsole debugConsole = FindObjectOfType<InGameDebugConsole>();

// 특정 키워드로 필터링
debugConsole.SetFilter("ChatManager");

// 로그 지우기
debugConsole.ClearLogs();

// 콘솔 토글
debugConsole.ToggleConsole();
```

## 성능 최적화
- **Max Log Lines**: ScriptableObject에서 설정 (기본값 1000)
- **Auto Clear Old Logs**: 설정된 시간이 지난 로그 자동 삭제
- **Max Visible Logs**: 화면에 표시할 최대 로그 수 제한
- **Filtering**: 불필요한 로그 숨김으로 성능 향상
- **Individual Text Objects**: 각 로그를 개별 UI 요소로 표시하여 성능 향상
- **Object Pooling**: 로그 엔트리 오브젝트 재사용으로 메모리 할당 최소화
  - Pool Size: 풀에 생성할 오브젝트 개수 (기본값 100)
  - Use Object Pooling: 풀링 사용 여부 (기본값 true)
- **Background Logging**: 콘솔이 숨겨져 있어도 로그 수집 (기본값 true)
- **Lazy Pool Initialization**: 콘솔이 처음 열릴 때만 오브젝트 풀 초기화 (성능 최적화)

### 오브젝트 풀링 동작 방식
1. **초기화**: 설정된 Pool Size만큼 로그 엔트리 오브젝트를 미리 생성하고 비활성화
2. **사용**: 로그 표시 시 풀에서 오브젝트를 가져와서 활성화하고 내용 설정
3. **순서 제어**: `SetAsLastSibling`을 사용하여 항상 마지막에 배치하여 순서 보장
4. **반환**: 로그 업데이트 시 사용된 오브젝트를 풀로 반환 (비활성화)
5. **안전장치**: 풀이 비어있으면 새로 생성하여 동작 보장

### 풀링 성능 향상 효과
- **메모리 할당 감소**: 오브젝트 생성/파괴 없이 재사용
- **GC 압박 감소**: 가비지 컬렉션 호출 횟수 감소
- **반응성 향상**: 로그 업데이트 시 지연 시간 감소
- **메모리 사용량 안정화**: 일정한 메모리 사용량 유지

### 백그라운드 로깅 성능 최적화
- **최소 처리**: 백그라운드에서는 로그 메시지만 저장 (스택트레이스 제외)
- **지연 초기화**: 오브젝트 풀을 콘솔이 처음 열릴 때만 초기화
- **UI 업데이트 제외**: 백그라운드에서는 UI 업데이트 없이 로그만 수집
- **메모리 효율성**: 백그라운드 로그는 최소한의 정보만 저장하여 메모리 사용량 최소화

## 주의사항
1. 빌드 시에는 디버그 콘솔을 비활성화하는 것을 권장
2. 민감한 정보가 로그에 포함되지 않도록 주의
3. 모바일에서는 3개 손가락 동시 터치로 콘솔 제어 (설정에서 변경 가능)
4. 오브젝트 풀링 사용 시 Pool Size를 적절히 설정하여 메모리 사용량 조절
5. 백그라운드 로깅 사용 시 게임 성능에 미치는 영향을 모니터링
6. 성능이 중요한 경우 `InitializePoolOnStart`를 false로 설정하여 지연 초기화 사용 