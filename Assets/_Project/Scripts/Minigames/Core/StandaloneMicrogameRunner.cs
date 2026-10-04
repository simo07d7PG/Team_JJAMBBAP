using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BariBarista.Minigames
{
    /// <summary>
    /// 코어 루프 없이 미니게임을 단독으로 돌리는 테스트 도구.
    /// R 재시작, 1/2/3 난이도, Tab 다음 미니게임, C 컵 이어 쓰기 토글. 화면은 IMGUI라 한글이 깨지지 않는다.
    /// </summary>
    public class StandaloneMicrogameRunner : MonoBehaviour
    {
        [Serializable]
        public struct Entry
        {
            public MicrogameBase microgame;
            [Tooltip("비우면 제한 시간 7초, 지시어는 Id로 대신한다")]
            public MicrogameDefinition definition;
        }

        [SerializeField] private Entry[] entries = Array.Empty<Entry>();
        [SerializeField] private int startIndex;
        [SerializeField] private bool autoStart = true;
        [Tooltip("켜면 재시작해도 컵을 비우지 않는다(컵 인계 테스트)")]
        [SerializeField] private bool keepCupBetweenRuns;
        [SerializeField] private float fallbackTimeLimit = 7f;
        [Tooltip("재시작 뒤 Begin 전까지 지시어 단계로 기다리는 시간(초, 일시정지 영향 없음)")]
        [SerializeField] private float instructionSeconds = 0.5f;
        [Tooltip("결과 연출이 끝나고 자동으로 다시 시작하기까지의 시간(초). 0이면 자동 재시작 안 함")]
        [SerializeField] private float autoRestartDelay = 1f;

        private readonly CupContents cup = new CupContents();
        private int index = -1;
        private int difficulty = 1;
        private int runCount;

        // 지시어 단계 대기와 자동 재시작은 게임 시간(Tick)과 별개라 실행기가 직접 잰다
        private MicrogameContext pendingContext;
        private float prepareTimer;
        private float restartTimer = -1f;

        // OnGUI에서 매 프레임 문자열을 만들지 않도록 상태가 바뀔 때만 갱신
        private string headerText = string.Empty;
        private string resultText = string.Empty;
        private string cupText = string.Empty;
        private string phaseText = string.Empty;
        private MicrogamePhase shownPhase = (MicrogamePhase)(-1);
        private int shownTenths = -2;
        private static readonly string[] PhaseNames = { "대기", "준비", "진행", "판정", "연출" };
        private const string HelpText = "R 재시작   1/2/3 난이도   Tab 다음 미니게임   C 컵 이어 쓰기";
        private GUIStyle bigStyle;
        private GUIStyle smallStyle;

        /// <summary>단독 실행기가 들고 있는 컵.</summary>
        public CupContents Cup => cup;

        public MicrogameBase Current => index >= 0 && index < entries.Length ? entries[index].microgame : null;

        private void Start()
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].microgame == null) continue;
                entries[i].microgame.Finished += OnFinished;
                entries[i].microgame.PresentationFinished += OnPresentationFinished;
                SetEntryActive(i, false);
            }
            if (entries.Length == 0) return;
            index = Mathf.Clamp(startIndex, 0, entries.Length - 1);
            if (entries[index].definition != null) difficulty = entries[index].definition.defaultDifficulty;
            if (autoStart) Restart();
            else SetEntryActive(index, true);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].microgame == null) continue;
                entries[i].microgame.Finished -= OnFinished;
                entries[i].microgame.PresentationFinished -= OnPresentationFinished;
            }
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.rKey.wasPressedThisFrame) Restart();
                else if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) SetDifficulty(1);
                else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) SetDifficulty(2);
                else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) SetDifficulty(3);
                else if (kb.tabKey.wasPressedThisFrame) Next();
                else if (kb.cKey.wasPressedThisFrame) { keepCupBetweenRuns = !keepCupBetweenRuns; RefreshHeader(); }
            }

            var game = Current;
            if (game == null) return;

            switch (game.Phase)
            {
                case MicrogamePhase.Preparing:
                    if (pendingContext != null)
                    {
                        prepareTimer -= Time.unscaledDeltaTime;
                        if (prepareTimer <= 0f) StartPlaying(game);
                    }
                    break;
                case MicrogamePhase.Playing:
                case MicrogamePhase.Presenting:
                    game.Tick(Time.deltaTime);
                    break;
            }

            if (restartTimer >= 0f)
            {
                restartTimer -= Time.unscaledDeltaTime;
                if (restartTimer < 0f) Restart();
            }

            RefreshPhaseText(game);
        }

        private void StartPlaying(MicrogameBase game)
        {
            var ctx = pendingContext;
            pendingContext = null;
            resultText = "진행 중...";
            game.Begin(ctx);
        }

        public void SetDifficulty(int level)
        {
            difficulty = Mathf.Clamp(level, 1, 3);
            Restart();
        }

        public void Next()
        {
            if (entries.Length == 0) return;
            var game = Current;
            if (game != null) game.ForceEnd();
            SetEntryActive(index, false);
            index = (index + 1) % entries.Length;
            Restart();
        }

        public void Restart()
        {
            var game = Current;
            if (game == null) return;
            game.ForceEnd();
            if (!keepCupBetweenRuns) cup.Clear();

            for (int i = 0; i < entries.Length; i++) SetEntryActive(i, i == index);

            var def = entries[index].definition;
            float limit = def != null ? def.baseTimeLimit : fallbackTimeLimit;
            runCount++;
            restartTimer = -1f;
            pendingContext = new MicrogameContext(def, limit, 0, difficulty, cup);
            prepareTimer = instructionSeconds;
            RefreshHeader();
            cupText = "컵: " + cup;
            game.Prepare(pendingContext);
            if (instructionSeconds <= 0f) StartPlaying(game);
            else resultText = "준비...";
        }

        private void OnPresentationFinished(MicrogameBase game, PresentationEnd how)
        {
            if (game != Current) return;
            // 연출이 끝나면 루트를 끄고(카메라·UI가 사라진다) 잠시 뒤 다시 시작
            SetEntryActive(index, false);
            restartTimer = autoRestartDelay > 0f ? autoRestartDelay : -1f;
        }

        private void RefreshPhaseText(MicrogameBase game)
        {
            var phase = game.Phase;
            int tenths = phase == MicrogamePhase.Presenting ? Mathf.CeilToInt(game.PresentationRemaining * 10f) : -1;
            if (phase == shownPhase && tenths == shownTenths) return;
            shownPhase = phase;
            shownTenths = tenths;
            string phaseName = PhaseNames[(int)phase];
            phaseText = tenths >= 0 ? $"단계: {phaseName}   연출 남은 시간 {tenths / 10f:0.0}초" : $"단계: {phaseName}";
        }

        private void OnFinished(MicrogameBase game, MicrogameResult result)
        {
            if (game != Current) return;
            // 컵은 Finished 전에 ApplyResult로 이미 반영돼 있다
            cupText = "컵: " + cup;
            resultText = result.Success
                ? $"성공!  점수 {result.Score:0.00}   ({result.Elapsed:0.0}초, 양 {result.Amount:0.#}, 버림 {result.Wasted:0.#})"
                : $"실패: {result.Reason}   ({result.Elapsed:0.0}초, 양 {result.Amount:0.#}, 버림 {result.Wasted:0.#})";
        }

        private void RefreshHeader()
        {
            var e = entries[index];
            string instruction = e.definition != null && !string.IsNullOrEmpty(e.definition.instruction)
                ? e.definition.instruction : e.microgame.Id;
            headerText = $"[{index + 1}/{entries.Length}] {instruction}   Lv{difficulty}   #{runCount}" + (keepCupBetweenRuns ? "   (컵 이어 쓰기)" : string.Empty);
        }

        private void SetEntryActive(int i, bool active)
        {
            if (i < 0 || i >= entries.Length || entries[i].microgame == null) return;
            var go = entries[i].microgame.gameObject;
            if (go.activeSelf != active) go.SetActive(active);
        }

        private void OnGUI()
        {
            var game = Current;
            if (game == null) return;
            bigStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold };
            smallStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 16 };

            GUI.Box(new Rect(10, 10, 640, 176), GUIContent.none);
            GUI.Label(new Rect(20, 14, 620, 36), headerText, bigStyle);

            // 남은 시간 바
            float r = game.IsRunning ? game.Remaining01 : 0f;
            GUI.Box(new Rect(20, 54, 600, 14), GUIContent.none);
            Color prev = GUI.color;
            GUI.color = Color.Lerp(Color.red, Color.green, r);
            GUI.DrawTexture(new Rect(21, 55, 598 * r, 12), Texture2D.whiteTexture);
            GUI.color = prev;

            GUI.Label(new Rect(20, 74, 620, 24), resultText, smallStyle);
            GUI.Label(new Rect(20, 98, 620, 24), cupText, smallStyle);
            GUI.Label(new Rect(20, 124, 620, 24), phaseText, smallStyle);
            GUI.Label(new Rect(20, 150, 620, 24), HelpText, smallStyle);
        }
    }
}
