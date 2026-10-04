using NUnit.Framework;

namespace BariBarista.Minigames.Tests
{
    public class LiquidLookTests
    {
        [Test]
        public void Empty_IsNone() => Assert.AreEqual(TopLayer.None, LiquidLook.TopLayerFor(0f, 0f, 0f, 0f));

        [Test]
        public void EspressoOnly_IsCrema() => Assert.AreEqual(TopLayer.Crema, LiquidLook.TopLayerFor(30f, 0f, 0f, 0f));

        [Test]
        public void EspressoWithWater_IsCrema() => Assert.AreEqual(TopLayer.Crema, LiquidLook.TopLayerFor(30f, 0f, 120f, 0f));

        [Test]
        public void MilkOnly_IsFoam() => Assert.AreEqual(TopLayer.Foam, LiquidLook.TopLayerFor(0f, 150f, 0f, 0f));

        [Test]
        public void Latte_IsFoam() => Assert.AreEqual(TopLayer.Foam, LiquidLook.TopLayerFor(30f, 150f, 0f, 0f));

        [Test]
        public void TinyMilkOverEspresso_StaysCrema() => Assert.AreEqual(TopLayer.Crema, LiquidLook.TopLayerFor(60f, 5f, 0f, 0f));

        [Test]
        public void WaterOnly_IsNone() => Assert.AreEqual(TopLayer.None, LiquidLook.TopLayerFor(0f, 0f, 100f, 0f));

        [Test]
        public void SyrupOnly_IsNone() => Assert.AreEqual(TopLayer.None, LiquidLook.TopLayerFor(0f, 0f, 0f, 20f));

        [Test]
        public void NegativeAmounts_AreIgnored() => Assert.AreEqual(TopLayer.None, LiquidLook.TopLayerFor(-5f, -5f, -5f, -5f));
    }
}
