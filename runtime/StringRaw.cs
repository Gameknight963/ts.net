using System.Globalization;
using System.Text;

public static partial class Js
{
    static object StringRaw(object[] arguments)
    {
        TsObject template = ToObject(FunctionArgument(arguments, 0));
        TsObject raw = ToObject(template.Get("raw"));
        long length = IndexedLength(raw);
        if (length == 0) return "";
        var result = new StringBuilder();
        for (long index = 0; index < length; index++)
        {
            result.Append(StringValue(raw.Get(index.ToString(CultureInfo.InvariantCulture))));
            if (index + 1 == length) break;
            // Missing substitutions are empty; an explicitly supplied undefined
            // is still converted to the string "undefined".
            if (index < arguments.Length - 1) result.Append(StringValue(arguments[(int)index + 1]));
        }
        return result.ToString();
    }
}
