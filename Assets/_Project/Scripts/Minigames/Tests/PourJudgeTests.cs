using NUnit.Framework;

namespace BariBarista.Minigames.Tests
{
    public class PourJudgeTests
    {
        private PourJudge judge;

        [SetUp]
        public void SetUp()
        {
            judge = new PourJudge();
            judge.Reset(0.6f, 0.85f, 300f, 0f, 60f); // Lv1
        }

        [TestCase(180f)] // 60%
        [TestCase(255f)] // 85%
        [TestCase(216f)]
        public void StopInsideRange_Succeeds(float ml)
        {
            judge.AddToCup(ml);
            var o = judge.StopPouring();
            Assert.IsTrue(o.Success);
        }

        [Test]
        public void StopBelowRange_KeepsGoing()
        {
            judge.AddToCup(179f);
            Assert.IsFalse(judge.StopPouring().IsDecided);
        }

        [Test]
        public void StopAboveRange_FailsTooMuch()
        {
            judge.AddToCup(256f);
            Assert.AreEqual(FailReason.TooMuch, judge.StopPouring().Reason);
        }

        [Test]
        public void Over100Percent_FailsOverflow()
        {
            judge.AddToCup(290f);
            var o = judge.AddToCup(20f);
            Assert.AreEqual(FailReason.Overflow, o.Reason);
            Assert.AreEqual(10f, judge.OverflowMl, 1e-3f);
            Assert.AreEqual(1f, judge.Fill01, 1e-4f);
        }

        [Test]
        public void OutsideSpillLimit_FailsSpilled()
        {
            Assert.IsFalse(judge.AddOutside(60f).IsDecided); // 한도까지는 괜찮음
            Assert.AreEqual(FailReason.Spilled, judge.AddOutside(1f).Reason);
        }

        [Test]
        public void InitialContents_CountTowardFill()
        {
            judge.Reset(0.6f, 0.85f, 300f, 60f, 60f); // 에스프레소 60ml가 이미 있음
            judge.AddToCup(120f);
            Assert.AreEqual(120f, judge.PouredInMl, 1e-3f);
            Assert.IsTrue(judge.StopPouring().Success);
        }

        [Test]
        public void StopWithoutPouringIntoCup_KeepsGoing()
        {
            // 이미 목표 구간인 컵에 컵 밖으로만 붓고 세우면 성공이 아니다
            judge.Reset(0.6f, 0.85f, 300f, 200f, 60f);
            judge.AddOutside(10f);
            Assert.IsFalse(judge.StopPouring().IsDecided);
            judge.AddToCup(5f);
            Assert.IsTrue(judge.StopPouring().Success);
        }

        [Test]
        public void ResultIsLatched()
        {
            judge.AddToCup(200f);
            Assert.IsTrue(judge.StopPouring().Success);
            Assert.IsTrue(judge.AddToCup(500f).Success);
            Assert.IsTrue(judge.AddOutside(500f).Success);
            Assert.AreEqual(200f, judge.LiquidMl, 1e-3f);
        }

        [Test]
        public void TimeUp_ReasonsByFill()
        {
            Assert.AreEqual(FailReason.Timeout, judge.TimeUp().Reason);

            judge.Reset(0.6f, 0.85f, 300f, 0f, 60f);
            judge.AddToCup(100f);
            Assert.AreEqual(FailReason.TooLittle, judge.TimeUp().Reason);

            judge.Reset(0.6f, 0.85f, 300f, 0f, 60f);
            judge.AddToCup(200f);
            Assert.IsTrue(judge.TimeUp().Success);
        }

        [Test]
        public void Level3Range_Boundaries()
        {
            judge.Reset(0.72f, 0.78f, 300f, 0f, 25f);
            judge.AddToCup(216f); // 72%
            Assert.IsTrue(judge.StopPouring().Success);

            judge.Reset(0.72f, 0.78f, 300f, 0f, 25f);
            judge.AddToCup(234.5f); // 78.2%
            Assert.AreEqual(FailReason.TooMuch, judge.StopPouring().Reason);
        }
    }
}
