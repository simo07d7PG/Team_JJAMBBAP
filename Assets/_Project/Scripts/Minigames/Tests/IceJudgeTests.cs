using NUnit.Framework;

namespace BariBarista.Minigames.Tests
{
    public class IceJudgeTests
    {
        private IceJudge judge;

        [SetUp]
        public void SetUp()
        {
            judge = new IceJudge();
            judge.Reset(4, 7, 9, 1f); // Lv1
        }

        [TestCase(4)]
        [TestCase(7)]
        public void HoldInsideRangeForHoldTime_Succeeds(int count)
        {
            Assert.IsFalse(judge.Update(count, 0.5f).IsDecided);
            Assert.IsFalse(judge.Update(count, 0.4f).IsDecided);
            var o = judge.Update(count, 0.1f);
            Assert.IsTrue(o.IsDecided);
            Assert.IsTrue(o.Success);
        }

        [TestCase(3)]
        [TestCase(8)]
        public void OutsideRange_NeverSucceeds(int count)
        {
            for (int i = 0; i < 10; i++)
                Assert.IsFalse(judge.Update(count, 0.5f).IsDecided);
        }

        [Test]
        public void LeavingRange_ResetsHoldTimer()
        {
            judge.Update(5, 0.9f);
            judge.Update(8, 0.1f); // 구간 이탈
            Assert.AreEqual(0f, judge.HoldTimer);
            Assert.IsFalse(judge.Update(5, 0.5f).IsDecided);
        }

        [Test]
        public void AboveCupMax_FailsOverflow()
        {
            var o = judge.Update(10, 0.02f);
            Assert.AreEqual(FailReason.Overflow, o.Reason);
            Assert.AreEqual(FailReason.Overflow, judge.Update(5, 2f).Reason); // 이후 무시
        }

        [Test]
        public void TimeUp_ReasonsByCount()
        {
            Assert.AreEqual(FailReason.Timeout, judge.TimeUp().Reason);

            judge.Reset(4, 7, 9, 1f);
            judge.Update(2, 0.1f);
            Assert.AreEqual(FailReason.TooLittle, judge.TimeUp().Reason);

            judge.Reset(4, 7, 9, 1f);
            judge.Update(8, 0.1f);
            Assert.AreEqual(FailReason.TooMuch, judge.TimeUp().Reason);

            // 구간 안이어도 1초를 못 채우면 실패
            judge.Reset(4, 7, 9, 1f);
            judge.Update(5, 0.1f);
            Assert.AreEqual(FailReason.Timeout, judge.TimeUp().Reason);
        }

        [Test]
        public void Level3_ExactlyFive()
        {
            judge.Reset(5, 5, 8, 1f);
            Assert.IsFalse(judge.Update(4, 2f).IsDecided);
            Assert.IsFalse(judge.Update(6, 2f).IsDecided);
            var o = judge.Update(5, 1f);
            Assert.IsTrue(o.Success);
            Assert.AreEqual(1f, o.Score, 1e-4f);
        }
    }
}
