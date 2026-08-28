using NUnit.Framework;
using UnityEngine;

namespace Mane.Unity.Text.Tests
{
    public class ManeTextTests
    {
        [Test]
        public void PublicProperties_RoundTripAndMarkDirty()
        {
            GameObject go = new("ManeText");
            try
            {
                ManeText text = go.AddComponent<ManeText>();
                text.Text = "Hello";
                text.FontSize = 42;
                text.CharacterSize = .2f;
                text.SpacingX = 3;
                text.SpacingY = -1;
                text.MaxWidth = 100;
                text.MaxHeight = 50;
                text.BreakDigits = false;
                text.Horizontal = ManeText.HorizontalAlignment.Left;
                text.Vertical = ManeText.VerticalAlignment.Top;
                text.OutlineSize = 2f;
                text.EffectsShiftZ = .25f;
                text.Effect = ManeText.TextEffect.Outline | ManeText.TextEffect.Shadow;

                Assert.AreEqual("Hello", text.Text);
                Assert.AreEqual(42, text.FontSize);
                Assert.AreEqual(.2f, text.CharacterSize);
                Assert.AreEqual(3, text.SpacingX);
                Assert.AreEqual(-1, text.SpacingY);
                Assert.AreEqual(100, text.MaxWidth);
                Assert.AreEqual(50, text.MaxHeight);
                Assert.IsFalse(text.BreakDigits);
                Assert.AreEqual(ManeText.HorizontalAlignment.Left, text.Horizontal);
                Assert.AreEqual(ManeText.VerticalAlignment.Top, text.Vertical);
                Assert.AreEqual(2f, text.OutlineSize);
                Assert.AreEqual(.25f, text.EffectsShiftZ);
                Assert.IsTrue(text.IsOutlineActive);
                Assert.IsTrue(text.IsShadowActive);
                Assert.IsFalse(text.IsNoEffectsActive);
                Assert.IsTrue(text.Dirty);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void EffectNone_ClearsEffectFlags()
        {
            GameObject go = new("ManeText");
            try
            {
                ManeText text = go.AddComponent<ManeText>();
                text.Effect = ManeText.TextEffect.Outline;
                text.Effect = ManeText.TextEffect.None;

                Assert.IsTrue(text.IsNoEffectsActive);
                Assert.IsFalse(text.IsOutlineActive);
                Assert.IsFalse(text.IsShadowActive);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
