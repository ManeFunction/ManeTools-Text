using System;
using UnityEngine.UIElements;

namespace Mane.Unity.Text.Editor
{
    [UxmlElement]
    internal partial class OptionalSizeField : IntegerField
    {
        private const string UndefinedLabel = "Undefined";

        protected override string ValueToString(int v)
        {
            return v <= 0 ? UndefinedLabel : base.ValueToString(v);
        }

        protected override int StringToValue(string str)
        {
            if (string.Equals(str, UndefinedLabel, StringComparison.OrdinalIgnoreCase))
                return 0;

            return base.StringToValue(str);
        }
    }
}
