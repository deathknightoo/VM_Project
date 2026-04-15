# VM_Project
웰니스 디바이스 부분코드입니다.

### 1-1. 팝업 생성 흐름

```
개발자 요청 → UI_Manager.ShowPopupUI<T>() → 리소스 로드 → 프리팹 인스턴스화 →
UI_Popup 컴포넌트 추가 → Stack에 Push → Canvas 설정 → 화면 표시
```

### 단계별 설명

| 단계 | 설명 |  |
| --- | --- | --- |
| **A. 요청 단계** | 개발자가 제네릭 타입으로 특정 팝업 클래스를 지정하여 호출 |  |
| **B. 경로설정** | ProjectType(VM3S/VM3F)과 CurrentSceneType에 따라 동적으로 프리팹 경로 결정 |  |
| **C. 리소스 로드** | Managers.Resource.Instantiate()를 통해 해당 경로의 프리팹 로드 |  |
| **D. 컴포넌트 구성** | Utils.GetOrAddComponent()로 팝업 스크립트 컴포넌트 연결 |  |
| **E. 스택 관리** | _popupStack.Push(popup)으로 팝업을 스택 최상단에 추가 |  |
| **F. 계층 구조** | @UI_Root를 부모로 설정하여 UI 계층 구조 유지 |  |
| **G. Canvas 설정** | SetCanvas()로 렌더 모드 및 sorting order 설정 |  |

### 1-2. 팝업 초기화 흐름

```
Unity Start() → UI_Base.Start() → UI_Popup.Init() → Canvas 설정 →
파생 클래스 Init() → Bind 메서드 호출 → UI 요소 바인딩 → 이벤트 핸들러 등록
```

### 단계별 설명

| 단계 | 설명 |
| --- | --- |
| **A. Unity 생명주기** | MonoBehaviour의 Start() 메서드가 자동 호출 |
| **B. 기본 초기화** | UI_Base.Init()에서 IsInit 플래그 설정으로 중복 초기화 방지 |
| **C. Canvas 구성** | UI_Popup.Init()에서 Managers.UI.SetCanvas() 호출 |
| **D. 요소 바인딩** | BindObject(), BindButton(), BindTmpText() 등으로 UI 요소를 Dictionary에 저장 |
| **E. 이벤트 등록** | BindEvent()를 통해 버튼 클릭 등의 이벤트 핸들러 연결 |
| **F. 커스텀 로직** | 파생 클래스별 고유한 초기화 로직 실행 |

### 1-3. 팝업 종료 흐름

| 단계 | 설명 |
| --- | --- |
| **A. 종료 요청** | 버튼 이벤트 또는 외부에서 ClosePopupUI() 호출 |
| **B. 스택 검증** | 스택이 비어있지 않은지 확인 후 Pop() 실행 |
| **C. 안전 검증** | 매개변수가 있는 오버로드의 경우 스택 최상단과 일치 여부 확인 |
| **D. 리소스 해제** | Managers.Resource.Destroy()로 GameObject 제거 |
| **E. 정리 작업** | OnDestroy() → Clear() 순서로 정리 메서드 호출 |
| **F. 상태 복원** | Footer 액션 스택에서 이전 상태로 복원 |
| **G. Order 관리** | _order–로 다음 팝업의 sorting order 조정 |

---

## 🔷 2. 아키텍처 구조 (Architecture)

![아키텍처 구조](attachment:833de843-728c-4b5d-af20-53acd204f13a:image2.png)

아키텍처 구조

### 2-1. 계층 구조

```
UI_Manager (매니저 계층)
    ↓
UI_Base (추상 기본 클래스)
    ↓
UI_Popup / UI_Scene (중간 계층)
    ↓
UI_TrainingGuide_Popup (구체 구현 클래스)
```

### 2-2. 계층별 역할

### A. UI_Manager (관리 계층)

```
역할: UI 시스템의 중앙관리자
책임:
  - 팝업 생성/소멸 총괄
  - 스택 기반 팝업 생명주기 관리
  - Canvas sorting order 자동 관리
  - 프로젝트 타입별 리소스 경로 라우팅
```

### B. UI_Base (기반 계층)

```
역할: 모든 UI의 추상 기본 클래스
책임:
  - UI 요소 바인딩 시스템 제공
  - Dictionary 기반 요소 관리
  - 이벤트 바인딩 유틸리티
  - 생명주기 메서드 정의 (Init(), Clear())l
```

### C. UI_Popup (중간 계층)

```
역할: 팝업 전용 기능 제공
책임:
  - Canvas 자동 설정
  - 자기 자신 종료 메서드
  - 키패드 타입 스위칭 유틸리티
  - Scene 스크립트 연결 가능
```

### D. UI_TrainingGuide_Popup (구현 계층)

```
역할: 실제 비즈니스 로직 구현 (구현 계층 대표 스크립트)
책임:
  - 운동 가이드 화면 표시
  - VideoPlayer 제어
  - 다국어 처리
  - Footer 네비게이션 관리
```

### 2-3. 의존성 구조

```
UI_TrainingGuide_Popup
    ↓ 의존
Managers (싱글톤)
    ├── UI_Manager
    ├── Resource Manager
    ├── Locale Manager
    └── Scene Manager 등등
```

### 의존성 설명

| 항목 | 설명 |
| --- | --- |
| **단방향 의존** | 구체 클래스 → 매니저 → 시스템 순서로 의존 |
| **싱글톤 패턴** | Manager 클래스를 통해 각종 매니저 접근 |
| **느슨한 결합** | 매니저를 통한 간접 접근으로 결합도 감소 |

---

## 🔷 3. 스택 관리 구조 (Stack Management)

![스택 관리 구조](attachment:0f565ca3-59d8-44ae-9bf0-f767e354dcd9:image3.png)

스택 관리 구조

### 3-1. 스택 기반 팝업 관리

```
Stack 구조:
    [Popup3] ← Top (가장 최근)
    [Popup2]
    [Popup1] ← Bottom (가장 먼저 열림)
```

### 3-2. 스택 관리 특징

### LIFO (Last In First Out) 원칙

- 가장 나중에 열린 팝업이 가장 먼저 닫힘
- 팝업의 계층적 표시 순서 보장
- 뒤로가기 동작의 자연스러운 구현

### Sorting Order 자동 관리

```csharp
canvas.sortingOrder = _order; // 10, 11, 12... 순차 증가
_order++;                      // 팝업 열 때마다 증가
_order--;                      // 팝업 닫을 때마다 감소
```

- 각 팝업의 Canvas sorting order가 열린 순서대로 자동 증가
- 나중에 열린 팝업이 항상 위에 표시됨
- 팝업을 닫으면 order가 감소하여 다음 팝업이 올바른 순서를 가짐

### 스택 조작 메서드

| 메서드 | 설명 |
| --- | --- |
| **Push** | ShowPopupUI() 호출 시 자동으로 스택에 추가 |
| **Pop** | ClosePopupUI() 호출 시 최상단 팝업 제거 |
| **Peek** | PeekPopupUI() - 제거하지 않고 최상단 팝업 조회 |
| **Clear** | CloseAllPopupUI() - 모든 팝업 제거 |

### 3-3. Footer 액션 스택

```
Footer Actions:
    [Action3] ← Current
    [Action2]
    [Action1] ← Initial
```

### Footer 스택의 역할

- 각 팝업마다 고유한 뒤로 가기 동작 정의
- 팝업이 열릴 때 액션 Push
- 팝업이 닫힐 때 자동으로 Pop
- 이전 화면의 Footer 상태 복원

---

## 🔷 4. 주요 작동 원리 (Core Mechanisms)

![주요 작동 원리](attachment:95dcf050-0057-48e8-9c6a-faac7ea9ee5c:image4.png)

주요 작동 원리

### 4-1. 제네릭 기반 타입 안정성

```csharp
public T ShowPopupUI<T>(string name = null) where T : UI_Popup
```

### 장점

| 항목 | 설명 |
| --- | --- |
| **컴파일 타임 타입 체크** | 잘못된 타입 전달 시 컴파일 오류 |
| **자동 타입 변환** | 캐스팅 불필요, 반환 시 올바른 타입 보장 |
| **리팩토링 안정성** | 타입 이름 변경 시 자동 추적 |

### 4-2. 동적 경로 결정 시스템

```csharp
switch (Managers.Scene.F_CurrentSceneType)
{
    case Define.F_Scene.Login:
        showGo = Managers.Resource.Instantiate($"UI/Login/{name}");
        break;
    case Define.F_Scene.PlayVT:
        showGo = Managers.Resource.Instantiate($"UI/PlayVT/{name}");
        break;
    // ...
}
```

### 작동방식

| 단계 | 설명 |
| --- | --- |
| **프로젝트 타입 판별** | VM3S 또는 VM3F 확인 |
| **현재 씬 확인** | Scene Manager에서 활성 씬 타입 조회 |
| **경로 매핑** | Scene Type에 따라 적절한 리소스 폴더 선택 |
| **유연성** | 새로운 씬 추가 시 Case문만 추가하면 됨 |

### 4-3. Dictionary 기반 UI 바인딩

```csharp
protected Dictionary<Type, UnityEngine.Object[]> Objects
    = new Dictionary<Type, UnityEngine.Object[]>();
```

### 바인딩 프로세스

**1. Enum 정의:** UI 요소를 enum으로 정의

```csharp
enum Buttons { CloseBtn, EnterBtn }
```

**2. 바인딩 실행:** BindButton(typeof(Buttons)) 호출

```csharp
// Enum 이름으로 자식 오브젝트 검색
objects[i] = Utils.FindChild<Button>(gameObject, "CloseBtn", true);
```

**3. Dictionary 저장:** typeof(Button)을 키로 배열 저장

```csharp
Objects.Add(typeof(Button), objects);
```

**4. 요소 접근:** GetButton((int)Button.CloseBtn)으로 접근

```csharp
return Objects[typeof(Button)][0] as Button;
```

### 장점

- **타입 안정성:** enum 사용으로 오타 방지
- **중앙 관리:** 모든 UI 요소를 한 곳에서 관리
- **유지보수에 좋음:** UI 요소 추가/삭제가 enum 수정만으로 가능

### 4-4. 이벤트 바인딩 시스템

```csharp
GetButton((int)Buttons.EnterBtn).gameObject.BindEvent(() =>
{
    Managers.UI.ClosePopupUI();
});
```

### 작동 매커니즘

- **Extension Method:** BindEvent()는 GameObject의 확장 메서드
- **중복 방지:** -= 연산자로 기존 핸들러 제거 후 += 로 추가
- **람다 표현식** 위주의 간결한 이벤트 핸들러 작성

### 4-5. 생명주기 관리

```
생성: Instantiate → GetOrAddComponent → Init() → Start()
사용: Update (프레임마다 호출)
소멸: ClosePopupUI() → Destroy → OnDestroy() → Clear()
```

### 중요 포인트

| 항목 | 설명 |
| --- | --- |
| **Init() 중복 방지** | IsInit 플래그로 한 번만 실행 |
| **Start() vs Init()** | Start()는 Unity 자동 호출, Init()은 명시적 초기화 |
| **OnDestroy() 활용** | Footer 액션 복원 등 정리 작업 수행 |
| **Clear() 메서드** | 리소스 해제 및 참조 제거 |

---

## 🏗️ 5. 핵심 설계 패턴

| 패턴 | 적용 내용 |
| --- | --- |
| **매니저 패턴** | UI_Manager가 모든 UI 생성/소멸 총괄, 싱글톤 Managers 클래스를 통한 전역 접근 |
| **템플릿 메서드 패턴** | UI_Base.Init()에서 기본 초기화 로직, 파생 클래스에서 override하여 확장 |
| **컴포지트 패턴** | @UI_Root 하위에 모든 UI 요소 배치, 계층적 UI 구조 관리 |
| **스택 패턴** | 팝업 및 Footer 액션의 LIFO 관리, 뒤로가기 동작의 자연스러운 구현 |

---

## ⚠️ 6. 주의사항 및 Best Practice

### 사용 시 주의사항

- **Stack 검증:** 팝업을 닫기 전 스택이 비어 있는지 확인
- **Order 관리:** 수동으로 sorting order를 변경하지 말 것
- **Footer 액션:** 팝업 소멸 시 자동 Pop되므로 수동 제거 불필요
