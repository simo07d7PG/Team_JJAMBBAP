# 미니게임 만드는 법

## 새 미니게임 만들기 (10단계)

1. `Assets/_Project/Scripts/Minigames/<이름>/` 폴더에 `MicrogameBase`를 상속한 클래스를 만든다 (네임스페이스 `BariBarista.Minigames`).
2. `ResetState()`는 필수다. 위치, Rigidbody 속도, 액체 양, 화면(HUD) 값, 풀 오브젝트를 전부 처음으로 되돌린다. 같은 미니게임이 한 판에 여러 번 켜지기 때문이다.
3. `OnPrepare()`에서 `Ctx.Cup`(컵 내용물)을 보고 컵 모델 상태(얼음, 액체 높이)를 맞춘다(입력·시간은 켜지 않는다). `OnBegin()`에서 입력을 켠다.
4. 판정은 `OnTick(remaining01)`에서 한다. 이번 틱에 흐른 시간은 `TickDelta`다. **`Time.deltaTime`으로 제한 시간을 세지 않는다.** 물리 조작은 평소처럼 `FixedUpdate`를 쓴다.
5. 결과가 나오면 `Succeed(score)` 또는 `Fail(FailReason.X)`를 부른다. 처음 한 번만 반영되고 이후 호출은 무시된다.
6. 시간 초과 처리를 바꾸려면 `OnTimeUp()`을 재정의한다(기본은 Timeout 실패). 버티기형이면 정의 에셋의 `successOnTimeout`을 켠다.
7. 결과는 `ApplyResult(result)`에서 `Ctx.Cup.Add(...)`로 컵에 남기고 통계(`MicrogameStats.Report`)를 보낸다. 이 메서드는 `Finished` 이벤트보다 먼저 불린다. 입력 끄기 같은 정리는 `OnEnd(result)`에서 한다. 강제 종료(`Aborted`)일 때는 ApplyResult가 불리지 않는다. `OnDisable`이 필요하면 `protected override`로 만들고 `base.OnDisable()`을 부른다.
8. 입력은 `Hand`(IHandInput: 포인터, 잡기(좌클릭), 기울이기(우클릭))로 읽는다. 마우스를 직접 읽지 않아야 나중에 물리 손으로 바꿀 수 있다. 오브젝트를 끌고 다녀야 하면 `MouseSpringFollower`를 붙인다.
9. 프리팹 규칙: 루트에 미니게임 컴포넌트 1개와 `Id`를 두고, 자식에 카메라 1개를 둔다. AudioListener와 EventSystem은 넣지 않는다. 모델·기계는 인스펙터 참조로만 연결한다(이름 하드코딩 금지). 수치는 `LevelSettings[]`(Lv1~3)로 뺀다.
10. `Create > BariBarista > Microgame Definition`으로 정의 에셋을 만든다(id는 `Id`와 같게, 지시어, stationId, 제한 시간, 프리팹). 그다음 샌드박스 씬의 `MicrogameRunner` → `entries`에 넣고 Play해서 테스트한다.

시간 초과는 호출하는 쪽이 `Tick`으로 시간을 넘겨서 처리한다(그래야 Finished와 컵 반영이 일어난다). `ForceEnd`는 게임오버·재시작 정리용이다. 단계와 결과 연출은 아래 "단계와 결과 연출"을 본다.

판정 로직은 가능하면 `Minigames/Logic/`의 순수 C# 클래스로 분리하고 `Minigames/Tests/`에 EditMode 테스트를 단다. 테스트는 `Tools/BariBarista/Run Minigame Tests`로 실행한다.

## 단계와 결과 연출

호출하는 쪽 순서: `Prepare(ctx)` → `Begin(ctx)` → `Tick(dt)`... → `Finished` → `Tick(dt)`... → `PresentationFinished`.
`Prepare`는 지시어 단계(초기화 + 안내, 시간·입력 꺼짐)이고 `Begin`부터 시간이 흐른다. `Prepare` 없이 `Begin`만 불러도 된다.
`Finished`는 결과가 정해진 순간이다(목숨·기록은 여기서). 그 뒤에도 `Tick`을 계속 넘기면 결과 연출 시계가 흐르고, 끝나면 `PresentationFinished`가 온다. 일시정지는 `Tick`을 멈추면 된다.

단계(`Phase`)와 호출별 동작:

| 호출 \ 현재 | Idle | Preparing | Playing | Presenting |
| --- | --- | --- | --- | --- |
| `Prepare` | 준비 시작 | 같은 ctx면 무시, 다른 ctx면 취소 후 다시 준비 | `ForceEnd` 후 준비 | 연출 취소(이벤트 없음) 후 준비 |
| `Begin` | 준비 후 시작 | 같은 ctx면 `ResetState` 다시 안 부르고 시작 | `ForceEnd` 후 준비·시작 | 연출 취소 후 준비·시작 |
| `Tick` | 무시 | 무시(지시어 대기는 호출하는 쪽이 잰다) | 시간 흐름, 0이면 시간 초과 | 연출 시계 진행, 끝나면 Idle |
| `ForceEnd` | 무시 | 정리 후 Idle (`OnEnd(Aborted)`) | 정리 후 Idle (`OnEnd(Aborted)`) | 연출 취소(이벤트 없음) 후 Idle |
| 결과 확정 | - | - | Resolving → `ApplyResult` → `Finished` → `OnEnd` → Presenting | - |

- Resolving은 결과를 확정하는 아주 짧은 순간이다. 이 안에서 `OnDisable`이나 `ForceEnd`가 와도 아무 일도 하지 않는다.
- `Finished` 처리 중 루트를 꺼 버리면 연출 없이 바로 `PresentationFinished(Skipped)`가 온다. 기다리는 쪽이 멈추지 않게 항상 보낸다.
- `Finished` 처리 중에 `Prepare`/`Begin`을 다시 부르면 옛 판은 아무 이벤트도 보내지 않는다.
- `ForceEnd`·`Prepare`·`Begin`으로 직접 취소한 연출은 `PresentationFinished`를 보내지 않는다.

연출 훅(미니게임이 재정의):

| 훅 | 시점 |
| --- | --- |
| `OnPrepare()` | `ResetState` 직후. 컵 모델·안내 맞추기 |
| `OnPresentStart(in result)` | 연출 시작(`OnEnd` 뒤) |
| `OnPresentTick(t01, dt)` | `Tick`마다. `t01`은 0에서 1. 보이는 피벗을 이 값으로 움직인다 |
| `OnPresentEnd(cancelled)` | 연출 끝. 취소됐으면 `true`. 연출 오브젝트는 여기서 정리한다 |
| `GetPresentationDuration(in result)` | 연출 시간. 기본은 실패 0.9초, 성공 1.4초 |

읽기 값: `Phase`, `PresentationDuration`, `PresentationRemaining`. 이벤트: `PresentationFinished(game, Completed 또는 Skipped)`.

## 화면 정렬 순서(Canvas)

- 미니게임 Canvas는 `sortingOrder = -10`(Screen Space - Overlay, 표시 전용). GameScene의 ESC 패널(0)보다 항상 아래다.
- 코어 루프 공통 HUD는 `sortingOrder` 100 이상으로 둔다.
- 결과 큰 글씨는 미니게임이 그린다. 코어 루프는 미니게임 연출 중 자기 결과 글씨를 그리지 않는다(지시어·타이머·목숨만).
- 일시정지 중에는 코어 루프가 자기 HUD를 숨긴다(HUD가 ESC 패널을 덮지 않게).

## 화면 UI(Canvas)와 한글 폰트

- 미니게임마다 자기 Canvas UI를 가진다(공용 템플릿 없음): 에스프레소는 오른쪽 세로 "샷 잔" 게이지, 얼음은 아래 얼음 칸 줄 + 유지 파이, 우유는 왼쪽 컵 단면 + 흘림 막대. `EspressoShotHud`, `IceScoopHud`, `MilkPourHud`가 표시만 한다.
- 안내는 `OnPrepare`부터 Playing 첫 1.5초 또는 첫 입력까지 크게(마우스 그림 + 짧은 한글) 보이고, 이후 구석 힌트로 줄어든다(`GuideTimer`). 결과가 나면 `ResultBanner`가 칭찬 또는 실패 이유를 크게 보여 준다.
- 모든 문구는 `Logic/PresentationRules.cs`의 `const string`에 둔다(실패 이유는 `ShotRules`/`IceRules`/`PourRules.FailText`). 새 문구를 추가하면 `PreloadCharacters`에도 들어가는지 `PresentationRulesTests`로 확인한다.
- 색은 `MinigameUiPalette`(팀 UI에서 뽑은 베이지·갈색·버건디)만 쓴다. 목표 구간은 초록이 아니라 버건디 테두리 + 밝은 갈색 채움이다.
- 한글 폰트는 `Assets/_Project/UI/Fonts/Galmuri11-Bold_Dynamic SDF.asset`(동적, SDFAA)이다. 없으면 메뉴 `Tools/BariBarista/Create Galmuri Dynamic Font` 또는 `Rebuild Minigame Prefabs`가 만든다. 기존 TMP 폰트 에셋과 TMP Settings는 건드리지 않는다. 큰 결과 글씨용 외곽선 재질은 `Galmuri11-Bold_Dynamic SDF Outline.mat`이다.
- Canvas는 Screen Space - Overlay라서 카메라 캡처에 안 찍힌다. 화면 확인은 `MinigameSimHarness.CaptureAllHud(폴더)`가 임시로 Camera 모드로 바꿔 찍고 되돌린다.
- `VerticalGauge`(월드 공간 게이지)는 없어졌다. 쓰던 게이지 재질 3개(`GaugeBg`, `GaugeBand`, `GaugeFill`)는 재질 정리 때 지운다.

## 샌드박스 사용법

- 메뉴 `Tools/BariBarista/Create Minigame Sandbox`: 테스트 씬, 프리팹, 정의 에셋을 새로 만든다. 같은 이름이 있으면 덮어쓰지 않고 새 이름으로 만든다.
- 메뉴 `Tools/BariBarista/Rebuild Minigame Prefabs`: 프리팹, 정의 에셋, 샌드박스 씬을 코드대로 다시 만든다. 같은 경로를 덮어써서 GUID가 그대로다. **프리팹을 손으로 고친 내용은 재생성 때 사라진다.** 바꾸고 싶은 값은 `MinigameSandboxBuilder`에 적는다.
- `Assets/_Project/Scenes/MinigameSandbox.unity`를 열고 Play한다.
  - **R** 재시작, **1/2/3** 난이도, **Tab** 다음 미니게임, **C** 컵 이어 쓰기(이전 미니게임 결과를 다음 미니게임에 넘기는 테스트)
  - 화면 왼쪽 위에 지시어, 남은 시간, 결과, 컵 내용물, 단계(연출 남은 시간 포함)가 표시된다.
  - 재시작하면 0.5초 지시어 단계(`Prepare`) 뒤 시작하고, 연출이 끝나면 루트를 끄고 1초 뒤 자동으로 다시 시작한다(`StandaloneMicrogameRunner`의 `instructionSeconds`, `autoRestartDelay`).

## 검증 도구(편집 모드, Play 불필요)

- `Tools/BariBarista/Sim/Fairness (Lv1-3)`: 미니게임 × Lv1~3마다 스크립트 입력으로 성공 경로가 있는지 확인하고 표를 콘솔에 남긴다.
- `Tools/BariBarista/Sim/Restart Soak x10`: 미니게임마다 `Prepare`/`Begin`을 10번 연속 하고 남은 상태(얼음, 컵 액체, 기울기, 단계, 연출 이벤트)를 확인한다.
- `Tools/BariBarista/Sim/Feel Metrics`: 손맛·연출 숫자 기준을 기록한다(지금은 현재 값만).
- 끝나면 샌드박스 씬을 저장하지 않고 다시 연다(얼음 풀이 편집 모드에서 오브젝트를 만들기 때문).

## 미니게임 3종 조작

| 미니게임 | 조작 | 성공 / 실패 |
| --- | --- | --- |
| 에스프레소 샷 | 좌클릭을 누르고 있으면 추출된다. Lv3은 먼저 컵을 클릭해 받침 위로 끌어다 놓는다 | 게이지의 목표 띠(버건디 테두리)에서 떼면 성공. 컵을 넘치게 하거나 구간을 넘겨서 떼면 실패 |
| 얼음 퍼기 | 스쿱이 마우스를 따라온다. 제빙기 위에서 좌클릭을 누르고 있으면 담기고, 컵 위에서 우클릭을 누르고 있으면 기울어지고, 떼면 다시 선다 | 컵 안 얼음 개수가 목표 구간에서 1초 유지되면 성공. 최대치를 넘기면 실패 |
| 우유 붓기 | 좌클릭을 누르고 있는 동안 우유팩을 잡는다. 우클릭을 누르고 있으면 기울고, 떼면 다시 선다 | 목표 선 안에서 팩을 세우면 성공. 넘치거나 컵 밖으로 많이 부으면 실패 |
