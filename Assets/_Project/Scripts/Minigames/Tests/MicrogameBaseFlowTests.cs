using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace BariBarista.Minigames.Tests
{
    /// <summary>
    /// MicrogameBase 단계 전이 계약 테스트. 편집 모드에서는 비-ExecuteAlways 스크립트의 OnEnable/OnDisable이 불리지 않으므로
    /// 생명주기 훅은 직접 부른다(DebugDisable).
    /// </summary>
    public class MicrogameBaseFlowTests
    {
        private sealed class FakeGame : MicrogameBase
        {
            public int ResetCount, PrepareCount, BeginCount, EndCount, PresentStartCount, PresentEndCompleted, PresentEndCancelled;
            public readonly List<string> Log = new List<string>();
            public readonly List<float> PresentT01 = new List<float>();
            public MicrogameResult LastEnd;
            public float? DurationOverride;

            protected override void ResetState() { ResetCount++; Log.Add("Reset"); }
            protected override void OnPrepare() { PrepareCount++; Log.Add("Prepare"); }
            protected override void OnBegin() { BeginCount++; Log.Add("Begin"); }
            protected override void ApplyResult(in MicrogameResult result) { Log.Add("Apply"); }
            protected override void OnEnd(MicrogameResult result) { EndCount++; LastEnd = result; Log.Add("End:" + result.Reason); }

            protected override float GetPresentationDuration(in MicrogameResult result)
                => DurationOverride ?? base.GetPresentationDuration(result);

            protected override void OnPresentStart(in MicrogameResult result) { PresentStartCount++; Log.Add("PresentStart"); }
            protected override void OnPresentTick(float t01, float dt) { PresentT01.Add(t01); }

            protected override void OnPresentEnd(bool cancelled)
            {
                if (cancelled) PresentEndCancelled++; else PresentEndCompleted++;
                Log.Add("PresentEnd:" + (cancelled ? "cancelled" : "done"));
            }

            public void ForceResult(MicrogameResult r) => DebugComplete(r);
            public void ForceFail(FailReason reason) => DebugForceFail(reason);
            public void SimulateDisable() => DebugDisable();
        }

        private GameObject go;
        private FakeGame game;
        private int finished;
        private int presentationCompleted;
        private int presentationSkipped;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject("FakeMicrogame");
            game = go.AddComponent<FakeGame>();
            finished = 0;
            presentationCompleted = 0;
            presentationSkipped = 0;
            game.Finished += (g, r) => { finished++; game.Log.Add("Finished"); };
            game.PresentationFinished += (g, how) =>
            {
                if (how == PresentationEnd.Completed) presentationCompleted++;
                else if (how == PresentationEnd.Skipped) presentationSkipped++;
                game.Log.Add("PresentationFinished:" + how);
            };
        }

        [TearDown]
        public void TearDown()
        {
            if (go != null) Object.DestroyImmediate(go);
        }

        private static MicrogameContext Ctx(float limit = 7f, int level = 1) => new MicrogameContext(null, limit, 0, level, new CupContents());

        [Test]
        public void Prepare_ThenBegin_ResetsStateOnlyOnce()
        {
            var ctx = Ctx();
            game.Prepare(ctx);
            Assert.AreEqual(MicrogamePhase.Preparing, game.Phase);
            Assert.AreEqual(1, game.ResetCount);
            Assert.AreEqual(1, game.PrepareCount);

            game.Begin(ctx);
            Assert.AreEqual(MicrogamePhase.Playing, game.Phase);
            Assert.AreEqual(1, game.ResetCount, "같은 ctx로 Prepare한 뒤 Begin은 ResetState를 다시 부르지 않는다");
            Assert.AreEqual(1, game.PrepareCount);
            Assert.AreEqual(1, game.BeginCount);
            Assert.IsTrue(game.IsRunning);
        }

        [Test]
        public void Begin_WithoutPrepare_StillWorks()
        {
            game.Begin(Ctx());
            Assert.AreEqual(MicrogamePhase.Playing, game.Phase);
            Assert.AreEqual(1, game.ResetCount);
            Assert.AreEqual(1, game.BeginCount);
        }

        [Test]
        public void Begin_AfterPrepareWithOtherContext_PreparesAgain()
        {
            game.Prepare(Ctx());
            game.Begin(Ctx());
            Assert.AreEqual(2, game.ResetCount);
            Assert.AreEqual(1, game.EndCount, "먼저 준비하던 판은 Aborted로 정리된다");
            Assert.AreEqual(FailReason.Aborted, game.LastEnd.Reason);
            Assert.AreEqual(MicrogamePhase.Playing, game.Phase);
        }

        [Test]
        public void Prepare_SameContext_IsIgnored_OtherContext_PreparesAgain()
        {
            var ctx = Ctx();
            game.Prepare(ctx);
            game.Prepare(ctx);
            Assert.AreEqual(1, game.ResetCount);
            Assert.AreEqual(0, game.EndCount);

            game.Prepare(Ctx());
            Assert.AreEqual(2, game.ResetCount);
            Assert.AreEqual(1, game.EndCount);
            Assert.AreEqual(MicrogamePhase.Preparing, game.Phase);
        }

        [Test]
        public void Prepare_ClearsPreviousResult()
        {
            game.Begin(Ctx());
            game.ForceFail(FailReason.Overflow);
            Assert.IsTrue(game.HasResult);

            game.Prepare(Ctx());
            Assert.IsFalse(game.HasResult, "Prepare 뒤 래치는 비어 있다");
            Assert.AreEqual(MicrogamePhase.Preparing, game.Phase);
        }

        [Test]
        public void Tick_WhileIdleOrPreparing_IsIgnored()
        {
            game.Tick(100f);
            Assert.AreEqual(MicrogamePhase.Idle, game.Phase);

            game.Prepare(Ctx(1f));
            game.Tick(100f);
            Assert.AreEqual(MicrogamePhase.Preparing, game.Phase);
            Assert.IsFalse(game.HasResult);
            Assert.AreEqual(0, finished);
        }

        [Test]
        public void Fail_PresentationEndsAt0_9Seconds()
        {
            game.Begin(Ctx());
            game.ForceFail(FailReason.Overflow);
            Assert.AreEqual(MicrogamePhase.Presenting, game.Phase);
            Assert.AreEqual(1, finished);
            Assert.AreEqual(0.9f, game.PresentationDuration, 1e-5f);

            game.Tick(0.89f);
            Assert.AreEqual(0, presentationCompleted);
            Assert.AreEqual(MicrogamePhase.Presenting, game.Phase);
            Assert.AreEqual(0.01f, game.PresentationRemaining, 1e-4f);

            game.Tick(0.02f);
            Assert.AreEqual(1, presentationCompleted);
            Assert.AreEqual(MicrogamePhase.Idle, game.Phase);
            Assert.AreEqual(1, game.PresentEndCompleted);
            Assert.AreEqual(0f, game.PresentationRemaining);

            game.Tick(5f);
            Assert.AreEqual(1, presentationCompleted, "끝난 뒤 Tick은 이벤트를 다시 보내지 않는다");
        }

        [Test]
        public void Success_PresentationEndsAt1_4Seconds()
        {
            game.Begin(Ctx());
            game.ForceResult(MicrogameResult.Succeeded(1f));
            Assert.AreEqual(1.4f, game.PresentationDuration, 1e-5f);

            game.Tick(1.39f);
            Assert.AreEqual(0, presentationCompleted);
            game.Tick(0.02f);
            Assert.AreEqual(1, presentationCompleted);
            Assert.AreEqual(MicrogamePhase.Idle, game.Phase);
        }

        [Test]
        public void Timeout_FailsThenPresents()
        {
            game.Begin(Ctx(1f));
            game.Tick(1f);
            Assert.AreEqual(1, finished);
            Assert.AreEqual(FailReason.Timeout, game.LastResult.Reason);
            Assert.AreEqual(MicrogamePhase.Presenting, game.Phase);
            Assert.IsFalse(game.IsRunning);
        }

        [Test]
        public void PresentTick_ReportsRisingProgressEndingAtOne()
        {
            game.Begin(Ctx());
            game.ForceFail(FailReason.Overflow);
            for (int i = 0; i < 6; i++) game.Tick(0.2f);

            Assert.GreaterOrEqual(game.PresentT01.Count, 5);
            for (int i = 1; i < game.PresentT01.Count; i++) Assert.GreaterOrEqual(game.PresentT01[i], game.PresentT01[i - 1]);
            Assert.AreEqual(1f, game.PresentT01[game.PresentT01.Count - 1], 1e-5f);
        }

        [Test]
        public void Order_ApplyThenFinishedThenEndThenPresent()
        {
            game.Begin(Ctx());
            game.Log.Clear();
            game.ForceFail(FailReason.TooLittle);
            game.Tick(1f);

            CollectionAssert.AreEqual(
                new[] { "Apply", "Finished", "End:TooLittle", "PresentStart", "PresentEnd:done", "PresentationFinished:Completed" },
                game.Log);
        }

        [Test]
        public void ForceEnd_WhilePreparing_StopsPrepareWithoutEvents()
        {
            game.Prepare(Ctx());
            game.ForceEnd();

            Assert.AreEqual(MicrogamePhase.Idle, game.Phase);
            Assert.AreEqual(1, game.EndCount);
            Assert.AreEqual(FailReason.Aborted, game.LastEnd.Reason);
            Assert.AreEqual(0, finished);
            Assert.AreEqual(0, presentationCompleted + presentationSkipped);
        }

        [Test]
        public void ForceEnd_WhilePlaying_AbortsWithoutEvents()
        {
            game.Begin(Ctx());
            game.ForceEnd();

            Assert.AreEqual(MicrogamePhase.Idle, game.Phase);
            Assert.AreEqual(1, game.EndCount);
            Assert.AreEqual(0, finished);
            Assert.AreEqual(0, game.PresentStartCount);
            Assert.AreEqual(0, presentationCompleted + presentationSkipped);
        }

        [Test]
        public void ForceEnd_WhilePresenting_CancelsWithoutEvent()
        {
            game.Begin(Ctx());
            game.ForceFail(FailReason.Overflow);
            game.Tick(0.3f);
            game.ForceEnd();

            Assert.AreEqual(MicrogamePhase.Idle, game.Phase);
            Assert.AreEqual(1, game.PresentEndCancelled);
            Assert.AreEqual(0, game.PresentEndCompleted);
            Assert.AreEqual(0, presentationCompleted + presentationSkipped);
            Assert.AreEqual(1, game.EndCount, "OnEnd는 결과 확정 때 한 번뿐");

            game.Tick(5f);
            Assert.AreEqual(0, presentationCompleted, "취소된 연출은 더 흐르지 않는다");
        }

        [Test]
        public void Prepare_WhilePresenting_CancelsPresentationAndPrepares()
        {
            game.Begin(Ctx());
            game.ForceFail(FailReason.Overflow);
            game.Tick(0.3f);
            game.Prepare(Ctx());

            Assert.AreEqual(MicrogamePhase.Preparing, game.Phase);
            Assert.AreEqual(1, game.PresentEndCancelled);
            Assert.AreEqual(0, presentationCompleted + presentationSkipped);
            Assert.AreEqual(2, game.ResetCount);
        }

        [Test]
        public void Begin_WhilePresenting_CancelsPresentationAndStarts()
        {
            game.Begin(Ctx());
            game.ForceResult(MicrogameResult.Succeeded(1f));
            game.Begin(Ctx());

            Assert.AreEqual(MicrogamePhase.Playing, game.Phase);
            Assert.AreEqual(1, game.PresentEndCancelled);
            Assert.AreEqual(0, presentationCompleted + presentationSkipped);
        }

        [Test]
        public void RootDisabledInsideFinished_EndsOnceAndSkippedOnce()
        {
            game.Finished += (g, r) =>
            {
                go.SetActive(false);
                game.SimulateDisable(); // 편집 모드에서는 OnDisable이 안 불리므로 직접 부른다
            };
            game.Begin(Ctx());
            game.ForceFail(FailReason.Overflow);

            Assert.AreEqual(1, game.EndCount, "Finished 안의 OnDisable→ForceEnd는 아무 일도 하지 않는다");
            Assert.AreEqual(FailReason.Overflow, game.LastEnd.Reason);
            Assert.AreEqual(1, presentationSkipped);
            Assert.AreEqual(0, presentationCompleted);
            Assert.AreEqual(0, game.PresentStartCount);
            Assert.AreEqual(MicrogamePhase.Idle, game.Phase);

            game.Tick(5f);
            Assert.AreEqual(1, presentationSkipped);
            Assert.AreEqual(1, game.EndCount);
        }

        [Test]
        public void DisableInsideFinished_WithRootStillActive_StillPresents()
        {
            game.Finished += (g, r) => game.SimulateDisable();
            game.Begin(Ctx());
            game.ForceFail(FailReason.Overflow);

            Assert.AreEqual(1, game.EndCount);
            Assert.AreEqual(MicrogamePhase.Presenting, game.Phase);
            Assert.AreEqual(0, presentationSkipped);
        }

        [Test]
        public void BeginInsideFinished_OldRunStaysSilent_NewRunContinues()
        {
            bool restarted = false;
            game.Finished += (g, r) =>
            {
                if (restarted) return;
                restarted = true;
                game.Begin(Ctx());
            };
            game.Begin(Ctx());
            game.ForceFail(FailReason.Overflow);

            Assert.AreEqual(MicrogamePhase.Playing, game.Phase, "새 판이 유지된다");
            Assert.AreEqual(0, game.EndCount, "옛 판의 OnEnd는 새 판을 건드리지 않는다");
            Assert.AreEqual(0, game.PresentStartCount);
            Assert.AreEqual(0, presentationCompleted + presentationSkipped);
            Assert.IsFalse(game.HasResult);
        }

        [Test]
        public void PrepareInsideFinished_OldRunStaysSilent()
        {
            game.Finished += (g, r) => game.Prepare(Ctx());
            game.Begin(Ctx());
            game.ForceFail(FailReason.Overflow);

            Assert.AreEqual(MicrogamePhase.Preparing, game.Phase);
            Assert.AreEqual(0, game.EndCount);
            Assert.AreEqual(0, game.PresentStartCount);
            Assert.AreEqual(0, presentationCompleted + presentationSkipped);
            Assert.IsFalse(game.HasResult, "Prepare가 래치를 비웠다");
        }

        [Test]
        public void PresentationDuration_CanBeOverridden()
        {
            game.DurationOverride = 0.3f;
            game.Begin(Ctx());
            game.ForceResult(MicrogameResult.Succeeded(1f));
            Assert.AreEqual(0.3f, game.PresentationDuration, 1e-5f);

            game.Tick(0.29f);
            Assert.AreEqual(0, presentationCompleted);
            game.Tick(0.02f);
            Assert.AreEqual(1, presentationCompleted);
        }

        [Test]
        public void PresentationDuration_Zero_FinishesImmediately()
        {
            game.DurationOverride = 0f;
            game.Begin(Ctx());
            game.ForceFail(FailReason.Overflow);

            Assert.AreEqual(1, game.PresentStartCount);
            Assert.AreEqual(1, presentationCompleted);
            Assert.AreEqual(MicrogamePhase.Idle, game.Phase);
        }

        [Test]
        public void DuplicateResults_AreIgnored()
        {
            game.Begin(Ctx());
            game.ForceFail(FailReason.Overflow);
            game.ForceFail(FailReason.Timeout);
            game.ForceResult(MicrogameResult.Succeeded(1f));

            Assert.AreEqual(1, finished);
            Assert.AreEqual(FailReason.Overflow, game.LastResult.Reason);
        }
    }
}
