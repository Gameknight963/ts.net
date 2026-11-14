namespace Tsnet.Compiler;

using static Tsnet.Compiler.CompilerFunctions;

internal static partial class CompilerFunctions
{
    // Larger shapes avoid a linear chain of string comparisons.
    internal static bool il_field_lookup(int type, Compiler c, IlBackend b, Action<int> found, Func<int, bool> include = null)
    {
        int count = 0;
        for (int member = get_type(type, c).fields; member != 0 && count < 32; member = get_field(member, c).next)
            if (include == null || include(member)) count++;
        if (count < 32) return false;
        var fields = new List<(int Member, uint Hash)>();
        for (int member = get_type(type, c).fields; member != 0; member = get_field(member, c).next)
        {
            if (include != null && !include(member)) continue;
            int name = get_field(member, c).name;
            var bytes = new byte[text_size(name, c)];
            for (int i = 0; i < bytes.Length; i++) bytes[i] = text_byte(name, i, c);
            string key = System.Text.Encoding.UTF8.GetString(bytes);
            uint hash = 2166136261;
            foreach (char value in key) hash = unchecked((hash ^ value) * 16777619);
            fields.Add((member, hash));
        }
        // Check early fields before calculating a hash.
        for (int i = 0; i < 4; i++)
        {
            int next = b.label(), member = fields[i].Member;
            b.emit(0xfe09, 1); il_text(get_field(member, c).name, c, b);
            b.call(RuntimeMember.StringEqual); b.emit(0x39, next);
            found(member); b.mark(next);
        }
        fields.RemoveRange(0, 4);
        fields.Sort((left, right) => left.Hash.CompareTo(right.Hash));
        var groups = new List<(uint Hash, List<int> Members)>();
        foreach (var field in fields)
        {
            if (groups.Count == 0 || groups[^1].Hash != field.Hash) groups.Add((field.Hash, new List<int>()));
            groups[^1].Members.Add(field.Member);
        }
        int hashSlot = b.local(-6);
        b.emit(0xfe09, 1); b.call(RuntimeMember.PropertyKeyHash); b.emit(0xfe0e, hashSlot);
        int missing = b.label();
        emit(0, groups.Count);
        b.mark(missing);
        return true;

        void emit(int start, int end)
        {
            if (start == end) { b.emit(0x38, missing); return; }
            int middle = start + (end - start) / 2;
            var group = groups[middle];
            if (end - start == 1) { bucket(group.Members); return; }
            int lower = b.label(), equal = b.label();
            b.emit(0xfe0c, hashSlot); b.emit(0x20, unchecked((int)group.Hash)); b.op(0xfe01); b.emit(0x3a, equal);
            b.emit(0xfe0c, hashSlot); b.emit(0x20, unchecked((int)group.Hash)); b.op(0xfe05); b.emit(0x3a, lower);
            emit(middle + 1, end);
            b.mark(lower); emit(start, middle);
            b.mark(equal); bucket(group.Members);
        }
        void bucket(List<int> members)
        {
            // Hashes select candidates; equality still decides the property.
            foreach (int member in members)
            {
                int next = b.label();
                b.emit(0xfe09, 1); il_text(get_field(member, c).name, c, b);
                b.call(RuntimeMember.StringEqual); b.emit(0x39, next);
                found(member); b.mark(next);
            }
            b.emit(0x38, missing);
        }
    }
}
