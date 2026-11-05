function run() {
    switch (0) {
        case 0: return read();
        default:
            let value = 3;
            function read(): number { return value; }
    }
}
run();
