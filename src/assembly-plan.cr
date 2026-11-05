import "lowering.cr";

#pragma crust begin_c
#include <stdint.h>
#include <string.h>
int tsnet_emit_assembly(const char* data, int length, const char* path, char* error, int capacity);
static uint64_t tsnet_double_bits(double value) {
    uint64_t result;
    memcpy(&result, &value, sizeof(result));
    return result;
}
static uint32_t tsnet_int_bits(int value) { return (uint32_t)value; }
static char tsnet_low_byte(uint64_t value) { return (char)(value & 255); }
#pragma crust end_c
unsafe extern int tsnet_emit_assembly(const char* data, int length, const char* path, char* error, int capacity);

struct AssemblyPlan {
    private Vector<char> bytes;
    private int next;
    static Self create() {
        Self result = (Self){.bytes=Vector<char>::create(), .next=1};
        result.integer(0x314c4954);
        result.text("App");
        return move(result);
    }
    void integer(int value) {
        uint32_t bits = tsnet_int_bits(value);
        for (int i = 0; i < 4; i++) { this.bytes.push(tsnet_low_byte(bits >> (i * 8))); }
    }
    void raw_text(const char[]& value) {
        this.integer((int)value.length);
        for (size_t i = 0; i < value.length; i++) { this.bytes.push(value[i]); }
    }
    void text(const char[]& value) {
        size_t length = value.length;
        if (length != 0 && value[length - 1] == 0) { length--; }
        this.integer((int)length);
        for (size_t i = 0; i < length; i++) { this.bytes.push(value[i]); }
    }
    int command(int code) {
        int id = this.next++;
        this.integer(code); this.integer(id);
        return id;
    }
    int reference(const char[]& assembly, const char[]& space, const char[]& name) {
        int id = this.command(1);
        this.text(assembly); this.text(space); this.text(name);
        return id;
    }
    int type(const char[]& name, int parent, int attributes) {
        int id = this.command(2);
        this.integer(attributes); this.text(name); this.integer(parent);
        return id;
    }
    void implements(int owner, int target) { this.integer(3); this.integer(owner); this.integer(target); }
    int field(int owner, int attributes, int type, const char[]& name) {
        int id = this.command(4);
        this.integer(owner); this.integer(attributes); this.integer(type); this.text(name);
        return id;
    }
    int method(int command, int owner, int attributes, int result, const int[]& parameters, const char[]& name) {
        int id = this.command(command);
        this.integer(owner); this.integer(attributes); this.integer(result);
        this.integer((int)parameters.length);
        for (size_t i = 0; i < parameters.length; i++) { this.integer(parameters[i]); }
        this.text(name);
        return id;
    }
    int method0(int command, int owner, int attributes, int result, const char[]& name) {
        Vector<int> parameters = Vector<int>::create();
        return this.method(command, owner, attributes, result, parameters.view(), name);
    }
    int method1(int command, int owner, int attributes, int result, int parameter, const char[]& name) {
        int parameters[1] = {parameter};
        return this.method(command, owner, attributes, result, &parameters, name);
    }
    int method2(int command, int owner, int attributes, int result, int first, int second, const char[]& name) {
        int parameters[2] = {first, second};
        return this.method(command, owner, attributes, result, &parameters, name);
    }
    int specialize(int method, int type) {
        int id = this.command(12); this.integer(method); this.integer(type); return id;
    }
    void local(int method, int type) { this.integer(7); this.integer(method); this.integer(type); }
    void instruction(int method, int code, int operand) {
        this.integer(8); this.integer(method); this.integer(code); this.integer(operand);
    }
    void number(int method, double value) {
        this.integer(9); this.integer(method);
        uint64_t bits = tsnet_double_bits(value);
        for (int i = 0; i < 8; i++) { this.bytes.push(tsnet_low_byte(bits >> (i * 8))); }
    }
    void string(int method, const char[]& value) { this.integer(10); this.integer(method); this.text(value); }
    void raw_string(int method, const char[]& value) { this.integer(10); this.integer(method); this.raw_text(value); }
    void finish(int entry, const char[]& path) {
        this.integer(11); this.integer(entry);
        if (path.length == 0 || path[path.length - 1] != 0 || this.bytes.count() > 2147483647) {
            print("tsnetc: invalid assembly writer input"); exit(1);
        }
        const char[]& data = this.bytes.view();
        char error[1024] = "";
        int status = 0;
        unsafe { status = tsnet_emit_assembly(&data[0], (int)data.length, &path[0], &error[0], 1024); }
        if (status != 0) { print("tsnetc: assembly writer failed"); print(&error); exit(1); }
    }
};

void name_number(int number, Vector<char>& result) {
    if (number >= 10) { name_number(number / 10, result); }
    result.push((char)('0' + number % 10));
}
Vector<char> numbered_name(char prefix, int number) {
    Vector<char> result = Vector<char>::create(); result.push(prefix); name_number(number, &result); return move(result);
}
Vector<char> field_name(TextId name, const char[]& prefix, const Compiler& c) {
    Vector<char> result = Vector<char>::create();
    for (size_t i = 0; i + 1 < prefix.length; i++) { result.push(prefix[i]); }
    for (size_t i = 0; i < text_size(name, c); i++) {
        int value = tsnet_unsigned_byte(text_byte(name, i, c));
        int high = value / 16; int low = value % 16;
        result.push((char)(high < 10 ? '0' + high : 'a' + high - 10));
        result.push((char)(low < 10 ? '0' + low : 'a' + low - 10));
    }
    return move(result);
}
