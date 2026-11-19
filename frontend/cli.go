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
	arguments := []string{}
	legacy := false
	project := ""
	args := os.Args[1:]
	for index := 0; index < len(args); index++ {
		argument := args[index]
		if argument == "--experimentalDecorators" {
			legacy = true
		} else if argument == "--project" || argument == "-p" {
			index++
			if project != "" || index == len(args) || len(args[index]) == 0 || args[index][0] == '-' {
				fmt.Fprintln(os.Stderr, errorReport("--project requires one config file or directory"))
				os.Exit(invalidArgument)
			}
			project = args[index]
		} else {
			arguments = append(arguments, argument)
		}
	}
	inspecting := len(arguments) > 0 && arguments[0] == "--inspect"
	if inspecting {
		arguments = arguments[1:]
	}
	if len(arguments) == 0 || !inspecting && len(arguments) != 1 {
		fmt.Fprintln(os.Stderr, "Usage: tsnet-frontend [--project TSCONFIG] [--experimentalDecorators] [--inspect] INPUT.ts [BINDING...]")
		os.Exit(invalidArgument)
	}
	session, report, status := loadProject(arguments[0], legacy, project)
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
