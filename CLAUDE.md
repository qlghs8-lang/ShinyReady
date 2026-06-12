# 🤖 Claude Code 프로젝트 가이드 - Shiny Ready!

세차 타이쿤 게임 **Shiny Ready!** 에서 Claude Code를 효과적으로 활용하기 위한 종합 가이드

## 🎮 프로젝트 컨텍스트

### 기본 정보
- **이름**: Shiny Ready!
- **장르**: 하이브리드/하이퍼캐주얼 아케이드 타이쿤
- **참고 모델**: Pizza Ready! (Supercent)
- **엔진**: Unity 2022.3 LTS (URP)
- **언어**: C#
- **타겟 플랫폼**: Android / iOS

### 주요 모듈
- **Player** - 플레이어 이동, 조작
- **Car** - 자동차 소환, 상태 관리
- **Cleaning System** - 세차 인터랙션 로직
- **Currency** - 재화 수집 및 관리
- **Zone Unlock** - 구역 해금 시스템

### 폴더 구조
```
Assets/
└── _Project/
    ├── Scripts/
    │   ├── Player/
    │   ├── Car/
    │   ├── Cleaning/
    │   ├── Currency/
    │   ├── Zone/
    │   └── UI/
    ├── Prefabs/
    ├── Scenes/
    ├── Materials/
    ├── Textures/
    └── Audio/
```

## 🚀 Claude Code 활용 예시

### 기능 구현
```
"rules/ai-assistant-rules.md를 참고해서
Player 이동 로직을 Joystick 입력 기반으로 구현해줘"
```

### 버그 수정
```
"Assets/_Project/Scripts/Player/PlayerController.cs의
이동 속도가 프레임레이트에 종속되는 문제를 수정해줘"
```

### 코드 리뷰
```
"Assets/_Project/Scripts/Cleaning/CleaningSystem.cs를 보고
Unity 모바일 최적화 관점에서 문제가 있는지 확인해줘"
```

## ✅ 작업 체크리스트

### 코드 작성 전
- [ ] 관련 모듈 구조 확인 (`Assets/_Project/Scripts/`)
- [ ] 기존 코드 패턴 참조
- [ ] 의존 컴포넌트 확인

### 코드 작성 후
- [ ] Unity 컴파일 오류 없음 확인
- [ ] 모바일 퍼포먼스 고려 여부 확인
- [ ] Null 체크 등 기본 방어 코드 포함 여부

## ⚠️ 핵심 원칙

### 작업 범위
- ✅ **명령한 내용만 수정** - 요청받지 않은 파일/함수 변경 금지
- ✅ **기존 구조 유지** - 일관된 네이밍 및 패턴 준수
- ❌ **자동 리팩토링 금지** - 요청 없는 코드 정리 금지
- ❌ **추가 기능 금지** - 명시적으로 요청하지 않은 기능 구현 금지

### 위험 작업 (사전 확인 필수)
- 🚨 씬(Scene) 파일 직접 수정
- 🚨 Prefab 구조 변경
- 🚨 ScriptableObject 스키마 변경
- 🚨 기존 API 인터페이스 변경

### 절대 금지
- ❌ 명시적 요청 없이 파일 삭제
- ❌ 전체 코드 블록 초기화
- ❌ 요청 범위를 벗어난 수정

> 📋 **상세 규칙 및 작업 절차**: [rules/ai-assistant-rules.md](./rules/ai-assistant-rules.md)

## 🏗️ 개발 가이드라인

### Unity 모바일 최적화 원칙
1. `Update()`에서 `GetComponent<>()` 호출 금지 → 캐싱 사용
2. 오브젝트 풀링(Object Pooling) 적극 활용
3. `FindObjectOfType()` 남용 금지 → 직접 참조 또는 싱글톤 사용
4. 문자열 연산 최소화 (StringBuilder 또는 상수 사용)
5. 물리 연산은 `FixedUpdate()`에서 처리

### 코딩 컨벤션
- **클래스**: PascalCase (예: `PlayerController`)
- **메서드**: PascalCase (예: `MovePlayer()`)
- **필드(private)**: camelCase + 언더스코어 (예: `_moveSpeed`)
- **프로퍼티**: PascalCase (예: `MoveSpeed`)
- **상수**: ALL_CAPS (예: `MAX_SPEED`)

---

**마지막 업데이트**: 2026-04-20
**관리**: Development Team
