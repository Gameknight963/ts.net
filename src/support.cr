#pragma crust begin_c
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#pragma crust end_c

struct Allocation { void* data; Allocation* next; };

// One owner for the compilation. AST links and text remain stable until exit.
struct Arena {
    Allocation* head;

    static Self create() { return (Self){ .head = 0 }; }

    ~Arena() {
        unsafe {
            Allocation* item = this.head;
            while (item != 0) {
                Allocation* next = item->next;
                free(item->data);
                free(item);
                item = next;
            }
        }
    }
};

unsafe void* allocate(Arena* arena, size_t bytes) {
    void* data = calloc(1, bytes);
    Allocation* item = (Allocation*)calloc(1, sizeof(Allocation));
    if (data == 0 || item == 0) {
        puts("tsnetc: out of memory");
        exit(1);
    }
    item->data = data;
    item->next = arena->head;
    arena->head = item;
    return data;
}

unsafe char* copy_text(Arena* arena, const char* text, size_t length) {
    char* result = (char*)allocate(arena, length + 1);
    memcpy(result, text, length);
    return result;
}

unsafe bool same(const char* left, const char* right) {
    return strcmp(left, right) == 0;
}

unsafe char* read_source(Arena* arena, const char* path) {
    FILE* input = fopen(path, "rb");
    if (input == 0) { printf("tsnetc: cannot open %s\n", path); exit(1); }
    if (fseek(input, 0, SEEK_END) != 0) { fclose(input); exit(1); }
    long length = ftell(input);
    if (length < 0 || length > 4 * 1024 * 1024) {
        fclose(input); puts("tsnetc: input exceeds 4 MiB limit"); exit(1);
    }
    rewind(input);
    char* text = (char*)allocate(arena, (size_t)length + 1);
    size_t count = fread(text, 1, (size_t)length, input);
    bool failed = ferror(input) != 0;
    fclose(input);
    if (failed || count != (size_t)length) { puts("tsnetc: input read failed"); exit(1); }
    if (memchr(text, 0, count) != 0) { puts("tsnetc: NUL in source"); exit(1); }
    return text;
}
