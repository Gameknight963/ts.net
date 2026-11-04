#pragma crust begin_c
int tsnet_write_test_assembly(const char* path);
#pragma crust end_c
unsafe extern int tsnet_write_test_assembly(const char* path);

int main() {
    unsafe { return tsnet_write_test_assembly("Generated.dll"); }
}
