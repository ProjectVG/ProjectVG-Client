# 컴포넌트 분석 문서

## 개요
이 문서는 ProjectVG-Client의 핵심 컴포넌트들을 분석하고 정리한 문서입니다. Live2D 캐릭터 시스템의 입력 처리, 시선 추적, 터치 인터랙션, 그리고 시스템 관리에 대한 구조를 설명합니다.

## 1. ScreenTapManager - 입력 처리 시스템

### 1.1 개요
`ScreenTapManager`는 플랫폼별 입력(터치/마우스)을 통합 관리하는 싱글톤 클래스입니다.

### 1.2 주요 인터페이스

#### IInputProvider
```csharp
public interface IInputProvider
{
    bool TryGetPosition(out Vector3 position);
}
```
- 현재 입력 위치를 반환하는 인터페이스
- 터치/마우스 입력을 통합 처리

#### IInputUpProvider
```csharp
public interface IInputUpProvider
{
    bool TryGetPosition(out Vector3 position);
}
```
- 터치 종료 시점의 위치만 반환하는 인터페이스
- Raycast 이벤트 처리에 사용

### 1.3 구현 클래스

#### DefaultInputProvider
- **플랫폼별 입력 처리**: iOS/Android는 터치, 데스크톱은 마우스
- **UI 무시 기능**: "IgnoreLookAt" 태그가 달린 UI 클릭 시 입력 무시
- **EventSystem 통합**: Unity UI 시스템과 연동하여 UI 오버레이 감지

#### DefaultInputUpProvider
- **터치 종료 감지**: 터치/마우스 버튼 해제 시점만 감지
- **Raycast 이벤트**: 터치 종료 시점에만 이벤트 발생

### 1.4 주요 기능

#### LookAt 기능
```csharp
public bool TryGetLookDirection(out Vector3 lookDir)
```
- 스크린 좌표를 Live2D 모델이 사용할 방향 벡터로 변환
- 뷰포트 좌표계로 정규화하여 [-1,1] 범위로 변환

#### Raycast 기능
```csharp
public bool TryGetTapUpPosition(out CubismRaycastHit[] hitResults)
```
- 터치 종료 시점에만 Raycast 수행
- Live2D 모델의 특정 영역 터치 감지

## 2. SystemManager - 시스템 통합 관리

### 2.1 개요
Live2D 캐릭터 시스템의 전체적인 초기화와 관리를 담당하는 싱글톤 클래스입니다.

### 2.2 주요 구성 요소
- **CubismLookTarget**: 시선 추적 타겟
- **Camera**: 메인 카메라 참조
- **ModelConfig**: 모델 설정 데이터
- **AudioSource**: 음성 입력 소스
- **Button**: 표정 변경 버튼

### 2.3 초기화 프로세스

#### Start() 메서드
1. `ScreenTapManager` 초기화
2. `AudioManager` 초기화
3. 모델 초기화 (`ModelInit`)

#### ModelInit() 메서드
1. 기존 모델 제거
2. 새 모델 인스턴스 생성
3. LookAt 설정
4. LipSync 설정
5. Raycast 설정

### 2.4 설정 메서드들

#### SetLockAt()
```csharp
private void SetLockAt(ModelConfig modelConfig)
{
    var lookController = _currentModel.GetComponent<CubismLookController>();
    lookController.Target = cubismLookTarget.gameObject;
    lookController.Damping = modelConfig.LockAtDamping;
    cubismLookTarget.Initialize(modelConfig);
}
```

#### SetLipSync()
```csharp
private void SetLipSync(ModelConfig modelConfig)
{
    var mouthController = _currentModel.GetComponent<CubismAudioMouthInput>();
    mouthController.AudioInput = voiceSource;
    mouthController.Gain = modelConfig.Gain;
    mouthController.Smoothing = modelConfig.Smoothing;
}
```

#### SetRayCast()
```csharp
private void SetRayCast(ModelConfig modelConfig)
{
    var hitHandler = _currentModel.GetComponent<CubismHitHandler>();
    hitHandler.Initialize();
    expressionChangeBtn.onClick.AddListener(hitHandler.ExpressionChange_Btn);
}
```

## 3. CubismLookTarget - 시선 추적 타겟

### 3.1 개요
Live2D 모델의 시선 추적을 위한 타겟 클래스로, `ICubismLookTarget` 인터페이스를 구현합니다.

### 3.2 주요 기능

#### Initialize()
```csharp
public void Initialize(ModelConfig modelConfig)
{
    _modelConfig = modelConfig;
}
```
- 모델 설정 데이터를 저장

#### GetPosition()
```csharp
public Vector3 GetPosition()
{
    if (!ScreenTapManager.Instance.TryGetLookDirection(out var lookDir))
        return Vector3.zero;
    return lookDir * _modelConfig.LookSensitivity;
}
```
- `ScreenTapManager`에서 입력 방향을 받아서 민감도 적용
- 시선 추적 활성화 여부 확인

#### IsActive()
```csharp
public bool IsActive()
{
    return _modelConfig.IsLockAtActive;
}
```
- 모델 설정에 따른 시선 추적 활성화 상태 반환

## 4. CubismHitHandler - 터치 인터랙션 처리

### 4.1 개요
Live2D 모델의 특정 영역 터치를 감지하고 반응을 처리하는 클래스입니다.

### 4.2 주요 구성 요소
- **CubismRaycaster**: 터치 감지를 위한 레이캐스터
- **CubismExpressionController**: 표정 변경을 위한 컨트롤러

### 4.3 초기화
```csharp
public void Initialize()
{
    _raycaster = GetComponent<CubismRaycaster>();
    _expressionController = GetComponent<CubismExpressionController>();
    ScreenTapManager.Instance.SetRaycaster(_raycaster);
}
```

### 4.4 터치 처리

#### Update() 메서드
```csharp
private void Update()
{
    if (ScreenTapManager.Instance.TryGetTapUpPosition(out var hits))
    {
        foreach (var hit in hits)
        {
            if(hit.Drawable is null) continue;
            HandleHit(hit.Drawable.name);
        }
    }
}
```

#### HandleHit() 메서드
```csharp
private void HandleHit(string drawableName)
{
    switch (drawableName)
    {
        case "HitAreaHead":
            Debug.Log("머리 터치 → 표정 변경 or 모션 재생");
            ExpressionChange();
            break;
        case "HitAreaBody":
            Debug.Log("몸통 터치 → 다른 반응");
            break;
    }
}
```

### 4.5 표정 변경 기능

#### ExpressionChange()
```csharp
private void ExpressionChange()
{
    _expressionController.CurrentExpressionIndex =
        GetNextExpressionIndex(_expressionController.CurrentExpressionIndex, 0,
            _expressionController.ExpressionsList.CubismExpressionObjects.Length);
}
```

#### GetNextExpressionIndex()
```csharp
private int GetNextExpressionIndex(int current, int min, int max)
{
    return ((current - min + 1) % (max - min + 1)) + min;
}
```

## 5. ModelConfig - 모델 설정 데이터

### 5.1 개요
ScriptableObject 기반의 모델 설정 데이터 클래스로, Live2D 모델의 다양한 설정을 관리합니다.

### 5.2 주요 설정 카테고리

#### 모델 정보
- **modelName**: 모델 식별용 이름
- **modelDescription**: 모델 설명
- **thumbnail**: 썸네일 이미지

#### 시선 설정
- **lookSensitivity**: 시선 추적 민감도 (0-30)
- **lockAtDamping**: 시선 추적 반응 속도 (0-5)
- **isLockAtActive**: 시선 추적 활성화 여부

#### 립싱크 설정
- **gain**: 음량 증폭 배율 (1-10)
- **smoothing**: 입 움직임 부드러움 (0-1)

#### 모델 프리팹
- **modelPrefab**: 실제 모델 프리팹

### 5.3 사용 예시
```csharp
// natoriConfig.asset 예시
modelName: Natori
modelDescription: "이지적인 집사"
lookSensitivity: 5
lockAtDamping: 0.15
isLockAtActive: true
gain: 10
smoothing: 1
```

## 6. 연관 클래스들

### 6.1 SystemManager
- 앱 전체의 초기화와 관리
- WebSocket, Session, HTTP API 클라이언트 관리
- 시스템 간 의존성 설정

### 6.2 AudioManager
- 음성, BGM, SFX 소스 관리
- 현재는 기본 구조만 구현된 상태

### 6.3 Live2D 프레임워크 클래스들
- **CubismLookController**: 시선 추적 컨트롤러
- **CubismAudioMouthInput**: 립싱크 입력 처리
- **CubismRaycaster**: 터치 감지 레이캐스터
- **CubismExpressionController**: 표정 변경 컨트롤러

## 7. 시스템 아키텍처

### 7.1 데이터 흐름
```
사용자 입력 → ScreenTapManager → CubismLookTarget → CubismLookController → Live2D 모델
터치 종료 → ScreenTapManager → CubismHitHandler → CubismExpressionController → 표정 변경
```

### 7.2 의존성 구조
```
SystemManager
├── ScreenTapManager (싱글톤)
├── AudioManager (싱글톤)
├── CubismLookTarget
├── ModelConfig (ScriptableObject)
└── CubismHitHandler
```

### 7.3 초기화 순서
1. `SystemManager.Start()` 호출
2. `ScreenTapManager.Initialize()` - 카메라 설정
3. `AudioManager.Initialize()` - 오디오 시스템 초기화
4. `ModelInit()` - 모델 생성 및 설정
5. 각 컴포넌트별 초기화 메서드 호출

## 8. 확장성 및 개선 사항

### 8.1 현재 구조의 장점
- **모듈화**: 각 기능이 독립적인 클래스로 분리
- **설정 기반**: ScriptableObject를 통한 데이터 관리
- **플랫폼 호환성**: 터치/마우스 입력 통합 처리
- **확장성**: 인터페이스 기반 설계로 새로운 기능 추가 용이

### 8.2 개선 가능한 부분
- **에러 처리**: 예외 상황에 대한 처리 부족
- **성능 최적화**: Update() 메서드의 최적화 필요
- **설정 검증**: ModelConfig의 유효성 검사 추가
- **로깅 시스템**: 디버깅을 위한 로깅 시스템 구축

## 9. 결론

이 시스템은 Live2D 캐릭터와의 상호작용을 위한 잘 구조화된 아키텍처를 제공합니다. 입력 처리, 시선 추적, 터치 인터랙션, 그리고 설정 관리가 체계적으로 분리되어 있어 유지보수성과 확장성이 우수합니다. 특히 ScriptableObject를 활용한 설정 관리와 인터페이스 기반 설계는 코드의 재사용성과 테스트 용이성을 높여줍니다.





