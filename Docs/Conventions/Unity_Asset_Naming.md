# Unity 에셋 네이밍

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
