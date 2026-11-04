// Match the Native AOT release runtime's CRT and linker requirements.
#pragma comment(linker, "/nodefaultlib:libucrt.lib")
#pragma comment(linker, "/defaultlib:ucrt.lib")
#pragma comment(linker, "/merge:.managedcode=.text")
#pragma comment(linker, "/merge:hydrated=.bss")
