namespace Tsnet.Compiler;
using static Tsnet.Compiler.CompilerFunctions;

internal sealed partial class IlBackend
{
    public Vector<int> assigned_targets = new();
    public Vector<int> assigned_sources = new();
    public Vector<int> assignment_initializers = new();
}
internal static partial class CompilerFunctions
{
    internal static readonly TextBytes text_object_assign = "assign";
    internal static bool il_is_object_namespace(int entry,Compiler c,IlBackend b,System.Collections.Generic.HashSet<int> visited=null)
    {
        visited ??= new();if(!visited.Add(entry))return false;
        Node owner=get_node(entry,c);
        if(owner.kind==NodeKind.BuiltinNamespace)return is_text(get_token(owner.token,c).text,text_object_constructor,c);
        if(owner.kind==NodeKind.Assert)return il_is_object_namespace(owner.left,c,b,visited);
        if(owner.kind==NodeKind.Name && owner.symbol!=0 && b.assignment_initializers.count()>owner.symbol)
        {int initializer=b.assignment_initializers.at(owner.symbol);if(initializer!=0)return il_is_object_namespace(initializer,c,b,visited);}
        return false;
    }
    internal static bool il_is_object_assign(int entry, Compiler c, IlBackend b, System.Collections.Generic.HashSet<int> visited = null)
    {
        visited ??= new();if(!visited.Add(entry))return false;
        Node callee=get_node(entry,c);
        if(callee.kind==NodeKind.Property && is_text(callee.name,text_object_assign,c))
        {
            return il_is_object_namespace(callee.left,c,b);
        }
        if(callee.kind==NodeKind.Name && callee.symbol!=0 && b.assignment_initializers.count()>callee.symbol)
        {
            int initializer=b.assignment_initializers.at(callee.symbol);
            if(initializer!=0)return il_is_object_assign(initializer,c,b,visited);
        }
        return false;
    }
    internal static int il_assignment_function(int entry, Compiler c, IlBackend b, System.Collections.Generic.HashSet<int> visited=null)
    {
        visited ??= new();if(!visited.Add(entry))return 0;
        Node value=get_node(entry,c);
        if(value.kind==NodeKind.FunctionValue)return value.left;
        if(value.kind==NodeKind.Name && value.symbol!=0)
        {
            int function=get_symbol(value.symbol,c).function;if(function!=0)return function;
            int initializer=b.assignment_initializers.at(value.symbol);
            if(initializer!=0)return il_assignment_function(initializer,c,b,visited);
        }
        return 0;
    }
    internal static void il_assignment_target(int entry, int source, Compiler c, IlBackend b, System.Collections.Generic.HashSet<int> visited=null)
    {
        visited ??= new();if(!visited.Add(entry))return;
        Node target=get_node(entry,c);int type=target.type;
        if(get_type(type,c).kind==TypeKind.Shape && get_type(type,c).name==0)
        {
            bool seen=false;for(int i=0;i<b.assigned_targets.count();i++)if(b.assigned_targets.at(i)==type && b.assigned_sources.at(i)==source)seen=true;
            if(!seen){b.assigned_targets.push(type);b.assigned_sources.push(source);}
        }
        if(target.kind==NodeKind.Call && target.children!=0 && il_is_object_assign(target.left,c,b))
            il_assignment_target(target.children,source,c,b,visited);
        if(target.kind!=NodeKind.Name)return;
        for(int i=1;i<=c.node_store.count();i++)
        {
            Node definition=get_node(i,c);
            if(definition.kind==NodeKind.Variable && definition.symbol==target.symbol && definition.left!=0)il_assignment_target(definition.left,source,c,b,visited);
            if(definition.kind!=NodeKind.Function)continue;
            int position=0;
            for(int parameter=definition.children;parameter!=0;parameter=get_node(parameter,c).next,position++)
            {
                if(get_node(parameter,c).symbol!=target.symbol)continue;
                for(int callId=1;callId<=c.node_store.count();callId++)
                {
                    Node call=get_node(callId,c);if(call.kind!=NodeKind.Call && call.kind!=NodeKind.SavedCall)continue;
                    if(il_assignment_function(call.left,c,b)!=i)continue;
                    int argument=call.children;for(int skip=0;skip<position && argument!=0;skip++)argument=get_node(argument,c).next;
                    if(argument!=0)il_assignment_target(argument,source,c,b,visited);
                }
            }
        }
    }
    internal static void il_prepare_assignments(Compiler c, IlBackend b)
    {
        bool used=false;
        for(int i=1;i<=c.node_store.count();i++)if(get_node(i,c).kind==NodeKind.Property && is_text(get_node(i,c).name,text_object_assign,c)){used=true;break;}
        if(!used)return;
        b.assignment_initializers=zero_ids(c.symbol_store.count()+1);
        for(int i=1;i<=c.node_store.count();i++){Node definition=get_node(i,c);if(definition.kind==NodeKind.Variable && definition.symbol!=0 && definition.left!=0 && b.assignment_initializers.at(definition.symbol)==0)b.assignment_initializers.at_mut(definition.symbol)=definition.left;}
        for(int i=1;i<=c.node_store.count();i++)
        {
            Node call=get_node(i,c);if((call.kind!=NodeKind.Call && call.kind!=NodeKind.SavedCall) || call.children==0 || !il_is_object_assign(call.left,c,b))continue;
            for(int source=get_node(call.children,c).next;source!=0;source=get_node(source,c).next)
                il_assignment_target(call.children,get_node(source,c).type,c,b);
        }
    }
    // Classes for assign targets retain identity while also providing interfaces
    // for the fields that known sources can add. Added getters read the dictionary.
    internal static bool il_assignment_fits(int source, int target, Compiler c, IlBackend b)
    {
        if(get_type(target,c).kind!=TypeKind.Shape || get_type(target,c).name!=0)return false;
        bool assigned=false;for(int i=0;i<b.assigned_targets.count();i++)if(b.assigned_targets.at(i)==source)assigned=true;
        if(!assigned)return false;
        for(int member=get_type(target,c).fields;member!=0;member=get_field(member,c).next)
        {
            Field required=get_field(member,c);int actual=field(source,required.name,c);
            bool found=required.optional || actual!=0 && fits(get_field(actual,c).type,required.type,c);
            for(int i=0;!found && i<b.assigned_targets.count();i++)
            {
                if(b.assigned_targets.at(i)!=source)continue;
                int added=b.assigned_sources.at(i);TypeKind kind=get_type(added,c).kind;
                if(kind==TypeKind.Any || kind==TypeKind.Unknown){found=true;break;}
                if(kind!=TypeKind.Shape)continue;
                int property=field(added,required.name,c);
                if(property!=0){int value=get_field(property,c).type;found=fits(value,required.type,c) || get_type(value,c).kind==TypeKind.Any || get_type(value,c).kind==TypeKind.Unknown;}
            }
            if(!found)return false;
        }
        return true;
    }
}
