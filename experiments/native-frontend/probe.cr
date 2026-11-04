#pragma crust begin_c
#include <stdio.h>
#include <string.h>
#include "tsnet_frontend.h"
#pragma crust end_c

struct FrontendSession {
    uint64_t handle;
    ~FrontendSession() { if (this.handle != 0) { tsnet_close_file(this.handle); } }
};

struct FrontendBuffer {
    char* data;
    ~FrontendBuffer() { tsnet_free_buffer(this.data); }
};

unsafe int lifetime_test(char* path) {
    uint64_t first = 0;
    uint64_t second = 0;
    char* opening = 0;
    if (tsnet_open_file(path, &first, &opening) != 0) {
        if (opening != 0) { puts(opening); }
        tsnet_free_buffer(opening);
        return 1;
    }
    tsnet_free_buffer(opening);
    FrontendSession a = { .handle = first };
    char* next = 0;
    int opened = tsnet_open_file(path, &second, &next);
    FrontendBuffer next_buffer = { .data = next };
    if (opened != 0 || second == first) { return 1; }
    FrontendSession b = { .handle = second };
    char binding[6] = "point";
    for (int i = 0; i < 64; i++) {
        char* text = 0;
        int status = tsnet_inspect_binding(a.handle, &binding[0], &text);
        FrontendBuffer buffer = { .data = text };
        if (status != 0 || text == 0 || strstr(text, "initializerType") == 0) { return 1; }
    }
    if (tsnet_close_file(a.handle) != 0) { return 1; }
    a.handle = 0;
    if (tsnet_close_file(first) != 4) { return 1; }
    {
        char* text = 0;
        int status = tsnet_inspect_binding(first, &binding[0], &text);
        FrontendBuffer buffer = { .data = text };
        if (status != 4 || text == 0) { return 1; }
    }
    {
        char* text = 0;
        int status = tsnet_inspect_binding(b.handle, &binding[0], &text);
        FrontendBuffer buffer = { .data = text };
        if (status != 0 || text == 0) { return 1; }
    }
    {
        char missing[8] = "missing";
        char* text = 0;
        int status = tsnet_inspect_binding(b.handle, &missing[0], &text);
        FrontendBuffer buffer = { .data = text };
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

int main(int argc, char** argv) {
    unsafe {
        if (argc < 3) { puts("Usage: frontend-probe INPUT.ts BINDING..."); return 2; }
        if (strcmp(argv[1], "--self-test") == 0) { return lifetime_test(argv[2]); }
        uint64_t handle = 0;
        char* report = 0;
        int opened = tsnet_open_file(argv[1], &handle, &report);
        FrontendBuffer opening = { .data = report };
        if (report != 0) { puts(report); }
        if (opened != 0) { return 1; }
        FrontendSession session = { .handle = handle };
        int result = 0;
        for (int i = 2; i < argc; i++) {
            char* text = 0;
            int status = tsnet_inspect_binding(session.handle, argv[i], &text);
            FrontendBuffer buffer = { .data = text };
            if (text != 0) { puts(text); }
            if (status != 0) { result = 1; }
        }
        return result;
    }
}
