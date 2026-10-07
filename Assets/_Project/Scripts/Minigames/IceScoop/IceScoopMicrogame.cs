using System;
using UnityEngine;

namespace BariBarista.Minigames
{
    /// <summary>
    /// "얼음 퍼라!" — 스쿱으로 제빙기에서 얼음을 퍼서 컵에 목표 개수만큼 담는다.
    /// </summary>
    public class IceScoopMicrogame : MicrogameBase
    {
        [Serializable]
        public struct LevelSettings
        {
            public int targetMin;
            public int targetMax;
            [Tooltip("이 개수를 넘으면 넘침 실패")]
            public int cupMax;
            [Tooltip("목표 구간에 머물러야 하는 시간(초)")]
            public float holdSeconds;
            [Tooltip("제빙기 안에서 누르고 있을 때 초당 담기는 얼음 수")]
            public float scoopRate;
            [Tooltip("스쿱에 한 번에 담기는 최대 개수")]
            public int scoopCapacity;
        }

        [Header("참조")]
        [SerializeField] private Camera viewCamera;
        [SerializeField] private MouseSpringFollower scoop;
        [Tooltip("새 얼음이 생기는 위치 (스쿱 안쪽)")]
        [SerializeField] private Transform scoopFillPoint;
        [Tooltip("스쿱 안을 덮는 트리거. 스쿱에 담긴 개수를 셀 때 쓴다")]
        [SerializeField] private Collider scoopZone;
        [Tooltip("제빙기 얼음통 영역 트리거. 스쿱 입구가 이 안에 있을 때만 퍼진다")]
        [SerializeField] private Collider binZone;
        [Tooltip("컵 안을 덮는 트리거. 여기 들어간 얼음을 센다")]
        [SerializeField] private Collider cupZone;
        [Tooltip("바닥 높이 기준")]
        [SerializeField] private Transform floor;
        [SerializeField] private IcePiecePool icePool;
        [SerializeField] private CupVisual cupVisual;
        [SerializeField] private IceScoopHud hud;

        [Header("손맛")]
        [Tooltip("바닥에서 이 높이 아래로 내려간 얼음은 떨어진 것으로 본다")]
        [SerializeField] private float dropHeight = 0.06f;
        [Tooltip("얼음이 생길 때 위치를 흩뜨리는 반경")]
        [SerializeField] private float spawnJitter = 0.03f;
        [Tooltip("얼음 크기 무작위 폭(기본 크기의 ±비율)")]
        [SerializeField] private float sizeVariation = 0.15f;
        [Tooltip("컵 액체 높이 표시용 컵 용량(ml)")]
        [SerializeField] private float cupCapacityMl = 300f;

        [Header("난이도 (Lv1~3)")]
        [SerializeField] private LevelSettings[] levels =
        {
            new LevelSettings { targetMin = 4, targetMax = 7, cupMax = 10, holdSeconds = 1f, scoopRate = 8f, scoopCapacity = 8 },
            new LevelSettings { targetMin = 5, targetMax = 6, cupMax = 9, holdSeconds = 1f, scoopRate = 10f, scoopCapacity = 9 },
            new LevelSettings { targetMin = 5, targetMax = 5, cupMax = 8, holdSeconds = 1f, scoopRate = 12f, scoopCapacity = 10 },
        };

        private readonly IceJudge judge = new IceJudge();
        private readonly GuideTimer guide = new GuideTimer();
        private Vector3 scoopStartPos;
        private Quaternion scoopStartRot;
        private bool startCaptured;
        private float spawnTimer;
        private int droppedCount;

        public int CountInCup => judge.Count;
        public int DroppedCount => droppedCount;

        private LevelSettings Level => levels[Mathf.Clamp(Difficulty - 1, 0, levels.Length - 1)];

        private void Awake()
        {
            CaptureStart();
        }

        private void CaptureStart()
        {
            if (startCaptured || scoop == null) return;
            startCaptured = true;
            scoopStartPos = scoop.transform.position;
            scoopStartRot = scoop.transform.rotation;
        }

        protected override void ResetState()
        {
            CaptureStart();
            if (icePool != null) icePool.DespawnAll();
            if (scoop != null)
            {
                scoop.Hand = Hand;
                if (viewCamera != null) scoop.ViewCamera = viewCamera;
                scoop.ResetTo(scoopStartPos, scoopStartRot);
                scoop.InputEnabled = false;
            }
            spawnTimer = 0f;
            droppedCount = 0;
            WarnIfUnsupported(cupZone);
            WarnIfUnsupported(binZone);
            WarnIfUnsupported(scoopZone);

            var lv = Level;
            judge.Reset(lv.targetMin, lv.targetMax, lv.cupMax, lv.holdSeconds);

            if (hud != null) hud.Configure(lv.targetMin, lv.targetMax, lv.cupMax);
        }

        protected override void OnPrepare()
        {
            guide.Reset();
            if (hud != null) hud.ShowGuide(Ctx.Definition != null ? Ctx.Definition.instruction : PresentationRules.InstructionIce);
            // 컵 모델 상태를 이전 내용물에 맞춘다
            if (cupVisual != null)
            {
                var cup = Ctx.Cup;
                cupVisual.SetIceVisible(cup.IceCount > 0);
                cupVisual.SetFill(Mathf.Clamp01(cup.TotalLiquid / cupCapacityMl));
                cupVisual.SetColorFrom(cup);
            }
        }

        protected override void OnBegin()
        {
            if (scoop != null) scoop.InputEnabled = true;
        }

        protected override void OnTick(float remaining01)
        {
            if (scoop == null) return;
            float dt = TickDelta;
            var lv = Level;
            if (Hand.GrabHeld) guide.MarkInput();
            if (hud != null) hud.SetGuideBig(guide.Tick(dt));

            // 제빙기 안에서 누르고 있으면 스쿱에 얼음이 담긴다
            if (Hand.GrabHeld && IsInside(binZone, scoopFillPoint != null ? scoopFillPoint.position : scoop.transform.position))
            {
                spawnTimer += dt * lv.scoopRate;
                while (spawnTimer >= 1f)
                {
                    spawnTimer -= 1f;
                    if (CountInZone(scoopZone) >= lv.scoopCapacity) { spawnTimer = 0f; break; }
                    SpawnIceInScoop();
                }
            }
            else
            {
                spawnTimer = 0f;
            }

            int inCup = CountInCupAndDrops();
            var outcome = judge.Update(inCup, dt);
            if (hud != null) hud.SetState(inCup, judge.Hold01);
            Complete(outcome);
        }

        protected override void OnTimeUp()
        {
            Complete(judge.TimeUp());
        }

        protected override void WriteResultStats(ref MicrogameResult result)
        {
            result.Amount = judge.Count;
            result.Wasted = droppedCount;
        }

        protected override void OnPresentStart(in MicrogameResult result)
        {
            if (hud != null) hud.ShowResult(result);
        }

        protected override void OnEnd(MicrogameResult result)
        {
            if (scoop != null) scoop.InputEnabled = false;
        }

        protected override void ApplyResult(in MicrogameResult result)
        {
            // 컵 안에 들어간 만큼 컵 데이터에 남는다 (실패해도 컵에 든 건 든 것)
            Ctx.Cup.Add(Ingredient.Ice, judge.Count);
            if (result.Reason == FailReason.Overflow) MicrogameStats.Report(StatKeys.IceOverflow, judge.Count - judge.CupMax);
            if (cupVisual != null) cupVisual.SetIceVisible(Ctx.Cup.IceCount > 0);
        }

        private void SpawnIceInScoop()
        {
            if (icePool == null || scoopFillPoint == null) return;
            Vector2 j = UnityEngine.Random.insideUnitCircle * spawnJitter;
            Vector3 pos = scoopFillPoint.position + new Vector3(j.x, 0f, j.y);
            float size = IceSizing.SizeFactor(UnityEngine.Random.value, sizeVariation);
            IcePiece piece = icePool.Spawn(pos, UnityEngine.Random.rotationUniform, size);
            if (piece != null && scoop != null && scoop.Body != null)
                piece.Body.linearVelocity = scoop.Body.linearVelocity;
        }

        /// <summary>컵 안 얼음을 세고, 바닥에 떨어진 얼음을 한 번씩 보고한다.</summary>
        private int CountInCupAndDrops()
        {
            if (icePool == null) return 0;
            var list = icePool.Active;
            float floorY = floor != null ? floor.position.y : float.NegativeInfinity;
            int count = 0;
            for (int i = 0; i < list.Count; i++)
            {
                IcePiece piece = list[i];
                Vector3 p = piece.transform.position;
                // 스쿱에 담긴 채로 컵 영역에 들어간 얼음은 세지 않는다(기울여서 쏟아야 한다)
                if (IsInside(cupZone, p) && !IsInside(scoopZone, p)) { count++; continue; }
                if (!piece.Dropped && p.y < floorY + dropHeight)
                {
                    piece.Dropped = true;
                    droppedCount++;
                    MicrogameStats.Report(StatKeys.IceDropped, 1f);
                }
            }
            return count;
        }

        private int CountInZone(Collider zone)
        {
            if (zone == null || icePool == null) return 0;
            var list = icePool.Active;
            int count = 0;
            for (int i = 0; i < list.Count; i++)
                if (IsInside(zone, list[i].transform.position)) count++;
            return count;
        }

        /// <summary>트리거 영역 안에 점이 있는지. OnTriggerExit이 비활성화 때 안 불리는 문제를 피하려고 기하로 센다.</summary>
        private static bool IsInside(Collider zone, Vector3 point)
        {
            if (zone == null || !zone.enabled || !zone.gameObject.activeInHierarchy) return false;
            // 오목 MeshCollider는 ClosestPoint를 지원하지 않아 항상 안쪽으로 판정되므로 쓰지 않는다
            if (zone is MeshCollider mesh && !mesh.convex) return false;
            return (zone.ClosestPoint(point) - point).sqrMagnitude < 1e-6f;
        }

        private void WarnIfUnsupported(Collider zone)
        {
            if (zone is MeshCollider mesh && !mesh.convex)
                Debug.LogWarning($"[IceScoop] {zone.name}: 영역 콜라이더는 Box/Sphere/Capsule 또는 convex MeshCollider여야 합니다.", zone);
        }
    }
}
