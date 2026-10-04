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

        private readonly CupContents cup = new CupContents();
        private int index = -1;
        private int difficulty = 1;
        private int runCount;

        // OnGUI에서 매 프레임 문자열을 만들지 않도록 상태가 바뀔 때만 갱신
        private string headerText = string.Empty;
        private string resultText = string.Empty;
        private string cupText = string.Empty;
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
                if (entries[i].microgame != null) entries[i].microgame.Finished -= OnFinished;
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
            if (game != null && game.IsRunning) game.Tick(Time.deltaTime);
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
            game.Begin(new MicrogameContext(def, limit, 0, difficulty, cup));
            RefreshHeader();
            resultText = "진행 중...";
            cupText = "컵: " + cup;
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

            GUI.Box(new Rect(10, 10, 640, 150), GUIContent.none);
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
            GUI.Label(new Rect(20, 124, 620, 24), HelpText, smallStyle);
        }
    }
}
