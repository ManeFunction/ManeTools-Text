using System.Collections.Generic;
using System.Linq;

namespace Mane.Unity.Text
{
    /// <summary>
    /// Line-by-line layout produced by wrapping. Widths are in font units
    /// (glyph advance plus <see cref="ManeText.SpacingX"/>), not world units.
    /// </summary>
    internal class ManeTextInfo
    {
        /// <summary>Wrapped lines, in draw order from top to bottom.</summary>
        public readonly List<string> String = new();

        /// <summary>Width of each line in <see cref="String"/>, in font units.</summary>
        public readonly List<float> Length = new();

        /// <summary>Total character count across all lines, including spaces.</summary>
        public int TotalCount => String.Sum(t => t.Length);

        /// <summary>Width of the longest line, or 0 when there are no lines.</summary>
        public float MaxLength => Length.Prepend(0f).Max();

        /// <summary>Appends a wrapped line and its measured width.</summary>
        /// <param name="str">Line text.</param>
        /// <param name="length">Line width in font units.</param>
        public void Append(string str, float length)
        {
            String.Add(str);
            Length.Add(length);
        }
    }
}
