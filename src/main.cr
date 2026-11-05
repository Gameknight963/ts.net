import "il-emitter.cr";
import "frontend.cr";

int compile(const Arguments& arguments) {
    if (arguments.count() != 3) {
        print("Usage: tsnetc INPUT.ts OUTPUT.dll");
        return 2;
    }
    Compiler compiler = Compiler::create(arguments.at(1).view());
    import_frontend(arguments.at(1).view(), &compiler);
    emit_assembly(arguments.at(2).view(), &compiler);
    return 0;
}

int main(int argc, char **argv) {
    unsafe {
        Arguments arguments = Arguments::from_process(argc, argv);
        return compile(&arguments);
    }
}
