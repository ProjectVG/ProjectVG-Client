# 커밋 메시지 규칙

## 구성 요소
| 항목 | 필수 | 설명 | 예시 |
|---|---|---|---|
| Type | 예 | Commit의 종류(소문자). 이모지 사용 금지 | `feat`, `fix`, `refactor` |
| Scope | 선택 | Commit의 범위(기능/함수/페이지/API 등) | `login`, `signup`, `network` |
| Subject | 예 | 제목은 간결하게, 명사형 어미로 종료 | `회원가입 기능 추가` |
| Body | 선택 | 왜/어떻게 변경했는지 요약 | 변경 배경, 접근, 영향 범위 |
| Footer | 선택 | 이슈 트래킹/참고 사항 | `Closes #123` |

## 헤더 예시
```
<type>(optional scope): <subject>
```

- 주의: 이모지 사용 금지, type은 전부 소문자 (예: `feat:`, `fix(login):`)

## 예시
| type | 예시 메시지 |
|---|---|
| feat | `feat(login/signup): 회원가입 기능 추가` |
| fix | `fix(login): 로그인 기능 수정` |
| style | `style: 코드 포맷 변경` |
| refactor | `refactor(signup): 회원 가입 로직 개선` |
| file | `file: 이미지 파일 추가` |
| test | `test: 테스트 코드 추가` |
| docs | `docs: README.md 업데이트` |
| remove | `remove: 사용하지 않는 파일 제거` |
| ci | `ci: 자동 배포 스크립트 변동` |
| release | `release: 릴리즈 버전 1.0.3` |
| chore | `chore: 설정파일 수정` |

## 메세지 구조
```
<type>(optional scope): <subject>

[optional body]

[optional footer(s)]
```
