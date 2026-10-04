using NUnit.Framework;

namespace BariBarista.Minigames.Tests
{
    public class CoreLogicTests
    {
        [Test]
        public void CupContents_AddClearTotalCopy()
        {
            var cup = new CupContents();
            cup.Add(Ingredient.Espresso, 30f);
            cup.Add(Ingredient.Milk, 150f);
            cup.Add(Ingredient.Ice, 5f);
            cup.Add(Ingredient.Syrup, -10f); // 음수는 무시

            Assert.AreEqual(180f, cup.TotalLiquid, 1e-4f);
            Assert.AreEqual(5, cup.IceCount);
            Assert.AreEqual(0f, cup.Get(Ingredient.Syrup));

            var copy = cup.Clone();
            cup.Clear();
            Assert.IsTrue(cup.IsEmpty);
            Assert.AreEqual(180f, copy.TotalLiquid, 1e-4f);

            cup.CopyFrom(copy);
            Assert.AreEqual(30f, cup.Get(Ingredient.Espresso), 1e-4f);
        }

        [Test]
        public void ResultLatch_IgnoresDuplicateResults()
        {
            var latch = new ResultLatch();
            Assert.IsTrue(latch.TrySet(MicrogameResult.Failed(FailReason.Overflow)));
            Assert.IsFalse(latch.TrySet(MicrogameResult.Succeeded(1f)));
            Assert.IsFalse(latch.TrySet(MicrogameResult.Failed(FailReason.Timeout)));
            Assert.IsFalse(latch.Result.Success);
            Assert.AreEqual(FailReason.Overflow, latch.Result.Reason);

            latch.Reset();
            Assert.IsFalse(latch.IsSet);
            Assert.IsTrue(latch.TrySet(MicrogameResult.Succeeded(0.7f)));
        }

        [Test]
        public void MicrogameClock_TimeUpFiresOnce()
        {
            var clock = new MicrogameClock();
            clock.Start(2f);
            Assert.AreEqual(1f, clock.Remaining01, 1e-4f);
            Assert.IsFalse(clock.Advance(1f));
            Assert.AreEqual(0.5f, clock.Remaining01, 1e-4f);
            Assert.IsTrue(clock.Advance(1.5f));
            Assert.AreEqual(0f, clock.Remaining);
            Assert.IsFalse(clock.Advance(1f));
            Assert.IsTrue(clock.IsTimeUp);

            clock.Start(1f); // 재시작하면 다시 처음부터
            Assert.IsFalse(clock.IsTimeUp);
            Assert.AreEqual(1f, clock.Remaining, 1e-4f);
        }

        [Test]
        public void MicrogameResult_ScoreIsClamped()
        {
            Assert.AreEqual(1f, MicrogameResult.Succeeded(3f).Score);
            Assert.AreEqual(0f, MicrogameResult.Failed(FailReason.Spilled).Score);
        }
    }
}
