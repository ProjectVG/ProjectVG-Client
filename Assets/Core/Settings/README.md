# ProjectVG 설정 시스템

ProjectVG 클라이언트의 확장 가능한 설정 시스템입니다.

## 개요

이 설정 시스템은 다음과 같은 기능을 제공합니다:

- **확장 가능한 설정 관리**: 다양한 타입의 설정을 쉽게 추가할 수 있습니다
- **일시정지 메뉴**: PC(ESC), 모바일(뒤로가기) 입력 지원
- **실시간 오디오 설정 연동**: AudioManager와 자동 동기화
- **플랫폼별 최적화**: PC, 모바일, WebGL 환경별 최적화
- **UI 애니메이션**: 부드러운 페이드 인/아웃 효과

## 구조

### 핵심 컴포넌트

1. **SettingsManager**: 전체 설정 데이터 관리
2. **PauseMenuManager**: 일시정지 메뉴 제어
3. **PlatformInputManager**: 플랫폼별 입력 처리
4. **AudioSettingsIntegration**: AudioManager와 설정 연동
5. **SettingsSystemManager**: 전체 시스템 통합 관리

### 설정 타입

- `FloatSettingEntry`: 슬라이더 설정 (볼륨, 민감도 등)
- `BoolSettingEntry`: 토글 설정 (켜기/끄기)
- `IntSettingEntry`: 정수 설정 (해상도, 프레임레이트 등)
- `StringSettingEntry`: 문자열 설정 (언어 등)

### 설정 카테고리

- **Audio**: 오디오 관련 설정 (마스터/BGM/SFX/음성/UI 볼륨)
- **Graphics**: 그래픽 관련 설정 (해상도, 전체화면, VSync 등)
- **Gameplay**: 게임플레이 설정 (언어, 자동저장 등)
- **Accessibility**: 접근성 설정 (폰트 크기, 고대비 등)

## 사용법

### 1. 기본 설정

씬에 다음 컴포넌트들을 배치하세요:

```csharp
// 필수 컴포넌트들
SettingsSystemManager (권장)
SettingsManager
PauseMenuManager
PlatformInputManager
AudioSettingsIntegration (오디오 기능 사용시)
```

### 2. UI 설정

1. **일시정지 메뉴 UI** 생성:
   ```
   Canvas
   └── PauseMenuPanel (PauseMenuUI 컴포넌트)
       ├── ResumeButton
       ├── SettingsButton
       └── QuitButton
   ```

2. **설정 메뉴 UI** 생성:
   ```
   Canvas
   └── SettingsMenuPanel (SettingsMenuUI 컴포넌트)
       ├── TabContainer (카테고리 탭들)
       ├── SettingsContainer (설정 항목들)
       └── ButtonContainer (적용/취소/기본값 버튼들)
   ```

3. **설정 UI 프리팹** 생성:
   - FloatSetting Prefab (FloatSettingUI 컴포넌트)
   - BoolSetting Prefab (BoolSettingUI 컴포넌트)

### 3. 코드 사용 예제

#### 설정 값 가져오기
```csharp
// 특정 설정 값 가져오기
float masterVolume = SettingsManager.Instance.GetFloatSetting("Audio_MasterVolume", 1.0f);
bool fullscreen = SettingsManager.Instance.GetBoolSetting("Graphics_Fullscreen", true);

// 설정 객체 직접 가져오기
var volumeSetting = SettingsManager.Instance.GetSetting<FloatSettingEntry>("Audio_MasterVolume");
if (volumeSetting != null)
{
    volumeSetting.SetFloatValue(0.8f);
}
```

#### 설정 변경 이벤트 구독
```csharp
private void Start()
{
    var settingsManager = SettingsManager.Instance;
    settingsManager.OnCategorySettingsChanged += OnAudioSettingsChanged;
}

private void OnAudioSettingsChanged(SettingCategory category)
{
    if (category == SettingCategory.Audio)
    {
        // 오디오 설정이 변경되었을 때의 처리
        Debug.Log("오디오 설정이 변경되었습니다.");
    }
}
```

#### 일시정지 메뉴 제어
```csharp
// 일시정지 메뉴 열기/닫기
PauseMenuManager.Instance.TogglePause();

// 설정 메뉴 열기
PauseMenuManager.Instance.OpenSettings();

// 이벤트 구독
PauseMenuManager.Instance.OnPauseStateChanged += OnPauseStateChanged;

private void OnPauseStateChanged(bool isPaused)
{
    Debug.Log($"일시정지 상태: {isPaused}");
}
```

### 4. 새로운 설정 추가

1. **SettingsManager.cs**의 해당 카테고리 생성 메서드에 설정 추가:
```csharp
private void CreateAudioSettings()
{
    var audioGroup = new SettingGroup(SettingCategory.Audio, "오디오");
    
    // 새로운 설정 추가
    var newSetting = new FloatSettingEntry(
        "Audio_NewVolume", 
        "새 볼륨", 
        "새로운 볼륨 설정", 
        1.0f, 0f, 1f
    );
    audioGroup.AddSetting(newSetting);
    
    // 기존 코드...
}
```

2. **AudioSettingsIntegration.cs**에서 새 설정 처리 추가:
```csharp
private void ApplyAudioSetting(BaseSettingEntry setting)
{
    switch (setting.Key)
    {
        case "Audio_NewVolume":
            if (setting is FloatSettingEntry newVolume)
            {
                // 새 볼륨 설정 적용 로직
            }
            break;
        // 기존 케이스들...
    }
}
```

### 5. 플랫폼별 설정

#### PC (Windows/Mac/Linux)
- ESC 키로 일시정지 메뉴 열기
- 마우스 클릭으로 UI 조작

#### 모바일 (Android/iOS)
- 뒤로가기 버튼으로 일시정지 메뉴 열기
- 터치로 UI 조작

#### WebGL
- 설정에 따라 ESC 키 활성화/비활성화 가능
- 브라우저 환경에 최적화

## ScriptableObject 설정

`Resources/` 폴더에 설정 파일들을 생성할 수 있습니다:

1. **SettingsConfig**: 기본 설정 값들
2. **SettingsPresetsConfig**: 프리셋 설정들 (저성능/균형/고성능 등)

## 디버그 기능

### 개발자 메뉴
각 컴포넌트에는 Context Menu가 제공됩니다:
- `Toggle Pause (Debug)`
- `Apply Audio Settings (Debug)`
- `Show System Health`

### 로그 출력
`SettingsSystemManager`에서 디버그 로그를 활성화하면 상세한 초기화 과정을 확인할 수 있습니다.

## 확장성

### 새로운 설정 타입 추가
1. `BaseSettingEntry`를 상속한 새 클래스 생성
2. 해당 UI 컴포넌트 생성 (`ISettingUI` 인터페이스 구현)
3. `SettingsMenuUI`에서 프리팹 등록

### 새로운 카테고리 추가
1. `SettingCategory` enum에 새 카테고리 추가
2. `SettingsManager`에서 해당 카테고리 생성 메서드 구현
3. 필요시 통합 컴포넌트 생성

## 주의사항

1. **초기화 순서**: `SettingsSystemManager`를 사용하면 자동으로 올바른 순서로 초기화됩니다
2. **Singleton 패턴**: 모든 매니저는 Singleton이므로 `Instance`로 접근하세요
3. **이벤트 구독 해제**: `OnDestroy`에서 이벤트 구독을 해제하세요
4. **PlayerPrefs**: 설정 값은 자동으로 PlayerPrefs에 저장됩니다

## 파일 구조

```
Assets/Core/Settings/
├── BaseSettingEntry.cs          # 설정 엔트리 기본 클래스들
├── SettingsManager.cs           # 설정 관리자
├── SettingsConfig.cs            # ScriptableObject 설정
├── PauseMenuManager.cs          # 일시정지 메뉴 관리자
├── AudioSettingsIntegration.cs  # 오디오 설정 통합
├── SettingsSystemManager.cs     # 전체 시스템 통합 관리자
└── README.md                    # 이 문서

Assets/Core/Input/
└── PlatformInputManager.cs      # 플랫폼별 입력 처리

Assets/UI/Scripts/
├── PauseMenuUI.cs               # 일시정지 메뉴 UI
├── SettingsMenuUI.cs            # 설정 메뉴 UI
├── FloatSettingUI.cs            # Float 설정 UI
└── BoolSettingUI.cs             # Bool 설정 UI
```

## 추가 개선 사항

향후 추가할 수 있는 기능들:

1. **키 바인딩 설정**: 사용자 정의 키 설정
2. **그래픽 설정 통합**: 그래픽스 설정 자동 적용
3. **설정 프로파일**: 여러 설정 프로파일 지원
4. **클라우드 동기화**: 설정의 클라우드 저장/로드
5. **설정 검증**: 잘못된 설정 값 자동 보정
6. **접근성 향상**: 화면 읽기 도구 지원

이 설정 시스템은 확장성과 유지보수성을 고려하여 설계되었으므로, 프로젝트 요구사항에 맞게 쉽게 확장할 수 있습니다.