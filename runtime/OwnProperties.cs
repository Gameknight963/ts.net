using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// Most dynamic records have only a few keys. Keep those entries together and
// use a dictionary for the tail, preserving insertion order across both.
sealed class OwnProperties
{
    string key0, key1, key2, key3;
    object value0, value1, value2, value3;
    int inlineCount, version;
    Dictionary<string, object> tail;
    public OwnProperties() { }
    public OwnProperties(OwnProperties source)
    {
        key0=source.key0;key1=source.key1;key2=source.key2;key3=source.key3;
        value0=source.value0;value1=source.value1;value2=source.value2;value3=source.value3;
        inlineCount=source.inlineCount;version=source.version;
        if (source.tail != null) tail = new Dictionary<string, object>(source.tail);
    }
    public int Count => inlineCount + (tail?.Count ?? 0);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(string key, out object value)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (key==key0) { value=value0;return true; }
        if (key==key1) { value=value1;return true; }
        if (key==key2) { value=value2;return true; }
        if (key==key3) { value=value3;return true; }
        value=null;
        return tail != null && tail.TryGetValue(key, out value);
    }
    public bool ContainsKey(string key) => TryGetValue(key, out _);
    public object this[string key]
    {
        set {
            ArgumentNullException.ThrowIfNull(key);
            if (key==key0) { value0=value;return; }
            if (key==key1) { value1=value;return; }
            if (key==key2) { value2=value;return; }
            if (key==key3) { value3=value;return; }
            if (inlineCount < 4) {
                switch (inlineCount++) {
                    case 0: key0=key;value0=value;break;
                    case 1: key1=key;value1=value;break;
                    case 2: key2=key;value2=value;break;
                    default: key3=key;value3=value;break;
                }
                version++;
            } else {
                tail ??= new Dictionary<string, object>();
                ref object slot = ref CollectionsMarshal.GetValueRefOrAddDefault(tail, key, out bool present);
                slot=value;
                if (!present) version++;
            }
        }
    }
    string InlineKey(int index) => index switch {0=>key0,1=>key1,2=>key2,_=>key3};
    public KeyCollection Keys => new KeyCollection(this);
    public readonly struct KeyCollection
    {
        readonly OwnProperties source;
        public KeyCollection(OwnProperties source) { this.source=source; }
        public Enumerator GetEnumerator() => new Enumerator(source);
    }
    public struct Enumerator : IDisposable
    {
        readonly OwnProperties source;
        readonly int version;
        int index;
        bool tailStarted;
        Dictionary<string, object>.KeyCollection.Enumerator tail;
        public string Current { get; private set; }
        public Enumerator(OwnProperties source)
        {
            this.source=source;version=source.version;index=-1;
            tailStarted=false;tail=default;Current=null;
        }
        public bool MoveNext()
        {
            if (version != source.version) throw new InvalidOperationException("Properties changed during enumeration");
            if (index+1 < source.inlineCount) { Current=source.InlineKey(++index);return true; }
            if (!tailStarted) { tailStarted=true;if (source.tail != null) tail=source.tail.Keys.GetEnumerator(); }
            if (source.tail != null && tail.MoveNext()) { Current=tail.Current;return true; }
            return false;
        }
        public void Dispose() => tail.Dispose();
    }
}
