import "il-call-spread.cr";

void il_globals(int program, const Compiler& c, IlBackend& b) {
    Vector<int> declarations=zero_ids(c.symbol_store.count()+1);
    for (size_t i=1;i<=c.node_store.count();i++) {
        Node n=get_node(i,c);
        if ((n.kind==NodeKind::Variable || n.kind==NodeKind::Binding) && n.symbol!=0 && scalar_owner(i,&b.node_parents,c)==0) {
            declarations.at_mut(n.symbol)=(int)i;
            if ((n.kind==NodeKind::Variable && n.fourth!=0) || (n.kind==NodeKind::Binding && n.name!=0 && is_text(n.name,&text_var,c))) { b.global_vars.at_mut(n.symbol)=1; }
        }
    }
    for (size_t i=1;i<=c.node_store.count();i++) {
        Node n=get_node(i,c);
        if (n.kind!=NodeKind::Name || n.symbol==0 || declarations.at(n.symbol)==0 || scalar_owner(i,&b.node_parents,c)==0 || b.global_fields.at(n.symbol)!=0) { continue; }
        TypeId type=get_symbol(n.symbol,c).type;
        if (get_type(type,c).kind==TypeKind::Void) { fail(n.token,"void-valued globals need lowering",c); }
        Vector<char> name=numbered_name('v',(int)n.symbol);Vector<char> flag=numbered_name('i',(int)n.symbol);
        int storage=b.type(type,c);b.global_fields.at_mut(n.symbol)=b.plan.field(program,0x11,storage,name.view());
        if (b.global_vars.at(n.symbol)==0) { b.global_flags.at_mut(n.symbol)=b.plan.field(program,0x11,-2,flag.view()); }
        b.raw_slots.at_mut(n.symbol)=0;
        SymbolId root=(SymbolId)b.scalar_roots.at(n.symbol);
        if (root!=0) {
            for (size_t alias=1;alias<b.scalar_roots.count();alias++) { if (b.scalar_roots.at(alias)==(int)root) { b.scalar_roots.at_mut(alias)=0; } }
        }
    }
    bool any=false;
    for (size_t symbol=1;symbol<b.global_fields.count();symbol++) { if (b.global_fields.at(symbol)!=0) { any=true; } }
    if (!any) { return; }
    int initializer=b.plan.method0(5,program,0x1891,-1,".cctor");b.begin(initializer);
    for (size_t symbol=1;symbol<b.global_fields.count();symbol++) {
        int field=b.global_fields.at(symbol);
        if (field==0) { continue; }
        b.empty(get_symbol(symbol,c).type,c);b.emit(0x80,field);
        int flag=b.global_flags.at(symbol);if (flag!=0) { b.emit(0x20,0);b.emit(0x80,flag); }
    }
    b.op(0x2a);
}
