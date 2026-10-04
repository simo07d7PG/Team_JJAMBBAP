using System;
using UnityEngine;

namespace BariBarista.Minigames
{
    /// <summary>
    /// 모든 미니게임의 부모. 코어 루프(또는 StandaloneMicrogameRunner)가 Begin → Tick... → (Finished) 순서로 돌린다.
    /// 시간은 Tick으로만 흐른다. 미니게임은 Time.deltaTime으로 제한 시간을 세지 않는다.
    /// </summary>
    public abstract class MicrogameBase : MonoBehaviour
    {
        [Tooltip("MicrogameDefinition.id와 매칭")]
        public string Id;

        /// <summary>결과가 확정되면 한 번 호출된다. 이후 OnEnd가 불린다.</summary>
        public event Action<MicrogameBase, MicrogameResult> Finished;

        [Tooltip("IHandInput을 구현한 컴포넌트. 비우면 마우스")]
        [SerializeField] private MonoBehaviour handInputSource;
        private IHandInput hand;

        private readonly MicrogameClock clock = new MicrogameClock();
        private readonly ResultLatch latch = new ResultLatch();
        private int runId;

        public bool IsRunning { get; private set; }
        public float TimeRemaining => clock.Remaining;
        public float Remaining01 => clock.Remaining01;
        public bool HasResult => latch.IsSet;
        public MicrogameResult LastResult => latch.Result;

        /// <summary>손 입력. 나중에 물리 손으로 바꿀 때 여기에 넣는다(다음 Begin부터 적용).</summary>
        public IHandInput Hand
        {
            get => hand ??= HandInputResolver.Resolve(handInputSource);
            set => hand = value;
        }

        protected MicrogameContext Ctx { get; private set; }
        protected int Difficulty => Ctx != null ? Ctx.Difficulty : 1;
        /// <summary>이번 Tick에 흐른 시간. 게임 로직(추출량, 유지 시간 등)은 이 값으로 계산한다.</summary>
        protected float TickDelta { get; private set; }
        protected float Elapsed => clock.Elapsed;

        // ───────── 호출하는 쪽 ─────────

        /// <summary>켜기 → ResetState() → OnBegin().</summary>
        public void Begin(MicrogameContext ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            if (IsRunning) ForceEnd();

            Ctx = ctx;
            runId++;
            latch.Reset();
            clock.Start(ctx.TimeLimit);
            TickDelta = 0f;

            if (!gameObject.activeSelf) gameObject.SetActive(true);
            IsRunning = true;
            ResetState();
            OnBegin();
        }

        /// <summary>남은 시간을 줄이고, 0이 되면 시간 초과 처리.</summary>
        public void Tick(float deltaTime)
        {
            if (!IsRunning) return;
            TickDelta = deltaTime;
            bool timeUp = clock.Advance(deltaTime);
            OnTick(clock.Remaining01);
            if (!IsRunning || !timeUp) return;

            OnTimeUp();
            // 재정의한 OnTimeUp이 결과를 내지 않았으면 시간 초과 실패로 마무리
            if (IsRunning) Fail(FailReason.Timeout);
        }

        /// <summary>게임오버·재시작 시 즉시 정리. Finished는 호출하지 않는다.</summary>
        public void ForceEnd()
        {
            if (!IsRunning) return;
            IsRunning = false;
            var result = MicrogameResult.Failed(FailReason.Aborted);
            result.Elapsed = clock.Elapsed;
            latch.TrySet(result);
            OnEnd(result);
        }

        // ───────── 미니게임이 구현 ─────────

        /// <summary>재사용 대비: 위치, 속도, 액체 양, 게이지를 전부 처음으로.</summary>
        protected abstract void ResetState();
        protected abstract void OnBegin();
        protected virtual void OnTick(float remaining01) { }

        /// <summary>시간 초과. 버티기형이면 재정의하거나 정의의 successOnTimeout을 켠다.</summary>
        protected virtual void OnTimeUp()
        {
            if (Ctx != null && Ctx.Definition != null && Ctx.Definition.successOnTimeout) Succeed();
            else Fail(FailReason.Timeout);
        }

        /// <summary>
        /// 결과를 컵(Ctx.Cup)과 통계에 반영한다. Finished보다 먼저 불리므로 구독자는 항상 최종 컵을 본다.
        /// ForceEnd(Aborted)일 때는 불리지 않는다.
        /// </summary>
        protected virtual void ApplyResult(in MicrogameResult result) { }

        /// <summary>입력 해제, 오브젝트 반납. result.Reason이 Aborted면 ForceEnd로 끝난 것. 컵 반영은 ApplyResult에서.</summary>
        protected virtual void OnEnd(MicrogameResult result) { }

        /// <summary>결과에 미니게임별 통계(Amount, Wasted)를 채운다.</summary>
        protected virtual void WriteResultStats(ref MicrogameResult result) { }

        // ───────── 미니게임이 호출 ─────────

        protected void Succeed(float score = 1f) => Complete(MicrogameResult.Succeeded(score));

        protected void Fail(FailReason reason) => Complete(MicrogameResult.Failed(reason));

        /// <summary>판정 클래스의 결과를 그대로 확정한다. 아직 미정이면 무시.</summary>
        protected void Complete(JudgeOutcome outcome)
        {
            if (!outcome.IsDecided) return;
            if (outcome.Success) Succeed(outcome.Score);
            else Fail(outcome.Reason);
        }

        private void Complete(MicrogameResult result)
        {
            if (!IsRunning || latch.IsSet) return;
            result.Elapsed = clock.Elapsed;
            WriteResultStats(ref result);
            latch.TrySet(result);
            IsRunning = false;
            // 반영 중 예외가 나도 Finished는 반드시 보내서 코어 루프가 멈추지 않게
            try { ApplyResult(result); }
            catch (Exception e) { Debug.LogException(e, this); }

            int run = runId;
            Finished?.Invoke(this, result);
            // Finished 처리 중에 다시 Begin됐으면 새 판을 정리하지 않도록 건너뛴다
            if (run == runId) OnEnd(result);
        }

        protected virtual void OnDisable()
        {
            // 호출한 쪽이 오브젝트를 꺼 버려도 상태가 남지 않게
            ForceEnd();
        }
    }
}
