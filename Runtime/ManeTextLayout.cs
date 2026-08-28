using System;
using System.Text;
using UnityEngine;

namespace Mane.Unity.Text
{
    internal static class ManeTextLayout
    {
        internal static ManeTextInfo Wrap(
            string text,
            int maxWidth,
            int maxLines,
            bool breakDigits,
            Func<char, float> getGlyphWidth)
        {
            if (maxWidth < 0 || maxLines < 0 || string.IsNullOrEmpty(text))
                return null;

            ManeTextInfo res = new();
            StringBuilder sb = new();
            int textLength = text.Length, start = 0, offset = 0, linesCount = 0;
            float substringWidth = 0f, lineWidth = 0f;
            bool lineIsEmpty = true;

            for (; offset < textLength; ++offset)
            {
                char ch = text[offset];

                if (ch == '\n')
                {
                    if (!lineIsEmpty)
                        sb.Append(' ');

                    if (start < offset)
                        sb.Append(text, start, offset - start);
                    res.Append(sb.ToString(), substringWidth);
                    linesCount++;
                    if (linesCount >= maxLines)
                        return res;

                    sb.Clear();
                    lineIsEmpty = true;
                    start = offset + 1;
                    substringWidth = 0f;
                    lineWidth = 0f;
                    continue;
                }

                float glyphWidth = getGlyphWidth(ch);

                if (ch == ' ' && start < offset)
                {
                    int end = offset - start + 1;

                    if (maxWidth > 0 && substringWidth > maxWidth && offset < textLength)
                    {
                        if (text[offset] <= ' ')
                            --end;
                    }

                    if (!breakDigits || !char.IsDigit(text[offset - 1]))
                    {
                        if (!lineIsEmpty)
                            sb.Append(' ');
                        sb.Append(text, start, end - 1);
                        lineIsEmpty = false;
                        lineWidth = substringWidth;
                        start = offset + 1;
                    }
                }

                substringWidth += glyphWidth;

                if (maxWidth > 0 && Mathf.RoundToInt(substringWidth) > maxWidth)
                {
                    if (lineIsEmpty)
                    {
                        res.Append(text.Substring(start, Mathf.Max(0, offset - start)),
                            substringWidth - glyphWidth);
                        linesCount++;
                        if (linesCount >= maxLines)
                            return res;

                        if (ch == ' ')
                        {
                            start = offset + 1;
                            substringWidth = 0f;
                        }
                        else
                        {
                            start = offset;
                            substringWidth = glyphWidth;
                        }
                    }
                    else
                    {
                        res.Append(sb.ToString(), lineWidth);

                        lineIsEmpty = true;
                        offset = start - 1;
                        linesCount++;
                        if (linesCount >= maxLines)
                            return res;
                        sb.Clear();
                        substringWidth = 0f;
                    }
                }
            }

            if (start < offset)
            {
                if (!lineIsEmpty)
                    sb.Append(' ');
                sb.Append(text, start, offset - start);
            }

            res.Append(sb.ToString(), substringWidth);

            return res;
        }
    }
}
