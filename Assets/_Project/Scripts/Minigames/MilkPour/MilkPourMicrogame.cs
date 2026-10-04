using System;
using UnityEngine;

namespace BariBarista.Minigames
{
    /// <summary>
    /// "우유 부어라!" — 우유팩을 잡고 우클릭으로 기울여 컵을 목표 선까지 채운 뒤 다시 세운다.
    /// 유체 시뮬레이션 없이 "레이 판정 + 양 데이터"로 계산하고 파티클은 보이기용이다.
    /// </summary>
    public class MilkPourMicrogame : MicrogameBase
    {
        [Serializable]
        public struct LevelSettings
        {
            [Range(0f, 1f)] public float targetMin01;
            [Range(0f, 1f)] public float targetMax01;
            [Tooltip("최대로 기울였을 때 초당 붓는 양(ml)")]
            public float maxFlowMlPerSec;
            [Tooltip("컵 밖으로 부어도 되는 한도(ml)")]
            public float maxOutsideMl;
            [Tooltip("휙 기울였을 때 처음에 콸 쏟아지는 배율")]
            public float glugMultiplier;
        }

        [Header("참조")]
        [SerializeField] private Camera viewCamera;
        [SerializeField] private MouseSpringFollower carton;
        [Tooltip("우유팩 입구")]
        [SerializeField] private Transform spout;
        [Tooltip("컵 입구를 덮는 콜라이더. 입구에서 아래로 쏜 레이가 여기 맞으면 컵에 들어간 것")]
        [SerializeField] private Collider cupMouth;
        [Tooltip("바닥 높이 기준 (줄기 길이)")]
        [SerializeField] private Transform floor;
        [SerializeField] private CupVisual cupVisual;
        [SerializeField] private MilkPourHud hud;
        [Tooltip("우유 줄기 피벗. localScale.y = 길이, 위쪽이 원점")]
        [SerializeField] private Transform stream;
        [SerializeField] private ParticleSystem pourParticles;

        [Header("손맛")]
        [SerializeField] private float cupCapacityMl = 300f;
        [Tooltip("얼음 1개가 차지하는 부피(ml). 액체 높이 계산에 더한다")]
        [SerializeField] private float iceDisplacementMl = 8f;
        [Tooltip("이 각도부터 우유가 나온다")]
        [SerializeField] private float pourStartAngle = 45f;
        [Tooltip("이 각도에서 최대 속도")]
        [SerializeField] private float pourFullAngle = 110f;
        [Tooltip("붓기 시작 각도보다 이만큼 세우면 '다시 세웠다'로 본다")]
        [SerializeField] private float stopHysteresis = 8f;
        [Tooltip("이 속도(도/초) 이상으로 기울이며 붓기 시작하면 콸 구간")]
        [SerializeField] private float glugTiltSpeed = 120f;
        [SerializeField] private float glugSeconds = 0.35f;
        [SerializeField] private float streamWidth = 0.025f;

        [Header("난이도 (Lv1~3)")]
        [SerializeField] private LevelSettings[] levels =
        {
            new LevelSettings { targetMin01 = 0.60f, targetMax01 = 0.85f, maxFlowMlPerSec = 140f, maxOutsideMl = 60f, glugMultiplier = 2f },
            new LevelSettings { targetMin01 = 0.68f, targetMax01 = 0.80f, maxFlowMlPerSec = 160f, maxOutsideMl = 40f, glugMultiplier = 2.5f },
            new LevelSettings { targetMin01 = 0.72f, targetMax01 = 0.78f, maxFlowMlPerSec = 180f, maxOutsideMl = 25f, glugMultiplier = 3f },
        };

        private readonly PourJudge judge = new PourJudge();
        private readonly GuideTimer guide = new GuideTimer();
        private Vector3 cartonStartPos;
        private Quaternion cartonStartRot;
        private Vector3 streamRestScale;
        private bool startCaptured;

        private bool pouring;
        private float lastAngle;
        private float glugRemaining;

        public float Fill01 => judge.Fill01;

        private LevelSettings Level => levels[Mathf.Clamp(Difficulty - 1, 0, levels.Length - 1)];

        private void Awake() => CaptureStart();

        private void CaptureStart()
        {
            if (startCaptured) return;
            startCaptured = true;
            if (carton != null) { cartonStartPos = carton.transform.position; cartonStartRot = carton.transform.rotation; }
            if (stream != null) streamRestScale = stream.localScale;
        }

        protected override void ResetState()
        {
            CaptureStart();
            if (carton != null)
            {
                carton.Hand = Hand;
                if (viewCamera != null) carton.ViewCamera = viewCamera;
                carton.ResetTo(cartonStartPos, cartonStartRot);
                carton.InputEnabled = false;
            }
            pouring = false;
            lastAngle = 0f;
            glugRemaining = 0f;
            SetStream(false, 0f);
            if (pourParticles != null) pourParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var lv = Level;
            judge.Reset(lv.targetMin01, lv.targetMax01, cupCapacityMl, 0f, lv.maxOutsideMl);
            if (hud != null) hud.Configure(lv.targetMin01, lv.targetMax01);
        }

        protected override void OnPrepare()
        {
            guide.Reset();
            if (hud != null) hud.ShowGuide(Ctx.Definition != null ? Ctx.Definition.instruction : PresentationRules.InstructionPour);
            // 기존 내용물(에스프레소·얼음)을 액체 높이에 합산한다
            var cup = Ctx.Cup;
            var lv = Level;
            float initial = cup.TotalLiquid + cup.IceCount * iceDisplacementMl;
            judge.Reset(lv.targetMin01, lv.targetMax01, cupCapacityMl, initial, lv.maxOutsideMl);

            if (cupVisual != null) cupVisual.SetIceVisible(cup.IceCount > 0);
            UpdateCupDisplay();
        }

        protected override void OnBegin()
        {
            if (carton != null) carton.InputEnabled = true;
        }

        protected override void OnTick(float remaining01)
        {
            float dt = TickDelta;
            if (carton == null || dt <= 0f) return;
            if (Hand.GrabHeld || Hand.TiltHeld) guide.MarkInput();
            if (hud != null) hud.SetGuideBig(guide.Tick(dt));

            float angle = carton.ActualTilt;
            float tiltSpeed = (angle - lastAngle) / dt;
            lastAngle = angle;

            if (!pouring)
            {
                if (angle < pourStartAngle) return;
                pouring = true;
                // 휙 기울이면 처음에 콸 쏟아진다
                glugRemaining = tiltSpeed >= glugTiltSpeed ? glugSeconds : 0f;
            }
            else if (angle < pourStartAngle - stopHysteresis)
            {
                // 팩을 다시 세웠다 → 판정
                pouring = false;
                SetStream(false, 0f);
                if (pourParticles != null) pourParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                Complete(judge.StopPouring());
                return;
            }

            if (angle < pourStartAngle)
            {
                // 히스테리시스 구간: 아직 멈춘 건 아니지만 나오지는 않는다
                SetStream(false, 0f);
                return;
            }

            var lv = Level;
            float t = Mathf.InverseLerp(pourStartAngle, pourFullAngle, angle);
            float flow = lv.maxFlowMlPerSec * t;
            if (glugRemaining > 0f)
            {
                flow *= lv.glugMultiplier;
                glugRemaining -= dt;
            }
            float ml = flow * dt;
            if (ml <= 0f) { SetStream(false, 0f); return; }

            if (pourParticles != null && !pourParticles.isEmitting) pourParticles.Play(true);

            JudgeOutcome outcome;
            if (TryHitCup(out float hitDistance))
            {
                outcome = judge.AddToCup(ml);
                SetStream(true, hitDistance);
                UpdateCupDisplay();
            }
            else
            {
                outcome = judge.AddOutside(ml);
                if (hud != null) hud.SetSpill(judge.MaxOutsideMl > 0f ? judge.OutsideMl / judge.MaxOutsideMl : 1f);
                SetStream(true, spout != null && floor != null ? Mathf.Max(0f, spout.position.y - floor.position.y) : 1f);
            }
            Complete(outcome);
        }

        /// <summary>입구에서 아래로 레이를 쏴서 컵 입구 콜라이더에 맞는지. 다른 콜라이더는 보지 않는다.</summary>
        private bool TryHitCup(out float distance)
        {
            distance = 0f;
            if (spout == null || cupMouth == null) return false;
            var ray = new Ray(spout.position, Vector3.down);
            if (!cupMouth.Raycast(ray, out RaycastHit hit, 5f)) return false;
            distance = hit.distance;
            return true;
        }

        private void SetStream(bool on, float length)
        {
            if (stream == null) return;
            on &= length > 0f && spout != null;
            if (stream.gameObject.activeSelf != on) stream.gameObject.SetActive(on);
            if (!on) { stream.localScale = streamRestScale; return; }
            // 줄기는 항상 수직 아래로
            stream.SetPositionAndRotation(spout.position, Quaternion.identity);
            stream.localScale = new Vector3(streamWidth, length, streamWidth);
        }

        private void UpdateCupDisplay()
        {
            if (cupVisual != null)
            {
                cupVisual.SetFill(judge.Fill01);
                cupVisual.SetColorFrom(Ctx.Cup, 0f, Mathf.Max(0f, judge.PouredInMl));
            }
            if (hud != null) hud.SetFill(judge.Fill01);
        }

        protected override void OnTimeUp()
        {
            Complete(judge.TimeUp());
        }

        protected override void WriteResultStats(ref MicrogameResult result)
        {
            result.Amount = Mathf.Max(0f, judge.PouredInMl);
            result.Wasted = judge.OverflowMl + judge.OutsideMl;
        }

        protected override void OnPresentStart(in MicrogameResult result)
        {
            if (hud != null) hud.ShowResult(result);
        }

        protected override void OnEnd(MicrogameResult result)
        {
            pouring = false;
            SetStream(false, 0f);
            if (pourParticles != null) pourParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (carton != null) carton.InputEnabled = false;
        }

        protected override void ApplyResult(in MicrogameResult result)
        {
            MicrogameStats.Report(StatKeys.MilkSpilledMl, judge.OverflowMl);
            MicrogameStats.Report(StatKeys.MilkFloorMl, judge.OutsideMl);
            Ctx.Cup.Add(Ingredient.Milk, Mathf.Max(0f, judge.PouredInMl));
        }
    }
}
