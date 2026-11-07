using System;

public static partial class Js
{
    static (string Name, int CodePage) LegacyDecoderEncoding(string label) => label switch
    {
        "windows-1250" or "cp1250" or "x-cp1250" => ("windows-1250", 1250),
        "windows-1251" or "cp1251" or "x-cp1251" => ("windows-1251", 1251),
        "windows-1253" or "cp1253" or "x-cp1253" => ("windows-1253", 1253),
        "windows-1254" or "cp1254" or "x-cp1254" or "iso-8859-9" or "latin5" => ("windows-1254", 1254),
        "windows-1255" or "cp1255" or "x-cp1255" => ("windows-1255", 1255),
        "windows-1256" or "cp1256" or "x-cp1256" => ("windows-1256", 1256),
        "windows-1257" or "cp1257" or "x-cp1257" => ("windows-1257", 1257),
        "windows-1258" or "cp1258" or "x-cp1258" => ("windows-1258", 1258),
        "windows-874" or "dos-874" or "tis-620" or "iso-8859-11" => ("windows-874", 874),
        "ibm866" or "866" or "cp866" or "csibm866" => ("ibm866", 866),
        "koi8-r" or "koi8" or "cskoi8r" => ("koi8-r", 20866),
        "koi8-u" => ("koi8-u", 21866),
        "iso-8859-2" or "latin2" or "l2" => ("iso-8859-2", 28592),
        "iso-8859-3" or "latin3" or "l3" => ("iso-8859-3", 28593),
        "iso-8859-4" or "latin4" or "l4" => ("iso-8859-4", 28594),
        "iso-8859-5" or "cyrillic" => ("iso-8859-5", 28595),
        "iso-8859-6" or "arabic" => ("iso-8859-6", 28596),
        "iso-8859-7" or "greek" or "greek8" => ("iso-8859-7", 28597),
        "iso-8859-8" or "hebrew" => ("iso-8859-8", 28598),
        "iso-8859-8-i" or "logical" => ("iso-8859-8-i", 28598),
        "iso-8859-13" => ("iso-8859-13", 28603),
        "iso-8859-15" or "latin9" => ("iso-8859-15", 28605),
        "shift_jis" or "shift-jis" or "sjis" or "ms_kanji" or "windows-31j" => ("shift_jis", 932),
        "euc-jp" or "cseucpkdfmtjapanese" => ("euc-jp", 51932),
        "iso-2022-jp" or "csiso2022jp" => ("iso-2022-jp", 50220),
        "gbk" or "gb2312" or "x-gbk" or "chinese" => ("gbk", 936),
        "gb18030" => ("gb18030", 54936),
        "big5" or "big5-hkscs" or "cn-big5" => ("big5", 950),
        "euc-kr" or "ks_c_5601-1987" or "windows-949" => ("euc-kr", 949),
        _ => throw new ArgumentOutOfRangeException(null, "TextDecoder encoding is unsupported")
    };
}
