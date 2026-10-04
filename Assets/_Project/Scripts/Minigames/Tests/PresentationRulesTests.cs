using System;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine.TextCore.LowLevel;

namespace BariBarista.Minigames.Tests
{
    public class PresentationRulesTests
    {
        private static readonly FailReason[] ShotReachable = { FailReason.Overflow, FailReason.TooMuch, FailReason.TooLittle, FailReason.Timeout };
        private static readonly FailReason[] IceReachable = { FailReason.Overflow, FailReason.TooMuch, FailReason.TooLittle, FailReason.Timeout };
        private static readonly FailReason[] PourReachable = { FailReason.Overflow, FailReason.Spilled, FailReason.TooMuch, FailReason.TooLittle, FailReason.Timeout };

        private static readonly Func<FailReason, string>[] FailTexts =
        {
            ShotRules.FailText, IceRules.FailText, PourRules.FailText,
        };

        [Test]
        public void FailText_ReachableReasonsAreNotEmpty()
        {
            foreach (var r in ShotReachable) Assert.IsNotEmpty(ShotRules.FailText(r), "Shot " + r);
            foreach (var r in IceReachable) Assert.IsNotEmpty(IceRules.FailText(r), "Ice " + r);
            foreach (var r in PourReachable) Assert.IsNotEmpty(PourRules.FailText(r), "Pour " + r);
        }

        [Test]
        public void FailText_AbortedAndNoneAreEmpty()
        {
            foreach (var f in FailTexts)
            {
                Assert.IsEmpty(f(FailReason.Aborted));
                Assert.IsEmpty(f(FailReason.None));
            }
        }

        [Test]
        public void FailText_DifferentReasonsHaveDifferentTexts()
        {
            var seen = new HashSet<string>();
            foreach (var r in PourReachable) Assert.IsTrue(seen.Add(PourRules.FailText(r)), "Pour 중복 " + r);
            seen.Clear();
            foreach (var r in ShotReachable) Assert.IsTrue(seen.Add(ShotRules.FailText(r)), "Shot 중복 " + r);
        }

        [Test]
        public void SuccessText_PerfectAtNinety()
        {
            Assert.AreEqual(PresentationRules.SuccessPerfect, PresentationRules.SuccessText(0.9f));
            Assert.AreEqual(PresentationRules.SuccessPerfect, PresentationRules.SuccessText(1f));
            Assert.AreEqual(PresentationRules.SuccessGood, PresentationRules.SuccessText(0.89f));
        }

        [Test]
        public void ResultText_SuccessUsesPraiseAndFailUsesReason()
        {
            Assert.AreEqual(PresentationRules.SuccessGood, PresentationRules.ResultText(MicrogameResult.Succeeded(0.5f), "x"));
            Assert.AreEqual(ShotRules.FailOverflow, PresentationRules.ResultText(MicrogameResult.Failed(FailReason.Overflow), ShotRules.FailText(FailReason.Overflow)));
        }

        [Test]
        public void PreloadCharacters_CoverEveryUiText()
        {
            var texts = new List<string>
            {
                PresentationRules.InstructionShot, PresentationRules.InstructionIce, PresentationRules.InstructionPour,
                PresentationRules.TimeoutText, PresentationRules.WrongOrderText, PresentationRules.SuccessPerfect, PresentationRules.SuccessGood,
                PresentationRules.MlFormat, PresentationRules.PercentFormat, PresentationRules.CountFormat, PresentationRules.Target,
                ShotRules.All, IceRules.All, PourRules.All,
                "샷 내려라!", "얼음 퍼라!", "우유 부어라!", // 정의 에셋의 instruction과 같아야 한다
                "0", "123456789", "ml",
            };
            foreach (var f in FailTexts)
                foreach (FailReason r in Enum.GetValues(typeof(FailReason))) texts.Add(f(r));

            foreach (string t in texts)
                foreach (char c in t)
                    if (!char.IsWhiteSpace(c) && c != '{' && c != '}')
                        Assert.GreaterOrEqual(PresentationRules.PreloadCharacters.IndexOf(c), 0, $"'{c}'(\"{t}\")가 PreloadCharacters에 없음");
        }

        [Test]
        public void DefinitionInstructions_MatchRules()
        {
            Assert.AreEqual("샷 내려라!", PresentationRules.InstructionShot);
            Assert.AreEqual("얼음 퍼라!", PresentationRules.InstructionIce);
            Assert.AreEqual("우유 부어라!", PresentationRules.InstructionPour);
        }

        [Test]
        public void DynamicFont_HasEveryPreloadedGlyph()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/UI/Fonts/Galmuri11-Bold_Dynamic SDF.asset");
            Assert.IsNotNull(font, "동적 폰트가 없음: Tools/BariBarista/Create Galmuri Dynamic Font");
            Assert.AreEqual(AtlasPopulationMode.Dynamic, font.atlasPopulationMode);
            var missing = new List<char>();
            foreach (char c in PresentationRules.PreloadCharacters)
                if (!char.IsWhiteSpace(c) && !font.characterLookupTable.ContainsKey(c) && !missing.Contains(c)) missing.Add(c);
            Assert.IsEmpty(missing, "폰트에 없는 글자: " + new string(missing.ToArray()));
        }
    }
}
