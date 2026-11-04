import "frontend.cr";
import "abi-tests.cr";

static int wrapper_test(const char[]& path) {
    char binding[6] = "point";
    char initializer[16] = "initializerType";
    char numeric[7] = "number";
    FrontendSession session = FrontendSession::create();
    FrontendReport opened = session.open(path);
    if (opened.code() != 0) { return 1; }
    FrontendReport already_open = session.open(path);
    if (already_open.code() != 1) { return 1; }
    FrontendReport report = session.inspect(&binding);
    if (report.code() != 0 || !report.contains(&initializer)) { return 1; }
    if (session.close() != 0 || session.close() != 0) { return 1; }
    // Result bytes belong to the report, independently of the session.
    if (!report.contains(&numeric)) { return 1; }
    FrontendReport moved = move(report);
    if (!moved.contains(&binding)) { return 1; }
    FrontendReport closed = session.inspect(&binding);
    if (closed.code() != 4 || !closed.has_text()) { return 1; }
    char unterminated[2] = { 'x', 'y' };
    FrontendReport rejected = session.open(&unterminated);
    if (rejected.code() != 1 || rejected.has_text()) { return 1; }
    char embedded[4] = { 'x', 0, 'y', 0 };
    FrontendReport invalid = session.inspect(&embedded);
    if (invalid.code() != 1 || invalid.has_text()) { return 1; }
    const char[]& empty = invalid.view();
    if (empty.length != 0) { return 1; }
    print("PASS safe wrapper: ownership transfer, independent reports, closed sessions, bounded C strings");
    return 0;
}

static int run(const CommandArguments& arguments) {
    if (arguments.count() < 3) { print("Usage: frontend-probe INPUT.ts BINDING..."); return 2; }
    char self_test[12] = "--self-test";
    if (text_equals(arguments.at(1).view(), &self_test)) {
        if (wrapper_test(arguments.at(2).view()) != 0) { return 1; }
        unsafe { return abi_lifetime_test(arguments.at(2).view()); }
    }
    FrontendSession session = FrontendSession::create();
    FrontendReport opened = session.open(arguments.at(1).view());
    if (opened.has_text()) { print(opened.view()); }
    if (opened.code() != 0) { return 1; }
    int result = 0;
    for (size_t i = 2; i < arguments.count(); i++) {
        FrontendReport report = session.inspect(arguments.at(i).view());
        if (report.has_text()) { print(report.view()); }
        if (report.code() != 0) { result = 1; }
    }
    return result;
}

int main(int argc, char** argv) {
    unsafe {
        CommandArguments arguments = CommandArguments::from_process(argc, argv);
        return run(&arguments);
    }
}
