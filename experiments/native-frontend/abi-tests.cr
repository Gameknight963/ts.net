import "frontend.cr";
#pragma crust begin_c
#include <stdio.h>
#pragma crust end_c

// Intentionally invalid C inputs live in tests, outside the safe wrapper API.
struct AbiTestSession {
    private uint64_t handle;
    static unsafe Self take(uint64_t handle) { return (Self){ .handle = handle }; }
    unsafe void forget() { this.handle = 0; }
    ~AbiTestSession() { unsafe { if (this.handle != 0) { tsnet_close_file(this.handle); } } }
};
struct AbiTestBuffer {
    private char* data;
    static unsafe Self take(char* data) { return (Self){ .data = data }; }
    ~AbiTestBuffer() { unsafe { tsnet_free_buffer(this.data); } }
};
unsafe int abi_lifetime_test(const char[]& path) {
    uint64_t first = 0;
    uint64_t second = 0;
    char* opening = 0;
    if (tsnet_open_file((char*)&path[0], &first, &opening) != 0) {
        if (opening != 0) { puts(opening); }
        tsnet_free_buffer(opening);
        return 1;
    }
    tsnet_free_buffer(opening);
    AbiTestSession a = AbiTestSession::take(first);
    char* next = 0;
    int opened = tsnet_open_file((char*)&path[0], &second, &next);
    AbiTestBuffer next_buffer = AbiTestBuffer::take(next);
    if (opened != 0 || second == first) { return 1; }
    AbiTestSession b = AbiTestSession::take(second);
    char binding[6] = "point";
    for (int i = 0; i < 64; i++) {
        char* text = 0;
        int status = tsnet_inspect_binding(first, &binding[0], &text);
        AbiTestBuffer buffer = AbiTestBuffer::take(text);
        if (status != 0 || text == 0 || strstr(text, "initializerType") == 0) { return 1; }
    }
    if (tsnet_close_file(first) != 0) { return 1; }
    a.forget();
    if (tsnet_close_file(first) != 4) { return 1; }
    {
        char* text = 0;
        int status = tsnet_inspect_binding(first, &binding[0], &text);
        AbiTestBuffer buffer = AbiTestBuffer::take(text);
        if (status != 4 || text == 0) { return 1; }
    }
    {
        char* text = 0;
        int status = tsnet_inspect_binding(second, &binding[0], &text);
        AbiTestBuffer buffer = AbiTestBuffer::take(text);
        if (status != 0 || text == 0) { return 1; }
    }
    {
        char missing[8] = "missing";
        char* text = 0;
        int status = tsnet_inspect_binding(second, &missing[0], &text);
        AbiTestBuffer buffer = AbiTestBuffer::take(text);
        if (status != 3 || text == 0) { return 1; }
    }
    char* empty = 0;
    uint64_t unused = 1;
    if (tsnet_open_file(0, &unused, &empty) != 1 || unused != 0 || empty != 0) { return 1; }
    if (tsnet_inspect_binding(0, 0, &empty) != 1 || empty != 0) { return 1; }
    tsnet_free_buffer(0);
    puts("PASS C ABI: repeated queries, two sessions, stale handles, missing binding, null arguments, buffer cleanup");
    return 0;
}
