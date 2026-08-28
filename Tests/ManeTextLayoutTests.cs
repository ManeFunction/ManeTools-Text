using System;
using NUnit.Framework;

namespace Mane.Unity.Text.Tests
{
    public class ManeTextLayoutTests
    {
        private static readonly Func<char, float> Width10 = _ => 10f;

        [Test]
        public void Wrap_RejectsEmptyOrNegativeLimits()
        {
            Assert.IsNull(ManeTextLayout.Wrap(null, 0, int.MaxValue, true, Width10));
            Assert.IsNull(ManeTextLayout.Wrap(string.Empty, 0, int.MaxValue, true, Width10));
            Assert.IsNull(ManeTextLayout.Wrap("A", -1, int.MaxValue, true, Width10));
            Assert.IsNull(ManeTextLayout.Wrap("A", 0, -1, true, Width10));
        }

        [Test]
        public void Wrap_SingleLine_UnlimitedWidth()
        {
            ManeTextInfo info = ManeTextLayout.Wrap("AB", 0, int.MaxValue, true, Width10);

            Assert.AreEqual(1, info.String.Count);
            Assert.AreEqual("AB", info.String[0]);
            Assert.AreEqual(20f, info.Length[0]);
            Assert.AreEqual(2, info.TotalCount);
            Assert.AreEqual(20f, info.MaxLength);
        }

        [Test]
        public void Wrap_ExplicitNewLine()
        {
            ManeTextInfo info = ManeTextLayout.Wrap("A\nB", 0, int.MaxValue, true, Width10);

            Assert.AreEqual(2, info.String.Count);
            Assert.AreEqual("A", info.String[0]);
            Assert.AreEqual("B", info.String[1]);
            Assert.AreEqual(10f, info.Length[0]);
            Assert.AreEqual(10f, info.Length[1]);
        }

        [Test]
        public void Wrap_HardBreaksWhenASingleWordDoesNotFit()
        {
            ManeTextInfo info = ManeTextLayout.Wrap("AAA", 20, int.MaxValue, true, Width10);

            Assert.AreEqual(2, info.String.Count);
            Assert.AreEqual("AA", info.String[0]);
            Assert.AreEqual("A", info.String[1]);
            Assert.AreEqual(20f, info.Length[0]);
            Assert.AreEqual(10f, info.Length[1]);
        }

        [Test]
        public void Wrap_MovesWholeWordToNextLine()
        {
            ManeTextInfo info = ManeTextLayout.Wrap("AA AA", 25, int.MaxValue, true, Width10);

            Assert.AreEqual(2, info.String.Count);
            Assert.AreEqual("AA", info.String[0]);
            Assert.AreEqual("AA", info.String[1]);
            Assert.AreEqual(20f, info.Length[0]);
            Assert.AreEqual(20f, info.Length[1]);
        }

        [Test]
        public void Wrap_MaxLines_ClipsNewLines()
        {
            ManeTextInfo info = ManeTextLayout.Wrap("A\nB\nC", 0, 1, true, Width10);

            Assert.AreEqual(1, info.String.Count);
            Assert.AreEqual("A", info.String[0]);
        }

        [Test]
        public void Wrap_MaxLines_ClipsHardBreaks()
        {
            ManeTextInfo info = ManeTextLayout.Wrap("AAA", 20, 1, true, Width10);

            Assert.AreEqual(1, info.String.Count);
            Assert.AreEqual("AA", info.String[0]);
        }

        [Test]
        public void Wrap_BreakDigitsFalse_WrapsTheFollowingWord()
        {
            ManeTextInfo info = ManeTextLayout.Wrap("12 ABC", 35, int.MaxValue, false, Width10);

            Assert.AreEqual(2, info.String.Count);
            Assert.AreEqual("12", info.String[0]);
            Assert.AreEqual("ABC", info.String[1]);
        }

        [Test]
        public void Wrap_BreakDigitsTrue_HardBreaksAfterDigitSpace()
        {
            ManeTextInfo info = ManeTextLayout.Wrap("12 ABC", 35, int.MaxValue, true, Width10);

            Assert.AreEqual(2, info.String.Count);
            Assert.AreEqual("12 ", info.String[0]);
            Assert.AreEqual("ABC", info.String[1]);
        }
    }
}
