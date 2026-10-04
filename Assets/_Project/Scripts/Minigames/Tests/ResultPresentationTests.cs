using NUnit.Framework;

namespace BariBarista.Minigames.Tests
{
    public class ResultPresentationTests
    {
        [Test]
        public void DurationFor_FailIsShort()
        {
            var fail = MicrogameResult.Failed(FailReason.Overflow);
            Assert.AreEqual(0.9f, ResultPresentation.DurationFor(fail), 1e-5f);
            Assert.AreEqual(0.9f, ResultPresentation.DurationFor(MicrogameResult.Failed(FailReason.Timeout)), 1e-5f);
        }

        [Test]
        public void DurationFor_SuccessIsLonger()
        {
            Assert.AreEqual(1.4f, ResultPresentation.DurationFor(MicrogameResult.Succeeded(1f)), 1e-5f);
            Assert.AreEqual(1.4f, ResultPresentation.DurationFor(MicrogameResult.Succeeded(0.2f)), 1e-5f);
        }

        [Test]
        public void DurationFor_AbortedHasNoPresentation()
        {
            Assert.AreEqual(0f, ResultPresentation.DurationFor(MicrogameResult.Failed(FailReason.Aborted)));
        }
    }
}
