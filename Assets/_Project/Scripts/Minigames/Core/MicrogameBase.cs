using System;
using UnityEngine;

namespace BariBarista.Minigames
{
    /// <summary>
    /// 모든 미니게임의 부모. 코어 루프(또는 StandaloneMicrogameRunner)가 Prepare → Begin → Tick... → (Finished) → Tick... → (PresentationFinished) 순서로 돌린다.
    /// 시간은 Tick으로만 흐른다. 미니게임은 Time.deltaTime으로 제한 시간이나 연출 시간을 세지 않는다.
    /// 모든 경로는 IsRunning이 아니라 Phase로 분기한다(단계 전이 표는 Docs/Minigames_README.md).
    /// </summary>
    public abstract class MicrogameBase : MonoBehaviour
    {
        [Tooltip("MicrogameDefinition.id와 매칭")]
        public string Id;

        /// <summary>결과가 확정되면 한 번 호출된다. 이후 OnEnd가 불린다.</summary>
        public event Action<MicrogameBase, MicrogameResult> Finished;

        /// <summary>
        /// 결과 연출이 끝나면 한 번 호출된다(Completed). Finished 처리 중 루트가 꺼져 연출을 못 한 경우에도 Skipped로 보낸다.
        /// ForceEnd·Prepare·Begin으로 호출한 쪽이 직접 취소한 경우에는 보내지 않는다.
        /// </summary>
        public event Action<MicrogameBase, PresentationEnd> PresentationFinished;

        [Tooltip("IHandInput을 구현한 컴포넌트. 비우면 마우스")]
        [SerializeField] private MonoBehaviour handInputSource;
        private IHandInput hand;

        private readonly MicrogameClock clock = new MicrogameClock();
        private readonly ResultLatch latch = new ResultLatch();
        private int runId;
        private float presentationDuration;
        private float presentationElapsed;

        public MicrogamePhase Phase { get; private set; }
        /// <summary>Playing 단계인지. 결과가 확정되는 순간(Resolving)부터 false다.</summary>
        public bool IsRunning => Phase == MicrogamePhase.Playing;
        public float TimeRemaining => clock.Remaining;
        public float Remaining01 => clock.Remaining01;
        public bool HasResult => latch.IsSet;
        public MicrogameResult LastResult => latch.Result;
        /// <summary>이번 결과 연출의 전체 시간(초). 연출이 시작된 뒤에 유효하다.</summary>
        public float PresentationDuration => presentationDuration;
        /// <summary>연출이 남은 시간(초). Presenting이 아니면 0.</summary>
        public float PresentationRemaining => Phase == MicrogamePhase.Presenting ? Mathf.Max(0f, presentationDuration - presentationElapsed) : 0f;

        /// <summary>손 입력. 나중에 물리 손으로 바꿀 때 여기에 넣는다(다음 Prepare/Begin부터 적용).</summary>
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

        /// <summary>
        /// 지시어 단계. 켜기 → ResetState() → OnPrepare(). 시간과 입력은 아직 꺼져 있고 Begin을 기다린다.
        /// 이미 같은 ctx로 준비 중이면 무시하고, 그 밖의 단계에서는 정리하고 다시 준비한다.
        /// </summary>
        public void Prepare(MicrogameContext ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            if (Phase == MicrogamePhase.Preparing && ReferenceEquals(Ctx, ctx)) return;
            CancelCurrent();
            StartPrepare(ctx);
        }

        /// <summary>
        /// Playing 시작. 같은 ctx로 Prepare한 상태면 ResetState를 다시 부르지 않고 OnBegin만 한다.
        /// 그 밖의 단계에서는 정리하고 Prepare부터 거쳐 시작한다.
        /// </summary>
        public void Begin(MicrogameContext ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            if (!(Phase == MicrogamePhase.Preparing && ReferenceEquals(Ctx, ctx)))
            {
                CancelCurrent();
                StartPrepare(ctx);
            }

            clock.Start(ctx.TimeLimit);
            TickDelta = 0f;
            Phase = MicrogamePhase.Playing;
            OnBegin();
        }

        /// <summary>Playing이면 남은 시간을 줄이고(0이 되면 시간 초과 처리), Presenting이면 연출 시계를 흘린다.</summary>
        public void Tick(float deltaTime)
        {
            switch (Phase)
            {
                case MicrogamePhase.Playing:
                    TickPlaying(deltaTime);
                    break;
                case MicrogamePhase.Presenting:
                    TickPresenting(deltaTime);
                    break;
            }
        }

        /// <summary>게임오버·재시작 시 즉시 정리. Finished와 PresentationFinished는 호출하지 않는다.</summary>
        public void ForceEnd()
        {
            switch (Phase)
            {
                case MicrogamePhase.Preparing:
                case MicrogamePhase.Playing:
                    var result = MicrogameResult.Failed(FailReason.Aborted);
                    result.Elapsed = Phase == MicrogamePhase.Playing ? clock.Elapsed : 0f;
                    Phase = MicrogamePhase.Idle;
                    latch.TrySet(result);
                    OnEnd(result);
                    break;
                case MicrogamePhase.Presenting:
                    Phase = MicrogamePhase.Idle;
                    OnPresentEnd(true);
                    break;
                // Idle, Resolving: 할 일 없음(Resolving 중 들어온 호출은 결과 확정이 끝까지 처리한다)
            }
        }

        private void StartPrepare(MicrogameContext ctx)
        {
            Ctx = ctx;
            runId++;
            latch.Reset();
            clock.Start(ctx.TimeLimit);
            TickDelta = 0f;
            presentationDuration = 0f;
            presentationElapsed = 0f;

            if (!gameObject.activeSelf) gameObject.SetActive(true);
            Phase = MicrogamePhase.Preparing;
            ResetState();
            OnPrepare();
        }

        /// <summary>Preparing·Playing은 ForceEnd, Presenting은 연출 취소. 다시 시작하기 전에 부른다.</summary>
        private void CancelCurrent()
        {
            if (Phase == MicrogamePhase.Preparing || Phase == MicrogamePhase.Playing || Phase == MicrogamePhase.Presenting)
                ForceEnd();
        }

        private void TickPlaying(float deltaTime)
        {
            int run = runId;
            TickDelta = deltaTime;
            bool timeUp = clock.Advance(deltaTime);
            OnTick(clock.Remaining01);
            if (run != runId || Phase != MicrogamePhase.Playing || !timeUp) return;

            OnTimeUp();
            // 재정의한 OnTimeUp이 결과를 내지 않았으면 시간 초과 실패로 마무리
            if (run == runId && Phase == MicrogamePhase.Playing) Fail(FailReason.Timeout);
        }

        private void TickPresenting(float deltaTime)
        {
            int run = runId;
            presentationElapsed += deltaTime;
            float t01 = presentationDuration > 0f ? Mathf.Clamp01(presentationElapsed / presentationDuration) : 1f;
            OnPresentTick(t01, deltaTime);
            // 훅 안에서 취소되거나 다시 시작됐으면 이 연출은 더 이어 가지 않는다
            if (run != runId || Phase != MicrogamePhase.Presenting) return;
            if (presentationElapsed >= presentationDuration) FinishPresentation(run);
        }

        private void FinishPresentation(int run)
        {
            Phase = MicrogamePhase.Idle;
            OnPresentEnd(false);
            if (run != runId) return;
            PresentationFinished?.Invoke(this, PresentationEnd.Completed);
        }

        // ───────── 미니게임이 구현 ─────────

        /// <summary>재사용 대비: 위치, 속도, 액체 양, 게이지를 전부 처음으로.</summary>
        protected abstract void ResetState();
        protected abstract void OnBegin();
        protected virtual void OnTick(float remaining01) { }

        /// <summary>지시어 단계 진입(ResetState 직후). 컵 모델 상태 맞추기, 안내 표시 등. 입력과 시간은 켜지 않는다.</summary>
        protected virtual void OnPrepare() { }

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

        /// <summary>결과 연출 시간(초). 기본은 ResultPresentation 규칙(실패 0.9, 성공 1.4). 0 이하면 연출 없이 바로 끝난다.</summary>
        protected virtual float GetPresentationDuration(in MicrogameResult result) => ResultPresentation.DurationFor(result);

        /// <summary>결과 연출 시작. 입력은 이미 OnEnd에서 꺼진 뒤다.</summary>
        protected virtual void OnPresentStart(in MicrogameResult result) { }

        /// <summary>결과 연출 진행. t01은 0 → 1, dt는 이번 Tick에 흐른 시간. 보이는 피벗을 이 값으로 돌린다.</summary>
        protected virtual void OnPresentTick(float t01, float dt) { }

        /// <summary>결과 연출 끝. cancelled가 true면 끝나기 전에 취소된 것. 연출 오브젝트를 여기서 정리한다.</summary>
        protected virtual void OnPresentEnd(bool cancelled) { }

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
            if (Phase != MicrogamePhase.Playing || latch.IsSet) return;
            result.Elapsed = clock.Elapsed;
            WriteResultStats(ref result);
            latch.TrySet(result);
            // 반영·알림 중 OnDisable→ForceEnd가 와도 아무 일도 하지 않도록 Playing을 먼저 벗어난다
            Phase = MicrogamePhase.Resolving;
            // 반영 중 예외가 나도 Finished는 반드시 보내서 코어 루프가 멈추지 않게
            try { ApplyResult(result); }
            catch (Exception e) { Debug.LogException(e, this); }

            int run = runId;
            Finished?.Invoke(this, result);
            // Finished 처리 중에 다시 Prepare/Begin됐으면 옛 판은 새 판을 건드리지 않는다
            if (run != runId) return;
            OnEnd(result);
            if (run != runId) return;

            if (gameObject.activeInHierarchy && enabled)
            {
                StartPresentation(result, run);
            }
            else
            {
                // 호출한 쪽이 루트를 꺼 버려 연출을 할 수 없다. 기다리는 쪽이 멈추지 않게 알린다
                Phase = MicrogamePhase.Idle;
                PresentationFinished?.Invoke(this, PresentationEnd.Skipped);
            }
        }

        private void StartPresentation(in MicrogameResult result, int run)
        {
            presentationDuration = Mathf.Max(0f, GetPresentationDuration(result));
            presentationElapsed = 0f;
            Phase = MicrogamePhase.Presenting;
            OnPresentStart(result);
            if (run != runId || Phase != MicrogamePhase.Presenting) return;
            if (presentationDuration <= 0f) FinishPresentation(run);
        }

        protected virtual void OnDisable()
        {
            // 호출한 쪽이 오브젝트를 꺼 버려도 상태가 남지 않게
            ForceEnd();
        }

        // ───────── 테스트·검증 도구용 ─────────

        /// <summary>결과를 강제로 확정한다(Playing일 때만).</summary>
        internal void DebugComplete(MicrogameResult result) => Complete(result);

        /// <summary>실패 결과를 강제로 확정한다(Playing일 때만).</summary>
        internal void DebugForceFail(FailReason reason) => Fail(reason);

        /// <summary>OnDisable과 같은 처리를 부른다. 편집 모드에서는 OnDisable이 안 불리므로 테스트가 대신 부른다.</summary>
        internal void DebugDisable() => OnDisable();
    }
}
