using System;
using UnityEngine;

namespace BariBarista.Minigames
{
    /// <summary>
    /// "샷 내려라!" — 누르고 있는 동안 추출, 목표 구간에서 떼면 성공.
    /// Lv3은 컵이 받침 밖에 비뚤게 놓여 있어서 먼저 끌어다 놓아야 한다.
    /// </summary>
    public class EspressoShotMicrogame : MicrogameBase
    {
        [Serializable]
        public struct LevelSettings
        {
            public float targetMinMl;
            public float targetMaxMl;
            [Tooltip("컵 용량. 넘으면 넘침 실패")]
            public float capacityMl;
            [Tooltip("평균 추출 속도(ml/초)")]
            public float flowMlPerSec;
            [Tooltip("추출 속도 출렁임 비율(0~1)")]
            public float flowNoise;
            [Tooltip("출렁임 빠르기")]
            public float noiseFrequency;
            [Tooltip("누른 뒤 안 나오는 '뜸' 최소/최대(초). 0이면 없음")]
            public float delayMin;
            public float delayMax;
            [Tooltip("뜸 직후 쏟아지는 배율과 지속 시간")]
            public float burstMultiplier;
            public float burstSeconds;
            [Tooltip("컵이 받침 밖에 놓여 시작")]
            public bool misplacedCup;
        }

        [Header("참조")]
        [SerializeField] private Camera viewCamera;
        [Tooltip("추출구 위치")]
        [SerializeField] private Transform spout;
        [Tooltip("컵을 놓는 받침 위치 (추출구 바로 아래)")]
        [SerializeField] private Transform saucer;
        [Tooltip("컵 루트 (원점이 컵 바닥)")]
        [SerializeField] private Transform cup;
        [Tooltip("컵을 클릭해서 끌 때 쓰는 콜라이더")]
        [SerializeField] private Collider cupCollider;
        [Tooltip("Lv3 컵 끌기용. 컵 루트에 붙은 MouseSpringFollower")]
        [SerializeField] private MouseSpringFollower cupFollower;
        [Tooltip("받침 아래 조리대 높이 기준 (컵 없이 추출할 때 줄기 길이)")]
        [SerializeField] private Transform counter;
        [SerializeField] private CupVisual cupVisual;
        [SerializeField] private VerticalGauge gauge;
        [Tooltip("추출 줄기 피벗. 추출구에 두고 localScale.y = 길이")]
        [SerializeField] private Transform stream;
        [SerializeField] private ParticleSystem streamParticles;
        [Tooltip("추출 버튼 (눌리면 살짝 들어간다)")]
        [SerializeField] private Transform extractButton;

        [Header("손맛")]
        [SerializeField] private float buttonPressDepth = 0.02f;
        [Tooltip("Lv3 시작 시 받침에서 벗어난 위치")]
        [SerializeField] private Vector3 misplacedOffset = new Vector3(0.45f, 0f, -0.15f);
        [SerializeField] private float misplacedYaw = 35f;
        [Tooltip("받침에서 이 거리 안에 놓으면 제자리에 붙는다")]
        [SerializeField] private float snapRadius = 0.12f;
        [Tooltip("줄기 굵기 배율 (추출 속도에 비례)")]
        [SerializeField] private float streamWidthPerFlow = 0.02f;

        [Header("난이도 (Lv1~3)")]
        [SerializeField] private LevelSettings[] levels =
        {
            new LevelSettings { targetMinMl = 25f, targetMaxMl = 40f, capacityMl = 60f, flowMlPerSec = 12f, flowNoise = 0.25f, noiseFrequency = 2f, delayMin = 0f, delayMax = 0f, burstMultiplier = 1f, burstSeconds = 0f, misplacedCup = false },
            new LevelSettings { targetMinMl = 27f, targetMaxMl = 37f, capacityMl = 50f, flowMlPerSec = 14f, flowNoise = 0.35f, noiseFrequency = 2.5f, delayMin = 0.3f, delayMax = 0.6f, burstMultiplier = 2.5f, burstSeconds = 0.3f, misplacedCup = false },
            new LevelSettings { targetMinMl = 28f, targetMaxMl = 34f, capacityMl = 45f, flowMlPerSec = 16f, flowNoise = 0.4f, noiseFrequency = 3f, delayMin = 0.3f, delayMax = 0.6f, burstMultiplier = 3f, burstSeconds = 0.3f, misplacedCup = true },
        };

        private readonly ShotJudge judge = new ShotJudge();
        private Vector3 cupRestPos;
        private Quaternion cupRestRot;
        private Vector3 buttonRestLocal;
        private Vector3 streamRestScale;
        private bool startCaptured;

        private bool extracting;
        private bool dragging;
        private bool cupPlaced;
        private float delayRemaining;
        private float burstRemaining;
        private float noiseTime;
        private float noiseSeed;
        private float floorMl;
        private float currentFlow;
        // 누름은 직접 감지한다: Tick이 한 프레임 건너뛰어도 누름을 놓치지 않게
        private bool wasHeld;

        public float Extracted => judge.Extracted;
        public bool CupPlaced => cupPlaced;

        private LevelSettings Level => levels[Mathf.Clamp(Difficulty - 1, 0, levels.Length - 1)];

        private void Awake() => CaptureStart();

        private void CaptureStart()
        {
            if (startCaptured) return;
            startCaptured = true;
            if (cup != null) { cupRestPos = cup.position; cupRestRot = cup.rotation; }
            if (extractButton != null) buttonRestLocal = extractButton.localPosition;
            if (stream != null) streamRestScale = stream.localScale;
        }

        protected override void ResetState()
        {
            CaptureStart();
            var lv = Level;

            extracting = false;
            dragging = false;
            delayRemaining = 0f;
            burstRemaining = 0f;
            noiseTime = 0f;
            noiseSeed = UnityEngine.Random.value * 100f;
            floorMl = 0f;
            currentFlow = 0f;
            judge.Reset(lv.targetMinMl, lv.targetMaxMl, lv.capacityMl);

            PlaceCup(lv.misplacedCup);
            SetStream(0f, 0f);
            if (streamParticles != null) streamParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (extractButton != null) extractButton.localPosition = buttonRestLocal;

            if (gauge != null)
            {
                gauge.SetRange(lv.targetMinMl / lv.capacityMl, lv.targetMaxMl / lv.capacityMl);
                gauge.SetValue(0f);
            }
        }

        private void PlaceCup(bool misplaced)
        {
            if (cup == null) return;
            Vector3 snapPos = SnapPosition();
            Vector3 pos = misplaced ? snapPos + misplacedOffset : snapPos;
            Quaternion rot = misplaced ? Quaternion.AngleAxis(misplacedYaw, Vector3.up) * cupRestRot : cupRestRot;

            if (cupFollower != null)
            {
                cupFollower.Hand = Hand;
                if (viewCamera != null) cupFollower.ViewCamera = viewCamera;
                cupFollower.Freeze(true);
                cupFollower.ResetTo(pos, rot);
                cupFollower.InputEnabled = false;
            }
            else
            {
                cup.SetPositionAndRotation(pos, rot);
            }
            cupPlaced = !misplaced;
        }

        private Vector3 SnapPosition()
        {
            if (saucer == null) return cupRestPos;
            Vector3 s = saucer.position;
            return new Vector3(s.x, cupRestPos.y, s.z);
        }

        protected override void OnBegin()
        {
            UpdateCupVisual();
            if (cupVisual != null) cupVisual.SetIceVisible(Ctx.Cup.IceCount > 0);
            // 메인 씬에서 기계를 클릭한 채로 들어와도 바로 추출되지 않게 현재 상태에서 시작
            wasHeld = Hand.GrabHeld;
        }

        protected override void OnTick(float remaining01)
        {
            float dt = TickDelta;
            bool held = Hand.GrabHeld;
            bool pressed = held && !wasHeld;
            wasHeld = held;

            // Lv3: 받침 밖의 컵을 클릭하면 끌기 시작
            if (!cupPlaced && !dragging && !extracting && pressed && PointerOverCup())
            {
                dragging = true;
                if (cupFollower != null)
                {
                    cupFollower.Freeze(false);
                    cupFollower.InputEnabled = true;
                    cupFollower.SetGrabbed(true);
                }
            }

            if (dragging)
            {
                if (!held)
                {
                    dragging = false;
                    if (cupFollower != null)
                    {
                        cupFollower.SetGrabbed(false);
                        cupFollower.InputEnabled = false;
                    }
                    TrySnapCup();
                }
                return;
            }

            if (!extracting && pressed) StartExtraction();

            if (extracting)
            {
                if (held)
                {
                    Extract(dt);
                }
                else
                {
                    StopExtraction();
                    if (cupPlaced && judge.Extracted > 0f) Complete(judge.Release());
                }
            }
        }

        private void StartExtraction()
        {
            var lv = Level;
            extracting = true;
            delayRemaining = lv.delayMax > 0f ? UnityEngine.Random.Range(lv.delayMin, lv.delayMax) : 0f;
            burstRemaining = 0f;
            if (extractButton != null) extractButton.localPosition = buttonRestLocal + Vector3.down * buttonPressDepth;
        }

        private void StopExtraction()
        {
            extracting = false;
            currentFlow = 0f;
            SetStream(0f, 0f);
            if (streamParticles != null) streamParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (extractButton != null) extractButton.localPosition = buttonRestLocal;
        }

        private void Extract(float dt)
        {
            var lv = Level;
            noiseTime += dt;

            // 뜸: 한동안 안 나오다가 갑자기 쏟아진다
            if (delayRemaining > 0f)
            {
                delayRemaining -= dt;
                if (delayRemaining <= 0f) burstRemaining = lv.burstSeconds;
                currentFlow = 0f;
                SetStream(0f, 0f);
                return;
            }

            float noise = Mathf.PerlinNoise(noiseSeed, noiseTime * lv.noiseFrequency) * 2f - 1f;
            float flow = lv.flowMlPerSec * Mathf.Max(0f, 1f + lv.flowNoise * noise);
            if (burstRemaining > 0f)
            {
                flow *= lv.burstMultiplier;
                burstRemaining -= dt;
            }
            currentFlow = flow;
            float ml = flow * dt;

            if (streamParticles != null && !streamParticles.isEmitting) streamParticles.Play(true);

            if (cupPlaced)
            {
                SetStream(flow, StreamLengthTo(cup.position.y));
                var outcome = judge.AddToCup(ml);
                UpdateCupVisual();
                if (gauge != null) gauge.SetValue(judge.Extracted / judge.Capacity);
                Complete(outcome);
            }
            else
            {
                // 컵 없이 추출하면 바닥으로 쏟아진다 → 대참사 통계
                floorMl += ml;
                SetStream(flow, StreamLengthTo(counter != null ? counter.position.y : cupRestPos.y));
            }
        }

        private float StreamLengthTo(float y)
        {
            return spout != null ? Mathf.Max(0f, spout.position.y - y) : 0f;
        }

        private void SetStream(float flow, float length)
        {
            if (stream == null) return;
            bool on = flow > 0f && length > 0f;
            if (stream.gameObject.activeSelf != on) stream.gameObject.SetActive(on);
            if (!on) { stream.localScale = streamRestScale; return; }
            float w = Mathf.Max(0.004f, flow * streamWidthPerFlow * 0.1f);
            stream.localScale = new Vector3(w, length, w);
        }

        private bool PointerOverCup()
        {
            if (cupCollider == null || viewCamera == null) return false;
            Ray ray = viewCamera.ScreenPointToRay(Hand.PointerPosition);
            return cupCollider.Raycast(ray, out _, 100f);
        }

        private void TrySnapCup()
        {
            if (cup == null) return;
            Vector3 snap = SnapPosition();
            Vector3 d = cup.position - snap;
            d.y = 0f;
            if (d.sqrMagnitude > snapRadius * snapRadius) return;

            cupPlaced = true;
            if (cupFollower != null)
            {
                cupFollower.Freeze(true);
                cupFollower.ResetTo(snap, cupRestRot);
            }
            else
            {
                cup.SetPositionAndRotation(snap, cupRestRot);
            }
        }

        private void UpdateCupVisual()
        {
            if (cupVisual == null || Ctx == null) return;
            float existing = Ctx.Cup.TotalLiquid;
            float cap = existing + judge.Capacity;
            cupVisual.SetFill(cap > 0f ? (existing + judge.Extracted) / cap : 0f);
            cupVisual.SetColorFrom(Ctx.Cup, judge.Extracted);
        }

        protected override void OnTimeUp()
        {
            Complete(judge.TimeUp());
        }

        protected override void WriteResultStats(ref MicrogameResult result)
        {
            result.Amount = judge.Extracted;
            result.Wasted = judge.Spilled + floorMl;
        }

        protected override void OnEnd(MicrogameResult result)
        {
            StopExtraction();
            dragging = false;
            if (cupFollower != null)
            {
                cupFollower.InputEnabled = false;
                cupFollower.Freeze(true);
            }
        }

        protected override void ApplyResult(in MicrogameResult result)
        {
            MicrogameStats.Report(StatKeys.EspressoSpilledMl, judge.Spilled);
            MicrogameStats.Report(StatKeys.EspressoFloorMl, floorMl);
            Ctx.Cup.Add(Ingredient.Espresso, judge.Extracted);
        }
    }
}
