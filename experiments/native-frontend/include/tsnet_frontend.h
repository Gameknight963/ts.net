#ifndef TSNET_FRONTEND_H
#define TSNET_FRONTEND_H
#include <stdint.h>

/* Each non-null output buffer belongs to the bridge and must be released with
   tsnet_free_buffer, including buffers returned with an error status.
   Session handles are opaque, nonzero, and invalid after close.
   Status: 0 success, 1 invalid argument, 2 compiler diagnostics,
   3 binding not found, 4 invalid/closed handle, 5 internal panic. */
int tsnet_open_file(char* path, uint64_t* handle, char** output);
int tsnet_inspect_binding(uint64_t handle, char* name, char** output);
int tsnet_close_file(uint64_t handle);
void tsnet_free_buffer(char* buffer);

#endif
