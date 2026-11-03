# Parry 개발 세션 인계

갱신: 2026-11-04 (Asia/Seoul). 프로젝트: `D:\GitHub\Parry`.

이 문서는 새 세션에서 대화를 이어가기 위한 현재 상태 기록이다. **지금 당장 구현을 시작하라는 지시는 아니다.** 사용자의 다음 요청 범위를 확인하고 실제 파일을 다시 읽어 진행한다.

## 현재 구현 — 2026-11-04 우선 읽기

아래의 1~12절은 10월 28일 기준의 이력이다. 현재 코드·입력·Animator와 충돌하는 경우 이 절과 실제 파일을 우선한다. 이 문서를 읽는 것만으로 다음 구현 범위가 정해지지는 않는다.

### 플레이어 구성과 입력

- 경로는 `Assets/Scripts/Player`. Player 프리팹에 `PlayerInput`, `MyController`, `PlayerCharacter`, `PlayerMovement`, `PlayerAttack`, `PlayerAnimation`, `PlayerStatComponent`가 있다.
- `MyController : PlayerController`가 Input System 콜백을 받는다. 부모 컨트롤러가 캐싱한 `Character`에 Move/Jump/Roll/Parry/Attack을 전달한다. `PlayerCharacter`는 행동 가능 여부와 구르기·패링 상태를 관리한다. 체력이나 물리 계산은 각 컴포넌트에 둔다.
- `PlayerMovement`는 Rigidbody2D, CapsuleCollider2D.Cast 접지 검사, 수평 이동, 점프, 구르기 속도, 스프라이트 방향을 담당한다. `PlayerAnimation`은 Rigidbody2D 대신 `character.Velocity → movement.Velocity`를 읽는다. 현재 플레이어 스프라이트 렌더러와 클립 바인딩은 루트 기준이다. 옛 기록의 Body 자식 바인딩을 그대로 가정하지 않는다.
- **입력 에셋/프리팹 이벤트는 사용자가 직접 설정한다.** 현재 액션 에셋 기준 좌클릭 공격, Alt 점프, Z 구르기, Space 패링. 프리팹에 `OnParry` 연결이 있다. 일부 프리팹 `m_ActionName` 표시에는 예전 키 이름이 남아 있으므로 실제 키는 액션 에셋 바인딩을 기준으로 본다.

### Animator와 행동

- `Assets/Animations/Player/PlayerAnimController.controller`의 Base Layer에 `PlayerMove`(Idle/Run)와 `Jump`(Jump/Fall/Landing) 하위 상태 머신이 있다. 공중 그룹을 만들 당시 이름은 Airborne이었다. 지상 상태에서 상승/낙하 조건에 따라 내부 상태로 직접 진입한다. 발판에서 떨어질 때는 Fall로 바로 들어간다.
- `PlayerAnimation.Update()`는 Speed(수평 속도 절댓값), VerticalSpeed, IsGround, IsRolling, IsParrying, IsDead를 갱신한다. **캐릭터의 Bool을 필드에 읽어 그대로 전달한다.** Animator 어댑터에서 사망 여부와 행동 상태를 조합해 다시 판단하지 않는다. 사망 중 Roll/Parry 진입 차단 조건은 Animator 전환에 있다.
- 구르기는 지상에서만 시작 가능하며 공격 모션·패링·기존 구르기·쿨다운 중에는 금지한다. 프리팹의 현재 rollSpeed는 10, rollCooldown은 0.12초. 현재 구현은 구르는 동안 FixedUpdate마다 수평 속도를 적용한다. 발판을 벗어나도 구르기를 끝까지 재생하고, 끝났을 때 공중이면 Fall로 전환한다. `PlayerRollState.OnStateExit()`가 상태를 끄고 쿨다운을 시작한다.
- 착지 순간 Z에서 미끄러지던 원인은 물리 접지가 먼저 갱신되어 구르기가 수락됐지만 Animator가 아직 Fall에 있었고, 기존 진입 경로가 Idle/Run에만 있었기 때문이다. **현재는 Any State → Roll을 사용한다.** 예전 LateUpdate 트리거 리셋은 이미 제거됐다. 구르기 종료 후 낙하 중 방향키 이동을 막는 실험도 사용자의 요청으로 되돌렸다.
- 패링은 공중에서도 가능하고 구르기 중과 이미 패링 중에는 시작할 수 없다. 공격 도중에는 공격을 취소한다. `IsParrying` Bool로 Any State → Parry, `PlayerParryState.OnStateExit()`로 종료한다. 지상 패링 중 좌우 이동은 다시 허용했다. 점프와 구르기는 패링 중 금지한다. 공중 패링 시 수직 물리는 그대로 진행한다. **실제 패링 판정, 반격, 쿨다운은 미구현**이다. 판정 타이밍의 Animation Event는 사용자가 나중에 넣을 예정이다.
- `PlayerStatComponent`의 임시 최대 HP는 100. `TakeDamage(float)`가 HP를 줄이고 `IsDead => Hp <= 0`을 제공한다. `PlayerCharacter.IsDead`는 그 값을 직접 읽는다. Animator는 Any State → Death로 들어가 비반복 클립을 한 번 재생한다. 사망 중 수평 이동·점프·공격·구르기·패링을 막되 공중이라면 중력은 적용된다.
- 테스트 방법: Play 모드에서 Player의 `PlayerStatComponent` 메뉴 **Take Damage (10)**을 실행하면 피해 10이 들어간다. 몬스터 공격과 플레이어 피해는 아직 연결되지 않았다.

### 공격과 남은 작업

- 공격은 `PlayerAttackData` ScriptableObject 3개를 순서대로 List에 연결했다. 데이터에는 피해량, 판정 오프셋·크기, Animator 상태 경로가 있다. 기존 20/20/35를 사용하며 데이터는 `Assets/Data/Player/Attacks`에 있다. 공격 재생은 현재 `PlayerAttack`이 `Animator.Play`로 한다.
- 각 공격 시도 시 시간을 저장한다. 다음 입력까지 1초 이상이면 1타. 그 전에는 직전 공격 적중 시 다음 인덱스로 진행하고, 빗나가면 1타다. 3타 다음은 1타. 판정 이벤트 전 추가 입력은 예약하지 않는다.
- 준비·타격·회복 타이머와 종료 이벤트는 제거했다. **`ApplyAttackHit()`는 Animation Event 순간에 한 번만 OverlapBox를 검사하고, 같은 타격에 같은 적을 중복 피해 처리하지 않는다.** 오프셋에 hitboxSize를 잘못 쓰던 문제는 수정했다. 현재 세 공격 클립 파일의 이벤트 목록은 비어 있으므로, 실제 적중과 콤보를 테스트하려면 사용자 지정 시점에 이벤트를 붙여야 한다.
- 공격·패링·사망의 최종 재생과 전투 수치, 몬스터 공격 AI, 스태미나·구르기 무적, 피격 경직·무적, 저장·진행 등은 아직 작업이 필요하다. 공중 일반 공격은 기획상 삭제된 상태다.

### 검증과 Editor 작업

- 수정 전 Play 모드에서 방향 입력 없이 발판 끝 제자리 점프 → 첫 착지 순간 구르기를 재현했다. Roll 재생이 아래층 착지 이후로 밀려 추가 8유닛 전진했다. Any State 수정 후 같은 시험에서 공중 Roll 속도 10, 정상 종료, 아래층 착지 후 추가 이동 0유닛, Roll 재진입 없음. 테스트는 실제 Player 프리팹 복제본과 물리·Animator를 사용하고 행동 요청 메서드를 호출했다.
- 테스트용 Player 복제본의 PlayerInput이 원본과 키보드/마우스를 공유하려고 하면서 경고를 냈다. 이후 비활성 부모 밑에서 복제하고 PlayerInput을 끈 뒤 활성화하여 경고·오류 0건으로 재검증했다. 당시 Play 모드를 종료하고 백그라운드 설정도 원복했다.
- 최신 구조 분리와 사망 Bool 연결은 로컬 C# 컴파일을 통과했다. 이후 테스트 메뉴까지 포함한 최신 플레이어 전체 흐름과 실제 Death 전환은 Play 모드에서 아직 검증하지 않았다.
- 샌드박스에서 CLI가 Editor 검색 파일을 못 읽어 `STATUS_DESCRIPTOR_UNREADABLE`가 나온 적이 있다. 승인된 프로젝트 대상 호출로 포트 37800 Editor에 연결했다. Play 전환 직후에는 Pipeline이 잠시 끊길 수 있어 `unity status --until-ready --timeout 20 --project-path D:/GitHub/Parry`로 대기한다. `Temp`의 재현 스크립트는 임시이며 현재 API와 맞지 않을 수 있다.

## 1. 2026-10-28 당시 작업 상태

**사용자 최신 확인:** 동작별 애니메이션 클립은 모두 만들어두었다. 소스 시트 폴더를 `Assets/Art/Characters/Player/Source/Hero`로 변경하고 그 안에 정리했다. 실제 파일 목록에서도 해당 폴더와 `Assets/Animations/Player/Player_*.anim` 11개(Idle, Run, Jump, Fall, Landing, Death, Attack1/2/3, Parry, Roll)를 확인했다. 이후 작업은 이 정리된 소스와 기존 클립을 기준으로 진행한다. 이 발언은 Animator 자동 전환 연결 완료나 마지막 달리기 품질 승인까지 명시한 것은 아니다.

**2026-10-28 이번 세션 추가 요약:** 최근 작업은 캐릭터 시트 재제작과 달리기 다리 교대 수정이다. 마지막 달리기 v7은 전달했지만 사용자의 품질 승인은 아직 없다. 이후 사용자가 다른 세션에서 이어가기 위해 이 문서 갱신을 요청했다. 이번 세션에서 assistant가 클립·Animator·게임플레이를 연결하거나 Unity Play 검증을 수행하지는 않았다. 아래 Idle 적용 기록은 이전 세션의 검증 이력이다. **현재 에셋 이름과 Animator 구성은 12절의 실제 파일 확인 결과를 우선한다.**

- 사용자는 우선 플레이어 캐릭터를 완성하고 싶어 한다. 별도 폐기용 프로토타입 대신 현재 프로젝트를 본게임으로 발전시키기로 했다.
- 지상 3연타 로직과 몬스터 피격이 구현돼 있다.
- 주인공 픽셀 시트를 생성하고 8개 Animation Clip을 만들었다. **현재 실제 Animator에 연결된 기본 동작은 Idle뿐이다.**
- 최초 Idle 호흡은 Body의 위치·스케일 곡선으로 구현했으나, 사용자가 이 방식을 거절하고 **스프라이트 그림 자체가 호흡하도록 변경**하라고 요청했다.
- 최신 작업에서 Idle 전용 8프레임 시트를 새로 생성하고 적용했다. `Hero_Idle.anim`의 위치·스케일 곡선을 제거했다.
- 실제 Play 모드에서 8개 Idle 프레임을 모두 관측했고 Body의 localPosition·localScale이 일정함을 검증했다. 마지막 확인 시 콘솔 오류·경고 0개, 편집 모드로 복귀했다.
- Run/Jump/Attack/Parry/Roll의 자동 상태 전환과 게임플레이 애니메이션 동기화는 아직 없다.
- 다음 구현 범위는 미지정이다. 최신 달리기 결과의 확인 여부와 사용자 다음 요청을 따른다.

## 2. 기획 기준 문서

원래 기획 문서 폴더:

`C:\Users\USER\Documents\Codex\2026-10-24\skill-creator-c-users-user-codex\outputs\parry-action`

- `GAME-DESIGN.md`: 전체 기획의 최신 통합 기준. 2026-10-28에 공중 일반 공격 삭제와 변형 스킬 추가까지 반영했다.
- `GLOSSARY.md`: 전투·진행·스킬 용어.
- `NEXT-SESSION.md`: 당시 인계문.
- `design-brief.md`: 이전 대화 이력. 과거 규칙을 최신 기획으로 오인하지 않는다.
- 원본 백업은 해당 폴더의 `backups`에 있다.

위 문서의 구현 현황은 이번 대화 후반을 아직 반영하지 않았다. **지상 3연타, currentAttack 리팩터링, 코루틴 점멸, 픽셀 시트·클립, 최신 Idle 적용 진행 상황은 이 인계문을 함께 읽는다.** 확정된 게임 규칙과 임시 구현값은 구분한다.

## 3. 게임의 확정된 큰 방향

- Unity/C#, Windows 싱글플레이 2D 횡스크롤 패링 액션.
- 게임 클라이언트 취업 포트폴리오 및 완성 게임 공개 목적. 1인·무료/직접 제작 자원, 첫 엔딩 20~30분 목표.
- 키보드·마우스, 일반 공격은 **좌클릭**. 게임패드 제외. 키 재설정은 최종 목표이며 아직 UI 미구현.
- 지상 일반 공격 **3연타**. **공중 일반 공격은 사용자가 삭제했다.** 공중 일반 공격을 다음 미완료 작업으로 되살리지 않는다.
- 일반 공격은 어느 단계에서나, 실행 조건을 만족한 패링·구르기로 취소 가능해야 한다. 현재는 취소 함수만 있고 실제 방어 행동은 미구현.
- 일반 패링은 정면의 패링 가능 타격에 대한 타이밍 방어. 성공 시 반격 피해·자세 피해와 즉시 재사용. 일반 성공만으로 적의 연속 공격을 끊지 않으며 자세 붕괴·사망 시 중단한다.
- 패링 쿨타임은 입력 수락 시 시작, 성공 시 초기화, 실패/피격 경직 종료 시 유지. 약 2초는 조정 가능한 초기값.
- 구르기는 스태미나 소모, 무적 후 무적 없는 회복 구간, 버튼 유지 시 자원이 충분하면 반복. 공격·패링은 스태미나 미소모.
- 피격 경직은 취소 불가. 피격 무적은 경직보다 조금 길고, 무적 중 추가 피해·경직·무적 연장 없음.
- 적 자세 붕괴 시 행동 중단, 근거리 공격 입력으로 추격타. 추격타 연출 중 플레이어 무적, 피해 1회.
- 일반 적 2종, 기존 일반 적 기반 정예 중간 보스 2회, 최종 보스 1종. 일반 교전 최대 2마리, 보스 단독.
- 정예 중간 보스 최초 처치로 포인트 총 2개. 공격력·반격 공격력·최대 체력·스태미나 회복 속도·패링 판정 시간 중 선택, 스탯당 최대 2단계.
- 선형 진행·체크포인트·자동 저장. 전투 중 적 위치/잔여 체력은 이어하기 저장 대상이 아니다.
- 상세 수치와 미정 항목은 원래 GAME-DESIGN.md를 확인한다.

## 4. 추가된 지상/공중 변형 스킬 — 기획만 존재

### 지상: 차징 후 돌진

- 일반 공격을 시작한 뒤 **공격 페이즈가 Idle이 아닐 때** 사용 가능한 차징형 준비 동작.
- 차징 후 전방으로 빠르게 돌진한다. 동작 참고는 태도의 앉아발도 기인베기이며, 원작 세부 규칙 전체를 승계하지 않는다.
- 돌진 중 특정 시간 구간에 들어오는 몬스터 공격을 모두 무시한다.
- 해당 구간에 공격한 몬스터에게 **칼날 히트 다단 피해**를 부여한다.
- 그 구간 이후 공격받으면 그대로 피격, 스킬 중단과 쿨타임 적용. 고위험·고보상 대응 수단.
- 일반 패링을 삭제하라는 요청은 없었다. 일반 반격 피해·추격타·칼날 히트는 별개다.

### 공중: 회전 준비 후 수직 낙하

- 일반 공격 선행 조건 없이 쿨타임이 준비되면 공중에서 사용 가능.
- 공중에서 한 바퀴 구르는 준비 애니메이션과 지연 후, 그 자리에서 수직 낙하한다.
- 몬스터 머리의 작은 트리거에 닿으면 해당 몬스터에게 큰 피해.

### 아직 합의하지 않은 것

입력 키, 유지/놓기 방식, 차징 시간·강화 효과, 지상 진입 정확한 페이즈/적중 필요 여부, 지상·공중 쿨타임 공유/시작 시점, 차징 중 피격, 칼날 히트 중복 정책, 공중 무적·적중/착지 종료 규칙 등.

assistant가 우클릭 유지→놓기, 최대 차징 자동 발동, 1타 타격 단계 이후 허용, 피해 증가·방어 시간 고정, 쿨타임 공유·준비 시작 시 적용 등의 7개 시작안을 제시했지만 사용자는 **“스킬보다 지상 3연타부터”**를 선택했다. 이 선택은 스킬 시작안 승인이 아니다. 미정으로 유지한다.

## 5. 프로젝트 환경과 연결

- Unity `6000.6.7f1`, URP `17.6.0`.
- Input System `1.20.1`, Pipeline `0.9.1-exp.1`.
- Rigidbody2D·Collider2D 물리, Input System의 PlayerInput 이벤트 콜백 방식.
- 씬: `Assets/Scenes/SampleScene.unity`. 바닥·양쪽 벽·Player·Monster가 있는 현재 작업 공간.
- 플레이어 프리팹: `Assets/Prefabs/Player/Player.prefab`.
- Input Actions: `Assets/InputSystem_Actions.inputactions`.
- Animator: Player 루트. SpriteRenderer: 자식 **Body**. 따라서 클립 Sprite 바인딩 경로는 `Body`다.
- `FacingMarker`와 `AttackVisual`은 기존 임시 표시 오브젝트다.

### CLI/MCP 주의

- Windows 예약 포트가 기본 `7800–7849` 전체와 겹쳐 Pipeline 서버가 시작되지 않았다. 프로젝트 포트는 **37800**으로 설정되어 있다.
- 설정 파일: `ProjectSettings/Packages/com.unity.pipeline/EditorPipelineConfig.json`.
- 상태 확인: `unity status --project-path D:/GitHub/Parry --format json`.
- Editor 명령은 항상 이 프로젝트 경로를 명시한다. 시작 전에 실제 지원 명령을 조회한다.
- 유효한 씬 명령은 `scene list_open`, `scene get_hierarchy`. 과거 잘못 호출한 `scene info`, `scene hierarchy`는 존재하지 않으며 Unity 오류 로그를 만들었다.
- `unity command script run --file <file.cs> --entry Type.Method --project-path D:/GitHub/Parry --format json`으로 Editor 내 스크립트 실행 가능. `script eval_file`도 지원한다.
- PowerShell/.NET이 메모리 관련 오류를 보였던 적이 있어 CLI는 `cmd.exe`, `login:false`로 실행해 왔다. cmd 명령에서 `;`로 명령을 연결하지 않는다.
- Play 진입·컴파일 도메인 재로드 중 잠시 연결이 끊길 수 있다. `unity status --until-ready --timeout 30 ...`로 준비를 확인한다.
- 실제 타임아웃/Editor 종료/로딩 실패는 사용자에게 알리고 Safe Mode·모달·임포트 상태를 확인한다.
- MCP 도구 노출은 세션에 따라 달랐다. CLI가 ready인 것과 현재 세션 MCP 도구 유무를 구분한다.
- 백그라운드 런타임 검증 시 `editor.playmode set_run_in_background --enable true`, 검증 후 false로 복원했다.
- 모든 최근 검증 후 Play를 종료했다. 현재 연결·씬 상태는 새 세션에서 다시 확인한다.

## 6. 현재 코드

### PlayerController

파일: `Assets/Scripts/Player/PlayerController.cs`, 네임스페이스 `Parry.PlayerClass`.

- A/D 이동, Space 1단 점프, 공중 방향 조절은 현재 시작안.
- 사용자가 PlayerInput 방식으로 직접 수정했다. `OnMove(InputAction.CallbackContext)`에서 Vector2.x, `OnJump`에서 점프 요청을 받는다.
- `CapsuleCollider2D.Cast`로 짧게 아래를 검사하고, 법선 y와 상승 여부로 접지 판정.
- FixedUpdate에서 `Rigidbody2D.linearVelocity`의 x 이동과 y 점프를 적용한다.
- 공격 중 이동은 허용하지만 바라보는 방향은 바꾸지 않는 현재 구현.
- 이동/공격 Animator 상태 자동 전환 코드는 아직 없다.

### PlayerAttack

파일: `Assets/Scripts/Player/PlayerAttack.cs`, 네임스페이스 `Parry.PlayerClass`.

- 좌클릭으로 지상 공격 시작. `Idle → Windup → Active → Recovery`.
- **firstAttack / secondAttack / thirdAttack을 모두 ComboAttack 객체로 통일**했다. 사용자의 구조 개선 제안에 따라 `currentAttack`이 현재 데이터를 가리킨다.
- `StartStrike(index)`에서 현재 객체를 선택하고 `currentAttack.GetDamage()` 등으로 수치를 읽는다. 예전 CurrentDamage/Followup 조건부 프로퍼티는 제거했다.
- 1타 기존 프리팹/씬 값은 새 firstAttack으로 이전해 보존했다.
- 현재 임시 기본값: 1타 20, 2타 20, 3타 35. 3타는 준비 0.18초·타격 0.12초·회복 0.4초. 1·2타는 0.12/0.10/0.25초.
- 공격 중 다음 클릭 1개를 예약한다. 회복 종료 후 접지 상태이면 다음 타로 진행. 3타에서는 다음 예약 없음. 홀드 자동 반복 없음.
- 클릭을 멈추면 현재 타격 후 종료. 취소 시 예약·타격 기록·단계 초기화.
- `OverlapBox` + Monster 레이어 필터. 결과 배열을 재사용하고 포화 시 확장한다.
- 부모 Monster 단위 HashSet으로 한 타격의 여러 콜라이더/프레임 중복 피해 방지. 다음 타격에서는 기록 초기화.
- `elapsed`와 `previousElapsed`로 타격 구간을 통과한 물리 업데이트도 놓치지 않게 검사한다.
- **입력 예약 구간은 현재 공격 전체에 열려 있다.** 사용자는 애니메이션이 준비되면 **Animation Event로 예약 가능 구간을 열고 닫는 방식**을 직접 적용하겠다고 했다. 타이머 방식 입력 구간을 추가하지 않았다. 이벤트 자체도 아직 없다.
- 공격 로직 타이머와 공격 클립 길이는 아직 동기화하지 않았다.
- 공중 일반 공격은 기획에서 삭제되어 지상 제한이 의도된 상태다.
- 일반 공격의 자세 피해는 아직 미구현이다.

### Monster

파일: `Assets/Scripts/Monster/Monster.cs`, 네임스페이스 `Parry.MonsterClass`.

- 사용자가 만든 클래스를 확장했다. hp 감소, 피격 색상, hp 0이면 GameObject 비활성화.
- 점멸은 Update 대신 코루틴. 재피격 시 기존 코루틴을 중단하고 다시 시작, 비활성화 시 정지·색상 복구.
- 사용자가 `StartCoroutine(nameof(HitFlash))` 방식으로 바꿨다. 임의로 원상복구하지 않는다.
- **확인할 기존 문제:** `public float Hp { get; private set; }`가 실제 `hp` 필드와 연결되어 있지 않다. 이번 대화에서 발견했지만 요청 범위 밖이라 수정하지 않았다. Hp로 상태를 검증하거나 HUD에 사용할 때 주의한다.
- 몬스터 공격·감지·추적 AI는 아직 없다.

## 7. 아트와 Animation Clip

### 사용자 요구

- 모든 플레이어·몬스터 그림체를 통일한다.
- 픽셀 아트, 고품질 장식보다 공격 준비/타격/회복 구별을 우선한다.
- 주인공은 검을 든 단순한 기계 검사. 회백색·남색/철색 장갑, 청록 바이저·강조색, 적갈색 허리 천.
- 향후 생성 시 기존 시트를 시각 참조로 첨부해 동일 인물을 유지한다.
- Idle은 **위치/스케일 곡선으로 흔들지 않고, 발은 고정한 채 상체·어깨·검의 자세가 바뀌는 픽셀 프레임**으로 만든다.

### 주요 파일

- 스타일 기준: `Assets/Art/ART-STYLE.md`.
- 최초 시트: `Assets/Art/Characters/Player/Source/Hero-Spritesheet-v1.png`.
- 원본 생성 프롬프트: 같은 폴더의 `Hero-Spritesheet-v1.prompt.txt`.
- 애니메이션용 복사본: `Assets/Art/Characters/Player/Animation/Hero-AnimationFrames-v1.png`.
- **최신 Idle 시트:** `Assets/Art/Characters/Player/Animation/Hero-IdleBreathing-v2.png`.
- Idle 생성 프롬프트: 같은 폴더의 `Hero-IdleBreathing-v2.prompt.txt`.
- 생성은 built-in image_gen 도구를 사용했다. 원본은 별도로 보존했다.

### 시트와 분리 주의

- 최초 시트는 1254×1254, 8×8 배치다. AI 생성물이므로 정확한 픽셀 격자와 일정한 자세/기준점이 보장되지 않는다. 검기가 셀 경계를 넘는 곳도 있다.
- 원본에는 사용자가 자동 분리한 조각이 있었으나 여러 프레임이 합쳐진 상태였다. 그 메타데이터는 보존하고 복사본에 64프레임을 분리했다.
- 복사본은 Point 필터·압축 없음·mipmap 없음. 프레임은 156/157px로 경계를 반올림했다.
- 최신 Idle은 1774×887, 4×2의 8프레임이다. 행 높이가 정확히 균등하지 않아 실제 투명 간격으로 행을 나눴고 알파 경계와 발 위치로 피벗을 잡았다.
- 최신 Idle PPU는 약 252.17392. 새 생성물의 픽셀 크기가 커서 기존 표시 높이와 발 기준 위치를 유지하도록 보정한 값이다. 다른 시트의 PPU와 숫자가 다르다고 바로 동일하게 바꾸지 않는다.
- 향후 아트 보정은 원본 보존 후 별도 버전으로 진행하고, Sprite 메타데이터는 Unity importer/provider API로 수정한다.

### 클립 상태

폴더: `Assets/Animations/Player`.

| 클립 | 현재 프레임 출처 | 시작 재생속도 | 반복 |
|---|---|---|---|
| Hero_Idle.anim | 최신 Idle v2, 8프레임 | 5fps | 예 |
| Hero_Run.anim | 최초 시트 2행, 8프레임 | 12fps | 예 |
| Hero_JumpFall.anim | 최초 시트 3행, 8프레임 | 10fps | 아니오 |
| Hero_Attack1.anim | 최초 시트 4행, 8프레임 | 12fps | 아니오 |
| Hero_Attack2.anim | 최초 시트 5행, 8프레임 | 12fps | 아니오 |
| Hero_Attack3.anim | 최초 시트 6행, 8프레임 | 10fps | 아니오 |
| Hero_Parry.anim | 최초 시트 7행, 8프레임 | 12fps | 아니오 |
| Hero_DodgeRoll.anim | 최초 시트 8행, 8프레임 | 12fps | 아니오 |

- 모든 클립의 Sprite 바인딩은 **Body / SpriteRenderer.m_Sprite**다. 처음 빈 경로로 생성해 Player 루트에 렌더러가 없어 재생되지 않았으나 수정했다.
- 최신 Idle에는 Transform 곡선이 없다. 오직 스프라이트 교체로 호흡한다.
- 이전 수정에서 다른 7개 클립에는 Idle 스트레칭 잔류 방지용 상수 Body y/scale-y 곡선이 들어갔다. 이후 크기·위치를 바꾸거나 애니메이션을 연결할 때 이 상수도 확인한다.
- Animator Controller: `Assets/Animations/Player/PlayerAnimController.controller`.
- 사용자 생성 컨트롤러의 Idle 상태에 실제 `Hero_Idle`을 지정하고 기본 상태·loop를 적용했다. Base Layer weight 1.
- Player 프리팹과 SampleScene에 적용·저장했다. Body 색상은 White로 설정해 캐릭터 원색을 표시한다.
- **현재는 이동·점프·공격을 해도 기본 Idle 상태를 유지한다.** Run 등 클립은 존재하지만 전환 파라미터/조건/코드가 없다.
- 차징/돌진, 공중 회전/낙하, 피격/사망 시트와 클립은 아직 없다.

## 8. 검증 기록과 한계

- 이동·점프: 실제 입력으로 이동·방향·점프·공중 제어·재착지·벽 충돌 검증을 수행했다.
- 최초 1타: 준비 중 무피해, 중복 콜라이더/프레임 피해 방지, 홀드, 사거리·후방, 취소, 사망을 검증했다.
- 3연타: 실제 좌클릭으로 한 칸 예약, 20/20/35 피해, 3타 후 종료, 2클릭이면 2타에서 종료, 취소 후 예약 제거, 지상 제한을 검증했다.
- currentAttack 리팩터링 이후에는 자동 마우스 입력 검증에서 입력 미전달로 타임아웃이 있었다. 공격 요청 직접 호출 방식으로 세 타격 피해·4타 거부·리셋·취소를 재검증했다. 자동 입력 실패를 게임플레이 코드 오류 확정으로 보지 않는다.
- 클립 생성 후 64프레임을 임시 오브젝트에서 SampleAnimation으로 검증했다. 비레거시 클립 샘플링에는 임시 Animator가 필요했다.
- 실제 Player에서 최초 Idle 프레임/호흡/루프 재생 확인 후, 사용자 요청으로 시트 호흡 방식으로 교체했다.
- 최신 Idle 검증은 2.3초 동안 새 스프라이트 8개를 관측하고 Body 위치/크기가 일정함을 검사했다. 관측 localPosition 약 (-0.438, -0.109, 0), localScale (1,1,1).
- 검증은 수행 시점 기록이다. 사용자가 코드를 수시로 편집하므로 실제 파일과 현재 Editor를 다시 확인한다.
- 실제 시트 프레임의 시각적 완성도·루프 매끄러움은 플레이 확인 후 추가 조정 가능하다. 에셋 연결 검증을 완성된 아트 품질 보장으로 표현하지 않는다.

## 9. 미구현 기능 / 이후 후보

사용자가 먼저 플레이어 캐릭터 작업을 원한다. 아래는 후보이며 다음 작업을 자동 확정하지 않는다.

1. 요청 시 Run·Jump·공격 애니메이션 상태와 실제 행동 연결.
2. 공격/콤보 입력 구간 Animation Event 연결(사용자가 직접 처리하겠다는 선호 확인).
3. 플레이어 체력·피격 경직·피격 무적·사망.
4. 일반 적 A 감지·접근·단타 공격과 재시작.
5. 일반 패링·자세·추격타, 구르기·스태미나.
6. 미정 규칙을 정한 뒤 지상 차징 돌진·공중 낙하 스킬.
7. 체크포인트·저장·성장·회복·메뉴·키 재설정 및 최종 콘텐츠.

공중 일반 공격은 이 목록에 포함하지 않는다. 3연타는 로직 구현 완료지만 최종 애니메이션·자세 피해·밸런스 완료를 의미하지 않는다.

## 10. 작업 방식과 사용자 선호

- 사용자는 코드를 직접 수정하며 구현 이유를 배워가고 있다. 변경 전 현재 파일을 읽고 사용자 변경을 보존한다.
- 코드 설명은 현재 코드 기준으로 준비/타격/회복, 경과 시간, 입력 예약 등 구체 흐름을 설명한다.
- 사용자는 과도한 조건부 접근자보다 현재 ComboAttack 객체를 명시적으로 참조하는 구성을 선호했다.
- 피격 점멸은 상시 Update보다 피격 시 시작하는 코루틴을 선호했다.
- “클립만 만들기”, “Idle만 적용”처럼 범위를 제한하면 다른 기능·상태 전환을 임의로 연결하지 않는다.
- 질문 묶음에 실제 답변할 때 생략된 항목을 추천안 동의로 보는 선호가 있었지만, 저장 요청·시간 경과·다른 작업 선택은 동의가 아니다.
- 기획을 읽었다면서 공중 일반 공격을 누락한 실수가 있었고 사용자가 지적했다. 최신 기획과 구현 범위를 분명히 구분한다.
- root의 이 인계문과 원래 기획서를 읽은 후 다음 요청에 필요한 범위만 구현한다.

## 11. 임시 도구·백업 주의

- `AgentScripts`에 과거 생성/검증 스크립트가 있다. 이전 네임스페이스 `Parry.Player`/`Parry.Monster`나 삭제된 필드를 가정하는 것이 있으므로 그대로 재실행하지 않는다.
- `Temp`에 이번 작업의 실행 스크립트와 백업이 있다. Unity가 정리할 수 있는 임시 영역이므로 인계의 필수 자료로 의존하지 않는다.
- 대표 임시 도구: `ApplyIdleBreathingFrames.cs`(최신 시트 적용/런타임 확인), `ApplyHeroIdle.cs`, `FixHeroClipBindings.cs`, `CreateHeroAnimationClips.cs`, `VerifyComboRefactor.cs`.
- 앞선 도구는 이전 Idle Transform 호흡이나 원본 프레임으로 되돌릴 수 있다. 최신 Idle 시트를 적용한 상태를 보존하고 필요한 작업만 새로 작성한다.
- 이 대화에서 Git 커밋/푸시는 수행하지 않았다. 초기부터 사용자 변경·untracked 파일이 많았다. 임의 정리/되돌리기를 하지 않는다.

## 12. 이번 세션: 시트 규격 수정·달리기·사망 (2026-10-28)

### 사용자 요구와 범위

- 기존 생성 시트의 격자가 일정하지 않아 잘린 프레임에 인접 모션 일부가 섞인 것을 사용자가 지적했다.
- 프레임 하나는 정확히 **256×256픽셀**, 각 동작은 **8프레임**, 캐릭터·검·이펙트가 인접 칸을 침범하지 않아야 한다.
- 공격 중 청록 검기 이펙트는 유지한다. Idle은 발이 고정되고 상체·어깨가 위아래로 들썩이는 전형적인 픽셀 호흡을 원한다.
- 이미지 생성 도구가 정확한 격자를 지키지 못하자 assistant가 코드로 분리·재배치하는 후처리를 제안했고 사용자가 **“그래”로 명시 허용**했다. 이후 Pillow/NumPy로 패킹·픽셀 보정을 수행했다.
- 사용자가 “시작” 후 요청한 추가 제작은 **달리기 8프레임과 사망 8프레임만**이다. 착지 제작 요청은 취소했다. 기존 착지 파일이 있어도 새 제작 지시로 해석하지 않는다. 사용자는 점프를 모션 없이 1프레임으로 했다고 설명했다. 실제 파일 구성은 아래와 별도로 확인한다.
- 이후 요청은 반복해서 **달리기만 수정**이었다. 사망이나 다른 동작을 함께 다시 만들지 않는다. 이 세션은 시트 작업 범위였으며 assistant가 Unity 클립·상태 전환을 수정하지 않았다.

### 달리기에서 반복된 실패와 정확한 수정 의도

- 생성 결과 및 첫 코드 보정에서 오른다리만 앞에서 굽혔다 펴고, 왼다리는 계속 뒤에 남는 현상을 사용자가 여러 차례 지적했다.
- 다리 색상 교환, 비슷한 자세 반복, 무릎만 굽히는 것으로 해결됐다고 주장하지 않는다.
- 사용자가 보여 준 순서: **앞의 오른발이 지면을 밀고 뒤로 이동 → 뒤의 왼쪽 무릎이 골반 아래를 지나 앞으로 상승 → 왼쪽 정강이가 펴져 앞을 디딤 → 반대쪽도 같은 순서로 반복**.
- 같은 다리의 골반-허벅지-무릎-발 연결이 프레임 사이에서 추적돼야 한다. 가까운 오른다리가 뒤로 갈 때도 먼 왼다리와의 겹침 순서가 맞아야 한다.
- 마지막 v7은 생성만으로 해결하지 못해 원본 허벅지·정강이·부츠 조각을 별도의 두 다리 관절 좌표로 변환하고, 가까운 다리를 마지막에 그려 고정된 앞 허벅지가 남지 않게 수정했다. 3~5프레임에 오른 무릎은 골반 뒤, 왼 무릎은 앞에 배치했다.
- 마지막 답변에 `Tools/Art/Previews/Hero-Run-v7.gif`를 제시했다. **최종 달리기 품질은 사용자 미확인**이며, 좌표·규격 검사를 자연스러운 달리기 품질 보장으로 표현하지 않는다.

### 제작 이력과 도구

- `Tools/Art/pack_hero_sheet.py`: v3-DRAFT 생성물을 256×256 셀로 재배치한 v4 도구. 2048×2048 통합 시트와 동작별 2048×256 시트를 당시 생성했다. 공격 1타 검기와 옆 캐릭터 검의 겹침을 마스크로 분리했다. Idle은 발을 고정하고 픽셀 행을 재배치해 상체 높이 0,2,4,6,4,2,0,-2 픽셀 호흡을 만들었다.
- `Tools/Art/build_run_death.py`: 달리기·사망 v5 제작 도구. 달리기 v5는 사용자에게 거절됐다. 이 스크립트는 `Temp/hero-v5` 원본에 의존하므로 재실행 전에 입력을 확인한다.
- `Tools/Art/pack_run_v6.py`: 추가 생성한 달리기 패킹 시도. 투명 경계 검사에서 실패했고 중간에 턴도 중단됐다. 성공한 완성본으로 취급하지 않는다.
- `Tools/Art/fix_run_v7.py`: 마지막 달리기 보정 도구. 원본 `Tools/Art/References/Hero-Run-v7/source.png`는 보존돼 있다. 실행 시 v7 출력 폴더를 만들므로 사용자가 정리한 현재 에셋을 자동 교체하는 용도로 실행하지 않는다.
- `Tools/Art/References/Hero-Run-v6/source.png`, `prompt.txt`도 남아 있다. v6는 다리 색으로 구분했으나 사용자가 요구한 실제 다리 교대의 해결로 승인받지 않았다.
- v4/v5/v7 당시 검사는 8개 프레임·정확한 셀 크기·최소 16픽셀 투명 여백을 확인했다. **이번 문서 갱신에서는 현재 이름으로 정리된 모든 시트의 동작 품질이나 Unity 재생을 다시 검증하지 않았다.**

### 인계문 갱신 시 실제 파일 확인 — 이전 답변 경로보다 우선

사용자가 파일을 정리·편집한 것으로 보인다. 정확한 이동/편집 경위는 확인하지 않았다. 과거 출력 경로 `Source/Hero-v5`, `Hero-v6`, `Hero-v7`과 v4 통합 시트는 **현재 파일 목록에서 확인되지 않았다**. 이를 복원하거나 지웠다고 단정하지 않고, 현재 파일을 보존한다.

현재 시트 폴더: `Assets/Art/Characters/Player/Source/Hero` (사용자가 `Hero-v4`에서 이름을 바꾸고 정리함).

| 현재 파일 | 실제 이미지 크기 |
|---|---|
| `Hero_Idle_8x256.png` | 2048×256 |
| `Hero_Attack1_8x256.png`, `Hero_Attack2_8x256.png`, `Hero_Attack3_8x256.png` | 각각 2048×256 |
| `Hero_Parry_8x256.png`, `Hero_DodgeRoll_8x256.png` | 각각 2048×256 |
| `Player_Run_8x256.png`, `Player_Dead_8x256.png` | 각각 2048×256 |
| `Player_Jump_8x256.png` | 768×256 (이름과 프레임 수를 동일시하지 않음) |
| `Player_Falling.png` | 256×256 |
| `Player_Landing.png` | 1024×256 |

현재 `Player_Run_8x256.png`가 마지막 v7과 동일한 픽셀인지 비교하지 않았다. 다음 달리기 작업에서는 **이 파일과 v7 GIF를 직접 비교**해 사용자가 어떤 결과를 채택했는지 확인한다.

현재 `Assets/Animations/Player`에는 `Player_Idle`, `Player_Run`, `Player_Jump`, `Player_Fall`, `Player_Landing`, `Player_Death`, `Player_Attack1/2/3`, `Player_Parry`, `Player_Roll`의 `.anim`이 있다. 이전 7절의 `Hero_*.anim` 표는 과거 기록이며 현재 파일명을 나타내지 않는다.

`PlayerAnimController.controller`에도 여러 `Player_*` 상태가 추가돼 있고 기본 상태는 `Player_Idle`이다. 파일 확인상 파라미터는 비어 있고 각 상태의 전환 목록도 비어 있다. 플레이어 코드 검색에서는 Animator 호출이 발견되지 않았다. **여러 상태가 존재하지만 자동 전환은 연결되지 않은 상태로 보이며, 현재 Editor/Play 상태는 재확인하지 않았다.** 이전의 “Idle 상태만 존재”라는 식의 해석은 틀리다.

### 함께 발견했지만 미수정인 코드 항목

- `PlayerAttack.GetHitboxCenter()`가 `hitboxOffset` 변수에 `(currentAttack ?? firstAttack).hitboxSize`를 읽는다. 이번 세션 시작과 문서 갱신 시 모두 확인했고 사용자에게 알렸으나 시트 작업 범위라 수정하지 않았다. 실제 의도는 다음 코드 작업에서 확인한다.
- Git 상태 확인은 dubious ownership 오류가 났다. 전역 safe.directory를 변경하지 않았고 커밋/푸시도 수행하지 않았다.

## 새 세션에 붙여넣을 짧은 메시지

> Parry의 SESSION-HANDOFF.md에서 맨 위 **현재 구현 — 2026-11-04** 절을 우선 읽어주세요. 입력 → MyController → PlayerCharacter 구조와 PlayerMovement, Bool Animator 연동, 3타 적중 콤보, 구르기 착지 버그 수정, 패링 모션, 임시 HP·Death 연결까지 정리돼 있습니다. 입력 설정과 공격·패링 이벤트 시점은 제가 직접 작업합니다. 다음 요청에 맞춰 진행하고 변경 전에 실제 파일을 확인해주세요.