import "../../crust/lib/vector.cr";

#pragma crust begin_c
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
static FILE *tsnet_console(void) {
    return stdout;
}
#pragma crust end_c

// Native pointer validity is established by the owners below, not by C's types.
unsafe extern FILE* tsnet_console();
unsafe extern FILE* fopen(const char* path, const char* mode);
unsafe extern size_t fread(void* data, size_t size, size_t count, FILE* file);
unsafe extern int ferror(FILE* file);
unsafe extern int fclose(FILE* file);
unsafe extern int fputc(int value, FILE* file);
unsafe extern int fprintf(FILE* file, const char* format, ...);
unsafe extern int puts(const char* text);
unsafe extern size_t strlen(const char* text);
unsafe extern double strtod(const char* text, char** end);

bool c_string_valid(const char[]& text) {
    if (text.length == 0 || text[text.length - 1] != 0) {
        return false;
    }
    for (size_t i = 0; i + 1 < text.length; i++) {
        if (text[i] == 0) {
            return false;
        }
    }
    return true;
}

struct Argument {
    private Vector<char> bytes;
    static unsafe Self copy_native(const char *text) {
        if (text == 0) {
            abort();
        }
        Vector<char> bytes = Vector<char>::create();
        size_t length = strlen(text);
        bytes.reserve(length + 1);
        for (size_t i = 0; i <= length; i++) {
            bytes.push(text[i]);
        }
        return (Self){.bytes = move(bytes)};
    }
    const char[]& view() const { return this.bytes.view(); }
};

struct Arguments {
    private Vector<Argument> items;
    static unsafe Self from_process(int argc, char **argv) {
        if (argc < 0 || argv == 0) {
            abort();
        }
        Vector<Argument> items = Vector<Argument>::create();
        for (int i = 0; i < argc; i++) {
            items.push(Argument::copy_native(argv[i]));
        }
        return (Self){.items = move(items)};
    }
    size_t count() const { return this.items.count(); }
    const Argument& at(size_t index) const { return this.items.at(index); }
};

struct Output {
    private FILE *file;
    private bool owned;

    static Self console() {
        unsafe {
            return (Self){.file = tsnet_console(), .owned = false};
        }
    }
    static Self open(const char[]& path) {
        if (!c_string_valid(path)) {
            print("tsnetc: invalid output path");
            exit(1);
        }
        unsafe {
            FILE *file = fopen(&path[0], "wb");
            if (file == 0) {
                puts("tsnetc: cannot open output");
                exit(1);
            }
            return (Self){.file = file, .owned = true};
        }
    }
    void put(char value) {
        unsafe {
            if (this.file == 0) {
                abort();
            }
            fputc(value, this.file);
        }
    }
    void integer(int value) {
        unsafe {
            if (this.file == 0) {
                abort();
            }
            fprintf(this.file, "%d", value);
        }
    }
    void number(double value) {
        unsafe {
            if (this.file == 0) {
                abort();
            }
            fprintf(this.file, "%.17g", value);
        }
    }
    void hex(char value) {
        unsigned int byte = (unsigned int)((int)value & 255);
        unsafe {
            if (this.file == 0) {
                abort();
            }
            fprintf(this.file, "%02x", byte);
        }
    }
    void bytes(const char[]& text) {
        for (size_t i = 0; i < text.length; i++) {
            if (text[i] != 0) {
                this.put(text[i]);
            }
        }
    }
    bool finish() {
        unsafe {
            if (this.file == 0) {
                return false;
            }
            bool success = ferror(this.file) == 0;
            if (this.owned) {
                if (fclose(this.file) != 0) {
                    success = false;
                }
                this.file = 0;
            }
            return success;
        }
    }
    ~Output() {
        unsafe {
            if (this.owned && this.file != 0) {
                fclose(this.file);
            }
        }
    }
};

struct Source {
    private Vector<char> bytes;
    static Self read(const char[]& path) {
        if (!c_string_valid(path)) {
            print("tsnetc: invalid input path");
            exit(1);
        }
        Vector<char> bytes = Vector<char>::create();
        Input input = Input::open(path);
        char block[4096] = "";
        bool finished = false;
        while (!finished) {
            size_t count = input.read(&block);
            if (count < sizeof(block)) {
                finished = true;
            }
            for (size_t i = 0; i < count; i++) {
                if (block[i] == 0 || bytes.count() >= 4 * 1024 * 1024) {
                    print("tsnetc: NUL in source or input exceeds 4 MiB limit");
                    exit(1);
                }
                bytes.push(block[i]);
            }
        }
        if (input.failed()) {
            print("tsnetc: input read failed");
            exit(1);
        }
        return (Self){.bytes = move(bytes)};
    }
    size_t length() const { return this.bytes.count(); }
    char peek(size_t index) const {
        if (index >= this.bytes.count()) {
            return 0;
        }
        return this.bytes.at(index);
    }
};

struct Input {
    private FILE *file;
    static Self open(const char[]& path) {
        if (!c_string_valid(path)) {
            print("tsnetc: invalid input path");
            exit(1);
        }
        unsafe {
            FILE *file = fopen(&path[0], "rb");
            if (file == 0) {
                puts("tsnetc: cannot open input");
                exit(1);
            }
            return (Self){.file = file};
        }
    }
    size_t read(char[]& destination) {
        if (destination.length == 0) {
            return 0;
        }
        unsafe {
            return fread(&destination[0], 1, destination.length, this.file);
        }
    }
    bool failed() const {
        unsafe {
            return ferror(this.file) != 0;
        }
    }
    ~Input() {
        unsafe {
            fclose(this.file);
        }
    }
};

double native_number(const char[]& text) {
    if (!c_string_valid(text)) {
        print("tsnetc: invalid numeric text");
        exit(1);
    }
    unsafe {
        return strtod(&text[0], 0);
    }
}
