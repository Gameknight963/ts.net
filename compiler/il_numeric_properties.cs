namespace Tsnet.Compiler;
using static Tsnet.Compiler.CompilerFunctions;
internal static partial class CompilerFunctions
{
    internal static bool il_dynamic_index_name(int name, Compiler c)
    {
        int length=text_size(name,c);if(length==0 || length>16 || length>1 && text_byte(name,0,c)=='0')return false;
        long value=0;for(int i=0;i<length;i++){byte ch=text_byte(name,i,c);if(ch<'0'||ch>'9')return false;value=value*10+ch-'0';}
        return value<=9007199254740991L;
    }
    internal static bool il_dynamic_index_field(int member, Compiler c) => il_dynamic_index_name(get_field(member,c).name,c);
    internal static bool il_has_dynamic_indices(int type, Compiler c)
    {
        for(int member=get_type(type,c).fields;member!=0;member=get_field(member,c).next)if(il_dynamic_index_field(member,c))return true;
        return false;
    }
}
