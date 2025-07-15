
# ProjectVG Unity Client

ProjectVG는 감정을 기억하고 반응하는 **실시간 AI 파트너**입니다.  
이 저장소는 Unity 기반 클라이언트 프로젝트로, Live2D 기반 캐릭터, 대화 UI, 음성 입출력, 네트워크 처리 등을 포함합니다.

---

## 📦 프로젝트 구조

```plaintext
Assets/
├── App/             # 앱 진입점 및 초기화
├── Core/            # 공통 유틸리티 및 시스템
├── Infrastructure/  # 외부 연동 (네트워크, 저장소, SDK 등)
├── Domain/          # 도메인 별 구성 (캐릭터, 채팅, 팝업 등)
├── UI/              # 공통 UI 구성 요소
├── Resources/       # Resources.Load() 대상
├── Addressables/    # Addressable 리소스
├── Plugins/         # 외부 플러그인 (Live2D SDK 등)
├── Editor/          # 커스텀 에디터 코드
├── Tests/           # 유닛 및 에디터 테스트
├── Art/             # 디자인 원본 (버전관리 제외)
└── Docs/            # 설계 및 문서화 자료
```

- [📁 프로젝트 구조 및 파일 네이밍 컨벤션 가이드](./Assets/Docs/ProjectVG_Structure_Guide.md)

---

## 🧩 기술 스택

- **Unity6** 2025
- **Live2D Cubism SDK** for Unity
- **Addressable Asset System**
- **Unity Test Framework** (PlayMode, EditMode)
- **Rest API Client** (UnityWebRequest 기반)
- **Android/iOS Native Bridge (필요시)**

---

## 💬 커밋 컨벤션

```
<type>(optional scope): <subject>
```

| 타입       | 설명                            |
|------------|---------------------------------|
| ✨ Feat     | 새로운 기능 추가                 |
| 🐛 Fix      | 버그 수정                        |
| ⭐️ Style    | 코드 스타일 수정 (로직 변화 없음) |
| ♻️ Refactor | 리팩토링                         |
| ✅ Test     | 테스트 코드 추가/수정            |
| 📝 Docs     | 문서 수정 (README 등)            |
| 🔥 Remove   | 불필요한 파일/코드 제거          |
| 💚 Ci       | CI/CD 관련 변경                  |
| 🔖 Release  | 버전 릴리즈                      |
| 🔧 Chore    | 기타 설정파일 수정               |

**예시:**
```
✨ Feat(Chat): 채팅 입력창 UI 추가
🐛 Fix(Network): 서버 요청 타임아웃 수정
📝 Docs: 프로젝트 구조 설명 추가
```

---

## 📐 코드 컨벤션

- **클래스/파일명**: PascalCase  
- **변수명/메서드명**: camelCase  
- **스크립트 파일 구조**:
  ```csharp
  public class ChatManager : MonoBehaviour
  {
      [SerializeField] private TMP_InputField inputField;

      private void Start()
      {
          InitChat();
      }

      private void InitChat()
      {
          // TODO: 초기화 처리
      }
  }
  ```
- **하드코딩 금지**: Configs 폴더에 JSON 또는 ScriptableObject로 설정 분리
- **공통 매니저**: `Core/` 또는 `Infrastructure/`에 위치

---

## 🧪 테스트

- `Assets/Tests/Editor` → 유닛 테스트
- `Assets/Tests/Runtime` → PlayMode 테스트
- 테스트는 [Unity Test Runner] 사용

---

## 🛠 협업 규칙

- `develop` 브랜치 기준 작업
- 기능 단위 브랜치 생성: `feature/chat-ui`, `fix/network-error`
- PR 머지 전: 테스트 통과 필수
- 문서 및 설명은 `Docs/` 폴더에 저장

