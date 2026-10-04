using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BariBarista.Minigames.EditorTools
{
    /// <summary>
    /// 스크립트로 정한 손 입력. 시뮬레이션 하네스가 마우스 대신 넣는다.
    /// 포인터는 화면 좌표, 누름 순간(Pressed/Released)은 EndFrame을 부를 때마다 갱신된다.
    /// </summary>
    public sealed class ScriptedHandInput : IHandInput
    {
        public Vector2 Pointer;
        public bool Grab;
        public bool Tilt;
        private bool prevGrab;

        public Vector2 PointerPosition => Pointer;
        public bool GrabHeld => Grab;
        public bool GrabPressedThisFrame => Grab && !prevGrab;
        public bool GrabReleasedThisFrame => !Grab && prevGrab;
        public bool TiltHeld => Tilt;
        public float TiltDelta => 0f;

        /// <summary>한 스텝이 끝날 때마다 부른다.</summary>
        public void EndFrame() => prevGrab = Grab;

        public void Clear()
        {
            Grab = false;
            Tilt = false;
            prevGrab = false;
        }
    }

    /// <summary>검증 도구가 미니게임을 정해진 자세로 세워 둘 때 쓰는 상태.</summary>
    public enum HarnessPose
    {
        Prepared,
        Playing,
        /// <summary>플레이 2초 뒤. 큰 안내가 구석 힌트로 바뀐 상태.</summary>
        PlayingLate,
        FailPresenting,
        SuccessPresenting,
    }

    /// <summary>
    /// 샌드박스 씬을 편집 모드에서 수동 스텝으로 돌려 숫자로 검증하는 도구.
    /// 한 스텝 = microgame.Tick(dt) → follower.SimulationStep(dt) → Physics.Simulate(dt). 플레이 루프를 쓰지 않아 에디터가 포커스를 잃어도 된다.
    /// 끝나면 Physics.simulationMode를 되돌리고, 샌드박스 씬을 저장하지 않고 다시 연다(얼음 풀이 편집 모드에서 오브젝트를 만들기 때문).
    /// </summary>
    public static class MinigameSimHarness
    {
        private const string SandboxScenePath = "Assets/_Project/Scenes/MinigameSandbox.unity";
        private const int FairnessSeeds = 20;
        private const int SoakRuns = 10;

        // 검증용 시뮬레이션 한 스텝의 길이. 물리 기본 간격과 같다
        private static float StepSeconds => Time.fixedDeltaTime;

        // ───────────────────────── 메뉴 ─────────────────────────

        [MenuItem("Tools/BariBarista/Sim/Fairness (Lv1-3)")]
        private static void FairnessMenu() => RunFairness();

        [MenuItem("Tools/BariBarista/Sim/Restart Soak x10")]
        private static void SoakMenu() => RunSoak();

        [MenuItem("Tools/BariBarista/Sim/Feel Metrics")]
        private static void FeelMetricsMenu() => RunFeelMetrics();

        // ───────────────────────── 공개 진입점 ─────────────────────────

        /// <summary>게임 × Lv1~3마다 스크립트 입력으로 성공 경로가 있는지 증명하고 표를 로그에 남긴다.</summary>
        /// <returns>9칸 모두 성공 경로가 있으면 true.</returns>
        public static bool RunFairness()
        {
            bool all = false;
            WithSandbox(rigs =>
            {
                var sb = new StringBuilder();
                sb.AppendLine("[SimHarness] Fairness (Lv1-3) — 시드 " + FairnessSeeds + "개, 성공률 = 기본 정책 / 성공 경로 = 정책 변형 포함 1건 이상");
                all = true;
                foreach (var rig in rigs)
                {
                    for (int level = 1; level <= 3; level++)
                    {
                        FairnessCell cell = rig.Game is EspressoShotMicrogame ? FairnessEspresso(rig, level)
                            : rig.Game is IceScoopMicrogame ? FairnessIce(rig, level)
                            : rig.Game is MilkPourMicrogame ? FairnessMilk(rig, level)
                            : default;
                        bool ok = cell.Successes > 0 || cell.VariantSuccess;
                        all &= ok;
                        sb.AppendLine($"  {rig.Game.Id,-14} Lv{level}  성공률 {cell.Successes}/{FairnessSeeds} ({100f * cell.Successes / FairnessSeeds:0}%)  {cell.Extra}  성공 경로 {(ok ? "있음" : "없음")}");
                    }
                }
                sb.Append(all ? "결과: 9칸 모두 성공 경로 있음" : "결과: 성공 경로가 없는 칸이 있음");
                if (all) Debug.Log(sb.ToString()); else Debug.LogError(sb.ToString());
            });
            return all;
        }

        /// <summary>게임마다 Prepare/Begin을 10번 연속 하고 남은 상태가 없는지 확인한다.</summary>
        /// <returns>모든 불변식을 지키면 true.</returns>
        public static bool RunSoak()
        {
            bool all = false;
            WithSandbox(rigs =>
            {
                var errors = new List<string>();
                var sb = new StringBuilder();
                sb.AppendLine("[SimHarness] Restart Soak x" + SoakRuns);
                foreach (var rig in rigs)
                {
                    int before = errors.Count;
                    SoakOne(rig, errors, out int finished, out int completed, out int cancelled);
                    sb.AppendLine($"  {rig.Game.Id,-14} Finished {finished}, PresentationFinished(Completed) {completed}, 연출 취소 {cancelled}, 위반 {errors.Count - before}");
                }
                all = errors.Count == 0;
                foreach (var e in errors) sb.AppendLine("  위반: " + e);
                sb.Append(all ? "결과: 불변식 모두 통과" : "결과: 불변식 위반 있음");
                if (all) Debug.Log(sb.ToString()); else Debug.LogError(sb.ToString());
            });
            return all;
        }

        /// <summary>
        /// 손맛·연출 숫자 기준을 로그에 남기는 자리. 지금은 현재 값만 기록하고, WP4·WP5가 항목을 채운다.
        /// </summary>
        public static void RunFeelMetrics()
        {
            WithSandbox(rigs =>
            {
                var sb = new StringBuilder();
                sb.AppendLine("[SimHarness] Feel Metrics (현재 값)");
                sb.AppendLine($"  연출 시간 기본값: 실패 {ResultPresentation.FailSeconds:0.0}초, 성공 {ResultPresentation.SuccessSeconds:0.0}초");
                foreach (var rig in rigs)
                {
                    rig.StartRun(1);
                    sb.Append($"  {rig.Game.Id,-14} 단계 {rig.Game.Phase}");
                    foreach (var f in rig.Followers)
                        sb.Append($", {f.name} 기울기 {f.ActualTilt:0.0}° / 목표 {f.TargetTilt:0.0}°");
                    sb.AppendLine();
                    rig.Game.ForceEnd();
                }
                Debug.Log(sb.ToString());
            });
        }

        /// <summary>미니게임을 지정한 자세로 세워 둔다(캡처용). 씬 로드·정리는 호출한 쪽 몫이다.</summary>
        public static void PoseState(MicrogameBase game, HarnessPose pose, FailReason failReason = FailReason.Overflow)
        {
            var rig = new Rig(game);
            try
            {
                switch (pose)
                {
                    case HarnessPose.Prepared:
                        rig.PrepareOnly(1);
                        break;
                    case HarnessPose.Playing:
                        rig.StartRun(1);
                        rig.StepFor(0.5f);
                        break;
                    case HarnessPose.PlayingLate:
                        rig.StartRun(1);
                        rig.StepFor(2f);
                        break;
                    case HarnessPose.FailPresenting:
                        rig.StartRun(1);
                        game.DebugForceFail(failReason);
                        rig.StepFor(game.PresentationDuration * 0.4f);
                        break;
                    case HarnessPose.SuccessPresenting:
                        rig.StartRun(1);
                        game.DebugComplete(MicrogameResult.Succeeded(0.95f));
                        rig.StepFor(game.PresentationDuration * 0.6f);
                        break;
                }
            }
            finally
            {
                rig.Dispose();
            }
        }

        /// <summary>
        /// 모든 미니게임의 안내·플레이·실패 이유별·성공 화면을 PNG로 저장한다(Canvas 캡처).
        /// 중간 상태 화면은 HUD에 예시 값을 직접 넣어 만든다. 씬은 저장하지 않는다.
        /// </summary>
        public static void CaptureAllHud(string dir)
        {
            WithSandbox(rigs =>
            {
                foreach (var rig in rigs)
                {
                    var g = rig.Game;
                    string id = g.Id;
                    PoseState(g, HarnessPose.Prepared);
                    CaptureHud(g, Path.Combine(dir, id + "_1_guide.png"));
                    PoseState(g, HarnessPose.PlayingLate);
                    PokeSampleValues(g);
                    CaptureHud(g, Path.Combine(dir, id + "_2_playing.png"));
                    foreach (var reason in new[] { FailReason.Overflow, FailReason.TooMuch, FailReason.TooLittle, FailReason.Timeout, FailReason.Spilled })
                    {
                        if (reason == FailReason.Spilled && !(g is MilkPourMicrogame)) continue;
                        PoseState(g, HarnessPose.FailPresenting, reason);
                        CaptureHud(g, Path.Combine(dir, id + "_3_fail_" + reason + ".png"));
                    }
                    PoseState(g, HarnessPose.SuccessPresenting);
                    CaptureHud(g, Path.Combine(dir, id + "_4_success.png"));
                    g.ForceEnd();
                }
            });
        }

        /// <summary>캡처용: 진행 중 화면에 보일 예시 값을 HUD에 넣는다.</summary>
        private static void PokeSampleValues(MicrogameBase g)
        {
            var shot = g.GetComponentInChildren<EspressoShotHud>(true);
            if (shot != null) shot.SetAmount(32f, 60f);
            var ice = g.GetComponentInChildren<IceScoopHud>(true);
            if (ice != null) ice.SetState(5, 0.6f);
            var milk = g.GetComponentInChildren<MilkPourHud>(true);
            if (milk != null) { milk.SetFill(0.55f); milk.SetSpill(0.3f); }
        }

        /// <summary>
        /// 미니게임 안의 Canvas를 임시로 Screen Space - Camera로 바꿔 프리팹 카메라로 찍어 PNG로 저장한다.
        /// 끝나면 Canvas 설정을 모두 되돌린다. 씬·프리팹은 저장하지 않는다.
        /// </summary>
        /// <returns>저장했으면 true, Canvas나 카메라가 없으면 false.</returns>
        public static bool CaptureHud(MicrogameBase game, string path)
        {
            var canvas = game.GetComponentInChildren<Canvas>(true);
            var cam = game.GetComponentInChildren<Camera>(true);
            if (canvas == null || cam == null)
            {
                Debug.LogWarning($"[SimHarness] {game.name}: Canvas 또는 Camera가 없어 캡처하지 못했습니다.");
                return false;
            }

            var prevMode = canvas.renderMode;
            var prevCamera = canvas.worldCamera;
            float prevDistance = canvas.planeDistance;
            int prevOrder = canvas.sortingOrder;
            var prevTarget = cam.targetTexture;
            var prevActive = RenderTexture.active;
            var rt = new RenderTexture(1280, 720, 24);
            Texture2D tex = null;
            try
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = cam.nearClipPlane + 0.1f;
                Canvas.ForceUpdateCanvases();
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                return true;
            }
            finally
            {
                canvas.renderMode = prevMode;
                canvas.worldCamera = prevCamera;
                canvas.planeDistance = prevDistance;
                canvas.sortingOrder = prevOrder;
                cam.targetTexture = prevTarget;
                RenderTexture.active = prevActive;
                rt.Release();
                Object.DestroyImmediate(rt);
                if (tex != null) Object.DestroyImmediate(tex);
            }
        }

        // ───────────────────────── 세션(씬 열기·물리 모드·정리) ─────────────────────────

        private static void WithSandbox(System.Action<List<Rig>> body)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[SimHarness] 플레이 중에는 실행할 수 없습니다.");
                return;
            }

            string returnPath = null;
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                var open = EditorSceneManager.GetSceneAt(i);
                if (open.isDirty && open.path != SandboxScenePath)
                {
                    Debug.LogWarning($"[SimHarness] 저장하지 않은 씬({open.path})이 있어 취소했습니다.");
                    return;
                }
                if (i == 0) returnPath = open.path;
            }

            var prevMode = Physics.simulationMode;
            var rigs = new List<Rig>();
            try
            {
                EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
                Physics.simulationMode = SimulationMode.Script;

                var games = Object.FindObjectsByType<MicrogameBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                System.Array.Sort(games, (a, b) => string.CompareOrdinal(a.name, b.name));
                foreach (var g in games) rigs.Add(new Rig(g));
                if (rigs.Count == 0)
                {
                    Debug.LogWarning("[SimHarness] 샌드박스 씬에 미니게임이 없습니다. Tools/BariBarista/Rebuild Minigame Prefabs를 먼저 실행하세요.");
                    return;
                }
                body(rigs);
            }
            finally
            {
                foreach (var r in rigs) r.Dispose();
                Physics.simulationMode = prevMode;
                // 저장하지 않고 다시 연다. 원래 열려 있던 씬이 있으면 그 씬으로 돌아간다
                string reopen = string.IsNullOrEmpty(returnPath) ? SandboxScenePath : returnPath;
                EditorSceneManager.OpenScene(reopen, OpenSceneMode.Single);
            }
        }

        // ───────────────────────── 한 미니게임을 돌리는 도구 ─────────────────────────

        private sealed class Rig
        {
            public readonly MicrogameBase Game;
            public readonly ScriptedHandInput Input = new ScriptedHandInput();
            public readonly Camera Cam;
            public readonly MouseSpringFollower[] Followers;
            public readonly MicrogameDefinition Definition;
            public readonly float TimeLimit;
            public int Finished;
            public int Completed;
            public int Skipped;
            public MicrogameResult Result;
            public float Elapsed;
            private RenderTexture target;

            public float Dt => StepSeconds;
            public bool Done => Finished > 0;

            public Rig(MicrogameBase game)
            {
                Game = game;
                Cam = RefOf<Camera>(game, "viewCamera");
                Followers = game.GetComponentsInChildren<MouseSpringFollower>(true);
                Definition = FindDefinition(game.Id);
                TimeLimit = Definition != null ? Definition.baseTimeLimit : 7f;
                game.Hand = Input;
                game.Finished += OnFinished;
                game.PresentationFinished += OnPresentationFinished;
                // 카메라 크기가 에디터 창에 따라 달라지지 않도록 고정 크기 렌더 텍스처를 붙인다
                if (Cam != null)
                {
                    target = new RenderTexture(1280, 720, 24);
                    Cam.targetTexture = target;
                }
            }

            public void Dispose()
            {
                Game.Finished -= OnFinished;
                Game.PresentationFinished -= OnPresentationFinished;
                ReleaseTarget();
            }

            public void ReleaseTarget()
            {
                if (target == null) return;
                if (Cam != null) Cam.targetTexture = null;
                target.Release();
                Object.DestroyImmediate(target);
                target = null;
            }

            private void OnFinished(MicrogameBase g, MicrogameResult r)
            {
                Finished++;
                Result = r;
            }

            private void OnPresentationFinished(MicrogameBase g, PresentationEnd how)
            {
                if (how == PresentationEnd.Completed) Completed++;
                else if (how == PresentationEnd.Skipped) Skipped++;
            }

            public MicrogameContext NewContext(int level) => new MicrogameContext(Definition, TimeLimit, 0, level, new CupContents());

            public void ResetCounters()
            {
                Finished = 0;
                Completed = 0;
                Skipped = 0;
                Result = default;
                Elapsed = 0f;
                Input.Clear();
            }

            public MicrogameContext PrepareOnly(int level)
            {
                ResetCounters();
                var ctx = NewContext(level);
                Game.Prepare(ctx);
                // ResetState가 옮긴 위치를 콜라이더 쿼리가 바로 보도록
                Physics.SyncTransforms();
                return ctx;
            }

            /// <summary>Prepare → Begin을 곧바로 한다.</summary>
            public MicrogameContext StartRun(int level)
            {
                var ctx = PrepareOnly(level);
                Game.Begin(ctx);
                return ctx;
            }

            /// <summary>한 스텝: Tick → 손 따라가기 → 물리.</summary>
            public void Step()
            {
                float dt = Dt;
                Physics.SyncTransforms();
                Game.Tick(dt);
                for (int i = 0; i < Followers.Length; i++) Followers[i].SimulationStep(dt);
                Physics.Simulate(dt);
                Input.EndFrame();
                Elapsed += dt;
            }

            public void StepFor(float seconds)
            {
                int n = Mathf.CeilToInt(seconds / Dt);
                for (int i = 0; i < n; i++) Step();
            }

            public int MaxSteps => Mathf.CeilToInt((TimeLimit + 3f) / Dt);

            public Vector2 ToScreen(Vector3 world) => Cam.WorldToScreenPoint(world);

            public void Finish()
            {
                Game.ForceEnd();
                Input.Clear();
            }
        }

        // ───────────────────────── 에셋·직렬화 필드 읽기 ─────────────────────────

        private static T RefOf<T>(Object owner, string field) where T : Object
        {
            var p = new SerializedObject(owner).FindProperty(field);
            return p != null ? p.objectReferenceValue as T : null;
        }

        private static SerializedProperty LevelOf(Object owner, int level)
        {
            var levels = new SerializedObject(owner).FindProperty("levels");
            return levels.GetArrayElementAtIndex(Mathf.Clamp(level - 1, 0, levels.arraySize - 1));
        }

        private static MicrogameDefinition FindDefinition(string id)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:MicrogameDefinition"))
            {
                var def = AssetDatabase.LoadAssetAtPath<MicrogameDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (def != null && def.id == id) return def;
            }
            return null;
        }

        private static float Horizontal(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return (a - b).magnitude;
        }

        private static bool Inside(Collider zone, Vector3 point)
        {
            if (zone == null || !zone.enabled || !zone.gameObject.activeInHierarchy) return false;
            return (zone.ClosestPoint(point) - point).sqrMagnitude < 1e-6f;
        }

        private static int CountInZone(IcePiecePool pool, Collider zone)
        {
            var list = pool.Active;
            int n = 0;
            for (int i = 0; i < list.Count; i++)
                if (Inside(zone, list[i].transform.position)) n++;
            return n;
        }

        // ───────────────────────── Fairness ─────────────────────────

        private struct FairnessCell
        {
            public int Successes;
            public bool VariantSuccess;
            public string Extra;
        }

        /// <summary>
        /// 에스프레소: (Lv3은 컵을 받침으로 끌고) 누름 → 구간 중앙에 닿은 뒤 reactSeconds만큼 더 추출하고 떼기.
        /// 0.12초가 기본 정책, 0초(즉시 떼기)는 성공 경로 존재 증명용 변형.
        /// </summary>
        private static FairnessCell FairnessEspresso(Rig rig, int level)
        {
            var cell = new FairnessCell();
            for (int seed = 1; seed <= FairnessSeeds; seed++)
                if (PlayEspresso(rig, level, seed, 0.12f)) cell.Successes++;

            int oracle = 0;
            for (int seed = 1; seed <= FairnessSeeds; seed++)
                if (PlayEspresso(rig, level, seed, 0f)) oracle++;
            cell.VariantSuccess = oracle > 0;
            cell.Extra = $"즉시 떼기 {oracle}/{FairnessSeeds}";
            return cell;
        }

        private static bool PlayEspresso(Rig rig, int level, int seed, float reactSeconds)
        {
            var g = (EspressoShotMicrogame)rig.Game;
            var lv = LevelOf(g, level);
            float center = (lv.FindPropertyRelative("targetMinMl").floatValue + lv.FindPropertyRelative("targetMaxMl").floatValue) * 0.5f;
            var cup = RefOf<Transform>(g, "cup");
            var cupCollider = RefOf<Collider>(g, "cupCollider");
            var saucer = RefOf<Transform>(g, "saucer");
            // 놓았을 때 제자리에 붙는 반경 안까지만 끌면 된다(사람도 그렇게 놓는다)
            float snapRadius = new SerializedObject(g).FindProperty("snapRadius").floatValue;

            Random.InitState(seed);
            rig.StartRun(level);

            // 0 컵 누르기, 1 끌기, 2 놓은 뒤 다시 누르기, 3 추출
            int stage = g.CupPlaced ? 3 : 0;
            if (stage == 3) rig.Input.Grab = true;
            float reacted = 0f;
            int max = rig.MaxSteps;
            for (int i = 0; i < max && !rig.Done; i++)
            {
                switch (stage)
                {
                    case 0:
                        rig.Input.Pointer = rig.ToScreen(cupCollider.bounds.center);
                        rig.Input.Grab = true;
                        stage = 1;
                        break;
                    case 1:
                        Vector3 snap = new Vector3(saucer.position.x, cup.position.y, saucer.position.z);
                        rig.Input.Pointer = rig.ToScreen(snap);
                        if (Horizontal(cup.position, snap) < snapRadius * 0.75f)
                        {
                            rig.Input.Grab = false;
                            stage = 2;
                        }
                        break;
                    case 2:
                        if (!g.CupPlaced) { i = max; break; }
                        rig.Input.Grab = true;
                        stage = 3;
                        break;
                    case 3:
                        if (g.Extracted >= center)
                        {
                            reacted += rig.Dt;
                            if (reacted >= reactSeconds - 1e-4f) rig.Input.Grab = false;
                        }
                        break;
                }
                rig.Step();
            }
            bool ok = rig.Done && rig.Result.Success;
            rig.Finish();
            return ok;
        }

        /// <summary>
        /// 얼음: 제빙기에서 담기 → 컵 위로 이동 → 우클릭으로 기울여 붓기 → 세우기를 목표 개수가 될 때까지 반복, 그 뒤 가만히 유지.
        /// </summary>
        private static FairnessCell FairnessIce(Rig rig, int level)
        {
            var cell = new FairnessCell();
            for (int seed = 1; seed <= FairnessSeeds; seed++)
                if (PlayIce(rig, level, seed, 5)) cell.Successes++;

            // 한 번에 담는 개수를 바꾼 변형
            int variant = 0;
            if (cell.Successes == 0)
            {
                foreach (int perTrip in new[] { 3, 4, 2 })
                {
                    for (int seed = 1; seed <= FairnessSeeds && variant == 0; seed++)
                        if (PlayIce(rig, level, seed, perTrip)) variant++;
                }
            }
            cell.VariantSuccess = variant > 0;
            cell.Extra = cell.Successes > 0 ? "" : $"변형 성공 {variant}";
            return cell;
        }

        private static bool PlayIce(Rig rig, int level, int seed, int perTrip)
        {
            var g = (IceScoopMicrogame)rig.Game;
            var scoop = RefOf<MouseSpringFollower>(g, "scoop");
            var fillPoint = RefOf<Transform>(g, "scoopFillPoint");
            var scoopZone = RefOf<Collider>(g, "scoopZone");
            var binZone = RefOf<Collider>(g, "binZone");
            var cupZone = RefOf<Collider>(g, "cupZone");
            var pool = RefOf<IcePiecePool>(g, "icePool");
            var lv = LevelOf(g, level);
            int targetMin = lv.FindPropertyRelative("targetMin").intValue;

            Random.InitState(seed);
            rig.StartRun(level);

            float planeY = scoop.transform.position.y;
            Vector3 fillOffset = fillPoint.position - scoop.transform.position;
            Vector3 binSpot = binZone.bounds.center - fillOffset;
            // 스쿱 앞쪽(+X)이 열려 있어 앞이 컵 가운데에 오도록 조금 뒤로 선다
            Vector3 cupSpot = cupZone.bounds.center - fillOffset - new Vector3(0.12f, 0f, 0f);
            binSpot.y = planeY;
            cupSpot.y = planeY;

            int state = 0;
            float stateTime = 0f;
            int max = rig.MaxSteps;
            for (int i = 0; i < max && !rig.Done; i++)
            {
                stateTime += rig.Dt;
                float speed = scoop.Body.linearVelocity.magnitude;
                switch (state)
                {
                    case 0: // 제빙기 위로
                        rig.Input.Grab = false;
                        rig.Input.Tilt = false;
                        rig.Input.Pointer = rig.ToScreen(binSpot);
                        if ((Horizontal(scoop.transform.position, binSpot) < 0.03f && speed < 0.3f) || stateTime > 2f) { state = 1; stateTime = 0f; }
                        break;
                    case 1: // 담기
                        rig.Input.Pointer = rig.ToScreen(binSpot);
                        rig.Input.Grab = true;
                        int need = Mathf.Clamp(targetMin - g.CountInCup, 1, perTrip);
                        if (CountInZone(pool, scoopZone) >= need || stateTime > 1.5f) { rig.Input.Grab = false; state = 2; stateTime = 0f; }
                        break;
                    case 2: // 컵 위로
                        rig.Input.Pointer = rig.ToScreen(cupSpot);
                        if ((Horizontal(scoop.transform.position, cupSpot) < 0.03f && speed < 0.3f) || stateTime > 2f) { state = 3; stateTime = 0f; }
                        break;
                    case 3: // 기울여 붓기
                        rig.Input.Tilt = true;
                        if ((CountInZone(pool, scoopZone) == 0 && stateTime > 0.4f) || stateTime > 1.6f) { rig.Input.Tilt = false; state = 4; stateTime = 0f; }
                        break;
                    case 4: // 다시 세우기
                        rig.Input.Tilt = false;
                        if (scoop.ActualTilt < 5f || stateTime > 0.8f)
                        {
                            state = g.CountInCup >= targetMin ? 5 : 0;
                            stateTime = 0f;
                        }
                        break;
                    case 5: // 유지
                        break;
                }
                rig.Step();
            }
            bool ok = rig.Done && rig.Result.Success;
            rig.Finish();
            return ok;
        }

        /// <summary>
        /// 우유: 잡고 컵 위로 이동 → 우클릭을 누르고 있다가 (현재 양 + 붓는 속도 × leadSeconds)가 구간 중앙에 닿으면 세우기.
        /// 기본 정책은 lead 0.07초, 성공이 없으면 다른 lead로 성공 경로가 있는지 본다.
        /// </summary>
        private static FairnessCell FairnessMilk(Rig rig, int level)
        {
            var cell = new FairnessCell();
            for (int seed = 1; seed <= FairnessSeeds; seed++)
                if (PlayMilk(rig, level, seed, 0.07f)) cell.Successes++;

            int variant = 0;
            if (cell.Successes == 0)
            {
                foreach (float lead in new[] { 0.05f, 0.09f, 0.11f, 0.03f, 0.13f })
                {
                    for (int seed = 1; seed <= FairnessSeeds && variant == 0; seed++)
                        if (PlayMilk(rig, level, seed, lead)) variant++;
                }
            }
            cell.VariantSuccess = variant > 0;
            cell.Extra = cell.Successes > 0 ? "" : $"변형 성공 {variant}";
            return cell;
        }

        private static bool PlayMilk(Rig rig, int level, int seed, float leadSeconds)
        {
            var g = (MilkPourMicrogame)rig.Game;
            var carton = RefOf<MouseSpringFollower>(g, "carton");
            var mouth = RefOf<Collider>(g, "cupMouth");
            var lv = LevelOf(g, level);
            float center = (lv.FindPropertyRelative("targetMin01").floatValue + lv.FindPropertyRelative("targetMax01").floatValue) * 0.5f;

            Random.InitState(seed);
            rig.StartRun(level);

            float planeY = carton.transform.position.y;
            // 입구는 팩 앞(+X)에 있고 기울이면 앞으로 더 나가므로 그만큼 뒤에 선다
            Vector3 spot = mouth.bounds.center - new Vector3(0.145f, 0f, 0f);
            spot.y = planeY;

            int state = 0;
            float stateTime = 0f;
            float lastFill = 0f;
            float flowPerSec = 0f;
            int max = rig.MaxSteps;
            for (int i = 0; i < max && !rig.Done; i++)
            {
                stateTime += rig.Dt;
                switch (state)
                {
                    case 0: // 잡고 컵 위로
                        rig.Input.Grab = true;
                        rig.Input.Pointer = rig.ToScreen(spot);
                        if ((Horizontal(carton.transform.position, spot) < 0.02f && carton.Body.linearVelocity.magnitude < 0.2f) || stateTime > 2f) { state = 1; stateTime = 0f; }
                        break;
                    case 1: // 기울이며 붓기
                        rig.Input.Tilt = true;
                        float fill = g.Fill01;
                        flowPerSec = Mathf.Lerp(flowPerSec, (fill - lastFill) / rig.Dt, 0.3f);
                        lastFill = fill;
                        if (fill + flowPerSec * leadSeconds >= center) { rig.Input.Tilt = false; state = 2; }
                        break;
                    case 2: // 세운 뒤 판정 기다리기
                        break;
                }
                rig.Step();
            }
            bool ok = rig.Done && rig.Result.Success;
            rig.Finish();
            return ok;
        }

        // ───────────────────────── Restart Soak ─────────────────────────

        private static void SoakOne(Rig rig, List<string> errors, out int finished, out int completed, out int cancelled)
        {
            var g = rig.Game;
            string id = g.Id;
            rig.ResetCounters();
            int finishedTotal = 0, completedTotal = 0, cancelledTotal = 0;
            bool presentationLeftOpen = false;

            for (int run = 0; run < SoakRuns; run++)
            {
                int level = run % 3 + 1;
                int completedBefore = rig.Completed;
                rig.Input.Clear();
                var ctx = rig.NewContext(level);

                // 앞 판의 연출을 끊고 Prepare하는 경로(이때 연출 끝 이벤트는 오지 않아야 한다)
                g.Prepare(ctx);
                if (presentationLeftOpen) cancelledTotal++;
                presentationLeftOpen = false;
                Expect(g.Phase == MicrogamePhase.Preparing, $"{id} #{run} Prepare 뒤 단계가 {g.Phase}", errors);
                CheckClean(rig, $"{id} #{run} Prepare 뒤", errors);

                g.Begin(ctx);
                Expect(g.Phase == MicrogamePhase.Playing, $"{id} #{run} Begin 뒤 단계가 {g.Phase}", errors);
                CheckClean(rig, $"{id} #{run} Begin 뒤", errors);

                // 상태를 어지럽힌다: 입력을 넣고 잠깐 돌린다
                DirtyState(rig);

                int finishedBefore = rig.Finished;
                if (g.Phase == MicrogamePhase.Playing)
                {
                    switch (run % 3)
                    {
                        case 0: g.DebugForceFail(FailReason.Overflow); break;
                        case 1: g.DebugComplete(MicrogameResult.Succeeded(0.95f)); break;
                        default: g.DebugForceFail(FailReason.Timeout); break;
                    }
                }
                Expect(rig.Finished == finishedBefore + 1, $"{id} #{run} Finished 호출 수 {rig.Finished - finishedBefore}", errors);
                Expect(g.Phase == MicrogamePhase.Presenting, $"{id} #{run} 결과 뒤 단계가 {g.Phase}", errors);
                float expected = rig.Result.Success ? ResultPresentation.SuccessSeconds : ResultPresentation.FailSeconds;
                Expect(Mathf.Abs(g.PresentationDuration - expected) < 1e-4f, $"{id} #{run} 연출 시간 {g.PresentationDuration}", errors);

                if (run % 4 == 3)
                {
                    // 연출 도중에 다음 판이 끼어든다(취소 경로)
                    rig.StepFor(0.3f);
                    Expect(g.Phase == MicrogamePhase.Presenting, $"{id} #{run} 연출 중 단계가 {g.Phase}", errors);
                    presentationLeftOpen = true;
                }
                else
                {
                    rig.StepFor(expected + 0.1f);
                    Expect(g.Phase == MicrogamePhase.Idle, $"{id} #{run} 연출 뒤 단계가 {g.Phase}", errors);
                    Expect(rig.Completed == completedBefore + 1, $"{id} #{run} PresentationFinished 수 {rig.Completed - completedBefore}", errors);
                }
                finishedTotal += rig.Finished - finishedBefore;
            }

            if (presentationLeftOpen) cancelledTotal++;
            g.ForceEnd();
            Expect(g.Phase == MicrogamePhase.Idle, $"{id} 마지막 ForceEnd 뒤 단계가 {g.Phase}", errors);
            completedTotal = rig.Completed;
            Expect(rig.Skipped == 0, $"{id} Skipped가 {rig.Skipped}번 호출됨", errors);
            Expect(completedTotal + cancelledTotal == SoakRuns, $"{id} 연출 끝 {completedTotal} + 취소 {cancelledTotal} != {SoakRuns}", errors);
            finished = finishedTotal;
            completed = completedTotal;
            cancelled = cancelledTotal;
        }

        private static void DirtyState(Rig rig)
        {
            var g = rig.Game;
            if (g is IceScoopMicrogame)
            {
                var pool = RefOf<IcePiecePool>(g, "icePool");
                var scoop = RefOf<MouseSpringFollower>(g, "scoop");
                for (int k = 0; k < 5; k++) pool.Spawn(scoop.transform.position + new Vector3(0.01f * k, 0.1f, 0f), Quaternion.identity);
            }
            rig.Input.Grab = true;
            rig.Input.Tilt = true;
            rig.StepFor(0.4f);
            rig.Input.Clear();
        }

        /// <summary>시작 직후 남은 것이 없어야 하는 항목들(WP1 범위: 얼음 풀, 컵 액체, 손 기울기·잡기).</summary>
        private static void CheckClean(Rig rig, string label, List<string> errors)
        {
            var g = rig.Game;
            if (g is IceScoopMicrogame)
            {
                var pool = RefOf<IcePiecePool>(g, "icePool");
                Expect(pool.ActiveCount == 0, $"{label}: 얼음 {pool.ActiveCount}개가 남음", errors);
            }

            CheckHud(g, label, errors);

            var cupVisual = g.GetComponentInChildren<CupVisual>(true);
            if (cupVisual != null)
            {
                var liquid = RefOf<Transform>(cupVisual, "liquid");
                bool empty = liquid == null || !liquid.gameObject.activeSelf || liquid.localScale.y <= 0.001f;
                Expect(empty, $"{label}: 컵 액체가 남음(높이 {(liquid != null ? liquid.localScale.y : 0f):0.###})", errors);
            }

            foreach (var f in rig.Followers)
            {
                Expect(f.ActualTilt < 0.5f && f.TargetTilt == 0f, $"{label}: {f.name} 기울기가 남음({f.ActualTilt:0.0}°/{f.TargetTilt:0.0}°)", errors);
                Expect(!f.IsGrabbed, $"{label}: {f.name} 잡기가 남음", errors);
            }

            if (g is EspressoShotMicrogame es) Expect(es.Extracted == 0f, $"{label}: 추출량 {es.Extracted}이 남음", errors);
            if (g is MilkPourMicrogame mp) Expect(mp.Fill01 == 0f, $"{label}: 우유 양 {mp.Fill01}이 남음", errors);
        }

        /// <summary>시작 직후 안내는 보이고 결과 글씨는 숨겨져 있어야 한다.</summary>
        private static void CheckHud(MicrogameBase g, string label, List<string> errors)
        {
            var hud = g.GetComponentInChildren<Canvas>(true);
            if (hud == null) { errors.Add($"{label}: HUD Canvas가 없음"); return; }
            Behaviour component = hud.GetComponent<EspressoShotHud>();
            if (component == null) component = hud.GetComponent<IceScoopHud>();
            if (component == null) component = hud.GetComponent<MilkPourHud>();
            if (component == null) { errors.Add($"{label}: HUD 컴포넌트가 없음"); return; }
            var guide = RefOf<GameObject>(component, "guideBig");
            var banner = RefOf<ResultBanner>(component, "banner");
            Expect(guide != null && guide.activeSelf, $"{label}: 큰 안내가 꺼져 있음", errors);
            Expect(banner != null && !banner.gameObject.activeSelf, $"{label}: 결과 글씨가 남음", errors);
            Expect(hud.renderMode == RenderMode.ScreenSpaceOverlay && hud.sortingOrder == -10, $"{label}: Canvas가 Overlay -10이 아님", errors);
        }

        private static void Expect(bool condition, string message, List<string> errors)
        {
            if (!condition) errors.Add(message);
        }
    }
}
