namespace Tsnet.Compiler;

using System.Text;
using static Tsnet.Compiler.CompilerFunctions;

internal static partial class CompilerFunctions
{
    internal static Vector<byte> il_method_name(Node function, char kind, Compiler c)
    {
        Symbol symbol = get_symbol(function.symbol, c);
        int text = function.name != 0 ? function.name : symbol.name;
        int length = text_size(text, c);
        if (length == 0) return numbered_name(kind, symbol.id);
        var bytes = new byte[length];
        for (int index = 0; index < length; index++) bytes[index] = text_byte(text, index, c);
        var name = new StringBuilder();
        // These labels are for CLR diagnostics; JavaScript names stay in the runtime.
        foreach (var character in Encoding.UTF8.GetString(bytes).EnumerateRunes()) {
            if (Rune.IsLetterOrDigit(character) || character.Value is '_' or '$' ||
                Rune.GetUnicodeCategory(character) is System.Globalization.UnicodeCategory.NonSpacingMark or System.Globalization.UnicodeCategory.SpacingCombiningMark) {
                if (character.IsBmp) name.Append((char)character.Value); else name.Append(character.ToString());
            } else name.Append('_');
        }
        Vector<byte> result = new();
        foreach (byte value in Encoding.UTF8.GetBytes(name.ToString())) result.push(value);
        // Distinguish shadowed declarations and generated adapters.
        result.push((byte)'_'); result.push((byte)kind); name_number(symbol.id, result);
        return result;
    }
}
