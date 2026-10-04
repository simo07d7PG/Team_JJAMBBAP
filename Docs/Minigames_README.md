# 미니게임 만드는 법

## 새 미니게임 만들기 (10단계)

1. `Assets/_Project/Scripts/Minigames/<이름>/` 폴더에 `MicrogameBase`를 상속한 클래스를 만든다 (네임스페이스 `BariBarista.Minigames`).
2. `ResetState()`는 필수다. 위치, Rigidbody 속도, 액체 양, 게이지, 풀 오브젝트를 전부 처음으로 되돌린다. 같은 미니게임이 한 판에 여러 번 켜지기 때문이다.
3. `OnBegin()`에서 `Ctx.Cup`(컵 내용물)을 보고 컵 모델 상태(얼음, 액체 높이)를 맞추고 입력을 켠다.
4. 판정은 `OnTick(remaining01)`에서 한다. 이번 틱에 흐른 시간은 `TickDelta`다. **`Time.deltaTime`으로 제한 시간을 세지 않는다.** 물리 조작은 평소처럼 `FixedUpdate`를 쓴다.
5. 결과가 나오면 `Succeed(score)` 또는 `Fail(FailReason.X)`를 부른다. 처음 한 번만 반영되고 이후 호출은 무시된다.
6. 시간 초과 처리를 바꾸려면 `OnTimeUp()`을 재정의한다(기본은 Timeout 실패). 버티기형이면 정의 에셋의 `successOnTimeout`을 켠다.
7. 결과는 `ApplyResult(result)`에서 `Ctx.Cup.Add(...)`로 컵에 남기고 통계(`MicrogameStats.Report`)를 보낸다. 이 메서드는 `Finished` 이벤트보다 먼저 불린다. 입력 끄기 같은 정리는 `OnEnd(result)`에서 한다. 강제 종료(`Aborted`)일 때는 ApplyResult가 불리지 않는다. `OnDisable`이 필요하면 `protected override`로 만들고 `base.OnDisable()`을 부른다.
8. 입력은 `Hand`(IHandInput: 포인터, 잡기, 기울기)로 읽는다. 마우스를 직접 읽지 않아야 나중에 물리 손으로 바꿀 수 있다. 오브젝트를 끌고 다녀야 하면 `MouseSpringFollower`를 붙인다.
9. 프리팹 규칙: 루트에 미니게임 컴포넌트 1개와 `Id`를 두고, 자식에 카메라 1개를 둔다. AudioListener와 EventSystem은 넣지 않는다. 모델·기계는 인스펙터 참조로만 연결한다(이름 하드코딩 금지). 수치는 `LevelSettings[]`(Lv1~3)로 뺀다.
10. `Create > BariBarista > Microgame Definition`으로 정의 에셋을 만든다(id는 `Id`와 같게, 지시어, stationId, 제한 시간, 프리팹). 그다음 샌드박스 씬의 `MicrogameRunner` → `entries`에 넣고 Play해서 테스트한다.

시간 초과는 호출하는 쪽이 `Tick`으로 시간을 넘겨서 처리한다(그래야 Finished와 컵 반영이 일어난다). `ForceEnd`는 게임오버·재시작 정리용이다.

판정 로직은 가능하면 `Minigames/Logic/`의 순수 C# 클래스로 분리하고 `Minigames/Tests/`에 EditMode 테스트를 단다. 테스트는 `Tools/BariBarista/Run Minigame Tests`로 실행한다.

## 샌드박스 사용법

- 메뉴 `Tools/BariBarista/Create Minigame Sandbox`: 테스트 씬, 프리팹, 정의 에셋을 새로 만든다. 같은 이름이 있으면 덮어쓰지 않고 새 이름으로 만든다.
- `Assets/_Project/Scenes/MinigameSandbox.unity`를 열고 Play한다.
  - **R** 재시작, **1/2/3** 난이도, **Tab** 다음 미니게임, **C** 컵 이어 쓰기(이전 미니게임 결과를 다음 미니게임에 넘기는 테스트)
  - 화면 왼쪽 위에 지시어, 남은 시간, 결과, 컵 내용물이 표시된다.

## 미니게임 3종 조작

| 미니게임 | 조작 | 성공 / 실패 |
| --- | --- | --- |
| 에스프레소 샷 | 좌클릭을 누르고 있으면 추출된다. Lv3은 먼저 컵을 클릭해 받침 위로 끌어다 놓는다 | 게이지의 초록 구간에서 떼면 성공. 컵을 넘치게 하거나 구간을 넘겨서 떼면 실패 |
| 얼음 퍼기 | 스쿱이 마우스를 따라온다. 제빙기 위에서 좌클릭을 누르고 있으면 담기고, 컵 위에서 휠을 아래로 굴리면 기울어진다 | 컵 안 얼음 개수가 목표 구간에서 1초 유지되면 성공. 최대치를 넘기면 실패 |
| 우유 붓기 | 좌클릭을 누르고 있는 동안 우유팩을 잡는다. 휠을 아래로 굴리면 기울고 위로 굴리면 다시 선다 | 목표 선 안에서 팩을 세우면 성공. 넘치거나 컵 밖으로 많이 부으면 실패 |
