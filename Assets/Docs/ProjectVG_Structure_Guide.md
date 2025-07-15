
# 📁 ProjectVG Unity 클라이언트 구조 가이드

이 문서는 `Assets/` 디렉토리 기준의 Unity 클라이언트 프로젝트 구조 및 디렉토리 용도/명명 규칙을 설명합니다.

---

## 🗂 전체 구조

```
Assets/
├── App/                           # 진입점, 전체 앱 설정
│   ├── Scenes/                    # 메인 씬, 로딩 씬 등
│   └── App.cs                     # 앱 초기화/엔트리 진입

├── Core/                          # 전역 공통 기능
│   ├── Input/                     # 입력 처리 (Touch, Mouse, Key 등)
│   ├── Audio/                     # BGM, SFX, 오디오 매니저
│   ├── Localization/              # 다국어 지원
│   ├── Time/                      # 시간 유틸 (타이머, 딜레이 등)
│   ├── Extensions/                # Unity, LINQ, System 확장 메서드
│   └── Utils/                     # 범용 유틸리티 클래스

├── Infrastructure/               # 외부 서비스, 저장소, 네트워크
│   ├── Data/                      # 로컬/클라우드 저장소 (SaveData, PlayerPrefs)
│   ├── Network/                   # 서버 API 호출, Response DTO
│   └── Bridge/                    # Native, 외부 SDK 연동 (예: Android, iOS 기능)

├── Domain/                        # 도메인(기능)별 묶음
│   ├── Character/
│   │   ├── Model/                 # Live2D 모델 파일들 (.moc3, .json 등)
│   │   ├── View/                  # 캐릭터 Prefab, UI 뷰
│   │   ├── Script/                # 제어 스크립트 (Controller, Motion)
│   │   └── Animation/             # 타임라인, 모션 시퀀스 관리
│   ├── Chat/
│   │   ├── Script/
│   │   ├── View/
│   │   └── Model/
│   ├── Popup/
│   │   └── System/                # 팝업 매니저, 팝업 큐
│   │   └── Instances/             # 실제 팝업 프리팹들
│   └── System/                    # 게임 로직 컨트롤러, FSM 등 (선택)

├── UI/                            # UI 공통 요소
│   ├── Prefabs/                   # 공용 UI 프리팹 (버튼, 아이콘, 툴팁 등)
│   ├── Panels/                    # HUD, 메인패널 등
│   ├── Scripts/                   # UI 상호작용 스크립트
│   ├── Transitions/               # UI 전환 효과 (페이드, 이동 등)
│   └── Fonts/                     # UI 폰트

├── Resources/                     # Resources.Load() 로드 대상
│   ├── Configs/                   # Json 기반 설정파일
│   └── AddressablesDummy/         # Addressable 사용 안 할 때 대체

├── Addressables/                  # 어드레서블 관리용 분리 리소스 (선택)
│   ├── UI/
│   ├── Characters/
│   └── Scenes/

├── Plugins/                       # 외부 라이브러리/Live2D SDK
│   └── Live2D/
│       └── CubismSdkForUnity/

├── Editor/                        # 커스텀 에디터 코드 (.cs만 가능)
│   └── Inspectors/
│   └── PropertyDrawers/
│   └── MenuItems/

├── Tests/                         # 테스트
│   ├── Editor/                    # Editor Test
│   └── Runtime/                   # PlayMode Test, 유닛 테스트

├── Art/                           # 디자인 원본 (PSD, AI 등) - 버전 관리 제외 가능

└── Docs/                          # 문서 (설계, 흐름도, README 등)
```

---

## 📌 네이밍 가이드 요약

- 디렉토리 및 클래스명: **PascalCase**
- 변수 및 메서드: **camelCase**
- JSON 및 설정파일: **snake_case**
- Addressable 키: `/` 구분, 예: `UI/PanelChat`
- UI 오브젝트 접두어: `Panel`, `Btn`, `Txt`, `Img`, `Group` 등

---

## ✅ 활용 예시

- `Domain/Character/Model/` → Live2D `.moc3`, `.json`, 모션 설정
- `Core/Audio/AudioManager.cs` → SFX, BGM 전역 제어
- `Infrastructure/Network/ApiClient.cs` → REST API 통신 처리
- `UI/Panels/PanelMain.prefab` → 메인 화면 UI
- `Tests/Runtime/CharacterMotionTest.cs` → 캐릭터 모션 유닛 테스트

---

> 이 문서는 신규 팀원이 구조를 빠르게 이해하고 규칙대로 작업할 수 있도록 작성된 **구조 및 명명 가이드라인**입니다.
