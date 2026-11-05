import <std/vector.cr>;

#pragma crust begin_c
#include <string.h>
#include "tsbridge.h"
#pragma crust end_c

// Pointer validity and ownership are caller obligations at the C boundary.
unsafe extern int tsnet_open_file(char* path, uint64_t* handle, char** output);
unsafe extern int tsnet_inspect_binding(uint64_t handle, char* name, char** output);
unsafe extern int tsnet_close_file(uint64_t handle);
unsafe extern void tsnet_free_buffer(char* buffer);
unsafe extern int tsnet_export_ir(uint64_t handle, char** output, int* length);

struct FrontendIr {
    private char* data;
    private int length;
    private int status;
    static unsafe Self take_native(char* data, int length, int status) {
        if (length < 0 || (length != 0 && data == 0)) { abort(); }
        return (Self){.data=data,.length=length,.status=status};
    }
    int code() const { return this.status; }
    const char[]& view() const { unsafe { return slice(this.data,(size_t)this.length); } }
    ~FrontendIr() { unsafe { tsnet_free_buffer(this.data); } }
};

static size_t text_length(const char[]& text) {
    size_t length = text.length;
    if (length != 0 && text[length - 1] == 0) { length--; }
    return length;
}

bool text_equals(const char[]& left, const char[]& right) {
    size_t length = text_length(left);
    if (length != text_length(right)) { return false; }
    for (size_t i = 0; i < length; i++) {
        if (left[i] != right[i]) { return false; }
    }
    return true;
}

static bool terminated_input(const char[]& text) {
    if (text.length == 0 || text[text.length - 1] != 0) { return false; }
    for (size_t i = 0; i + 1 < text.length; i++) {
        if (text[i] == 0) { return false; }
    }
    return true;
}

struct CommandText {
    private Vector<char> bytes;

    // Copy native arguments once; callers retain no pointers into argv.
    static unsafe Self copy_native(const char* text) {
        if (text == 0) { abort(); }
        Vector<char> bytes = Vector<char>::create();
        size_t length = strlen(text);
        bytes.reserve(length + 1);
        for (size_t i = 0; i <= length; i++) { bytes.push(text[i]); }
        return (Self){ .bytes = move(bytes) };
    }

    const char[]& view() const { return this.bytes.view(); }
};

struct CommandArguments {
    private Vector<CommandText> items;

    static unsafe Self from_process(int argc, char** argv) {
        if (argc < 0 || argv == 0) { abort(); }
        Vector<CommandText> items = Vector<CommandText>::create();
        items.reserve((size_t)argc);
        for (int i = 0; i < argc; i++) { items.push(CommandText::copy_native(argv[i])); }
        return (Self){ .items = move(items) };
    }

    size_t count() const { return this.items.count(); }
    const CommandText& at(size_t index) const { return this.items.at(index); }
};

// Keep the foreign allocation, with an immutable checked view tied to its owner.
struct FrontendReport {
    private int status;
    private char* data;
    private size_t length;

    static Self empty(int status) { return (Self){ .status = status, .data = 0, .length = 0 }; }

    // data must be a unique bridge-allocated NUL-terminated buffer, or null.
    static unsafe Self take_native(int status, char* data) {
        size_t length = data == 0 ? 0 : strlen(data);
        return (Self){ .status = status, .data = data, .length = length };
    }

    int code() const { return this.status; }
    bool has_text() const { return this.length != 0; }
    const char[]& view() const { unsafe { return slice(this.data, this.length); } }

    bool contains(const char[]& needle) const {
        size_t length = text_length(needle);
        if (length > this.length) { return false; }
        const char[]& text = this.view();
        for (size_t i = 0; i <= text.length - length; i++) {
            bool matches = true;
            for (size_t j = 0; j < length; j++) {
                if (text[i + j] != needle[j]) { matches = false; break; }
            }
            if (matches) { return true; }
        }
        return false;
    }

    ~FrontendReport() { unsafe { tsnet_free_buffer(this.data); } }
};

struct FrontendSession {
    private uint64_t handle;

    static Self create() { return (Self){ .handle = 0 }; }

    FrontendReport open(const char[]& path) {
        if (this.handle != 0 || !terminated_input(path)) { return FrontendReport::empty(1); }
        unsafe {
            uint64_t handle = 0;
            char* output = 0;
            int status = tsnet_open_file((char*)&path[0], &handle, &output);
            if (status == 0 && (handle == 0 || output == 0)) { status = 5; }
            this.handle = handle;
            return FrontendReport::take_native(status, output);
        }
    }

    FrontendReport inspect(const char[]& name) const {
        if (!terminated_input(name)) { return FrontendReport::empty(1); }
        unsafe {
            char* output = 0;
            int status = tsnet_inspect_binding(this.handle, (char*)&name[0], &output);
            if (status == 0 && output == 0) { status = 5; }
            return FrontendReport::take_native(status, output);
        }
    }

    int close() {
        uint64_t handle = this.handle;
        this.handle = 0;
        if (handle == 0) { return 0; }
        unsafe { return tsnet_close_file(handle); }
    }

    FrontendIr export_ir() const {
        unsafe {
            char* data=0; int length=0;
            int status=tsnet_export_ir(this.handle,&data,&length);
            return FrontendIr::take_native(data,length,status);
        }
    }

    ~FrontendSession() { this.close(); }
};
