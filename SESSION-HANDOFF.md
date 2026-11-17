# Parry 개발 세션 인계

갱신: 2026-11-18 (Asia/Seoul). 프로젝트: `D:/GitHub/Parry`.

## 현재 구현과 대화 — 2026-11-18 보스 제작 전 최종 인계

이 절과 실제 최신 파일이 아래 같은 날짜의 이전 기록보다 우선한다. 이번 요청은 세션 인계이며 **런타임 코드·씬·에셋을 수정하지 않고 이 문서만 갱신**한다.

### 다음 세션에서 할 일

1. 이 절을 읽고 실제 코드·Git 작업 트리부터 확인한다. 브랜치는 main이며 사용자 수정·미추적 에셋이 많다. 이번 세션에서 커밋·푸시하지 않았다. 사용자 편집을 보존한다.
2. **사용자는 보스를 제외한 일반 몬스터와 플레이어 기능은 완료했다고 선언했다. 다음 작업은 보스다. 보스의 구상은 사용자가 생각해서 알려줄 예정이므로, 설명을 받을 때까지 보스 설계·코드·에셋을 임의로 만들지 않는다.**
3. 포트폴리오용 베타 범위는 고블린 한 종류 + 추후 보스 한 종류, 짧은 1스테이지다. 보스 콘셉트·패턴·페이즈는 미정이다. 기존 `Assets/Art/Boss_Golem` 자료가 있다고 보스를 골렘으로 확정하지 않는다.
4. 과거 문서의 미구현 항목은 자동 착수할 목록이 아니다. 예를 들어 버서커 패링 반격의 저장 참격 추가는 이번 세션에도 연결하지 않았지만, 사용자 완료 선언을 무시하고 먼저 구현하지 않는다. 기절 첫 타 추가 피해는 철회된 설계다.

### 사용자 선호와 작업 방식

- 설명 질문과 코드 수정 요청을 구분한다. 필수 참조에 불필요한 fallback·null 검사·추상 계층을 추가하지 않는다. 기존 `PlayerBerseker`, `burserkerHitVfx` 등의 철자를 임의로 바꾸지 않는다.
- **첫 Init에서 Unbind를 호출하는 패턴을 새로 추가하지 않는다.** 구독 해제는 OnDestroy에서 한다. 단, 실제 최신 `PlayerStatUI.Init`에는 이전부터 Unbind 호출이 남아 있다. 이 인계에서 그것을 제거하지 않았고, 모든 기존 UI가 동일하다고 주장하지 않는다. ParryUI/SkillUI/MonsterStatUI.Init에는 선행 Unbind가 없다.
- UI는 이벤트를 구독한 뒤 컴포넌트의 인자 없는 InitUI를 호출한다. InitUI는 현재 값 이벤트만 발행한다. 컴포넌트가 UI 메서드를 직접 구독하는 방식으로 되돌리지 않는다.
- 월드 UI는 공통 프리팹을 쓰되 몬스터별 전체 위치·크기는 별도 부모로 조절할 수 있다. 내부 바의 개별 위치 차이는 몬스터 프리팹에 중첩 프리팹 Override로 저장한다. 공통 MonsterStatCanvas 원본에 Apply All하면 다른 몬스터에도 배치 변경이 전파될 수 있음을 설명했다.
- 맵의 지형·타일은 **Tilemap**으로 만든다. 개별 SpriteRenderer 지형 배치를 사용자가 명확히 원하지 않았다.

### PlayerData와 플레이어 초기화 — 구현됨 / 최신 사용자 변경 중요

- `Assets/Scripts/Player/PlayerData.cs`: ScriptableObject PlayerData와 기능별 Serializable struct를 한 파일에 정의했다. 실제 설정 에셋은 `Assets/Data/Player/PlayerData.asset`이다. Player.prefab과 Stage01/SampleScene의 플레이어에 연결했다. 마이그레이션 당시 세 설정이 같아서 하나의 에셋을 공유한다.
- 구조체: PlayerCharacterData(구르기·적중 감속), PlayerStatData(체력·기력·회복·피해), PlayerMovementData, PlayerAttackSetData(일반/버서커 공격 목록·저장 비율·판정 레이어·이펙트), PlayerParryData, PlayerParryGaugeData, PlayerBerserkerData, PlayerHitReactionData. 개별 타격용 기존 PlayerAttackData/BerserkerAttackData 에셋은 그대로 참조한다.
- PlayerCharacter는 serialized PlayerData data / `Data => data`를 갖는다. **Awake에서 관련 컴포넌트를 모두 GetComponent한 뒤 각 컴포넌트.Initialize(data의 해당 구조체)를 호출한다.** 각 컴포넌트는 전달받은 구조체를 private data로 보관하며 참조 캐시·필터·초기 스탯을 설정한다. 관련 컴포넌트의 기존 Awake 초기화는 Initialize로 옮겼다. PlayerAnimation/MyController 등의 독립 Awake는 남아 있다.
- 현재 체력·기력·게이지·쿨타임·행동 상태는 컴포넌트별 런타임 값이다. 공유 ScriptableObject에 현재값을 쓰지 않는다. PlayerParryGauge.currentGauge는 런타임 확인용 SerializeField로 남았으며 Initialize에서 data.initialGauge로 설정한다. 구조체의 List는 기존 에셋 목록을 참조하며 현재 코드에서는 목록 자체를 수정하지 않는다.
- **최신 실제 UI 진입점은 사용자 수정으로 `PlayerCharacter.Start → UIManager.Instance.Init(this)`다.** UIManager에는 Start/플레이어 검색이 없다. 이전 답변과 아래 과거 기록의 UIManager.Start 설명보다 이 사실이 우선한다.
- 전체 흐름: `PlayerCharacter.Awake → 구조체별 Initialize 완료 → PlayerCharacter.Start → UIManager.Init → PlayerStatUI/ParryUI/SkillUI.Init → 이벤트 구독 → 컴포넌트.InitUI`. 모든 씬 Awake가 Start보다 먼저 끝나는 초기 배치 경로를 사용한다. UIManager는 게임 씬의 필수 참조이며 자동 검색/생성/지연 스포너를 추가하지 않았다.
- `Assets/Editor/MonsterAttackPreviewWindow.cs`도 PlayerCharacter.Data의 attack 아래 일반/버서커 목록을 읽도록 수정했다. 컴포넌트에서 삭제한 serialized attacks 필드를 다시 조회하면 안 된다.
- 인계 시 PlayerData 실제 값: HP/기력100, 기력 회복간격1/회복량10, 반격피해10, 치명타10%/피해난수10%; 이동6/점프12/구르기속도8; 구르기 쿨타임0.12/비용20/무적0.2, 적중 재생배율0.7; 패링 유효0.3/시도쿨3/성공쿨0.2; 게이지 최대100/패링획득15/감소0/공격보너스0.5/시작20; 버서커 비용30/지속10/종료쿨180/피격손실1/패링회복1, 저장피해50%. 사용자 값이 우선이며 과거 테스트값으로 되돌리지 않는다.

### 몬스터 체력·기절·처형 UI — 구현됨

- 공통 프리팹 `Assets/Prefabs/UI/MonsterStatCanvas.prefab`. World Space Canvas이며 HealthSet 아래 HealthSlider/WhiteHealthSlider, FlinchSlider, ExecutionSet 아래 배경 해골/ExecutionImageSlider(Image)가 있다. 사용자가 만든 배치·크기·색을 보존한다.
- `MonsterStatUI`는 serialized MonsterHealthUI / MonsterFlinchUI / MonsterExecutionUI를 관리하고 이벤트 연결을 담당한다. **UI Start에서 부모 몬스터를 검색하지 않는다. Monster.Start에서 GetComponentInChildren<MonsterStatUI>(true)로 찾고, 있으면 statUI.Init(this)를 호출한다.** UI 없는 몬스터도 허용한다.
- Init은 Monster.OnHpChanged → healthUI.SetHealth, OnFlinchResistanceChanged → flinchUI.SetResistance, OnStoredDamageChanged → executionUI.SetValue를 구독한 뒤 monster.InitUI, healthUI.ResetWhiteHealth를 호출한다. OnDestroy에서 같은 핸들러를 해제한다.
- MonsterHealthUI: 실제 체력바는 current/max로 즉시 갱신한다. 흰 바는 기본0.2초 대기 후 초당0.8 비율로 Mathf.MoveTowards(흰값,실제값,speed*deltaTime)로 따라온다. **현재 Update 기반**이다. 코루틴 대안은 설명만 했고 바꾸지 않았다.
- SetHealth는 매 호출마다 `whiteHealthStartTime=Time.time+whiteHealthDelay`를 설정한다. 연속 피격 시 대기를 다시 시작하며, 감소 중 추가 피격도 다시 대기한다. 기존 `value < healthSlider.value` 조건은 사용자 요청으로 제거했다. 회복/초기화 때 흰 바가 실제 체력보다 낮으면 즉시 올리는 조건은 남아 있다.
- 중간에 초기화 이벤트 OnHpInitialized를 별도로 분리했다가 사용자가 롤백을 요청해 제거했다. 분리안을 복원하지 않는다.
- MonsterFlinchUI: 남은 저항 current/max를 Slider에 표시한다. 패링 반격 시 Monster가 저항을 감소시키고 이벤트를 발행하며, 기절이 끝나 ResetFlinchResistance에서 다시 채운다. **ApplyDamage에서 HP가0이 되면 저항도0과 이벤트를 발행하여 노란 바를 비운다.** 일반 사망/처형 피해 모두 해당한다.
- **최신 사용자 MonsterFlinchUI는 0분모 보호 없이 `(float)current/max`를 계산한다.** 이전 에이전트가 넣었던 max>0 보호는 사용자가 제거했다. 저항 데이터 없이 초기화하는 TrainingDummy에 이 UI를 붙이면 0/0이 될 수 있다. 과거 더미 UI 검증은 현재 파일을 보장하지 않으며 요청 없이 방어 코드를 복원하지 않는다.
- MonsterExecutionUI.SetValue는 **(float storedDamage, float remainingHp)** 두 인자를 받는다. storedDamage>0이면 ExecutionSet을 활성화하고, 누적 피해/남은 체력으로 fillImage.fillAmount를 설정한다. 누적0이면 비활성화+fill0, 남은HP0이고 누적>0이면 fill1. Slider 이동이 아니라 Image Filled/Vertical/Bottom으로 해골이 밑에서부터 드러난다.
- Monster.OnStoredDamageChanged는 초기 UI 발행, 저장 피해 증가, 실제 HP 변경, ClearStoredDamage의 초기화에서 (totalDamage,hp)를 발행한다. 수동 Burst·자동 소멸·사망 정리에서도 UI가 숨겨진다. 기존 엄격한 처형 조건 `totalDamage+context.StoredDamage > 직접 피해 후 남은 HP`는 바꾸지 않았다.
- 인계 시 Goblin.asset 사용자 값은 **HP500 / 기절 지속100초 / 저항1**이다. 이전의 HP800만/기절5초 기록으로 되돌리지 않는다.

### 전투 Context는 ref로 전달 — 구현됨

- MonsterHitContext/AttackHitContext는 struct 그대로다. PlayerAttack → Monster.TakeDamage, MonsterAttack → PlayerCharacter.TakeDamage, 패링 반격의 Monster.TakeDamage 호출을 ref로 통일했다.
- Monster의 TakeBerserkerDamage/TakeNormalDamage, DamageNumberSpawner.Spawn(MonsterHitContext), PlayerHitReaction.StartReaction도 ref를 유지한다. 패링 반격은 CreateMonsterHit 반환값을 지역 counterContext에 담아 ref 전달한다.
- 값만 받는 피해 숫자/Burst API나 Context를 생성해서 반환하는 CreateMonsterHit까지 불필요하게 out/ref로 바꾼 것은 아니다. 과거 AgentScripts에서 예전 TakeDamage 시그니처/필드를 직접 쓰면 최신 코드에 맞춰 확인해야 한다.

### Stage01 맵·폰트·카메라 — 구현됨

- `Assets/Scenes/Stage01.unity`는 SampleScene을 보존해 별도로 만든 짧은 프로토타입 맵이다. 기본 동선은 시작 → 고블린 첫 전투 → 낮은 단차/선택 발판 → 고블린 전투 → 빈 보스 구역이다. 제작 당시 x=-8~118, 시작x3, 고블린4개(x18/39/72/79), 보스예정x105로 구성했다. 이후 사용자 배치 수정이 우선한다.
- `Stage01_OuterRuins` 아래 GroundGrid/Ground, BackgroundGrid/Background, DecorationGrid/Decoration의 **3 Tilemap**. 지형용 개별 SpriteRenderer는 최종 제거했다. Ground는 static Rigidbody2D + TilemapCollider2D(composite Merge) + CompositeCollider2D(Polygons). 배경/장식 타일은 비충돌. 기본 셀1, 장식 셀0.25. `Assets/Data/Stage01`에 타일 에셋이 있다.
- 기존 Hero Knight EnvironmentTiles를 재사용한 간단한 폐허 지형이다. 보스·체크포인트·씬전환·전투 봉쇄 문은 구현하지 않았다. 보스 자리와 경계만 준비했다. 원본 SampleScene의 기존 사용자 수정은 보존했다.
- 사용자가 제목을 `Parry Knight`, Subtitle을 `01 / 폐허가 된 성지`로 편집했다. `D:/GitHub/ddolking/Assets/Resources/LegacyFont/Font/Galmuri11.ttf`를 Parry의 `Assets/Fonts/Galmuri`로 복사하고 Subtitle에 Galmuri11 SDF를 적용했다. 현재 문자+ASCII용 static TMP 에셋과 추가 한글용 dynamic fallback(1024, multiatlas, ClearDynamicDataOnBuild)을 생성했다. 다른 UI 폰트를 전부 바꾼 것은 아니다.
- Main Camera는 실제 렌더링과 CinemachineBrain 담당. Player Cinemachine Camera 하나에 CinemachineCamera/PositionComposer/Confiner2D/ImpulseListener/ImpulseSource/CameraFeedback이 붙는다. 별도의 바운딩용 가상카메라를 만든 것이 아니다.
- Stage01 카메라: OrthographicSize4.5, TargetOffset(1.8,1.6,0), PositionComposer Damping **(0.7,0.8,0)**, Confiner Damping0.2 및 CameraBounds. 큰 Damping이 더 늦게 따라오는 설정이다. 이후 사용자 값이 우선한다.
- CameraFeedback은 **Singleton 상속을 제거한 MonoBehaviour**이며 static Instance를 Awake=this / OnDestroy=null로 관리한다(UIManager와 같은 방식). 자동 생성/검색 getter와 DontDestroyOnLoad가 없다. 기존 독립 CameraFeedback(Singleton) 오브젝트를 제거하고 Cinemachine 카메라로 이동했으며, 원래 Impulse 설정/강도를 보존했다. Stage01/SampleScene 모두 적용했다.

### Selectable.OnDisable 배열 오류 — 원인 확인 / 에디터 우회 적용됨

- `UnityEngine.UI.Selectable.OnDisable:570 → Slider.OnDisable:448`의 IndexOutOfRangeException이 반복됐다. 처음 생성/삭제와 Play 검증만으로 재현 못했다고 마무리했으나, 이후 **프리팹 편집 스테이지 Slider와 일반 씬 Slider가 등록 인덱스를 중복 사용**하는 상태를 포착했다. 재컴파일의 OnDisable에서 예외가 났다.
- Unity 공식 UUM-150797: https://issuetracker.unity.com/issues/24342/indexoutofrangeexception-is-thrown-when-exiting-prefab-mode-and-then-play-mode . 6000.6계열은 수정 미제공, 공식 수정은6000.7.0b7/7000.0.0a8이다. 현재 Editor를 임의로 업그레이드하지 않았다.
- `Assets/Editor/SelectableRegistryWorkaround.cs`는 6000.6에서만 동작하는 InitializeOnLoad 에디터 전용 우회다. assembly reload 전/Play 상태 변경/hierarchy·project 변경 후 등록 목록을 검사하고, 불일치할 때만 reflection으로 s_Selectables/s_SelectableCount/m_CurrentIndex를 재구성한다. 패키지 원본 파일은 수정하지 않았고 플레이어 빌드에는 포함되지 않는다. 수정된 Editor 버전으로 옮기면 제거를 검토한다.
- 강화한 `AgentScripts/VerifySliderLifecycle.cs`에서 수정 전 Unregistered active Selectable: SampleScene/FlinchSlider를 재현했고, 수정 후 프리팹 모드 포함9개 등록, 생성/제거10회, 재컴파일, Play 진입/종료를 통과했다. 콘솔의 도구 누적 로그와 실제 현재 오류 개수가 다를 수 있으므로 console status의 groundTruth.consoleErrors를 본다. 필요 시 UnityEditor.LogEntries로 실제 콘솔을 읽는다.
- 갑작스러운 컴퓨터 종료로 Monster.cs/MonsterStatUI.cs가 전부 null byte가 된 적이 있다. 종료 전 읽은 코드와 Git 기준으로 복구했으며 관련 변경 텍스트40개를 검사하고 컴파일·UI 검증을 통과했다. 이 손상은 복구된 과거 이력이다.

### 검증·현재 상태·도구 주의

- 작업별 검증: 몬스터 HP 즉시/흰 바 지연·감소/리셋, 기절 저항3→2→1→0/리셋, 처형 UI 활성/비율/HP변경/초기화; ref 전투 경로 컴파일; 맵 실제 걷기/점프로 시작→보스구역 통과; CameraFeedback Instance/양쪽 Impulse 호출/종료해제; 한글 Subtitle 실제 렌더링을 확인했다.
- PlayerData 리팩터링 당시 Play에서 구조체8개가 에셋과 일치, 초기HP/기력UI, 이동·구르기 비용·기력회복·피격UI·패링·공격·버서커 진입/포효종료/종료를 검증했다. **그 후 사용자가 UI 진입점을 PlayerCharacter.Start로 옮겼다. 이번 인계는 읽기 확인만 했으며 이 최신 변경에 대한 새 Play 검증을 실행하지 않았다.**
- 인계 시 읽기 확인: Unity6000.6.7f1, port37800, Stage01, Edit(stopped), compiling=false, scene.isDirty=false, runInBackground=false. 실제 현재 콘솔 오류0/경고0. 이전 누적 로그 수가0은 아니므로 혼동하지 않는다.
- Git main의 변경에는 Player/Goblin 프리팹·데이터, 두 씬, 전투/카메라/UI 스크립트, 기존 문서, TimeManager와 TMP fallback 등이 있다. 미추적 PlayerData/Stage01/타일/폰트/몬스터·플레이어 UI/에디터 우회/AgentScripts 등을 포함한다. 사용자 변경이므로 임의 정리·커밋·푸시하지 않는다.
- AgentScripts는 Assets 밖의 수동 도구다. MigratePlayerData는 **구 컴포넌트 필드에서 옮긴 일회성 도구**이고 이미 데이터 에셋이 있으면 거절한다. BuildStage01은 초안 생성이고 최종 Tilemap 전환은 FinishStage01Tilemaps다. 새로 실행하면 현재 사용자 지형을 재구성할 수 있으므로 실행하지 않는다. ApplySubtitleKoreanFont/ConnectMonsterStatUI도 사용자 에셋을 다시 저장하므로 단순 확인 목적으로 재실행하지 않는다.
- VerifyPlayerData는 Play에서 몬스터를 잠시 비활성화하고 실제 행동을 시험한다. VerifyStage01Traversal도 전투를 끄고 이동한다. 테스트 후 Play 종료로 원복하고 Run In Background override를 off로 복원한다. 옛 검증 스크립트의 데이터값·필드명·Context 시그니처 가정은 현재에 맞춰 읽고 실행한다.
- Unity CLI는 --project-path D:/GitHub/Parry를 명시한다. 샌드박스에서 Editor 검색이 안 되면 읽기/연결 명령에 적절한 권한을 사용한다. 스크립트 실행은 `unity command script run --file ... --entry ...`다. 재컴파일/Play 직후 domain reload 동안 연결이 끊길 수 있어 status --until-ready로 완료를 확인한다. 일시적인 Hold on/Reloading Domain 창을 사용자 응답이 필요한 대화상자로 단정하지 않는다.
- CLI현재1.0.0-beta.14, stable1.0.1 업데이트가 확인됐지만 사용자 승인 없이 업데이트하지 않았다.

## 이전 기록 — 2026-11-18 플레이어 UI 단계

이 절과 실제 최신 파일이 아래 과거 기록보다 우선한다. 이번 인계 요청에서는 코드·에셋을 변경하지 않고 문서만 갱신한다.

### 다음 세션 시작과 다음 작업

- 실제 파일과 Git 상태부터 확인한다. 현재 main에 사용자 수정·미추적 파일이 있다. 이번 대화에서 커밋·푸시하지 않았다. 사용자 편집을 보존한다.
- **다음 작업은 몬스터 체력·기절(자세)·처형 게이지 UI다. 사용자가 먼저 UI를 배치하기로 했다.** 준비된 구성을 확인한 뒤 사용자 요청에 맞춰 연결한다. UI를 임의로 새로 만들거나 몬스터 전투 로직부터 변경하지 않는다.
- 사용자는 일반 몬스터와 플레이어의 기본 전투 구조를 마쳤다고 보고 UI 단계로 넘어왔다. 아래 미구현 설계는 자동 착수할 작업 목록이 아니다.
- 설명 질문과 수정 요청을 구분한다. 정상 경로에서 보장된 필수 참조에 불필요한 대체값·검사·계층을 추가하지 않는다. 이전 절의 기절 첫 타 추가 피해 철회와 기존 철자 보존 규칙도 유효하다.

### UI 연결 — 최종 합의와 실제 구현

- **UI에서 이벤트를 구독한 뒤 플레이어 컴포넌트의 인자 없는 `InitUI()`를 호출한다. 컴포넌트의 InitUI는 현재 값 이벤트만 발행한다.** 컴포넌트에서 UI 메서드를 구독하거나 UI 객체를 인자로 받던 중간 구조로 되돌리지 않는다.
- 흐름: `UIManager.Start → 플레이어 검색 → UIManager.Init(character) → 각 UI.Init(character) → 구독 → 컴포넌트.InitUI()`.
- `UIManager`는 `Singleton<UIManager>` 상속이 아닌 일반 MonoBehaviour다. Awake에서 `static Instance=this`, OnDestroy에서 `Instance=null`. 자동 검색/생성 getter와 DontDestroyOnLoad는 없다. private PlayerStatUI/ParryUI/SkillUI 참조를 관리한다.
- 게임 씬마다 UIManager를 둔다. 로비 UI는 별도 `LobbyUI`로 만들 계획이다. 로비 UI 구현은 아직 하지 않았다. 씬 전환 시 게임 씬의 새 UIManager.Start가 실행된다. 향후 플레이어가 늦게 생성되면 생성 완료 측에서 UIManager.Init(character)를 호출하는 방식으로 연결할 수 있지만 현재 스포너는 추가하지 않았다.
- `PlayerCharacter`는 Stats, ParryGauge, PlayerBerserker 참조를 Awake에서 채운다. **최신 `PlayerParryComponent => parry`는 기존 private parry를 반환한다.** 한때 자동 프로퍼티를 선언만 하고 할당하지 않아 SkillUI.Init에서 NullReferenceException이 났다. Awake/Start 순서 문제가 아니었으며 사용자가 표현식 프로퍼티로 수정했다.
- UI는 Unbind용으로 플레이어 컴포넌트 참조를 보관하고 OnDestroy에서 해제한다. **현재 사용자 파일은 PlayerStatUI.Init만 처음에 Unbind를 호출하고, ParryUI.Init/SkillUI.Init에는 이 호출이 없다.** 과거 에이전트의 반복 Init 검증이 이 최신 사용자 파일의 중복 방지를 보장하는 것은 아니다. 요청 없이 초기화 구조를 다시 변경하지 않는다.
- C# event Action은 중복 구독과 파괴된 MonoBehaviour의 관리 객체에 대한 콜백 호출이 가능하므로 구독 해제가 필요하다고 설명했다. UnityEvent와 동작을 혼동하지 않는다.

### 플레이어 기력과 체력 UI — 구현됨

- PlayerStatComponent가 maxStamina/Stamina/MaxStamina와 TryConsumeStamina를 관리한다. Awake에서 최대 기력으로 시작한다.
- PlayerCharacter.Roll은 다른 시작 조건 통과 후 기력을 소비한다. 부족하면 공격 취소·구르기 시작 전에 반환한다. 소비량과 정확히 같은 기력이면 가능하다. 패링에는 기력 조건·소비가 없다.
- 기력 소비 시 `nextStaminaRecoveryTime=Time.time+staminaRecoveryInterval`. 이후 interval마다 amount 회복, 최대 기력으로 제한한다. 다시 소비하면 대기가 다시 시작한다. **현재 구현은 Update 기반이다.** 코루틴이 자연스럽다는 설명은 했지만 기력 회복을 코루틴으로 바꾸라는 요청/수정은 없었다. 버서커 타이머 변경과 혼동하지 않는다.
- OnHpChanged/OnStaminaChanged가 현재값·최대값을 전달한다. PlayerStatUI는 HealthSlider/StaminaSlider에 SetValueWithoutNotify(current/max)로 표시한다. 초기 EnergySlider 이름은 사용자가 StaminaSlider로 변경했다.
- Time.time/Time.deltaTime을 사용하므로 포효의 timeScale=0 동안 기력 회복 대기도 멈춘다.

### 패링·버서커 게이지 UI — 구현됨

- `ParryUI`가 ParryGaugeUI/BerserkerGaugeUI 참조, 플레이어 컴포넌트 참조, 이벤트 연결, 모드 전환을 담당한다. **각 Gauge UI에 Init(character)를 옮긴 변경은 사용자 요청으로 되돌렸다.**
- `ParryGaugeUI`: Slider/gaugeMark/gaugeGlow 보관. `SetGaugeMark(cost,maxGauge)`로 눈금 기준을 설정하고 `SetValue(normalizedGauge)`로 값과 준비 완료 발광을 표시한다. BerserkerGaugeUI는 Slider/빨간 Glow와 값 표시만 담당한다.
- 현재 세로 BottomToTop 슬라이더다. 눈금은 Fill Area 아래의 Image이며 부모 영역의 가로 중앙, 비용/최대 게이지 높이에 앵커를 둔다. Glow는 기존 슬라이더 위에 겹친 Image다.
- 사용자 이미지 `Assets/Art/UI/ParryGaugeMark.png`, `GaugeGlow.png`를 사용한다. 흰 Glow를 Image 색으로 패링 파랑·버서커 빨강으로 표시한다. Glow 스프라이트의 border는 Sprite Editor 데이터 제공자를 통해 (8,8,8,8)로 설정했고 Image.Type=Sliced다. 원본 PNG 색을 편집한 것이 아니다.
- 평소 패링 게이지, 버서커 진입 시 버서커 게이지만 켠다. EnterBerserker/ExitBerserker 이벤트로 전환한다. 모드와 게이지 값을 합친 bool/float 매 프레임 표시 전환 구조는 제거했다.
- 파랑 Glow는 패링 게이지가 비용 이상일 때 켠다. 이는 게이지 충족 표시이며 공격·쿨타임 등 모든 발동 조건을 의미하지 않는다. 빨강 Glow는 버서커 게이지가 0보다 클 때 켠다. 맥동/블룸/추가 애니메이션은 구현하지 않았다.

### 버서커 시간과 패링·피격 — 구현됨

- **RemainingSeconds는 float**, NormalizedRemainingTime은 `RemainingSeconds/duration`이다. 포효가 끝난 뒤 Update에서 Time.deltaTime만큼 감소한다. 0이면 EndBerserker(false). 정수 1초 Countdown 코루틴·countdownStepStartTime·보간 계산은 제거했다.
- OnRemainingTimeChanged로 UI를 부드럽게 갱신한다. 포효·게임 정지에서는 감소하지 않는다. 패링 게이지의 기존 갱신 방식은 별도로 유지했다.
- 실제 PlayerCharacter.TakeDamage에서 패링·구르기 무적·피격 무적 판정을 통과하고 Stats.TakeDamage 직후 ReduceTimeOnHit를 호출한다. 버서커일 때 timeLossOnHit만큼 감소, 소진 시 자동 종료하여 저장 참격을 버린다.
- PlayerParry.TryParryHit 성공 시 버서커 중이면 RecoverTimeOnParry, 평소에는 gauge.GainOnParrySuccess. 시간은 최초 duration까지 회복하며 버서커 중 일반 패링 게이지는 획득하지 않는다.
- **버서커 중 패링 성공 시 시간 회복은 이미 요청받고 구현한 기능이다.** 사용자가 마지막에 재확인했다. 반격의 버서커 참격 1개 저장은 별도 미연결 항목이며 이번 작업에 추가하지 않았다.

### SkillUI·쿨타임 — 구현됨 / 사용자 정상 동작 확인

- `SkillUI`는 ParrySkillSet/BerserkerInterSkillSet/BerserkerExitSkillSet의 세 SkillIconUI를 관리한다. 현재 종료 오브젝트 이름에는 끝 공백이 있으므로 이름 기반 도구 작성 시 확인한다.
- `SkillIconUI`가 BlindImage와 TMP cooldownText를 보관한다. SetCooldown은 **Time.time+쿨타임으로 계산한 사용 가능 절대 시각**을 받는다. UI가 남은 시간을 계산한다. 플레이어에서 매 프레임 남은 쿨타임을 보내지 않는다.
- 1초 초과는 올림 정수 초, 1초 이하는 올림 0.1초 단위로 표시한다. Update에서 검사하되 표시 단위가 바뀔 때만 문자열을 갱신한다. 게임 시간 기준이라 포효 중에는 쿨타임 표시도 멈춘다.
- PlayerParry.OnCooldownChanged: 패링 시도 때 Time.time+parryCooldown, 성공할 때 Time.time+successCooldown으로 다시 발행한다. 실패한 시도는 쿨타임을 새로 만들지 않는다.
- PlayerBerseker.OnCooldownChanged는 **버서커 종료 시** Time.time+cooldown을 발행한다. 기존 종료 후 쿨타임 규칙을 바꾼 것이 아니다. 각 컴포넌트.InitUI에서도 현재 사용 가능 시각을 발행한다.
- R은 버서커 전이면 진입 아이콘, 버서커 중이면 종료 아이콘만 활성화한다. 종료 아이콘은 BlindImage/쿨타임 텍스트가 있지만 사용하지 않는다.
- 진입 아이콘의 Blind 조건은 `coolingDown || !resourceReady`, 텍스트 조건은 **coolingDown만**이다. 패링 게이지 부족과 관계없이 남은 쿨타임 숫자는 끝까지 보이고, 쿨타임이 끝난 뒤 게이지가 부족하면 Blind만 남는다.
- **쿨타임이 안 보인다는 문의는 당시 사용자가 cooldown=0으로 설정한 것이 원인이었다. UI 버그가 아니며 사용자가 현재 정상 동작한다고 확인했다.** 미해결 버그로 인계하지 않는다.

### 마지막 확인 값·검증 범위

- 2026-11-18 인계 직전 실제 Player.prefab 및 열린 씬: 버서커 비용30/지속10/쿨타임180, 피격 시간 감소1/패링 시간 회복1, 최대 기력100/회복 간격1/회복량10, 구르기 비용20. Player.prefab 시작 패링 게이지20. 사용자 값이므로 과거 비용1/쿨타임0/게이지100으로 되돌리지 않는다.
- Git 작업 트리에는 Player.prefab, SampleScene, Player 관련 스크립트, UIManager, ProjectSettings/TimeManager.asset 수정과 UI 스크립트·이미지 미추적 파일이 있다. TimeManager 변경도 사용자 변경으로 보존한다.
- 각 작업 당시 컴파일·Play 검증: 체력/기력 초기 표시와 갱신, 회복 대기/재소비/상한, 버서커 연속 시간 감소와 정지/만료, 패링 시간 회복/상한/일반 게이지 미획득, 실제 피격·무적 경로, 눈금/파랑·빨강 Glow, SkillUI 표시 단위·성공 쿨타임 교체·R 전환·게이지 조건·자동 쿨타임 완료를 확인했다. 이후 사용자 직접 수정 전체를 과거 테스트가 보장하지 않는다.
- 이번 인계에서는 새 Play 테스트를 실행하지 않았다. 읽기 확인 결과 SampleScene, Edit 모드, scene.isDirty=false다. 테스트 때 켰던 Run In Background override는 off로 복원했다.
- Unity 도구가 Start 직후 Busy/연결 끊김을 반환할 수 있다. domain reload/컴파일 완료를 확인한다. 백그라운드에서 프레임이 멈추면 단순 스크린샷만으로 실행을 검증하지 않는다. 필요 시 unity-cli의 references/playmode-verification-loop.md를 따른다. CLI 버전 업데이트는 사용자 승인 없이 실행하지 않았다.

## 현재 구현과 대화 — 2026-11-17 우선 읽기

이 절과 실제 최신 파일이 아래 과거 기록보다 우선한다. 이번 요청은 다음 세션 인계이며, 런타임 코드·에셋은 수정하지 않고 문서만 갱신했다.

### 다음 세션 시작과 사용자 작업 방식

- 관련 실제 파일과 Git 상태를 먼저 읽는다. 사용자가 코드·프리팹·씬·데이터·클립을 계속 직접 편집하며, main에 수정·미추적 파일이 많다. 사용자 변경을 보존한다. 이번 대화에서 커밋·푸시하지 않았다.
- 코드 수정 요청과 설명·확인 요청을 구분한다. 정상 흐름에서 이미 보장된 타입·입력·데이터 범위를 재검사하거나 불필요한 계층을 추가하지 않는다. 호출 경로와 소유 관계를 확인하고 필요한 검사만 둔다.
- **몬스터 기절 후 첫 공격에 폭발적인 추가 피해를 주는 설계는 철회됐다. 구현하지 않는다.** 버서커가 있으므로 별도의 기절 첫 타 보너스는 사용하지 않는다.
- 현재 구현을 인계받고 다음 요청을 기다린다. 아래 미구현 항목은 사용자 요청 없이 자동 착수할 작업 목록이 아니다.

### 버서커 공격 데이터와 피해 전달 — 구현됨

- 클래스명은 `PlayerBerseker`다. 사용자 코드의 `burserkerHitVfx` 등 기존 철자를 임의로 고치지 않는다.
- `PlayerAttackData`는 상속 가능한 클래스이고, `BerserkerAttackData : PlayerAttackData`에 최소 1인 `slashStackCount`가 있다. 일반·버서커 각각 3개 데이터가 `PlayerAttack`에 연결되어 있다. 현재 버서커 1·2·3타 중첩은 1·1·2다.
- `StartAttack(index)`에서 재생을 시작할 때만 버서커 여부로 공격 데이터를 선택한다. 타수·콤보 예약을 공유한다. 모드가 도중에 끝나도 현재 클립은 끝내고, 다음 타수는 일반 공격으로 이어진다.
- `Player_BerserkerAttack1/2/3.anim`은 당시 실제 일반 클립을 1.6배속으로 복사했다. 스프라이트 키·판정·예약·종료 이벤트를 1/1.6로 압축했다. 기존 적중 후 0.7배속 감속은 유지한다.
- 버서커 데이터의 피해량은 일반 원본과 동일하게 만들었고 판정 크기는 가로·세로 20% 확대했다. 현재 사용자가 조절할 수 있는 에셋 값이 우선한다. Attack Preview는 일반·버서커 데이터를 모두 읽는다.
- `PlayerAttack.berserkerStoredDamagePercent`는 추가 저장할 최종 피해 비율(현재 50%)이다. 직접 피해를 줄이는 비율이 아니다.
- 사용자 `AddBerserkerInfo(ref MonsterHitContext context)`는 `IsBerserking=true`, `Berseker=berseker`, `berserkerAttacks[attackIndex].SlashStackCount`, 최종 피해×비율을 직접 대입한다. 호출부에서 적중 시점의 버서커 여부를 검사한다. 불필요한 타입 검사나 0 대체값을 복원하지 않는다.
- `MonsterHitContext`는 수정 가능한 struct. 기존 Damage/AttackType/IsCritical 외에 IsBerserking/Berseker/StoredDamage/SlashStackCount를 가진다. **IsBerserking은 참조와 별개로 직관적인 표시용으로 계속 유지하기를 사용자가 원한다.**

### 몬스터 중첩·이벤트·Burst — 구현됨

- 별도 `MonsterBerserkerDamage` 컴포넌트는 사용자 요청으로 제거했다. `Monster`가 `totalDamage`, `count`, `storedBerseker`를 직접 관리한다.
- 첫 저장 때만 해당 버서커의 `OnBurstStoredSlashes(Action<GameObject>)`, `OnDiscardStoredSlashes(Action)`를 구독한다. 이후 적중은 합산만 한다. 발동·소멸·초기화·비활성화 시 저장값과 구독을 정리한다.
- 버서커는 발동/소멸 신호만 보낸다. 수동 종료 이벤트는 버서커에 등록된 Burst 이펙트 프리팹도 전달한다. 시간 종료는 저장값을 버린다.
- 저장 count에는 상한이 없다. 발동 때 `burstCount = Mathf.Min(count, 10)`, `burstDamage = totalDamage / burstCount`로 계산한다. 분모에 원래 count를 사용하지 않는다. 총 피해를 유지하며 최대 10번 출력한다.
- 현재 Burst 간격은 고블린·허수아비 모두 0.1초다. 사용자 `BurstDamageRoutine`은 매 타격 후(마지막 포함) 대기하고 처형 중이면 끝에 사망 상태로 간다. HP가 0이어도 숫자는 모두 출력하며 `ApplyDamage(float)`는 이미 죽었으면 체력 변경만 생략한다.
- 숫자와 체력 감소를 분리했다. `TakeNormalDamage`는 context로 숫자 생성 후 `ApplyDamage(context.Damage)`, `TakeBurstDamage`는 float로 숫자 생성 후 `ApplyDamage(damage)`를 호출한다. Burst에서 숫자 표시용 context를 새로 만들지 않는다.
- `GetDamageSpawnPosition()`은 `bodyCollider.bounds`의 X/Y 범위에서 무작위 Vector3(z는 bounds.center.z)를 반환한다. 일반·Burst 모두 사용한다. `damageSpawnPos` 필드와 두 몬스터 프리팹의 동명 자식은 제거했다.
- `DamageNumberSpawner.Spawn(context, position)`은 숫자 타입 선택을 유지하며, float를 받는 오버로드로 위임한다. Burst는 float 오버로드를 직접 사용한다.

### 처형·사망과 VFX — 구현됨

- 피격에서 직접 피해 이후의 예상 체력 `remainingHp = Max(0, hp-context.Damage)`를 먼저 계산한다. 현재 처형 조건은 **`totalDamage + context.StoredDamage > remainingHp`**(엄격한 초과)다. 같을 때 실행되는 조건으로 임의 변경하지 않는다.
- 처형 가능하면 먼저 `EnterExecutionState`와 처형 이펙트 생성, 이후 직접 피해·중첩 저장·Burst를 진행한다. 해당 대상만 처형하고 버서커와 다른 대상의 저장값은 유지한다.
- `MonsterExecutionState`: 공격·이동 중단, `Goblin_Execution.anim` 재생. 이 클립은 Goblin `Take Hit_2` 한 장을 유지한다. `IsExecuting` 동안 외부 피격을 거절하고 HP 0이 되어도 Burst 숫자 출력을 끝낸다.
- 출력 완료 후 `MonsterDeadState`로 전환한다. 사망 애니메이션 재생 시 Rigidbody2D.simulated=false. `Goblin_Death.anim`은 원본 Death 4프레임이며 마지막 `EndDeath` 이벤트가 Destroy한다. 일반 치명타도 사망 상태를 거친다.
- 고블린은 Brain의 FSM, AI 없는 허수아비는 Monster의 애니메이션 경로로 처형·사망을 처리한다. 더미에 AI를 추가한 것이 아니다.
- Burst 이펙트는 `Slash_X Small` 원본으로 만들었고 **현재 프리팹 이름은 사용자가 변경한 `Assets/Prefabs/Effects/BurserkerHit.prefab`**다. 애니메이션·컨트롤러 파일은 `Slash_X_Small` 이름이다. 실제 Player.prefab 참조를 사용한다.
- 처형 이펙트는 `Hit_Heavy Small` 원본의 `Execution_Effect.prefab`이다. 둘 다 15fps 단발, 끝 `EndEffect` → `EffectLifetime.Release()`로 기존 풀에 반환한다.
- 공통 스킬 이펙트 프리팹은 버서커가 소유한다. Monster는 수동 Burst 이벤트 인자로 Burst 프리팹을 받고, 처형 시 context.Berseker.ExecutionEffectPrefab으로 처형 프리팹을 얻는다.
- **Burst·처형 이펙트는 부모 없이 몬스터의 현재 transform.position에 1회 생성한다.** 몬스터 사망 시 자식 이펙트를 분리하는 처리는 제거했다. 부모가 없어 사망 뒤에도 재생·풀 반환할 수 있다. 오러처럼 추종 효과만 부모를 쓴다.

### 몬스터 Flinch — 구현됨 / 최신 소유 관계

- 사용자가 몬스터의 기절을 `Flinch`로 명명했다. 플레이어의 `Faint`와 구분한다.
- `MonsterData.cs`의 `[Serializable] struct MonsterFlinchData`에 `clip`, `duration`, `resistance`가 있고 MonsterData.Flinch로 읽는다. 저항 최소치는 1이다.
- **현재 고블린 에셋은 HP 8,000,000 / 기절 시간 5초 / 기절 저항 1**이다. 최초 검증 당시 저항 3과 구분하며 사용자 값을 되돌리지 않는다.
- `Monster.Initialize(stats, flinchData)`가 인스턴스별 데이터·현재 저항을 초기화한다. `Monster`가 CurrentFlinchResistance, 감소/초기화, Flinch 진입 판단을 소유한다. Brain에 현재 저항이나 감소/초기화/Flinch 판단 함수를 다시 넣지 않는다.
- **최신 사용자 편집은 `Monster.TakeDamage` 앞부분에서 ParryCounter이고 brain이 있을 때 저항을 감소시킨 뒤 일반/버서커 피해 경로로 분기**한다. 이전에 TakeNormalDamage 이후 감소하던 순서보다 최신이다.
- 일반 공격에는 저항 감소가 없다. 성공한 패링의 반격은 이미 PlayerCharacter에서 ParryCounter를 보내므로 별도의 패링 알림을 추가하지 않았다. 저항이 정확히 0이 되면 Flinch에 진입한다.
- `MonsterBrain`: FSM과 AI 관리. `MonsterFlinchState`: 공격·이동 중단, clip 재생, 게임 시간으로 duration 유지. 만료 시 Monster.ResetFlinchResistance 후 기존 WanderState의 정지 대기로 복귀한다. 독립 IdleState는 없다.
- FSM은 ChangeState가 Exit/Enter를 호출하고 매 프레임 현재 State.Tick을 실행한다. AI 조건 전이는 State.Tick에서, 처형·사망·기절 같은 전투 판단은 Monster에서 전이를 요청한다.
- `Goblin_Flinch.anim`은 이제 사용자가 만든 `Goblin_Flinch.png`의 두 스프라이트를 반복한다. **현재 사용자 설정은 Samples=2, 1초 루프**다. 최초 제작 당시의 10fps로 되돌리지 않는다.
- 새 스프라이트는 프레임 64×64, 기존은 150×150, PPU는 둘 다 64. 발 위치를 맞추려고 새 두 프레임 피벗을 custom (0.5, 0.65625) = (32,42)px로 맞췄다. 기존 Attack2_2의 발은 pivot보다 26px 아래, 새 발은 하단 16px이므로 pivotY=16+26=42다. 월드 발 높이는 모두 -0.40625다.
- 피벗은 Sprite Editor 데이터 제공자와 기능 지원 검사 후 변경했다. 스프라이트 ID/클립 참조는 유지했다. 수동 meta 편집으로 피벗을 바꾸지 않는다.

### 풀 종료 오류 — 최종 단순화 상태

- 오류 원인은 Player.OnDisable → 오러 반환 → Pool.Release의 SetParent와 부모 비활성화 처리 충돌, 종료 중 ObjectPoolManager.Instance가 관리자를 재생성하는 문제였다.
- **최종 선택은 게임 종료 중에는 반환을 하지 않는 것만**이다. `Pool.Release` 맨 위에서 ObjectPoolManager.IsQuitting이면 return한다.
- IsQuitting은 static. SubsystemRegistration에서 false 초기화와 Application.quitting 이벤트 재등록, 이벤트에서 true 설정. 관리자 인스턴스가 먼저 없어져도 정적 상태와 소유 Pool 참조로 확인 가능하다.
- 오러는 auraInstance의 PooledObject.Release를 직접 호출하며 싱글톤을 조회하지 않는다. Pool은 일반 C# 객체다.
- 중간에 추가했던 부모 비활성 상태별 파괴/보관 처리와 파괴된 보관 객체 RemoveAll은 사용자 요청으로 제거했다. 현재 정상 반환은 비활성화 → SetParent(storageParent) → 목록 추가다. 플레이 중 부모 비활성화/파괴는 종료 검사와 별개이며 현재 일반 해법을 추가하지 않았다.

### 사용자 에셋 값과 후속 범위

- 현재 Player.prefab: 버서커 비용 1 / 지속 10 / 쿨타임 0, 저장 피해 비율 50%, 게이지 감소 0 / currentGauge 100. PlayerParryGauge.Awake의 0 초기화는 사용자 주석 상태라 시작 시 Inspector 값을 유지한다.
- 게이지 배율은 일반 공격만 적용한다. 현재 버서커 중에도 패링 게이지를 얻는다. 버서커 중 패링의 시간 회복·게이지 획득 차단, 피격 시간 감소, HUD는 아직 미구현이다.
- 버서커 중 패링 반격의 참격 1개 저장은 설계에 있으나 아직 PlayerCharacter의 반격 context에 버서커 정보를 추가하지 않았다. 현재 중첩 정보 추가는 PlayerAttack의 일반 공격 경로다.
- 기절 첫 타 추가 피해는 철회됐다. 그 외 남은 설계는 BERSERKER-DESIGN.md와 실제 코드를 대조하고 다음 요청에 맞춰 진행한다.

### 검증과 인계 시 주의

- 생성/검증 도구는 Assets 밖 `AgentScripts`에 있다. VerifyExecutionStates, VerifyParryFlinchResistance, VerifyMonsterFlinch, VerifyPoolShutdown, FixGoblinFlinchPivot 등으로 각 구현 당시 검증했다. 프리팹 생성 도구는 기존 에셋 덮어쓰기용으로 실행하지 않는다.
- 실제 패링 경로의 3→2→1→0, 개체별 저항, 지속시간/회복/반복 기절, 처형 10회 숫자 출력 후 사망/Destroy, 부모 없는 VFX 반환, 활성 오러가 있는 Play 종료를 검증한 이력이 있다.
- **이후 사용자가 저항 1, 클립 Samples=2, TakeDamage 내 저항 감소 위치 등을 편집했다. 과거 검증을 현재 전체 파일의 보장으로 말하지 않는다.** VerifyParryFlinchResistance는 저항 3을 가정하고, VerifyMonsterFlinch 등은 옛 스프라이트/시간 가정을 포함할 수 있으므로 실행 전 현재 에셋에 맞춰 확인한다.
- 이번 인계에서는 새로운 Play 검증·게임 코드 변경을 하지 않았다. 마지막 에이전트 Play 검증은 종료했다. 실제 다음 작업 때 Editor 상태를 다시 확인한다.
- Unity CLI는 `--project-path D:/GitHub/Parry`를 지정한다. 현재 명령은 `unity command script run --file ... --entry ...`이며 예전 `run_script` 이름은 실패한다. 연결은 샌드박스 밖 실행이 필요했던 이력이 있다.

## 현재 구현과 대화 — 2026-11-15 우선 읽기

이 절과 실제 최신 코드가 이전 기록보다 우선한다. 사용자는 다음 세션 인계를 요청했다. 코드·프리팹·씬은 이번 인계에서 수정하지 않았고, 다음 요청을 기다린다.

### 사용자 작업 방식

- 사용자가 코드·프리팹·씬·클립을 직접 계속 수정한다. 작업 전 관련 실제 파일과 Git 상태를 다시 읽고 보존한다. main에 수정·삭제·미추적 파일이 많다. 이번 세션에서 커밋·푸시하지 않았다.
- PlayerCharacter.Attack/Parry/Skill은 각각 TryStartAttack/TryStartParry/ToggleBerserker 호출만 전달한다. 행동 조건은 담당 기능 내부에서 한 번 검사한다. 호출부에 포효/사망 검사를 중복 추가하지 않는다.
- 정상 흐름에서 불가능한 상황을 가정한 방어 코드와 중복 검사를 줄이는 것을 선호한다. 피격 분기의 berseker.EndRoar()는 제거했다. 필수 참조의 null 대체 조건·재생 전 검사 직후 Play 반환값 재검사·IsHitReacting에 포함되는 IsFainted 진입 중복 검사·미사용 damageFeedback 캐시도 제거했다.
- 실제 필요한 상태 검사·공격 이벤트 중복 방지·풀 중복 반환 방지·비활성화 정리는 유지한다. 특히 포효 중 R 재입력 차단은 필요하다. 버서커는 이미 켜져 있어 그 R은 재진입이 아니라 즉시 종료로 해석되며, 종료 후 쿨타임이 이를 대신 막아주지는 못한다.
- 질문과 구현 요청을 구분한다. 불필요한 인터페이스·상태 계층·인스턴스 역조회 Dictionary를 추가하지 않는다.

### 패링 게이지 — 구현됨

- Assets/Scripts/Player/PlayerParryGauge.cs를 Player 루트에 연결했다. CurrentGauge/MaxGauge/NormalizedGauge/AttackMultiplier, GainOnParrySuccess(), TryConsume(float)를 제공한다.
- 코드 기본값: 최대 100, 패링당 15 획득, 초당 3 감소, 최대 게이지에서 일반 공격 피해 +50%. 임시 조절값이며 확정 밸런스가 아니다.
- **현재 사용자 Player.prefab 값: maxGauge=100, gaugePerParry=15, decayPerSecond=0, maxAttackBonus=0.5, currentGauge=100. 단 Awake가 currentGauge=0으로 초기화하므로 Inspector의 100이 시작 게이지로 사용되지는 않는다.** 요청 없이 이 값을 바꾸지 않는다.
- PlayerParry.TryParryHit 성공 시 게이지 획득. PlayerStatComponent.CreateMonsterHit에서 Normal 공격만 현재 게이지 배율을 적용한다. 패링 반격은 별도 스탯을 유지한다.
- 게이지 감소·소비에 따라 배율도 즉시 줄어든다. 부족한 소비는 실패하며 일부만 소비하지 않는다. 게이지 UI는 아직 없다.

### 버서커 — 현재 구현

- 정확한 클래스/파일 이름은 PlayerBerseker / Assets/Scripts/Player/PlayerBerseker.cs다. 철자를 임의로 바꾸지 않는다.
- 기존 InputSystem_Actions의 Player/Skill(<Keyboard>/r) → MyController.OnSkill(performed만) → PlayerCharacter.Skill → ToggleBerserker. Player.prefab에 UnityEvent 연결을 완료했다.
- 코드 기본값: gaugeCost=70, duration=10초(int), cooldown=3초. **현재 사용자 Player.prefab 실제 값은 gaugeCost=1, duration=10, cooldown=30초**다. 원래 설계 비용 70과 시험 에셋 값 1을 혼동하지 않는다.
- 진입 조건: 비활성/사망/포효 중 불가. 쿨타임, 구르기·공격·패링 중, 공중·점프 예약·수직 속도, 피격 반응 중, 게임 정지 중, 클립 재생 가능 여부를 검사한다. 현재 CanEnterBerserker의 animationReady는 사용자가 clipPlayer.CanPlay만 남겼다. roarClip null 검사를 임의로 복원하지 않는다.
- 진입 성공 시 비용을 소비하고 roaring=true, 포효 재생, Time.timeScale=0, IsBerserking=true, RemainingSeconds=duration, 오러 풀 생성 순으로 진행한다. 다른 동작을 취소하고 진입하는 구조가 아니다.
- 포효 동안 공격·패링·점프·구르기·R 종료 입력을 막는다. 이전부터 누르던 이동도 포효 중에는 적용하지 않는다. 게임 시간이 멈추므로 정상 전투에서는 새 피격이 발생하지 않는다. 포효 종료 직후에는 별도 무적 없이 적의 진행 중 공격이 이어질 수 있다.
- EndRoar()는 포효 클립 끝 Animation Event 전용: StopRoar()로 roaring=false/기존 timeScale 복구/클립 Stop 후 Countdown 코루틴 시작. 버서커 지속시간은 **포효 종료 후부터** 게임 시간 1초마다 1 감소한다.
- R 재입력은 포효가 끝난 뒤 EndBerserker(true), 0초 자동 종료는 EndBerserker(false). 종료 시 nextBerserkerTime=Time.time+cooldown, 오러 반환, 코루틴 정리. 쿨타임 중에는 게이지가 충분해도 소비/재진입 불가다.
- OnDisable → EndBerserker(false). 포효 중 비활성화라면 StopRoar로 게임 정지를 복구한다. 정상 종료 때 포효는 이미 끝났으므로 다시 EndRoar를 호출하지 않는다.
- ReleaseStoredSlashes/DiscardStoredSlashes는 아직 빈 함수다. 참격 저장/실제 발동/처형, 피격 시간 감소, 패링 시간 회복, 버서커 중 게이지 획득 차단, 전용 1.5배속 공격 클립, HUD는 아직 미구현이다. 현재 패링은 버서커 중에도 기존 GainOnParrySuccess를 호출하며 이는 후속 구현 대상이다.

### 포효 클립과 게임 정지

- Assets/Animations/Player/Player_BerserkerRoar.anim: 사용자가 지정한 HeroKnight_78 → _47 → _46. **현재 사용자 편집 후 길이 약 0.45714286초, 끝 EndRoar 이벤트도 같은 시각**이다. 초기 생성 시 약 0.64초였던 기록을 최신으로 사용하지 않는다.
- AnimationClipPlayer.Play(AnimationClip, bool holdLastFrame=false, bool unscaledTime=false)는 매 재생에 Graph 시간 모드를 설정한다. 포효는 Play(roarClip,false,true), 나머지 클립은 GameTime을 사용한다.
- Time.time은 누적 게임 시간이며 되돌리지 않는다. timeScaleBeforeRoar는 정지 직전 배율을 저장해 EndRoar/비활성화에서 복구하는 값이다. 현재 코드는 항상 1로 고정하는 대신 기존 배율을 복원한다.
- MonsterBrain.Update는 Time.timeScale==0이면 Tick하지 않는다. 물리와 몬스터 클립·일반 VFX도 게임 시간 기준으로 멈춘다.
- DamageNumbers의 네 프리팹(Normal/Critical/Parry/ParryCritical)은 unscaledTime=false로 바꿔 포효 중 숫자도 정지를 따른다. 과거 unscaledTime=true 설명은 이전 기록이다.

### 오러와 타격 VFX

- Berserker_Aura.prefab 및 동명 anim/controller: Heal_Aura Small 18프레임 기반 15fps 반복. SpriteRenderer 색 (1,0.08,0.06,0.85), Player Sorting Layer/order -1, 발밑 기준 피벗. 현재 실제 에셋을 우선한다.
- 버서커가 auraPrefab/auraOffset/auraInstance를 관리한다. 기존 ObjectPoolManager.SpawnObject(prefab, transform.position+auraOffset, transform)로 추종시키고 종료 시 Release한다. 반복 오러는 PooledObject이고 자동 종료 EndEffect 이벤트는 없다.
- 일반 공격 3타(attackIndex==2)에만 Hit_Directional을 표시한다. PlayerAttack.MakeThirdAttackEffect(Vector3 pos), hit.GetComponent<Monster>(), hit.bounds.center를 사용하고 attackDirection으로 flipX한다. 사용자 제거한 랜덤 오프셋·프리팹 null 검사를 복원하지 않는다.
- Hit_Spark.prefab은 준비만 했으며 전투에는 넣지 않기로 했다. 필요할 때 나중에 연결한다.
- PlayerParry는 Shield_Bubble과 serialized parryHitPrefab(Hit_Parry)을 모두 가진다. PlayerCharacter 패링 성공 분기에서 PlaySuccessEffects(context.Attacker.transform.position). 버블은 플레이어 위치+offset/부모 추종, Hit_Parry는 공격한 몬스터 월드 위치에 생성한다. EndEffect로 풀 반환한다.
- 기존 PlayerCharacter.PlayHitStop은 시간 정지가 아니라 공격 적중 후 남은 클립의 0.7배속 감속+ShakePlayerAttack이다. 지금은 실제 적중에서만 호출하므로 IsAttacking 재검사를 제거했다. 전역 히트스톱은 사용하지 않는다. 포효 일시정지와 혼동하지 않는다.
- VFX 팩은 Assets/AssetStoreImport/AW_VFX_Mega_Pack_Free. 메타데이터 effects.json/asset_list.txt, 기본 15fps. 라이선스 개인·상업 이용/수정 허용, 에셋 자체 재배포·재판매/AI 학습/NFT 금지. LICENSE.txt를 참고한다.

### 풀과 허수아비

- ObjectPoolManager는 원본 prefab → Pool Dictionary, Pool은 비활성 List<GameObject>, maxSize 기본16. PooledObject가 소유 Pool을 직접 참조한다. CreateNewObjectAndInsert에서 AddComponent 반환값 대입은 반영됐다.
- EffectLifetime : PooledObject, EndEffect()=>Release(). 인스턴스 역조회 Dictionary/IPoolable/자기 프리팹 키는 없다. 보관 객체를 임의 Destroy한 경우 건너뛰는 기능은 현재 없다.
- Assets/Prefabs/Monster/TrainingDummy.prefab: 고블린 외형, AI·공격 없음, 체력 100만, TrainingDummy.OnEnable 초기화와 ContextMenu Reset Health. Kinematic/FreezeAll/Trigger이므로 바닥 충돌로 서 있는 것이 아니라 처음 배치한 높이에 고정된다.
- SampleScene에서 플레이어 시작 위치 오른쪽 약3유닛에 배치했다. 플레이어 공격 필터는 Trigger도 감지한다.

### 확정 설계와 다음 구현 범위

- 스킬 작업 시 BERSERKER-DESIGN.md를 먼저 읽는다. 최신 구현 범위를 갱신했다. 원래 기획서는 C:/Users/USER/Documents/Codex/2026-10-24/skill-creator-c-users-user-codex/outputs/parry-action/GAME-DESIGN.md이며 이전 스킬을 버서커 설계로 교체해두었다. 최신 구현 현황은 이 인계와 실제 코드가 우선한다.
- 일반 상태 패링은 게이지 획득, 버서커 중 패링은 게이지 획득 없이 시간 회복, 실제 피격은 시간 감소가 목표다. 후자의 연결은 아직 없다.
- 참격은 **몬스터 쪽 별도 기능이 대상별 피해 목록을 저장**하고, 해당 PlayerBerseker의 발동/소멸 이벤트를 구독하는 구조로 구현하기로 했다. 첫 저장 시 구독, 발동/소멸/사망/비활성화 시 구독 정리. 버서커는 신호만 보내고 대상별 목록을 직접 관리하지 않는다. 이벤트 자체도 아직 미구현이다.
- 일반1·2타 각1개, 3타2개, 패링 반격1개 저장. 저장 개수 상한 없음. 각 적중의 최종 피해 중 일정 비율을 저장하고 3타는 두 개에 나눠 담는다. 발동 때 난수/치명타/공격력을 재계산하지 않는다.
- 수동 종료는 전 대상 참격 순차 발동, 시간 소진은 미사용 참격 소멸. 직접 피해+참격 저장 후 저장합계>=현재 체력이면 해당 대상만 자동 처형하며 모드/다른 대상은 유지한다. 발동 목록을 저장에서 분리하고 사망 후 후속 타격은 종료한다.
- 버서커 전용1·2·3타는 기존 모션 시간1/1.5 압축과 이벤트 비례 이동을 계획했다. 적중0.7 감속 유지 시 원본 대비 약1.05배. 아직 제작하지 않았다.
- R 홀드 충전·3단계 발도·입력 시작 공격력 고정·게이지 칸·참격 개수 상한·시간 만료 자동 발동은 철회했다.

### 검증과 작업 종료 상태

- 단계별 검증: 실제 패링 Hit_Parry 위치/피해 방어/애니메이션 종료 반환/재사용, 게이지 성공 시 획득/상한/소비/배율/자연 감소, 허수아비 피격/위치 고정을 확인했다.
- 최근 검증은 VerifyRoarPause에서 실제 사용자 비용을 읽어 포효 중 내부/경유 공격·패링·R 차단, 진입 불가 상태의 미소비, 게임/몬스터 정지, 포효 종료·타이머 시작, 비활성화 시 배율 복구를 확인했다. VerifyPlayerCleanup은 공격→패링/구르기 취소와 실제 피격 중단을 확인했다. VerifyBerserkerCooldown은 수동/자동 종료 쿨타임, 거절 시 미소비, 쿨타임 만료 재진입을 통과했다.
- 조건식을 의미별 bool로 분리한 뒤 컴파일 completed를 확인했다. 이후 사용자 직접 편집(프리팹 비용1/쿨타임30/감소0, 클립 길이 변경 등)이 있으므로 과거 테스트가 최신 파일 전체를 보장한다고 말하지 않는다. 이번 인계에서는 새 Play 테스트를 실행하지 않았다.
- 마지막 확인: Unity 6000.6.7f1, 포트37800, ready, compiling=false, playMode=stopped, runInBackground=false. 프로젝트 D:/GitHub/Parry. CLI 명령은 --project-path를 지정하며 샌드박스에서는 연결 불가할 수 있어 필요한 명령을 권한 상승 실행했다.
- AgentScripts는 Assets 밖의 수동 생성/검증 도구다. BuildBersekerInput/BuildBerserkerAura/BuildBerserkerRoar, VerifyPlayerParryGauge/VerifyRoarPause/VerifyPlayerCleanup/VerifyBerserkerCooldown 등이 있다. 과거 VerifyBersekerDuration/BuildBerserkerRoar.Verify는 진입 규칙·포효 정지·쿨타임 추가 이전 가정을 포함하므로 그대로 성공한다고 보장하지 않는다. 생성 도구로 사용자 에셋을 덮어쓰지 않는다.
- Play 검증 중 변경한 설정/객체는 종료·정리한다. Edit 모드 임시 객체는 자동 제거되지 않을 수 있다. 기존 씬 객체를 임의 삭제하지 않는다.
- 과거 TMP 에디터 null 대상 오류는 임시 정리한 이력이 있고 Animator Graphs.Edge.WakeUp 오류는 Animator 창을 닫아 우회했으나 근본 원인은 미확정이다. 필요하면 아래 이전 기록을 참고한다.
- 사용자는 다음 세션에서 현재 상태를 읽고 다음 요청을 기다리기를 원한다.

## 현재 구현과 대화 — 2026-11-14 우선 읽기

이 절과 실제 코드가 최신 기준이다. 아래 2026-11-12 기록은 당시 상태이며, 특히 클래스 이름·피해 전달·구르기·피드백 설명은 이 절을 우선한다. 사용자는 세션 인계를 요청했다. 다음 요청을 기다린다.

### 작업 방식과 사용자 의도

- 사용자가 계속 코드·클립·프리팹·씬을 직접 편집한다. 다음 작업 전에 Git 상태와 관련 실제 코드를 읽고 사용자 변경을 보존한다. 현재 main이며 수정·삭제·미추적 파일이 많다. 이 대화에서 커밋·푸시는 하지 않았다.
- 클래스는 이름과 역할에 맞게 구성하고, 불필요한 중계·방어 코드·매 프레임 감시를 줄이는 것을 선호한다. 질문에 대한 설명과 구현 요청을 구분한다.
- 플레이어는 프리팹이므로 씬 카메라를 프리팹 Inspector에 직접 연결하는 구조를 원하지 않는다. 카메라 소유 구조를 여러 번 바꿨으며 최종 사용자 편집은 아래 싱글톤 구조다. 이전 중간 구조로 되돌리지 않는다.
- 마지막 대화에서는 namespace를 설명했다. C# using은 파일 포함이 아니며 다른 파일로 전파되지 않는다. 같은 namespace의 타입은 여러 파일에서 선언 가능하고 private 접근 범위는 별개다. 사용자가 요청해 전투용 enum·구조체를 CombatTypes.cs에 통합했다.

### 전투 타입과 현재 사용자 추가 변경

- `Assets/Scripts/Combat/CombatTypes.cs`, namespace `Parry.CombatClass`에 `PlayerAttackType { Normal, ParryCounter }`, `AttackHitContext`, `MonsterHitContext`가 있다. 이전 개별 AttackHitContext.cs와 MonsterHitContext.cs는 통합 후 삭제했다.
- AttackHitContext는 몬스터 → 플레이어 타격 정보다. Damage, Attacker, MonsterAttackType, CanKnockback, KnockbackDistance, CausesFaint, AttackDirection을 보관한다. float 생성자와 PlayerCharacter.TakeDamage(float, Monster) 편의 오버로드는 사용자 지시로 제거했다. 실제 타격에는 공격자가 항상 있다.
- MonsterHitContext는 플레이어 → 몬스터의 최종 피해 정보다. **사용자가 최근 AttackType을 추가했다. 현재 생성자는 `(float damage, PlayerAttackType attackType, bool isCritical = false)`다.** Damage와 IsCritical도 보관한다.
- PlayerStatComponent.CreateMonsterHit의 **현재 인자는 `(float baseDamage, PlayerAttackType attackType)`**다. PlayerAttack.ApplyAttackHit은 Normal, 패링 반격은 ParryCounter를 전달한다. 이 최신 사용자 변경 이후 전체 Play 검증을 다시 실행하지 않았다.

### 넉백과 기절 용어·공격 데이터

- 사용자 정의로 기존 stagger(움찔)는 **knockback(넉백)**, 기존 knockdown(쓰러짐)은 **faint(기절)**로 재명명했다.
- MonsterAttackData의 `CanKnockback`은 넉백 모션·밀림·행동 잠금을 적용할지 결정한다. 꺼지면 밀림도 없다. KnockbackDistance는 별도 거리다.
- Normal은 기절할 수 없고 Heavy는 무조건 기절한다. `CausesFaint => attackType == MonsterAttackType.Heavy`로 계산하며 기절 bool을 Inspector에서 선택하지 않는다. 두 종류 모두 CanKnockback은 선택 가능하다.
- `Assets/Editor/MonsterAttackDataEditor.cs`는 CanKnockback을 켰을 때만 거리를 표시하고, Heavy 선택 시 항상 기절한다는 안내를 표시한다. 이전 직렬화 이름은 FormerlySerializedAs로 읽는다.
- PlayerHitReaction의 이름은 `IsFainted`, `faintClip`, `faintHoldDuration`, `bIsInvulnerableWhileFainted`, `hitInvulnerabilityEndTime`이다.
- StartReaction에서 CancelReaction 호출은 주석 상태다. 넉백 또는 기절이 있으면 ReactionRoutine을 시작한다. 넉백만 있을 때도 코루틴이 종료되어 행동 잠금이 풀리도록 수정했다. 기절 클립과 유지 시간은 WaitForSeconds(faintClip.length + faintHoldDuration)으로 합쳐졌다.
- 공격자와의 X 차이가 0.01보다 크면 멀어지는 방향으로 밀고, 거의 겹치면 전달받은 AttackDirection을 사용한다. 공격자 null·방향 0 보정은 제거했다.
- PlayerMovement.FixedUpdate의 구르기는 FacingDirection * rollSpeed, 공격 중 수평 속도는 0이다. 피격 반응 분기에서 넉백 속도를 적용한다.

### 구르기와 패링

- Animator의 구르기 상태·전환·IsRolling 파라미터, PlayerRollState.cs/.meta를 제거했다. PlayerAnimation도 구르기 파라미터를 전달하지 않는다.
- PlayerCharacter.Roll이 공용 AnimationClipPlayer로 rollClip을 한 번 재생한다. 이동은 기존 rollSpeed를 사용한다. 별도 애니메이션 속도 변수는 추가하지 않았다.
- **구르기 종료를 Update에서 감시하지 않는다.** Player_Roll.anim의 끝 0.3초에 `EndRoll` 이벤트가 있다. public EndRoll이 상태·쿨타임·무적을 정리하고 자신의 구르기 클립을 Stop한다. 피격·비활성화 때도 EndRoll을 호출한다.
- 구르기 시작 시 rollInvulnerabilityEndTime을 계산한다. IsRollInvulnerable은 IsRolling과 현재 시간을 확인한다. 무적 시간이 끝나면 구르기 중에도 피해를 받는다. 코드 초기값 0.1초, **현재 Player.prefab 값은 사용자 편집으로 0.2초**다. 실제 에셋을 확인한다.
- 패링 성공 시 유효 창을 유지하고 성공 쿨타임을 갱신하는 기존 합의는 유지된다. 성공할 때마다 `Stats.CreateMonsterHit(Stats.ParryCounterDamage, PlayerAttackType.ParryCounter)`를 계산해 context.Attacker.TakeDamage에 전달한다.
- PlayerStatComponent의 디버그 ContextMenu는 Stats 체력만 직접 차감한다. PlayerCharacter의 방어·피격 피드백을 통과하지 않는다.

### 몬스터 스탯과 클립

- MonsterData에 `[Serializable] MonsterBaseStats { maxHp, moveSpeed }`를 추가했다. MonsterBrain.Awake에서 연결된 데이터의 BaseStats를 읽어 Monster.Initialize와 MonsterMovement.Initialize에 전달한다.
- Monster의 hp는 런타임 개체별 private 필드이며 Hp로 조회한다. 공유 ScriptableObject 체력을 깎지 않는다. Goblin.asset 현재 maxHp는 사용자의 기존 시험값 **8,000,000**, moveSpeed는 2다.
- Monster.TakeDamage는 MonsterHitContext를 받는다. 체력 감소와 숫자 생성 후 체력이 0이면 비활성화한다. 생존 중 기존 공격·AI·속도를 중단하지 않는다. 빨간 플래시 코루틴은 StartCoroutine(HitFlash())로 직접 호출한다.
- **사용자가 최근 Monster에 serialized Transform `damageSpawnPos`를 추가했다.** 숫자 위치는 이 Transform.position을 사용하며 null 보정은 없다. Goblin.prefab에 연결되어 있다. 임시 몬스터를 만드는 검증 스크립트도 이를 고려해야 한다.
- 고블린이 맞으면 공격이 끊기는 것처럼 보인다는 문의가 있었다. 공격 중간에 직접 피해를 주어 동일 클립 시간이 계속 진행됨을 확인했다. 사용자는 공격 마지막 자세에서 대기로 급격히 돌아오는 스프라이트 때문으로 판단했다.
- Attack1·Attack2 끝에 기존 자세를 재사용한 복귀 두 프레임을 넣었다가, **사용자 요청으로 Attack1만 원래 상태로 복원**했다. 현재 Attack1은 1초, Attack2는 복귀 두 프레임을 유지해 약 0.85714287초다. ApplyAttackHit 이벤트 시점은 변경하지 않았다. 늘어난 클립 길이 뒤에 기존 후딜레이가 시작한다.

### 카메라 피드백과 싱글톤의 최종 구조

- Cinemachine 패키지를 설치했다. 현재 manifest/PackageCache의 버전은 **6.6.0**이다. Main Camera의 CinemachineBrain, 별도 Player Cinemachine Camera와 CinemachineImpulseListener를 구성했다. 기존 화면 구도를 유지했다.
- 직접 Update/LateUpdate에서 위치를 흔들던 CameraShake.cs는 삭제했다. Noise Basic Multi Channel Perlin도 사용 가능하다고 설명했으나 사용자는 **Impulse Listener 방식**을 선택했다.
- **최종 현재 경로는 `PlayerCharacter.TakeDamage → PlayerDamageFeedback.PlayHitFeedback → CameraFeedback.Instance.PlayShake → CinemachineImpulseSource → 활성 카메라 ImpulseListener`다.** PlayerController는 입력/캐릭터 참조 역할로 돌아갔고 카메라 Inspector 참조·중계 함수가 없다. Source는 플레이어에도 가상 카메라에도 없고 씬의 공용 CameraFeedback 오브젝트에 있다.
- 사용자가 최종적으로 CameraFeedback도 Singleton<CameraFeedback>으로 변경했다. PlayerDamageFeedback은 캐시 없이 효과 호출 순간 Instance를 사용한다.
- CameraFeedback.Awake는 protected override이며 base.Awake 호출 후 Source를 가져온다. PlayShake는 `GenerateImpulseWithForce(impulseIntensityWithPlayerHit)`를 사용한다. 사용자는 Test Invoke에서 5가 적절하다고 했지만 **현재 코드 기본값과 씬 저장값은 1**이다. 사용자 값을 임의로 5로 덮어쓰지 않는다.
- `Assets/Scripts/Singleton.cs`는 lazy Instance 검색, 없으면 GameObject/AddComponent 자동 생성, DontDestroyOnLoad 구조다. Awake 중복 검사는 instance != null && instance != this이며 OnDestroy에서 자신의 static 참조를 해제한다. FindAnyObjectByType를 사용한다.
- lazy 자동 생성으로 만들어진 매니저는 Inspector 설정이 없는 기본값을 사용한다. 자식 Awake에서는 부모 초기화를 명시적으로 호출해야 한다고 설명했다. UIManager와 DamageNumberSpawner도 이 싱글톤을 상속한다.

### UI와 Screen Damage 제거

- 최상단 UIManager GameObject에 Canvas(Screen Space Overlay), CanvasScaler(Scale With Screen Size, 1920×1080, Match 0.5), GraphicRaycaster와 UIManager 스크립트를 두었다. 하위 UI는 패널/RectTransform이며 Stretch를 사용한다. 별도 Canvas였던 효과 인스턴스는 씬에서 unpack했다.
- Screen Damage를 잠깐 연결했다가 사용자 요청으로 **PlayerScreenDamage 전용 프리팹·스폰/재생 연결·검증 스크립트를 제거**했다. imported `Assets/AssetStoreImport/Screen Damage/` 원본은 남아 있지만 게임 피드백에 연결하지 않는다. 사운드는 사용자 지시로 작업하지 않는다.
- PlayerDamageFeedback이라는 이름은 현재 카메라 피격 피드백용으로 다시 사용 중이다. 옛 Screen Damage 생성 코드와 혼동하지 않는다. UIManager 클래스는 현재 빈 싱글톤이며 카메라 흔들림을 담당하지 않는다.

### Damage Numbers Pro와 치명타·난수

- 에셋 위치는 `Assets/AssetStoreImport/DamageNumbersPro/`다. 숫자 스폰은 Asset API `DamageNumber.Spawn(position, damage)`를 사용한다. 에셋 자체 enablePooling, PrewarmPool, 수명 종료 반환/재사용을 확인했다.
- PlayerStatComponent에 parryCounterDamage(기본 10), criticalChance(기본 10%), damageVariancePercent(기본 ±10%)가 있다. **기본 피해 × 난수 → 치명타 시 1.5배 → 정수 반올림** 순서이며 양수 기본 피해는 최소 1이다. 일반 공격과 패링 반격 모두 같은 CreateMonsterHit 함수를 사용한다. 각 대상의 적중 때 계산한다.
- ±10%는 게임별 난수 방식 조사 후 이 프로젝트의 시작값으로 선택했다. 조절 가능한 스탯이며 사용자가 확정한 고정 규칙은 치명타 1.5배다. 조사 참고는 https://wiki.guildwars2.com/wiki/Damage_calculation 이다.
- DamageNumberSpawner 싱글톤이 context.AttackType과 IsCritical로 프리팹을 선택한다. **사용자가 일반·치명타 외에 패링·패링 치명타 분기를 직접 추가해 현재 네 프리팹을 선택한다.** 씬의 네 필드는 모두 연결되어 있다.
- 프리팹은 `Assets/Prefabs/UI/DamageNumbers/{NormalDamage,CriticalDamage,ParryDamage,ParryCriticalDamage}.prefab`이다. normal은 노랑, critical은 빨강이다. 에셋의 2D Pixel Text / Pixel-Shadow 폰트를 기반으로 생성했다.
- 일반·치명타의 생성 시 설정은 lifetime 0.65초, unscaledTime true, 좌우 속도 -0.9~0.9 / 상향 1.4~2 / 중력 4, 회전·추가 흔들림·합치기 비활성이다. poolSize 24, maxActiveInstances 20이다. 실제 프리팹은 사용자 편집이 있으므로 다시 확인한다.
- 현재 Spawner.Start는 **일반·치명타 두 프리팹만 PrewarmPool**한다. 사용자 추가 패링 프리팹은 아직 사전 풀 생성 호출을 추가하지 않았다. 패링 프리팹 자체의 풀링 설정도 다음 작업 시 확인한다.
- 사용자가 ParryDamage 색을 바꾸지 못한다고 했을 때, DNP Number Settings.customColor가 TMP 색상을 노랑으로 덮어쓰고 있었다. **ParryDamage는 #2EB1FF로 설정**했고 노란 rich-text 태그·TMP 그라데이션을 정리했다. 패링 치명타 색은 사용자 에셋을 확인한다.
- TMP Essential Resources가 없어 TMP 초기화가 중단되어 숫자 메시가 비던 문제를 발견했다. `TMP_PackageResourceImporter.ImportResources(true, false, false)`로 `Assets/TextMesh Pro/` 기본 리소스를 추가한 뒤 실제 메시 생성과 Game 화면을 확인했다. `Temp/DamageNumbersPreview.png`는 임시 확인 캡처다.
- TMP_BaseEditorPanel.OnEnable의 SerializedObjectNotCreatableException 문의 때 target=null인 TMP_EditorPanel 인스턴스 하나를 확인해 제거하고 Inspector tracker를 갱신했다. 그 뒤 재발 인스턴스가 없고 현재 Console 오류 0을 확인했다. null이 생긴 최초 계기는 확정하지 않았다. 패키지 코드를 수정하지 않았다.

### 미리보기 도구

- 공격 미리보기 메뉴는 **Tools > Parry > Attack Preview 하나**다. 기존 MonsterAttackPreviewWindow 클래스가 몬스터와 플레이어를 함께 지원한다. OpenPlayer와 두 개의 메뉴는 제거했다.
- 몬스터의 공격 목록은 MonsterBrain.Data.Attacks, 플레이어는 PlayerAttack의 serialized attacks 목록에서 읽는다. Animator Controller의 모든 클립을 자동 검색하는 구조가 아니다.
- 별도 **Tools > Parry > Animation Preview** 창은 AnimationPreviewWindow.cs다. 오브젝트와 클립을 직접 지정하고 재생/일시정지/프레임 이동한다. 2D SpriteRenderer 계층을 임시 복제하여 Scene에 표시한다.
- 원본과 4유닛 떨어진 복제본에 Animator를 함께 구성해 클립을 샘플링한다. 처음 구현에서 Animator가 없어 시간이 흐르지만 스프라이트가 안 바뀌던 문제를 재현해 수정했다. 샘플링·원본 보존·창 종료 정리를 검증했다. 실제 게임 오브젝트를 재생하는 기능으로 혼동하지 않는다.

### 검증 범위와 다음 세션의 주의점

- VerifyClipRoll은 단발 클립·이동속도·EndRoll 이벤트 종료·쿨타임 재사용·비활성 정리를 통과했다.
- VerifyMonsterBaseStats는 Awake 체력/속도 초기화·개체별 체력·공유 데이터 보존을 통과했다.
- VerifyParryCounterDamage는 당시 고정 반격 피해·공격자 선택·몬스터 공격/상태/속도 유지·치명타 사망을 통과했다. 이후 난수·치명타·damageSpawnPos 등 변경이 있어 현재 그대로 재실행 가능한 상태라고 보장하지 않는다.
- VerifyHitCameraShake는 실제 피격 후 카메라 이동·무적 타격 차단·원위치 복구를 여러 구조 변경 후 검증했다. 이후 사용자가 최종 싱글톤 구조로 바꾸고 부모 Awake 호출을 수정했으며 컴파일 완료를 확인했다. 최신 상태 전체 Play 검증은 별개다.
- VerifyDamageNumbers는 일반/치명타 계산·색상·실제 TMP 메시·±10% 난수·일반 공격 ApplyAttackHit와 중복 방지·패링 반격·움직임·풀 반환/재사용을 통과했고 Game 캡처로 노랑/빨강 픽셀 숫자를 확인했다. **이 검증은 최근 사용자 AttackType 인자 추가 이전이다. 현재 스크립트에 CreateMonsterHit(100f) 호출들이 남아 있어 인자를 맞춰야 한다.** 임시 몬스터 생성 스크립트는 damageSpawnPos 연결도 고려한다.
- AgentScripts는 Assets 밖의 수동 검증 코드이며 일반 게임 빌드에 포함되지 않는다. 스탯·위치·AI를 일시적으로 바꾸므로 테스트 Play 세션에서만 실행한다. 오래된 검증은 API 변경 때문에 그대로 실행되지 않을 수 있다. 요청 범위 없이 일괄 재작성하지 않는다.
- 이번 인계에서는 실제 최신 코드를 읽었고 새 Play 테스트를 실행하지 않았다. Unity 상태 확인 결과 6000.6.7f1, 포트 37800, ready, 컴파일 false, Play stopped, Run In Background false였다.
- CLI는 프로젝트 소유권/샌드박스 문제로 필요한 Unity 명령을 권한 상승하여 실행했다. 모든 명령에 `--project-path D:/GitHub/Parry`를 지정한다. 로컬 CLI beta.14의 1.0.0 업데이트 알림이 있지만 self-update는 승인/실행하지 않았다.
- 현재 명령 목록을 확인하고 `unity command script recompile` 후 recompile_status completed/up_to_date를 확인한다. triggered가 지속되면 AssetDatabase.Refresh(ForceUpdate) 후 연결·상태를 재확인하는 방식이 통했다. 연결이 끊기는 도메인 리로드와 실제 실패를 구분한다.
- 후속 후보로 몬스터 타격 파티클·히트스톱·공격 적중 카메라 Impulse를 논의했다. **아직 구현하지 않았다.** 플레이어 피격·몬스터 숫자 표시 이후 다음 요청에 따라 진행한다. 소리는 아직 작업하지 않기로 했다.

## 현재 구현과 대화 — 2026-11-12 우선 읽기

이 절이 최신 기준이다. 사용자는 마지막으로 세션 인계를 요청했다. 다음 요청부터 해당 범위의 작업을 진행한다. 아래 2026-11-11 기록과 원래 기획서에 남아 있는 공격 우선순위·패링·피격 설명은 이 절 및 현재 코드와 대조한다.

### 다음 세션 시작

1. 이 절과 Git 상태, 요청에 해당하는 실제 코드를 읽는다. 사용자 편집과 미추적 파일을 보존한다.
2. 이번 세션에서 만든 몬스터 공격·패링·피격·미리보기 구현은 아래 파일들에 있다. 사용자도 계속 파일과 클립을 직접 수정한다.
3. Unity 작업은 `unity-pipeline` 스킬과 현재 명령 목록을 확인하고 진행한다. 연결·컴파일·Play 상태를 매번 확인한다.
4. 원래 전체 기획서는 `C:/Users/USER/Documents/Codex/2026-10-24/skill-creator-c-users-user-codex/outputs/parry-action/GAME-DESIGN.md`다. 이번 세션의 변경 사항은 프로젝트 인계문에 기록했으며 원래 기획서는 이번에 갱신하지 않았다. 최신 사용자 합의는 이 절을 우선한다.

### 사용자 의도와 작성 선호

- 목표는 던전에서 적 한 마리씩 순차적으로 상대하며 패턴을 읽는 전투다. 사용자는 엘든 링·몬헌과 같은 전투 감각을 참고하고 있다.
- 복잡한 조건은 의미 있는 bool이나 함수로 풀고, 상황과 역할을 한글 주석으로 설명한다. 컴포넌트 참조와 런타임 상태를 묶고 생명주기 함수는 일반 함수보다 위에 둔다. 사용자가 직접 바꾼 순서와 주석을 확인한다.
- 공격 목록은 `List`를 선호한다. 공격 선택은 가능한 인덱스를 재사용하는 `List<int>`에 모은 뒤 하나를 뽑는 방식이다.
- 사용자는 호출 시점과 변수 의미를 배우고 있다. 코드 수정 요청과 설명 요청을 구분한다. 패링 성공 시 판정 창을 닫는 처리를 임의로 추가했다가 사용자 지시로 제거한 이력이 있다.
- 마지막 구현 요청은 밀림 거리와 움찔 여부를 공격 데이터에 넣고, 움찔 중 모든 행동을 막는 것이었다. 구현과 Play 검증까지 완료했다.

### 몬스터 공격·데이터

- `Assets/Scripts/Monster/MonsterAttackData.cs`에 클립, 피해량, `AttackType`(Normal/Heavy), `CausesStagger`, `KnockbackDistance`, `KnocksDown`, 거리 조건·사거리, 쿨타임, 후딜레이가 있다. Inspector 변수에는 한글 Tooltip을 넣었다.
- 기본 공격/패턴 구분과 패턴 우선 선택을 없앴다. `MonsterData.Attacks` 한 목록에 등록하고, 모든 공격에 동일한 쿨타임 규칙을 적용한다. 쿨타임 0은 재사용 대기 없는 공격이다.
- `MonsterAttack.Initialize`는 목록을 새 List로 복사한다. 데이터 에셋 참조는 공유하고, `attackReadyTimes`는 몬스터 인스턴스마다 따로 보관한다.
- `TryStartAttack`은 거리 조건과 쿨타임이 맞는 실제 인덱스를 `availableIndices`에 모으고, `Random.Range(0, availableIndices.Count)`로 목록 위치를 뽑는다. int 최댓값은 제외된다. 목록이 비어 있으면 공격하지 않는다.
- 시작 순간 클립을 재생하고 방향을 고정하며 쿨타임을 시작한다. Brain은 성공하면 `MonsterAttackState`로 전환한다.
- `MonsterAttackState.Enter`는 이동을 멈추고 `isWaitingForAttackEndDelay = false`로 초기화한다. `Tick`은 `IsMotionPlaying`인 동안 return한다. 모션 종료를 확인한 순간에만 `attackEndDelayEndTime = Time.time + AttackEndDelayDuration`을 계산한다. 후딜레이 후 전투 종료면 Wander, 아니면 Chase로 간다.
- `RecoveryDuration`은 `AttackEndDelayDuration`/`attackEndDelayDuration`으로 바꿨고, 데이터에는 이전 이름을 읽기 위한 `FormerlySerializedAs("recoveryDuration")`가 남아 있다.
- 공격 중 전투 종료는 현재 공격과 후딜레이 종료 후 적용한다. 몬스터 AI의 시작 Wander·추적·시야 상실·지역 배회 정책은 아래 이전 기록을 참고한다.

공격 에셋은 사용자가 `Assets/Data/Monster/Attacks/Goblin/`으로 이동했다. 이전 경로 `Attacks/Goblin_Attack1.asset`를 가정하지 않는다.

| 현재 에셋 | 종류 | 움찔 | 밀림 거리 | 넘어짐 | 피해 | 사거리 | 쿨타임 | 후딜레이 |
|---|---|---|---|---|---|---|---|---|
| Goblin_Attack1.asset | Normal | 켜짐 | 0.3 | 꺼짐 | 10 | 1.8 | 0 | 0.5초 |
| Goblin_Attack2.asset | Heavy | 켜짐 | 0.9 | 켜짐 | 10 | 2.2 | 4초 | 0.8초 |

이 표는 인계 시 파일 확인값이다. 사용자가 판정 박스와 프레임을 계속 편집하므로 실제 파일을 다시 읽는다. 현재 `ApplyAttackHit` 이벤트 시점은 1번 0.8초, 2번 0.5714286초다. 이전 대화의 2번 0.5초를 고정값으로 사용하지 않는다.

### 타격 전달과 플레이어 측 판정

- `MonsterAttack.ApplyAttackHit()`은 이벤트 순간 `OverlapBox`로 한 번 검사한다. 재사용 결과 List와 `HashSet<PlayerCharacter>`로 중복 콜라이더·이벤트 피해를 막는다. 구간 판정용 시작/끝 이벤트는 아직 구현하지 않았다.
- 실제 공격·기즈모·미리보기는 모두 **Transform 원점 + HitboxOffset**으로 계산하며 X만 공격 방향에 맞춰 반전한다. 초기에 사용했던 콜라이더 중심 기준은 제거했다. 콜라이더 Offset·크기가 공격 위치를 바꾸지 않도록 했다. Offset/Size는 월드 단위다.
- `Assets/Scripts/Combat/AttackHitContext.cs`, 네임스페이스 `Parry.CombatClass`의 `readonly struct`로 전달한다. 필드는 Damage, Attacker(Monster 참조), AttackType, CausesStagger, KnockbackDistance, KnocksDown, AttackDirection이다. 타격 순간 데이터 값을 복사한다.
- 몬스터는 `new AttackHitContext(CurrentAttack, monster, attackDirection)`을 만들어 `target.TakeDamage(context)`를 호출한다. 몬스터 코드에는 패링 검사·플레이어 무적 판단이 없다.
- `PlayerCharacter.TakeDamage(context)`가 유효 피해/생존 확인 → `parry.TryParryHit()` → 피격 무적 확인 → Stats 체력 감소 → 필요한 행동 취소 → 생존 시 `hitReaction.StartReaction(context)` 순서로 처리한다. 무적 확인보다 패링 확인이 먼저라 피격 무적 중에도 패링 성공 판정이 가능하다.
- 공격자에게 반격 피해나 성공 알림을 보낼 수 있도록 Attacker를 전달하지만, 해당 반격·자세 피해·알림 처리는 아직 연결하지 않았다.
- `PlayerCharacter.TakeDamage(float damage, Monster attacker = null)` 편의 오버로드와 컨텍스트의 float 생성자가 **현재 남아 있다**. 일반 공격/움찔 있음/밀림 0으로 감싸며 검증 스크립트가 사용한다. 사용자는 이 오버로드의 존재를 질문했고 설명을 들었다. 제거 요청이 확정된 것은 아니며, 변경 시 검증 호출도 함께 점검한다.
- `PlayerStatComponent.TakeDamage(float)`는 원시 체력 감소 함수다. 직접 호출하면 PlayerCharacter의 패링·피격 모션 경로를 우회한다. 디버그 ContextMenu도 현재 Stats를 직접 호출한다.

### 시간 기반 패링 — 최종 합의

- `Assets/Scripts/Player/PlayerParry.cs`가 패링을 관리한다. PlayerCharacter.Parry → TryStartParry이며 Input System의 Parry는 Space다.
- Animator의 Player_Parry 상태와 진입 전환, IsParrying 파라미터를 제거했다. `PlayerParryState.cs/.meta`도 삭제했고 PlayerAnimation에서 패링 bool 전달을 제거했다. 구르기의 PlayerRollState는 유지한다.
- 패링은 공용 `AnimationClipPlayer`로 재생한다. 현재 클립은 약 0.2초, `parryDuration`은 0.3초다. 유효 시간은 클립 종료와 별도로 계산한다.
- `IsParrying`은 내부 활성 bool·생존·`Time.time < parryEndTime`을 확인한다. 타격 순간 직접 시간을 확인해 Update 순서에 기대지 않는다.
- 시작 시 `nextParryTime = Time.time + parryCooldown`(3초), 성공 시 성공 순간 기준 `Time.time + successCooldown`(0.2초)로 변경한다. Inspector에서 조절 가능하다.
- **성공해도 현재 패링 유효 시간을 종료하지 않는다.** 사용자는 성공 직후 입력이 따닥 중복 실행되는 것을 방지하려고 0.2초를 정했다. 판정 창을 닫는 기능을 다시 추가하지 않는다. 유효 시간 안의 여러 타격은 각각 성공하며 각 성공이 0.2초 쿨타임을 다시 설정한다.
- 재시도에는 현재 패링 종료와 쿨타임 종료가 모두 필요하다. 거절된 입력은 타이머를 연장하지 않는다. 일반 공격 중 패링은 공격을 취소하며, 피격 경직·구르기·사망 중에는 시작할 수 없다.
- `CancelParry()`는 패링 상태만 정리하고 자신의 클립이 재생 중일 때만 Stop한다. 일반 공격 등 이후에 재생한 다른 클립을 종료하지 않도록 `IsPlayingClip`으로 확인한다.
- 정면 조건·패링 불가 공격·성공 시 반격/자세 피해는 아직 연결되지 않았다. 현재 성공 처리는 피해 방어와 쿨타임 변경이다.

### 피격·움찔·밀림·넘어짐

- `Assets/Scripts/Player/PlayerHitReaction.cs`는 피해 처리 후 컨텍스트를 받아 클립·경직·무적·밀림을 관리한다. 피격 Animator 상태는 추가하지 않았다.
- **최종 사용자 지시로 움찔 여부와 밀림 거리는 공격 데이터 소유**다. 플레이어의 normalHit/heavyHit에는 반응 지속 시간만 남아 있다. 이전 플레이어 쪽 knockbackDistance 필드는 제거했다.
- CausesStagger 켜짐이면 공격을 취소하고 움찔하며, 이동·점프·공격·패링·구르기의 실행을 막는다. 거리가 0이어도 경직과 행동 잠금은 적용한다.
- CausesStagger 꺼짐·넘어짐 꺼짐이면 피해와 설정한 밀림만 적용하고 기존 공격/행동을 유지한다. 거리도 0이면 움찔/밀림이 모두 없다. 이 경우에도 피격 무적은 적용한다.
- KnocksDown은 Heavy일 때만 적용한다. 넘어짐이 켜져 있으면 CausesStagger와 별도로 행동 잠금을 적용한다. 움찔/밀림 시간 → 무릎 꿇기 클립 → 유지 → 기상 클립 종료 후 해제한다.
- 사용자의 명시적 선택은 **잠깐 유지한 뒤 자동 기상하며 완전히 일어날 때까지 행동 입력 불가·무적**이다. 눕는 최종 사망 프레임까지 진행하지 않는다.
- 현재 초기값은 일반 반응 0.2초, 강공격 반응 0.35초, 추가 피격 무적 0.15초, 무릎 유지 0.35초다. 넘어짐 공격은 반응 시작부터 기상 완료까지 무적이다. 보호 중 타격은 추가 피해/경직을 주거나 보호 시간을 늘리지 않는다.
- PlayerMovement가 Rigidbody2D의 수평 속도로 감속 밀림을 적용한다. 수직 속도와 벽/바닥 충돌은 유지한다. 움찔 중에는 입력 이동 대신 밀림만 적용하고, 움찔이 없으면 일반 이동/공격/구르기 속도에 밀림을 합친다. 이동 축 값은 최신 상태로 유지해 키를 놓는 입력이 유실되지 않도록 한다. 행동 입력 저장 기능은 추가하지 않았다.
- 피격 중단·비활성화·사망 시 코루틴/밀림/자신의 클립을 정리한다. 치명타는 피격 모션보다 기존 사망 동작을 우선한다. 공중 피격의 착지·기상에 관한 추가 규칙은 별도로 확정하지 않았다.

| 새 클립 | 사용 스프라이트 | 현재 길이 |
|---|---|---|
| Assets/Animations/Player/Player_Hurt.anim | 원본 HeroKnight_46/47, 흰색 구간 제외 | 0.2초 |
| Assets/Animations/Player/Player_Knockdown.anim | DeathNoBlood_3/4/5, 무릎 꿇기까지 | 0.3초 |
| Assets/Animations/Player/Player_GetUp.anim | 같은 구간 5/4/3 역재생, 임시 기상 모션 | 0.3초 |

- 클립은 모두 10fps·비반복이며 프리팹과 씬의 PlayerHitReaction에 연결했다. 원본 스프라이트 에셋의 분할/피벗은 변경하지 않았다.
- AnimationClipPlayer.Play에 `holdLastFrame = false` 선택 매개변수를 추가했다. true면 마지막 프레임에서 속도 0으로 유지하고 반응 종료 시 Stop한다. 일반 공격·패링은 기본 false를 사용한다. `IsPlaying`은 노드 유효 여부이므로 마지막 프레임 유지 중에도 true다.
- 흰색 피격 플래시·SpriteRenderer 색 점멸·히트스톱·슬로모션·카메라 반응은 아직 추가하지 않았다. 사용자가 첫 플래시 요청을 보류하고 피격 설계부터 논의했다. SpriteRenderer 기본색이 이미 흰색이므로 기본 머티리얼에서 color=white만으로 흰 실루엣 플래시가 되지는 않는다고 설명했다.

### 공격 판정 미리보기 Editor

- `Assets/Editor/MonsterAttackPreviewWindow.cs`와 `.uss`의 UI Toolkit EditorWindow다. 클래스 이름은 Monster지만 플레이어와 몬스터 모두 지원한다.
- 메뉴는 `Tools > Parry > Monster Attack Preview`와 `Tools > Parry > Player Attack Preview`다. 같은 공용 창의 기준 캐릭터를 전환한다.
- 공격별 선택, 재생/일시정지, 이전/다음 프레임, 시간 슬라이더, ApplyAttackHit 이벤트 프레임으로 이동, 좌우 보기, 판정 위치/크기 입력, Scene 핸들, 공격 데이터 저장 기능이 있다.
- 미리보기 위치 좌표와 `Scene에서 캐릭터 이동` 토글이 있다. 켜면 임시 캐릭터 위치, 끄면 판정 박스를 드래그한다. 공격/프레임 변경 및 재생 중 배치 위치를 유지한다.
- 실제 전투 오브젝트 옆에 HideAndDontSave 임시 SpriteRenderer/Animator를 만들고 AnimationMode.SampleAnimationClip으로 표시한다. 창 종료·Play 진입·리로드 때 정리한다. 몬스터/플레이어 모두 Transform 원점 기준으로 표시한다. 실제 공격 데이터 편집은 SerializedObject를 통해 수행하며 저장 버튼을 제공한다.
- 초기 테스트용 `Goblin_AttackPreview` 씬 오브젝트와 `Goblin_AttackPreview.controller`도 남아 있다. EditorOnly 태그이며 기본 상태는 이벤트 없는 PreviewIdle이다. 이 오브젝트에는 실제 전투 컴포넌트가 없다.
- 일반 기즈모는 몬스터 선택 시 편집 모드 하늘색(등록 공격들), 실행 중 노란색/빨간색(이벤트 전/후) 박스와 중심점을 표시한다. 공격별 편집에는 전용 창을 사용한다.

### 검증 범위·현재 환경

- `AgentScripts/VerifyPlayerHitReactions.cs`는 최종 움찔/밀림 설정 변경 후 Play 검증을 통과했다. 일반/강공격, 공격 취소, 작은/큰 밀림, 벽, 보호 시간 연장 방지, 넘어짐·기상 전체 입력 잠금과 무적, 비활성/사망, 움찔×밀림 4가지 조합을 확인했다. 밀림 0에서도 이동·점프·공격·패링·구르기가 실행되지 않음을 확인했다.
- `VerifyMonsterAttackHits.cs`는 실제 두 클립 이벤트·중복 피해 방지·범위 밖 빗나감·취소·좌측 공격·사망을 검증했다. 컨텍스트 변경 후에도 통과했으며 최종 움찔 옵션 추가 후에는 전체를 다시 실행하지 않았다.
- `VerifyTimedPlayerParry.cs`는 실제 이벤트 패링, 클립보다 긴 유효 시간, 성공 후 창 유지, 시도 3초/성공 0.2초, 재입력 거절, Update와 독립적인 만료, 공격 취소·다음 클립 보호·비활성/사망을 검증했다. 최종 움찔 옵션 추가 후 전체 재실행은 하지 않았다.
- `VerifyMonsterAttackPreview.cs`, `VerifyPlayerAttackPreview.cs`는 공격 프레임·원점 기준·위치 유지·재생/정지·데이터 편집·정리를 검증했다. 몬스터 검증에는 콜라이더 크기/Offset이 좌우 공격 위치에 영향을 주지 않는 검사도 있다. 최종 런타임 변경 뒤 UI 전체를 다시 검사한 것은 아니다.
- 이번 문서 인계 시에는 코드·에셋과 상태를 읽었으며 Play 테스트를 새로 돌리지 않았다. 프로그램 방식 검증과 실제 키보드 입력 플레이 테스트를 구분한다. CUA 화면 캡처는 시간 초과가 있었으므로 UI 화면 캡처 검증 성공으로 보고하지 않는다.
- 인계 시 Unity 6000.6.7f1, 포트 37800, 편집 모드 stopped, 컴파일 완료, 현재 Console 오류 0/경고 0, Run In Background false다. Pipeline 보관 누계(error 1/warn 6)는 현재 콘솔 수와 다르다.
- 모든 CLI 명령에 `--project-path D:/GitHub/Parry`를 지정한다. 다른 OS 사용자/샌드박스 접근 때문에 필요한 명령은 프로젝트 대상 권한 상승으로 실행했다. beta.17 CLI 업데이트는 여전히 승인/실행하지 않았다.
- `script recompile` 응답 뒤 `script recompile_status`가 completed인지 확인한다. triggered가 오래 유지되거나 리로드 중 연결이 끊길 수 있다. `editor status`·`unity status --until-ready --timeout 20`으로 실제 상태를 다시 확인한다. 필요 시 `editor set_auto_tick --enable true`를 사용한다.
- Play 검증은 `editor.playmode set_run_in_background --enable true` → `editor.playmode start` → 준비 확인 → `script run --file AgentScripts/파일.cs --entry 클래스.Run` → stop → run_in_background false 순서로 진행했다. 초기 생성용 Setup 스크립트들은 실행 후 제거했다.
- Git은 main이며 사용자 변경과 미추적 코드·에셋이 많다. 이번 세션에서 커밋/푸시는 하지 않았다. 소유권 오류가 나면 읽기 명령에 `git -c safe.directory=D:/GitHub/Parry ...`를 사용한다. 전역 Git 설정은 바꾸지 않았다.

### 다음 요청에 따라 다룰 미연결 부분

패링 반격/자세 피해·공격자 성공 알림, 정면/패링 불가 조건, 구르기 무적·스태미나, 구간 판정 시작/끝 이벤트, 피격 표시와 시간 효과, 공중 피격 세부 규칙은 후속 요구에 맞춰 연결한다. 다음 구현을 인계문만으로 자동 선택하지 않는다.

## 보관 — 현재 구현과 대화 2026-11-11

이 절은 몬스터 공격·패링·피격 구현 전의 기록이다. AI 감지·배회·콜리전 배경과 원본 기획 경로를 참고하고, 최신 구현은 맨 위 2026-11-12 절과 현재 파일을 따른다.

### 다음 세션 시작 순서

1. 이 절을 읽고 Git 상태 및 관련 실제 파일을 확인한다. 사용자 변경과 미추적 에셋을 보존한다.
2. 몬스터 작업 전 원래 `GAME-DESIGN.md`의 5절·6.3절·6.4절을 읽는다. 특히 이전 스폰 귀환 규칙 대신 최종 전투 종료·배회 규칙을 따른다.
3. Unity 작업 시 현재 Editor 연결·모드를 다시 확인한다. 검증 명령과 주의사항은 아래에 있다.
4. 사용자가 코드를 배우며 직접 편집하고 있다. 현재 코드에 근거해 호출 순서·값의 의미를 설명하고, 다음 요청이 오기 전 임의 기능을 추가하지 않는다.

### 원래 기획 기준 문서

`C:/Users/USER/Documents/Codex/2026-10-24/skill-creator-c-users-user-codex/outputs/parry-action/GAME-DESIGN.md`

- 전체 기획의 원본이다. 사용자는 원본에 계속 통합 갱신하기를 원한다. 프로젝트 루트에 새 GAME-DESIGN.md를 만들지 않는다.
- 이번 세션에서 6.3의 감지·전투 종료·배회를 최종 규칙으로 갱신했다. 원본 폴더의 backups에 변경 전 사본을 남겼다.
- 같은 폴더의 GLOSSARY.md/NEXT-SESSION.md 및 문서 하단의 과거 변경 이력은 최신 규칙과 다를 수 있다.
- 원본은 워크스페이스 밖이다. 수정할 때 도구의 외부 쓰기 권한 절차를 따른다.

### 대화 끝과 사용자 코드 작성 선호

- 마지막 요청은 세션 인계 정리다. 직전에는 IdleState를 제거하고 시작 상태를 Wander로 통일했으며, Wander.Tick의 상황별 한글 주석을 정리했다.
- 함수의 역할과 복잡한 판단을 읽기 쉽게 표현하고, 필요한 상황과 의도는 한글 주석으로 설명한다.
- 컴포넌트 참조 필드는 맨 위, 관련 상태 변수는 한곳에 묶는다. 프로퍼티는 Awake 위, Awake/Start/Update 등 생명주기 함수는 일반 함수보다 위를 선호한다. 기존 파일에서 사용자가 바꾼 순서는 읽고 보존하며 요청 범위에서 정리한다.
- “사람들은 보통 어떻게 하냐”는 실무의 유지보수·확장 가능한 설계를 묻는 뜻이다. 단순 하드코딩 API 예제를 설계 답변으로 대체하지 않는다. 업계 다수가 쓴다고 근거 없이 단정하지 않는다.
- 역할과 의존성을 먼저 설명한다. 사용자는 불필요한 옵션·복잡한 구조·검증하지 않은 방어 코드를 싫어한다. 요청하지 않은 기능을 미리 추가하지 않는다.
- 같은 설정·참조·결과 배열은 가능한 한 초기화 때 캐싱해 재사용한다. 구조체 new가 곧 GC 할당은 아니라는 점은 설명했다. 최적화 효과를 측정 없이 과장하지 않는다.
- PlayerAttack의 bool 예약 흐름을 유지한다. 과거 AttackPhase enum은 삭제했다. 피격 점멸은 코루틴과 `StartCoroutine(nameof(HitFlash))` 방식을 유지한다.
- 사용자는 Inspector·클립 이벤트·입력 설정을 직접 수정한다. 설명 중 과거 값이 아닌 현재 파일을 다시 읽는다.

### 몬스터 최종 행동 규칙

- 일반 몬스터 고블린부터 구현 중이다. AI 방식은 사용자가 **코드 FSM**을 선택했다. Unity Behavior를 추가하지 않았다.
- 활성화 → Wander. 플레이어 발견 → Chase. 전투 종료 → 종료 순간의 현재 위치를 중심으로 Wander. 배회 중 재발견 → Chase.
- 전투 종료: 플레이어와 몬스터의 실제 2D 거리가 기준 초과 **OR** 감지 실패 상태가 지정 시간 지속. 대상이 사망·비활성·없어진 경우도 현재 정책은 종료로 판단한다.
- **스폰 귀환은 폐기했다.** ShouldReturn, ReturnState, ReturnPolicy, spawnPosition, spawnDistanceLimit은 제거했다. 과거 OR/AND 스폰 귀환 논의를 되살리지 않는다.
- IdleState도 제거했다. 가만히 기다리는 구간은 Wander 내부의 Wait로 처리한다.
- 사망 이벤트와 DeadState는 사용자 요청으로 제거했다. Brain.Update는 `monster.IsDead`이면 return만 한다. Monster.TakeDamage에서 체력 0이면 GameObject 비활성화, Brain.OnDisable에서 상태 Exit/Clear와 이동 정리를 수행한다.
- 전방만 감지하는 frontOnly/detectOnlyInFront는 사용자가 요청하지 않은 옵션이라 제거했다. 기본적으로 앞뒤 구분 없이 감지한다.
- 높이 검사는 논의했지만 **아직 제거하지 않았다**. 콜라이더 발 위치 차이가 heightTolerance보다 크면 감지 실패한다. 원래 기획의 다른 높이 발판 감지 제한을 근사 구현한 것이다. 같은 발판 객체인지 검사하지는 않는다.

### 몬스터 클래스와 데이터

모두 `Assets/Scripts/Monster/`, 네임스페이스 `Parry.MonsterClass`.

| 파일 | 현재 역할 |
|---|---|
| IMonsterState.cs | 일반 C# 상태의 Enter/Tick/Exit 규약 |
| MonsterStateMachine.cs | 현재 상태 저장, Tick 위임, 이전 Exit → 새 Enter, Clear. 같은 객체 중복 전환은 `CurrentState == nextState`로 거절 |
| MonsterBrain.cs | Data/컴포넌트 참조 확보, 상태·정책 객체 생성, 상태머신 실행, 센서 결과 전달. ShouldEndCombat은 정책에 위임 |
| MonsterSensor.cs | 독립 Update에서 감지하고 IsTargetVisible·LastSeenTime·LastSeenPosition 저장 |
| MonsterMovement.cs | 물리 이동·방향·Animator Speed 전달, 배회 이동 가능 검사 |
| MonsterChaseState.cs | 전투 종료 검사, 마지막 목격 위치를 향한 수평 이동·방향·정지 |
| MonsterWanderState.cs | 현재 위치를 중심으로 목적지 무작위 선택 → 이동 → 대기를 반복. 상황별 한글 주석과 판단 함수 존재 |
| MonsterCombatPolicy.cs | 거리·시야 상실 시간으로 공통 전투 종료 판단. 향후 공격 상태에서도 재사용할 수 있음 |
| MonsterData.cs | ScriptableObject와 감지/추적/전투/배회 설정 구조체 |
| Monster.cs | 기존 체력 감소·피격 점멸·사망 비활성화. Hp 프로퍼티는 아직 실제 hp 필드와 연결되지 않았으므로 검증 시 주의 |

- Data 에셋: `Assets/Data/Monster/Goblin.asset`. 현재 씬의 MonsterBrain.data에 연결했다.
- Data는 이동 속도와 Detection/Chase/Combat/Wander 설정을 보관한다. 이동 속도·감지 설정은 컴포넌트 Initialize에 전달하고, 상태/정책 설정 구조체는 생성자에서 복사한다. 런타임 기록은 에셋이 아닌 각 인스턴스에 보관한다.
- 원본 에셋을 실행 중 바꿔도 이미 복사한 설정은 자동 갱신되지 않는다. 상태 객체는 몬스터마다 한 번 생성해서 재사용하며 ChangeState에 주입한다.
- 현재 기본값: 이동 2, 감지 6, 높이 허용 0.5, 추적 정지 거리 1.2, 전투 이탈 거리 9, 시야 상실 3초, 배회 반경 2, 대기 1초, 이동 제한 3초, 도착 허용 0.05. 확정 밸런스가 아닌 조정 가능한 초기값이다.
- 체력·피격 색상/점멸 시간은 기존 Monster에 남아 있다. 공격 데이터·자세 데이터는 아직 추가하지 않았다.

### 감지·이동의 구체 흐름과 최근 논의

- Sensor는 `[DefaultExecutionOrder(-100)]`으로 Brain보다 먼저 매 프레임 갱신한다. 감지/판단 interval은 사용자 요청으로 제거했다. Brain.ObserveTarget도 제거했다.
- 플레이어는 Sensor.target의 **Inspector 직접 참조**다. 시야 Trigger 진입으로 탐색하는 방식은 논의만 했으며 구현하지 않았다. 레이어와 태그는 별개다.
- CanSeeTarget: 대상 유효성 → IsWithinDetectionDistance → IsAtDetectableHeight → HasNoObstacleToTarget.
- HasNoObstacleToTarget은 몬스터 중심에서 지정 플레이어 중심까지 Raycast한다. Player를 실제로 맞혀야 성공하는 방식이 아니라 **사이에 장애물이 없으면 성공**하는 가림 검사다. 기존 HasClearLineOfSight라는 이름은 사용자가 오해하기 쉽다고 하여 변경했다.
- 필터는 Initialize에서 설정하고 Trigger는 제외한다. 결과 sightHits[32]를 재사용한다. Monster/Player 태그 콜라이더는 CompareTag로 건너뛰며 IsChildOf는 제거했다. 배열이 꽉 차면 누락 장애물 가능성 때문에 감지 실패로 처리한다.
- 보이는 동안 매 프레임 LastSeenPosition은 실제 대상 위치로 갱신된다. 놓치면 기록이 고정되어 마지막 위치까지만 접근하고, 시야 상실 시간 만료 후 Wander로 전환한다.
- Chase의 이동·정지 판단은 x축 거리다. 공통 전투 종료 거리 비교는 실제 대상의 x/y 제곱 거리다. 이동 정지 거리를 미래의 공격 유효 범위 판정으로 재사용하지 않는다.
- Movement.MoveTo는 목적지만 저장한다. FixedUpdate에서 수평 속도를 정하고 수직 속도는 유지한다. 현재 실제 코드는 `Clamp(distance / Time.fixedDeltaTime, -moveSpeed, moveSpeed)`다. `/` 대신 `*`로 바꾸는 실험을 했지만 현재 파일은 다시 `/`이므로 이전 대화의 곱셈 변경을 최신 상태로 오인하지 않는다.
- 방향은 Chase에서 Face(lastSeenX)로 정지 거리 안에서도 갱신한다. 거의 겹친 경우 0.01 허용치를 둔다.
- Animator Speed는 Update에서 **실제 Rigidbody2D.linearVelocity.x 절댓값**을 전달한다. 이전에는 이동 명령 속도를 전달해 벽에 막혀도 Speed=2/Walk가 유지됐고, 로그로 실제 속도 0과 차이를 확인해 수정했다.
- CanWalkTowards는 Initialize에서 캐싱한 필터와 결과 배열(각 1칸)을 사용한다. 몸 앞 얇은 OverlapBox로 벽 검사 → 없으면 발 앞 아래 Raycast로 지면 검사. 몸 Bounds는 현재 위치/크기 기준으로 매번 읽는다. 기존 Collider.Cast/법선 순회는 단순화했다.
- Wander에서만 이동 가능 검사를 호출한다. Chase에 경로 탐색·점프·장애물 우회는 없다. 목적지에 막히면 실제 속도 0/Idle로 표시되고 전투 종료 조건을 기다린다.
- Wander.Enter는 `brain.transform.position.x`를 중심으로 복사하고 Wait한다. 재진입할 때 새 중심으로 갱신한다. 초기 활성화 시 다른 컴포넌트 Awake 순서에 의존하지 않으려고 Transform을 사용했다.

### 플레이어 클립 재생기 — 기존 인계와 달라진 부분

- `Assets/Scripts/Animation/AnimationClipPlayer.cs`, `Parry.AnimationClass`. Animator가 기본 Controller를 직접 실행하고, 필요할 때 클립만 Playables로 임시 출력한다.
- **믹서와 AnimatorControllerPlayable은 제거했다.** PlayerAnimation도 Animator에 직접 SetFloat/SetBool한다. 클립 재생기의 파라미터 전달 함수는 없다.
- Awake에서 PlayableGraph/GameTime과 AnimationPlayableOutput을 한 번 생성한다. 초기 Target은 null.
- Play: 이전 노드 보관 → 새 AnimationClipPlayable 생성/시간 0 → Output 소스 교체 → SetTarget(animator) → 이전 노드 제거 → graph.Play.
- 연속 클립 교체에서는 Stop을 호출하지 않아 Animator 연결을 유지한다. 현재 SetTarget은 조건 없이 호출한다. 이 중복 호출을 줄이는 조건에 대한 대화는 있었으나 현재 파일이 기준이다.
- Stop: SetTarget(null), 소스 해제, graph.Stop, 현재 클립 노드 제거. 기본 Controller가 다시 출력한다. 그래프/Output은 재사용하고 OnDestroy에서 그래프 해제한다.
- Update에서 비반복 클립 자연 종료 시 Stop한다. 반복 클립은 명시적으로 Stop한다. 공격 논리 종료는 PlayerAttack.EndAttackMotion 이벤트가 담당한다.
- `Evaluate(0)`은 제거했다. 클립별 Dictionary 캐싱은 논의만 했고 구현하지 않았다. 그래프·Output만 캐싱하고 클립 노드는 재생 요청마다 만든다.
- **공격 끊김의 실제 원인:** 기본 Controller에 남아 있던 Player_Attack 상태가 1타를 별도로 재생해 Playables와 중복 이벤트를 발생시켰다. 로그에서 현재 2/3타인데 발신 이벤트는 1타인 호출을 확인했다. 기본 Controller의 Player_Attack 상태와 진입 전환을 제거해 해결했다.
- 이 문제를 막으려고 임시로 넣었던 이벤트 클립/시간 검증 IsCurrentEvent는 제거했다. 이전 이벤트가 엔진 내부에서 지연됐다는 초기 추측을 확정 원인으로 재서술하지 않는다.
- 공격 이벤트는 매개변수 없는 ApplyAttackHit/OpenComboBuffer/EndAttackMotion이며 bool 예약 흐름을 유지한다. 종료 이벤트 → 예약 있으면 다음 클립, 없으면 Stop.
- 현재 공격 세 클립 모두 14fps지만 사용자 편집으로 프레임 배치와 이벤트가 달라졌다. 인계 시 파일 확인상 모두 길이 약 0.5714초/EndAttackMotion 0.5초다. 수치를 고정해서 가정하지 말고 실제 클립을 읽는다. 입력 설정과 이벤트 시점은 사용자가 관리한다.
- 기존 PlayerMovement/PlayerCharacter의 이동·점프 제한, 구르기/패링/사망 공격 취소, 적중 콤보, OverlapBox 판정은 유지한다. 자세 피해·실제 패링 전투 연결·스태미나는 아직 작업 대상이다.

### 씬·콜리전·태그

- 씬: `Assets/Scenes/SampleScene.unity`, Player 프리팹: `Assets/Prefabs/Player/Player.prefab`.
- 현재 Monster에 Rigidbody2D/BoxCollider2D/SpriteRenderer/Animator와 Monster/Movement/Sensor/Brain이 붙어 있다. Sensor.target은 씬 Player 참조다.
- 플레이어는 Player 레이어(9), 몬스터는 Monster 레이어(8). 플레이어와 몬스터는 서로 밟거나 밀지 않고 통과한다.
- 사용자 요청으로 **프로젝트 충돌표의 Player↔Monster는 활성화**하고, 플레이어 CapsuleCollider2D의 Layer Overrides → Exclude Layers에 Monster(256)를 지정했다. Rigidbody exclude는 0이다. 바닥·벽 접촉과 공격 Overlap은 유지한다.
- 플레이어 프리팹/씬 콜라이더 오브젝트에는 Player 태그, 몬스터에는 Monster 태그를 지정했다. 자식 콜라이더를 추가하면 태그도 맞춰야 CompareTag 제외가 적용된다.
- Layer Collision Matrix와 개별 Exclude Layers는 별도 설정이다. Exclude는 전역 결과를 합쳐 표시하지 않고 해당 콜라이더에 지정한 예외만 보여 준다고 설명했다.
- MonsterController는 Speed Float로 Idle/Walk를 전환한다. FSM IdleState 제거와 Animator Idle 모션 제거는 다른 문제이므로 Animator Idle은 유지한다.

### 검증 기록과 현재 Editor 상태

- `AgentScripts/VerifyMonsterBrain.cs`: 상태 주입 생명주기, 센서 독립 갱신, 벽/높이 감지 제외, 추적, 스폰 제한 없는 교전, 거리/시야 상실 종료, 지역 배회, 재감지, 벽/낭떠러지/낮고 높은 장애물/Trigger 처리, 비활성/재활성, 사망 시 Tick 중단.
- `AgentScripts/VerifyMonsterPlatform.cs`: 플레이어 뒤쪽 이동 시 두 프레임 내 방향 전환, 발판에 막힌 실제 속도 0/Animator Speed 0, 지속 시야 상실 후 Wander 전환. 중간 기록을 반환하는 재현 스크립트다.
- `AgentScripts/VerifyPassThrough.cs`: 별도 PhysicsScene2D에서 플레이어 Collider 제외 설정으로 통과/밀림 방지/몬스터 위 착지 방지/바닥 접촉/공격 Overlap 유지.
- 각 스크립트는 해당 변경 시점에 통과했다. 마지막 전체 AI Play 검증은 Idle 제거 후 통과했다. 최신 인계 정리에서는 코드를 다시 읽고 컴파일·콘솔 상태만 확인했으며, 모든 테스트를 새로 실행한 것은 아니다. 실제 키 입력 검증으로 보고하지 않는다.
- 현재 Editor는 편집 모드(stopped), 컴파일 완료, 현재 Console 오류 0/경고 1. 경고는 검증용 Player(Clone)의 입력 장치가 기존 Player와 중복돼 control scheme을 찾지 못한 기록이다. Pipeline 보관 로그의 과거 오류/경고 누계는 현재 콘솔 수와 다르다.
- 검증 후 임시 오브젝트는 제거했고 Run In Background는 false로 복원했다. 이 세션에서 Git 커밋/푸시는 하지 않았다. 기존부터 사용자 변경과 미추적 파일이 많다.
- IDE에서 ObserveTarget/ShouldReturn(visible) 오류를 사용자가 보고했을 때 디스크 파일에는 이미 새 API가 반영돼 있었다. Unity 성공을 IDE 해결로 단정하지 않는다. 재발하면 저장되지 않은 편집 내용/IDE 동기화를 확인하고 사용자 편집을 덮어쓰지 않는다. 과거 Rider 캡처는 실패했다.

### Unity 재개 명령과 주의사항

- `unity-pipeline` 스킬을 읽고 현재 CLI 명령을 조회한다. 스킬 예시의 일부 이름은 현 패키지에서 바뀌었다.
- CLI 설치 버전 beta.14, beta.17 업데이트 알림을 확인했지만 업데이트 승인은 받지 않았다. 임의 self-update는 하지 않는다.
- 프로젝트는 `D:/GitHub/Parry`, Pipeline 포트 37800, Editor Unity 6000.6.7f1이다. 모든 명령에 프로젝트 경로를 명시한다.
- STATUS_DESCRIPTOR_UNREADABLE은 다른 OS 사용자/샌드박스 접근 문제로 발생했고, 해당 프로젝트 대상 권한 상승 호출로 연결했다.
- 상태: `unity status --until-ready --timeout 20 --project-path D:/GitHub/Parry`.
- 백그라운드 갱신: `unity command editor set_auto_tick --enable true --project-path D:/GitHub/Parry`.
- 컴파일: `unity command script recompile ...` 이후 `script recompile_status`가 completed인지 확인. 도메인 리로드 중 socket hang up/인스턴스 없음은 준비 확인 후 재연결한다.
- Play: `editor.playmode start` / `editor.playmode stop`. 실행 중 검증은 `editor.playmode set_run_in_background --enable true`가 필요했고 검증 후 false로 복원했다.
- 검증 실행: `unity command script run --file AgentScripts/VerifyMonsterBrain.cs --entry VerifyMonsterBrain.Run --project-path D:/GitHub/Parry` 등. Play 전후 리로드가 끝난 것을 확인한다.
- 콘솔: `console status`, `console get --tail N --level error`. 과거 누계와 groundTruth 현재 콘솔을 구분한다.

### 남은 작업 — 다음 요청에 맞춰 진행

- 몬스터 공격·패턴·공격 데이터·회복·자세·스턴·플레이어 전투 연결은 아직 없다. 공격 클립 제작은 사용자가 진행한다는 선호를 유지한다.
- 기본 공격 1종 + 패턴 1종, 패턴 우선 선택/개별 쿨타임·거리 조건/고정 회복 시간, 일반 피격으로 행동 중단 안 함, 패링으로 자세 감소 등은 원래 기획서를 따른다.
- 공격/기절 중 전투 종료를 즉시 적용할지 종료 후 배회할지 확정이 필요하다. IdleState가 없어졌으므로 과거 기획의 스턴 회복 후 Idle 복귀 문구와 현재 Wander 구조도 해당 구현 전에 조율한다.
- 높이 감지 규칙은 토론만 했고 남아 있다. 이동 가능성과 시야는 다르다. 요청 없이 높이 검사나 앞쪽 제한을 새로 추가/제거하지 않는다.
- 몬스터별 데이터는 준비됐지만 현재 공통 상태 조합은 Chase/Wander 두 개다. 미래 Attack/Stun 상태와 몬스터별 행동 구성을 어떻게 조립할지는 실제 요구 범위에 맞춰 확장한다.
- 다음 할 일을 인계문만으로 자동 확정하지 않는다. 사용자의 다음 요청을 기다린다.

---

아래 절은 이전 세션의 보관 이력이다. 기획과 최신 구현은 위 절과 실제 파일, 원래 통합 기획서를 먼저 확인한다.

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

- `GAME-DESIGN.md`: 전체 기획의 최신 통합 기준. 2026-11-09 몬스터·패턴·자세·귀환 규칙까지 반영했다.
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

> SESSION-HANDOFF.md 맨 위 **현재 구현과 대화 — 2026-11-11**을 읽고 이어가자. 몬스터 작업 전 원래 GAME-DESIGN.md의 5절·6.3절·6.4절도 읽어줘. 현재 몬스터는 ScriptableObject 설정 복사 + 주입식 코드 FSM의 Chase/Wander이고, 스폰 귀환·IdleState·DeadState·전방 감지 옵션은 제거했어. 센서는 독립 갱신, 전투 종료는 거리 초과 또는 시야 상실 시간 만료 후 현재 위치 배회야. 복잡한 조건은 판단 함수/bool로 분리하고 한글 상황 주석을 달아줘. 현재 코드와 사용자 변경을 먼저 확인하고 내가 다음 요청할 때까지 기다려줘.

