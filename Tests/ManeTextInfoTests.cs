using NUnit.Framework;

namespace Mane.Unity.Text.Tests
{
    public class ManeTextInfoTests
    {
        [Test]
        public void Empty_HasZeroTotals()
        {
            ManeTextInfo info = new();
            Assert.AreEqual(0, info.TotalCount);
            Assert.AreEqual(0f, info.MaxLength);
        }

        [Test]
        public void Append_TracksCountAndMaxLength()
        {
            ManeTextInfo info = new();
            info.Append("ab", 12f);
            info.Append("c", 5f);
            info.Append("defg", 9f);

            Assert.AreEqual(3, info.String.Count);
            Assert.AreEqual("ab", info.String[0]);
            Assert.AreEqual("c", info.String[1]);
            Assert.AreEqual("defg", info.String[2]);
            Assert.AreEqual(12f, info.Length[0]);
            Assert.AreEqual(7, info.TotalCount);
            Assert.AreEqual(12f, info.MaxLength);
        }
    }
}
