using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public static partial class Js
{
    sealed partial class UnicodeRegExpReader
    {
        readonly bool trackSensitiveAtoms = RegExpHasCaptureReferences(source);
        bool sensitiveAtoms, sensitiveReferences, insensitiveReferences;
        internal bool NeedsFoldInput => insensitiveReferences;
        internal void ValidateCaseModes()
        {
            if (insensitiveReferences && (sensitiveAtoms || sensitiveReferences))
                throw new NotSupportedException("Mixed Unicode case modes with backreferences are not supported yet");
        }
        void TrackReference()
        {
            if (ignoreCase) insensitiveReferences = true;
            else sensitiveReferences = true;
        }
        void TrackSensitiveRanges(List<RegExpCodePointRange> ranges)
        {
            if (ignoreCase || !trackSensitiveAtoms || sensitiveAtoms) return;
            var original = MergeRegExpRanges(ranges);
            var closed = UnicodeRegExpFolding.Close(original);
            if (original.Count != closed.Count) {sensitiveAtoms = true; return;}
            for (int index = 0; index < original.Count; index++)
                if (original[index] != closed[index]) {sensitiveAtoms = true; return;}
        }
        void TrackSensitiveSet(RegExpCharacterSet set)
        {
            if (ignoreCase || !trackSensitiveAtoms || sensitiveAtoms) return;
            TrackSensitiveRanges(set.Ranges);
            foreach (string value in set.Strings)
                for (int index = 0; index < value.Length && !sensitiveAtoms; index += 6) {
                    int point = int.Parse(value.AsSpan(index, 6), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
                    TrackSensitiveRanges([new(point, point)]);
                }
        }
        void CaseModifiers(int end, StringBuilder result)
        {
            var enabled = new StringBuilder(); var disabled = new StringBuilder();
            bool enable = true;
            for (int index = position + 2; index < end; index++) {
                char flag = source[index];
                if (flag == '-') enable = false;
                else if (flag == 'i') ignoreCase = enable;
                else (enable ? enabled : disabled).Append(flag);
            }
            result.Append("(?").Append(enabled);
            if (disabled.Length != 0) result.Append('-').Append(disabled);
            result.Append(':');
        }
    }
}
