package main

import (
	"fmt"
	"os"
)

func main() {
	defer func() {
		if failure := recover(); failure != nil {
			fmt.Fprintln(os.Stderr, errorReport(fmt.Sprintf("frontend panic: %v", failure)))
			os.Exit(internalFailure)
		}
	}()
	arguments := os.Args[1:]
	inspecting := len(arguments) > 0 && arguments[0] == "--inspect"
	if inspecting {
		arguments = arguments[1:]
	}
	if len(arguments) == 0 || !inspecting && len(arguments) != 1 {
		fmt.Fprintln(os.Stderr, "Usage: tsnet-frontend [--inspect] INPUT.ts [BINDING...]")
		os.Exit(invalidArgument)
	}
	session, report, status := load(arguments[0])
	if status != ok {
		fmt.Fprintln(os.Stderr, report)
		os.Exit(status)
	}
	if inspecting {
		fmt.Println(report)
		for _, name := range arguments[1:] {
			report, status = inspect(session, name)
			if status != ok {
				fmt.Fprintln(os.Stderr, report)
				os.Exit(status)
			}
			fmt.Println(report)
		}
		return
	}
	data, err := exportIR(session)
	if err != nil {
		fmt.Fprintln(os.Stderr, err)
		os.Exit(compilerFailure)
	}
	if _, err = os.Stdout.Write(data); err != nil {
		fmt.Fprintln(os.Stderr, err)
		os.Exit(internalFailure)
	}
}
