import "model.cr";
import "../experiments/native-frontend/frontend.cr";

struct IrReader {
    Vector<char> bytes;
    size_t position;
    static Self create(const char[]& data) {
        Vector<char> bytes=Vector<char>::create();
        for (size_t i=0;i<data.length;i++) { bytes.push(data[i]); }
        return (Self){.bytes=move(bytes),.position=0};
    }
    char byte() {
        if (this.position>=this.bytes.count()) { print("tsnetc: truncated frontend IR");exit(1); }
        return this.bytes.at(this.position++);
    }
    int integer() {
        uint32_t value=0;
        for (int i=0;i<4;i++) { value|=(uint32_t)tsnet_unsigned_byte(this.byte()) << (8*i); }
        if (value>2147483647) { print("tsnetc: invalid frontend IR integer");exit(1); }
        return (int)value;
    }
    size_t id() { return (size_t)this.integer(); }
    void text(Compiler& c) {
        size_t length=this.id(); size_t start=c.source.length();
        for (size_t i=0;i<length;i++) { c.source.append(this.byte()); }
        source_text(start,length,c);
    }
};

void import_frontend(const char[]& path, Compiler& c) {
    FrontendSession session=FrontendSession::create();
    FrontendReport report=session.open(path);
    if (report.code()!=0) { print(report.view());exit(1); }
    FrontendIr snapshot=session.export_ir();
    if (snapshot.code()!=0) { print(snapshot.view());exit(1); }
    session.close();
    IrReader r=IrReader::create(snapshot.view());
    if (r.integer()!=0x31524954) { print("tsnetc: unsupported frontend IR version");exit(1); }
    size_t count=r.id();
    for (size_t i=0;i<count;i++) { r.text(c); }
    count=r.id();c.next_type=(int)count;c.types=count==0 ? 0 : 1;
    for (size_t i=1;i<=count;i++) {
        int kind=r.integer();TextId name=r.id();FieldId fields=r.id();
        Type record={.kind=(TypeKind)kind,.name=name,.id=(int)i-1,.fields=fields,.defined=true,.next=i==count ? 0 : i+1};
        c.type_store.add(record);
    }
    count=r.id();
    for (size_t i=0;i<count;i++) {
        TextId name=r.id();TypeId type=r.id();FieldId next=r.id();
        Field record={.name=name,.type=type,.next=next};c.field_store.add(record);
    }
    count=r.id();
    for (size_t i=0;i<count;i++) {
        TextId text=r.id();int line=r.integer();int column=r.integer();
        Token record={.kind=TokenKind::Name,.text=text,.line=line,.column=column,.next=0};c.token_store.add(record);
    }
    count=r.id();
    for (size_t i=0;i<count;i++) {
        NodeId id=new_node(c);
        set_node_kind(id,(NodeKind)r.integer(),c);set_node_token(id,r.id(),c);set_node_name(id,r.id(),c);set_node_type(id,r.id(),c);
        set_node_left(id,r.id(),c);set_node_right(id,r.id(),c);set_node_third(id,r.id(),c);set_node_fourth(id,r.id(),c);
        set_node_children(id,r.id(),c);set_node_next(id,r.id(),c);set_node_symbol(id,r.id(),c);set_node_constant(id,r.integer()!=0,c);
    }
    count=r.id();c.next_symbol=(int)count;
    for (size_t i=0;i<count;i++) {
        TextId name=r.id();TypeId type=r.id();NodeId function=r.id();
        Symbol record={.name=name,.type=type,.function=function,.id=(int)i,.scope=0,.constant=false,.next=0};c.symbol_store.add(record);
    }
    c.functions=r.id();c.statements=r.id();
    if (r.position!=r.bytes.count()) { print("tsnetc: trailing frontend IR data");exit(1); }
}
