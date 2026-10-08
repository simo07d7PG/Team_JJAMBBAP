using NUnit.Framework;

namespace BariBarista.Minigames.Tests
{
    public class IceSizingTests
    {
        [Test]
        public void SizeFactor_StaysWithinVariation()
        {
            Assert.AreEqual(0.85f, IceSizing.SizeFactor(0f, 0.15f), 1e-5f);
            Assert.AreEqual(1f, IceSizing.SizeFactor(0.5f, 0.15f), 1e-5f);
            Assert.AreEqual(1.15f, IceSizing.SizeFactor(1f, 0.15f), 1e-5f);
        }

        [Test]
        public void SizeFactor_ClampsOutOfRangeInput()
        {
            Assert.AreEqual(0.85f, IceSizing.SizeFactor(-3f, 0.15f), 1e-5f);
            Assert.AreEqual(1.15f, IceSizing.SizeFactor(9f, 0.15f), 1e-5f);
            Assert.AreEqual(1f, IceSizing.SizeFactor(0.2f, -1f), 1e-5f);
        }

        [Test]
        public void GrowScale_InterpolatesFromStartToOne()
        {
            Assert.AreEqual(0.3f, IceSizing.GrowScale(0f, 0.1f, 0.3f), 1e-5f);
            Assert.AreEqual(0.65f, IceSizing.GrowScale(0.05f, 0.1f, 0.3f), 1e-5f);
            Assert.AreEqual(1f, IceSizing.GrowScale(0.1f, 0.1f, 0.3f), 1e-5f);
            Assert.AreEqual(1f, IceSizing.GrowScale(5f, 0.1f, 0.3f), 1e-5f);
        }

        [Test]
        public void GrowScale_ZeroDurationIsInstant()
        {
            Assert.AreEqual(1f, IceSizing.GrowScale(0f, 0f, 0.3f), 1e-5f);
        }
    }
}
