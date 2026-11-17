namespace Tsnet.Compiler;

internal sealed partial class IlBackend { internal HashSet<int> numeric_class_initializers; internal HashSet<int> numeric_class_types; internal Dictionary<int,List<int>> numeric_class_members; }

internal static partial class CompilerFunctions
{
    static bool il_numeric_class_initializer(int entry,Compiler c,IlBackend b)
    {
        if(b.numeric_class_initializers==null)
        {
            b.numeric_class_initializers=[];
            for(int i=1;i<=c.node_store.count();i++)
            {
                var node=get_node(i,c);
                if(node.kind!=NodeKind.ClassValue || node.fourth==0 || !il_numeric_class_eligible(get_node(node.fourth,c).type,c,b)) continue;
                if(node.left!=0)
                {
                    var function=get_node(node.left,c);
                    if(function.symbol!=0) b.numeric_class_initializers.Add(get_symbol(function.symbol,c).function);
                }
            }
        }
        return b.numeric_class_initializers.Contains(scalar_owner(entry,b.node_parents,c));
    }
    static bool il_numeric_class_eligible(int type, Compiler c, IlBackend b)
    {
        if (b.numeric_class_types == null)
        {
            b.numeric_class_types = [];
            b.numeric_class_members = [];
            var seen = new HashSet<int>();
            var ambiguous = new HashSet<int>();
            for (int i = 1; i <= c.node_store.count(); i++)
            {
                var node = get_node(i, c);
                if (node.kind != NodeKind.ClassValue || node.fourth == 0) continue;
                int candidate = get_node(node.fourth, c).type;
                if (!seen.Add(candidate)) ambiguous.Add(candidate);
                if (node.third != 0) continue;
                var prototypeNames = new List<int>();
                bool supported = true;
                for (int child=node.children;child!=0;child=get_node(child,c).next)
                {
                    var method=get_node(child,c);
                    if (method.name==0 || method.symbol!=0 || method.fourth!=0 || method.right!=0) {supported=false;break;}
                    if (!method.constant) prototypeNames.Add(method.name);
                }
                if (!supported) continue;
                var shape = get_type(candidate, c);
                if (shape.name == 0 || shape.element != 0 || shape.fields == 0) continue;
                var members = new List<int>();
                bool numeric = true;
                for (int field = shape.fields; field != 0; field = get_field(field, c).next)
                {
                    var member=get_field(field,c);
                    if(prototypeNames.Any(name=>same(name,member.name,c))) continue;
                    if(get_type(member.type,c).kind!=TypeKind.Number || members.Count==16) {numeric=false;break;}
                    members.Add(field);
                }
                if (numeric && members.Count!=0) {b.numeric_class_types.Add(candidate);b.numeric_class_members[candidate]=members;}
            }
            b.numeric_class_types.ExceptWith(ambiguous);
        }
        return b.numeric_class_types.Contains(type);
    }
    static void il_numeric_class_fields(int type,int klass,Compiler c,IlBackend b)
    {
        var fields=new List<(int Name,int Token)>();
        foreach(int member in b.numeric_class_members[type])
            fields.Add((get_field(member,c).name,b.plan.field(klass,1,-3,"number_"+fields.Count)));
        b.begin(b.plan.method1(5,klass,0xc4,-6,-4,"NumericFieldIndex"));
        for(int i=0;i<fields.Count;i++)
        {
            int next=b.label();b.emit(0xfe09,1);il_text(fields[i].Name,c,b);
            b.call(RuntimeMember.StringEqual);b.emit(0x39,next);b.emit(0x20,i);b.op(0x2a);b.mark(next);
        }
        b.emit(0x20,-1);b.op(0x2a);
        b.begin(b.plan.method1(5,klass,0xc4,-3,-6,"ReadNumericField"));
        for(int i=0;i<fields.Count;i++)
        {
            int next=b.label();b.emit(0xfe09,1);b.emit(0x20,i);b.op(0xfe01);b.emit(0x39,next);
            b.emit(0xfe09,0);b.emit(0x7b,fields[i].Token);b.op(0x2a);b.mark(next);
        }
        b.plan.number(b.method,0);b.op(0x2a);
        b.begin(b.plan.method2(5,klass,0xc4,-1,-6,-3,"WriteNumericField"));
        for(int i=0;i<fields.Count;i++)
        {
            int next=b.label();b.emit(0xfe09,1);b.emit(0x20,i);b.op(0xfe01);b.emit(0x39,next);
            b.emit(0xfe09,0);b.emit(0xfe09,2);b.emit(0x7d,fields[i].Token);b.op(0x2a);b.mark(next);
        }
        b.op(0x2a);
        b.begin(b.plan.method0(5,klass,0xc4,-1,"CopyNumericFields"));
        for(int i=0;i<fields.Count;i++)
        {b.emit(0xfe09,0);b.emit(0x20,i);il_text(fields[i].Name,c,b);b.call(RuntimeMember.NumericClassCopy);}
        b.op(0x2a);
    }
}
