package main

import (
	"context"
	"encoding/json"
	"os"
	"path/filepath"
	"strings"

	"github.com/microsoft/typescript-go/internal/ast"
	"github.com/microsoft/typescript-go/internal/bundled"
	"github.com/microsoft/typescript-go/internal/checker"
	"github.com/microsoft/typescript-go/internal/compiler"
	"github.com/microsoft/typescript-go/internal/core"
	"github.com/microsoft/typescript-go/internal/diagnosticwriter"
	"github.com/microsoft/typescript-go/internal/locale"
	"github.com/microsoft/typescript-go/internal/tsoptions"
	"github.com/microsoft/typescript-go/internal/tspath"
	"github.com/microsoft/typescript-go/internal/vfs/osvfs"
)

const (
	ok              = 0
	invalidArgument = 1
	compilerFailure = 2
	notFound        = 3
	invalidHandle   = 4
	internalFailure = 5
)

type session struct {
	legacyDecorators bool
	program          *compiler.Program
	file             *ast.SourceFile
}

func encode(value any) string {
	data, err := json.Marshal(value)
	if err != nil {
		panic(err)
	}
	return string(data)
}
func errorReport(message string) string { return encode(map[string]string{"error": message}) }

func load(path string) (*session, string, int) { return loadWithOptions(path, false) }
func loadWithOptions(path string, legacy bool) (*session, string, int) {
	return loadProject(path, legacy, "")
}
func loadProject(path string, legacy bool, project string) (*session, string, int) {
	absolute, err := filepath.Abs(path)
	if err != nil {
		return nil, errorReport(err.Error()), invalidArgument
	}
	if info, err := os.Stat(absolute); err != nil || info.IsDir() {
		return nil, errorReport("input file does not exist or is not a file"), invalidArgument
	}
	absolute = tspath.NormalizeSlashes(absolute)
	options := &core.CompilerOptions{
		Strict: core.TSTrue, NoEmit: core.TSTrue,
		Target: core.ScriptTargetESNext, Module: core.ModuleKindPreserve,
		ModuleResolution:    core.ModuleResolutionKindBundler,
		Types:               []string{},
		ResolveJsonModule:   core.TSTrue,
		SkipDefaultLibCheck: core.TSTrue,
	}
	config := &tsoptions.ParsedCommandLine{ParsedConfig: &core.ParsedOptions{
		FileNames: []string{absolute}, CompilerOptions: options,
	}}
	directory := tspath.GetDirectoryPath(absolute)
	if project != "" {
		project, err = filepath.Abs(project)
		if err != nil {
			return nil, errorReport(err.Error()), invalidArgument
		}
		if info, statErr := os.Stat(project); statErr == nil && info.IsDir() {
			project = filepath.Join(project, "tsconfig.json")
		}
		project = tspath.NormalizeSlashes(project)
		directory = tspath.GetDirectoryPath(project)
	}
	host := compiler.NewCompilerHost(directory,
		bundled.WrapFS(osvfs.FS()), bundled.LibPath(), nil, nil)
	if project != "" {
		var diagnostics []*ast.Diagnostic
		config, diagnostics = tsoptions.GetParsedCommandLineOfConfigFile(project, nil, nil, host, nil)
		if len(diagnostics) != 0 {
			return nil, diagnosticReport(diagnostics), compilerFailure
		}
		if len(config.Errors) != 0 {
			return nil, diagnosticReport(config.Errors), compilerFailure
		}
		options = config.CompilerOptions()
	}
	// The frontend checks source; assembly output is owned by the IL backend.
	options.NoEmit = core.TSTrue
	if legacy {
		options.ExperimentalDecorators = core.TSTrue
	}
	program := compiler.NewProgram(compiler.ProgramOptions{
		Host: host, Config: config, SingleThreaded: core.TSTrue,
	})
	file := program.GetSourceFile(absolute)
	if file == nil {
		return nil, errorReport("frontend did not load the input file"), compilerFailure
	}
	ctx := context.Background()
	diagnostics := program.GetConfigFileParsingDiagnostics()
	if project != "" {
		diagnostics = append(diagnostics, program.GetProgramDiagnostics()...)
	}
	diagnostics = append(diagnostics, program.GetSyntacticDiagnostics(ctx, nil)...)
	diagnostics = append(diagnostics, program.GetGlobalDiagnostics(ctx)...)
	diagnostics = append(diagnostics, program.GetSemanticDiagnostics(ctx, nil)...)
	if len(diagnostics) != 0 {
		return nil, diagnosticReport(diagnostics), compilerFailure
	}
	return &session{program: program, file: file, legacyDecorators: options.ExperimentalDecorators.IsTrue()}, encode(map[string]any{
		"file": absolute, "sourceFiles": len(program.GetSourceFiles()), "diagnostics": []any{},
	}), ok
}

func diagnosticReport(diagnostics []*ast.Diagnostic) string {
	type diagnostic struct {
		Code       int32  `json:"code"`
		File       string `json:"file,omitempty"`
		ByteOffset int    `json:"byteOffset"`
		Message    string `json:"message"`
	}
	entries := make([]diagnostic, 0, len(diagnostics))
	for _, item := range diagnostics {
		var message strings.Builder
		diagnosticwriter.WriteFlattenedASTDiagnosticMessage(&message, item, "\n", locale.Default)
		entry := diagnostic{Code: item.Code(), ByteOffset: item.Pos(), Message: message.String()}
		if item.File() != nil {
			entry.File = item.File().FileName()
		}
		entries = append(entries, entry)
	}
	return encode(map[string]any{"diagnostics": entries})
}

func inspect(s *session, name string) (string, int) {
	// The first experiment queries top-level variable bindings, then returns both
	// the binding's type and the initializer expression's inferred type.
	var declaration *ast.Node
	for _, statement := range s.file.Statements.Nodes {
		if statement.Kind != ast.KindVariableStatement {
			continue
		}
		for _, entry := range statement.AsVariableStatement().DeclarationList.AsVariableDeclarationList().Declarations.Nodes {
			if entry.Name().Kind == ast.KindIdentifier && entry.Name().Text() == name {
				declaration = entry
				break
			}
		}
		if declaration != nil {
			break
		}
	}
	if declaration == nil {
		return errorReport("no top-level variable named " + name), notFound
	}
	c, release := s.program.GetTypeCheckerForFileExclusive(context.Background(), s.file)
	defer release()
	t := c.GetTypeAtLocation(declaration.Name())
	type property struct {
		Name string `json:"name"`
		Type string `json:"type"`
	}
	properties := make([]property, 0)
	for _, member := range c.GetPropertiesOfType(t) {
		properties = append(properties, property{Name: ast.SymbolName(member), Type: c.TypeToString(c.GetTypeOfSymbol(member))})
	}
	result := map[string]any{
		"name": name, "type": c.TypeToString(t), "typeFlags": uint32(t.Flags()),
		"flagNames": checker.FormatTypeFlags(t.Flags()), "properties": properties,
		"syntaxKind": declaration.Kind.String(), "byteStart": declaration.Pos(), "byteEnd": declaration.End(),
	}
	if initializer := declaration.AsVariableDeclaration().Initializer; initializer != nil {
		result["initializerType"] = c.TypeToString(c.GetTypeAtLocation(initializer))
		result["initializerKind"] = initializer.Kind.String()
	}
	return encode(result), ok
}
