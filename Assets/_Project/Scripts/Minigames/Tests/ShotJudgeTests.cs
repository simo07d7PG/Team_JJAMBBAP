using NUnit.Framework;

namespace BariBarista.Minigames.Tests
{
    public class ShotJudgeTests
    {
        private ShotJudge judge;

        [SetUp]
        public void SetUp()
        {
            judge = new ShotJudge();
            judge.Reset(25f, 40f, 60f); // Lv1
        }

        [TestCase(25f)]
        [TestCase(40f)]
        [TestCase(32.5f)]
        public void Release_InsideRange_Succeeds(float ml)
        {
            judge.AddToCup(ml);
            var o = judge.Release();
            Assert.IsTrue(o.IsDecided);
            Assert.IsTrue(o.Success);
        }

        [Test]
        public void Release_JustBelowMin_KeepsGoing()
        {
            judge.AddToCup(24.9f);
            var o = judge.Release();
            Assert.IsFalse(o.IsDecided);
            judge.AddToCup(0.2f); // 다시 눌러서 채우면
            Assert.IsTrue(judge.Release().Success);
        }

        [Test]
        public void Release_JustAboveMax_FailsTooMuch()
        {
            judge.AddToCup(40.1f);
            var o = judge.Release();
            Assert.IsTrue(o.IsDecided);
            Assert.IsFalse(o.Success);
            Assert.AreEqual(FailReason.TooMuch, o.Reason);
        }

        [Test]
        public void Overflow_FailsAndTracksSpill()
        {
            judge.AddToCup(50f);
            var o = judge.AddToCup(15f);
            Assert.AreEqual(FailReason.Overflow, o.Reason);
            Assert.AreEqual(60f, judge.Extracted, 1e-4f);
            Assert.AreEqual(5f, judge.Spilled, 1e-4f);
        }

        [Test]
        public void ResultIsLatched_LaterCallsIgnored()
        {
            judge.AddToCup(30f);
            Assert.IsTrue(judge.Release().Success);
            var o = judge.AddToCup(100f); // 성공 이후엔 넘쳐도 무시
            Assert.IsTrue(o.Success);
            Assert.AreEqual(30f, judge.Extracted, 1e-4f);
            Assert.IsTrue(judge.TimeUp().Success);
        }

        [Test]
        public void TimeUp_BelowRange_Fails()
        {
            Assert.AreEqual(FailReason.Timeout, judge.TimeUp().Reason);

            judge.Reset(25f, 40f, 60f);
            judge.AddToCup(10f);
            Assert.AreEqual(FailReason.TooLittle, judge.TimeUp().Reason);
        }

        [Test]
        public void TimeUp_InsideRange_Succeeds()
        {
            judge.AddToCup(30f);
            Assert.IsTrue(judge.TimeUp().Success);
        }

        [Test]
        public void Score_CenterIsOne_EdgeIsHalf()
        {
            judge.AddToCup(32.5f);
            Assert.AreEqual(1f, judge.Release().Score, 1e-4f);

            judge.Reset(25f, 40f, 60f);
            judge.AddToCup(25f);
            Assert.AreEqual(0.5f, judge.Release().Score, 1e-3f);
        }

        [Test]
        public void Level3Range_Boundaries()
        {
            judge.Reset(28f, 34f, 45f);
            judge.AddToCup(27.9f);
            Assert.IsFalse(judge.Release().IsDecided);
            judge.AddToCup(0.1f);
            Assert.IsTrue(judge.Release().Success);

            judge.Reset(28f, 34f, 45f);
            judge.AddToCup(34.01f);
            Assert.AreEqual(FailReason.TooMuch, judge.Release().Reason);
        }
    }
}
