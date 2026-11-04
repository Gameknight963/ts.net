import "emitter.cr";

int main(int argc, char** argv) {
    unsafe {
        if (argc != 3) { puts("Usage: tsnetc INPUT.ts OUTPUT.cs"); return 2; }
        Arena arena = Arena::create();
        Compiler compiler = {
            .arena = &arena, .path = argv[1], .token = 0, .types = 0,
            .functions = 0, .statements = 0, .symbols = 0,
            .next_type = 0, .next_symbol = 0, .scope = 0, .depth = 0,
            .loops = 0, .return_type = 0
        };
        char* source = read_source(&arena, argv[1]);
        compiler.token = lex(&compiler, source);
        parse(&compiler);
        check_program(&compiler);
        emit_program(&compiler, argv[2]);
        return 0;
    }
}
