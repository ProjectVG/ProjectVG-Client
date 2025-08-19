# Live2D 모델 해상도별 크기 조정 시스템 가이드

## 개요

이 시스템은 다양한 해상도와 디바이스에서 Live2D 모델의 크기를 자동으로 조정하여 일관된 사용자 경험을 제공합니다.

## 주요 구성 요소

### 1. ResolutionModelScaleConfig
- 해상도별 스케일 규칙을 정의하는 ScriptableObject
- 자동 스케일 계산 로직 포함
- PC/모바일 구분 지원

### 2. Live2DModelScaler
- 개별 Live2D 모델에 스케일을 적용하는 컴포넌트
- 해상도 변경 시 자동 스케일 조정
- 원본 크기 보존 및 복원 기능

### 3. ResolutionManager
- 전체 시스템을 관리하는 싱글톤 매니저
- 모든 모델 스케일러를 중앙에서 관리
- 해상도 변경 감지 및 자동 적용

## 사용 방법

### 1. 기본 설정

1. **ResolutionModelScaleConfig 생성**
   ```
   Assets > Create > ProjectVG > Character > Resolution Model Scale Config
   ```

2. **기본 설정 구성**
   - Reference Resolution: 기준 해상도 (예: 1920x1080)
   - Reference Scale: 기준 스케일 (예: 1.0)
   - Scale Mode: 스케일 계산 방식 선택

### 2. 해상도별 규칙 설정

```csharp
// 기본 규칙들 (모바일 기준 최적화)
- 모바일 기본: 0.85x 스케일
- 모바일 세로 (작은 화면): 0.7x 스케일
- 모바일 가로/태블릿 세로: 0.8x 스케일
- 태블릿 가로: 0.9x 스케일
- PC FHD (1920x1080): 1.1x 스케일
- PC QHD 이상: 1.3x 스케일
```

### 3. 자동 적용

1. **CharacterFacade 사용 시**
   - 모델 스케일러가 자동으로 추가됨
   - 별도 설정 불필요

2. **수동 적용**
   ```csharp
   // Live2DModelScaler 컴포넌트 추가
   var scaler = modelObject.AddComponent<Live2DModelScaler>();
   scaler.ApplyScale();
   ```

## 해상도별 상세 설정

### 모바일 환경
- **기준 해상도**: 1080x1920 (세로 모드)
- **모바일 기본**: 0.85x 스케일 (대부분의 모바일 기기)
- **작은 화면**: 0.7x 스케일 (480px 이하 너비)
- **가로 모드**: 0.8x 스케일 (481-768px 너비)
- **고해상도**: 0.9x 스케일 (1440px 이상 너비, 2960px 이상 높이)

### 태블릿 환경
- **태블릿 세로**: 0.8x 스케일 (481-768px 너비)
- **태블릿 가로**: 0.9x 스케일 (769-1024px 너비)

### PC 환경
- **PC FHD**: 1.1x 스케일 (1025-1920px 너비, 1080px 이하 높이)
- **PC QHD+**: 1.3x 스케일 (1921px 이상 너비, 1081px 이상 높이)

## 스케일 계산 방식

### 1. HeightBased
- 화면 높이를 기준으로 스케일 계산
- 세로 모드에 적합

### 2. WidthBased
- 화면 너비를 기준으로 스케일 계산
- 가로 모드에 적합

### 3. AspectRatioBased (권장)
- 종횡비를 고려한 스케일 계산
- 다양한 화면 비율에 대응
- 모바일과 PC 환경 모두에 최적화

## 설정 예시

### 모바일 환경 (기준)
```yaml
Reference Resolution: 1080x1920
Reference Scale: 1.0
Scale Mode: AspectRatioBased
Min Scale: 0.6
Max Scale: 1.5
```

### PC 환경
```yaml
Reference Resolution: 1920x1080
Reference Scale: 1.1
Scale Mode: AspectRatioBased
Min Scale: 0.6
Max Scale: 1.5
```

## 디버깅

### 1. 디버그 정보 활성화
```csharp
// ResolutionManager에서
showDebugInfo = true;

// Live2DModelScaler에서
showDebugInfo = true;
```

### 2. 현재 상태 확인
```csharp
var info = ResolutionManager.Instance.GetCurrentResolutionInfo();
Debug.Log($"해상도: {info.Resolution}, 스케일: {info.Scale}");
```

## 고급 기능

### 1. 수동 스케일 설정
```csharp
var scaler = GetComponent<Live2DModelScaler>();
scaler.SetManualScale(1.5f);
```

### 2. 원본 크기 복원
```csharp
scaler.ResetToOriginalScale();
```

### 3. 동적 규칙 추가
```csharp
// 런타임에 규칙 추가 가능
var config = Resources.Load<ResolutionModelScaleConfig>("ResolutionModelScaleConfig");
// 규칙 수정 후 적용
```

## 주의사항

1. **Resources 폴더**
   - ResolutionModelScaleConfig는 반드시 Resources 폴더에 위치해야 함
   - 기본 경로: `Assets/Resources/ResolutionModelScaleConfig.asset`

2. **성능 고려**
   - 해상도 변경 감지는 Update에서 수행
   - 불필요한 경우 `applyOnResolutionChange = false` 설정

3. **스케일 범위**
   - Min/Max Scale 범위를 적절히 설정
   - 너무 극단적인 값은 피하기

## 실제 사용 예시

### 일반적인 해상도별 결과
```csharp
// iPhone SE (375x667) - 모바일 세로 (작은 화면)
// 스케일: 0.7x

// iPhone 12 (390x844) - 모바일 기본
// 스케일: 0.85x

// 모바일 고해상도 (1440x2960) - 모바일 고해상도
// 스케일: 0.9x

// iPad (768x1024) - 태블릿 세로
// 스케일: 0.8x

// iPad 가로 (1024x768) - 태블릿 가로
// 스케일: 0.9x

// PC FHD (1920x1080) - PC FHD
// 스케일: 1.1x

// PC QHD (2560x1440) - PC QHD+
// 스케일: 1.3x
```

### 디버그 정보 확인
```csharp
// 현재 해상도 정보 출력
var info = ResolutionManager.Instance.GetCurrentResolutionInfo();
Debug.Log($"현재 해상도: {info.Resolution}");
Debug.Log($"적용된 스케일: {info.Scale:F2}");
Debug.Log($"종횡비: {info.AspectRatio:F2}");
Debug.Log($"모바일 여부: {info.IsMobile}");
```

## 문제 해결

### 1. 스케일이 적용되지 않는 경우
- ResolutionModelScaleConfig가 Resources 폴더에 있는지 확인
- Live2DModelScaler 컴포넌트가 추가되었는지 확인
- 디버그 정보를 활성화하여 로그 확인

### 2. 스케일이 예상과 다른 경우
- Reference Resolution과 Reference Scale 설정 확인
- Scale Mode 설정 확인
- 해상도별 규칙 우선순위 확인

### 3. 성능 문제
- `autoDetectScalers = false` 설정
- 필요한 경우에만 수동으로 스케일러 등록

### 4. 특정 해상도에서 문제가 있는 경우
```csharp
// 특정 해상도에 대한 스케일 확인
var config = Resources.Load<ResolutionModelScaleConfig>("ResolutionModelScaleConfig");
var testResolution = new Vector2(1920, 1080);
var scale = config.CalculateScaleForResolution(testResolution);
Debug.Log($"1920x1080에서의 스케일: {scale:F2}");
```
