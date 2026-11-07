package main

import (
	"bytes"
	"context"
	"encoding/binary"
	"fmt"
	"math/big"
	"strconv"
	"strings"
	"unicode"

	"github.com/microsoft/typescript-go/internal/ast"
	"github.com/microsoft/typescript-go/internal/checker"
	"github.com/microsoft/typescript-go/internal/evaluator"
)

// Versioned, pointer-free typed IR. Display strings are never used as types.
type irType struct{ kind, name, fields, element int }
type irField struct{ name, typ, next, optional int }
type irToken struct{ text, line, column int }
type irNode struct{ kind, token, name, typ, left, right, third, fourth, children, next, symbol, constant int }
type irSymbol struct{ name, typ, function int }
type irControl struct {
	id   int
	loop bool
}
type irLabel struct {
	id        int
	statement *ast.Node
}
type irBuilder struct {
	classDecorations      map[*ast.Node]*classDecoration
	moduleAsync           map[*ast.SourceFile]bool
	moduleLoader          bool
	moduleDependencies    map[*ast.SourceFile][]*ast.SourceFile
	moduleAliases         map[*ast.SourceFile]map[string]*ast.SourceFile
	moduleRecords         map[*ast.SourceFile]int
	moduleRegistry        int
	usingFor              *ast.Node
	usingAsync            map[int]bool
	usingStack            int
	dynamicNumericReturns map[*ast.Node]bool
	awaitReturnOwner      *ast.Node
	generatorOwner        *ast.Node
	c                     *checker.Checker
	file                  *ast.SourceFile
	texts                 []string
	textIDs               map[string]int
	types                 []irType
	typeIDs               map[*checker.Type]int
	primitives            map[int]int
	contextualShapes      map[string]int
	fields                []irField
	tokens                []irToken
	nodes                 []irNode
	symbols               []irSymbol
	symbolIDs             map[*ast.Symbol]int
	functions, statements int
	controls              []irControl
	controlIDs            map[*ast.Node]int
	labels                map[string]irLabel
	deletedNames          map[string]bool
	deleteAnyName         bool
	enumValues            map[*ast.Node]evaluator.Result
	enumDeclared          map[*ast.Symbol]bool
	namespaceExports      map[*ast.Symbol]*ast.Node
	importAliases         map[*ast.Node]bool
	namespaceOwners       map[*ast.Symbol]int
	moduleNamespaces      map[*ast.Node]int
	moduleNamespaceOrder  []*ast.Symbol
	thisSymbol            int
	thisScope             bool
	newTargetUsed         bool
	argumentsUsed         bool
	superSymbol           int
	superStatic           bool
	classIDs              map[*ast.Symbol]int
	classFieldKeys        map[*ast.Node]int
	classAutoSlots        map[*ast.Node]int
	privateMembers        map[*ast.Symbol]int
	classKeySuper         int
	classKeyStatic        bool
}

func (b *irBuilder) beginControl(n *ast.Node, id int, loop bool) {
	b.controlIDs[n] = id
	b.controls = append(b.controls, irControl{id, loop})
}
func (b *irBuilder) endControl() { b.controls = b.controls[:len(b.controls)-1] }

func (b *irBuilder) text(s string) int {
	if id := b.textIDs[s]; id != 0 {
		return id
	}
	b.texts = append(b.texts, s)
	id := len(b.texts)
	b.textIDs[s] = id
	return id
}
func (b *irBuilder) typ(t *checker.Type) int {
	if t == nil {
		t = b.c.GetAnyType()
	}
	if t.Flags()&(checker.TypeFlagsESSymbolLike|checker.TypeFlagsBigIntLike) != 0 {
		t = b.c.GetAnyType()
	}
	// Enum literals retain their nominal identity in the checker, but their
	// representation is still a number or string. Mixed enum variables stay boxed.
	if t.Flags()&checker.TypeFlagsEnumLike != 0 {
		parts := []*checker.Type{t}
		if t.Flags()&checker.TypeFlagsUnion != 0 {
			parts = t.Types()
		}
		number, text := true, true
		for _, part := range parts {
			number = number && part.Flags()&checker.TypeFlagsNumberLike != 0
			text = text && part.Flags()&checker.TypeFlagsStringLike != 0
		}
		if number {
			t = b.c.GetNumberType()
		} else if text {
			t = b.c.GetStringType()
		}
	}
	if symbol := t.Symbol(); symbol != nil && strings.HasSuffix(symbol.Name, "DecoratorContext") {
		for _, declaration := range symbol.Declarations {
			if file := ast.GetSourceFileOfNode(declaration); file != nil && strings.HasPrefix(file.FileName(), "bundled:///libs/") {
				if access := b.c.GetTypeOfPropertyOfType(t, "access"); access != nil {
					b.typeIDs[access] = b.typ(b.c.GetAnyType())
				}
				break
			}
		}
	}
	if symbol := t.Symbol(); symbol != nil && (errorKind(symbol.Name) || collectionType(symbol.Name) || strings.HasSuffix(symbol.Name, "DecoratorContext") || symbol.Name == "ClassAccessorDecoratorTarget" || symbol.Name == "ClassAccessorDecoratorResult" || symbol.Name == "PropertyDescriptor" || symbol.Name == "TypedPropertyDescriptor" || symbol.Name == "IArguments" || symbol.Name == "Object" || symbol.Name == "Number" || symbol.Name == "String" || symbol.Name == "Boolean" || symbol.Name == "Function" || symbol.Name == "Console" || symbol.Name == "Performance" || symbol.Name == "Symbol" || symbol.Name == "SymbolConstructor" || symbol.Name == "BigInt" || symbol.Name == "BigIntConstructor" || symbol.Name == "ImportMeta" || symbol.Name == "DisposableStack" || symbol.Name == "DisposableStackConstructor" || symbol.Name == "AsyncDisposableStack" || symbol.Name == "AsyncDisposableStackConstructor" || binaryBuiltinName(symbol.Name) || binaryBuiltinName(strings.TrimSuffix(symbol.Name, "Constructor")) || symbol.Name == "File" || symbol.Name == "Blob" || symbol.Name == "URL" || symbol.Name == "URLSearchParams" || symbol.Name == "TextEncoder" || symbol.Name == "TextDecoder" || symbol.Name == "TextEncoderEncodeIntoResult" || symbol.Name == "ArrayBuffer" || symbol.Name == "ArrayBufferConstructor" || symbol.Name == "DataView" || symbol.Name == "DataViewConstructor" || symbol.Name == "WeakRef" || symbol.Name == "WeakRefConstructor" || symbol.Name == "TemplateStringsArray" || symbol.Name == "Date" || symbol.Name == "DateConstructor" || symbol.Name == "RegExp" || symbol.Name == "RegExpConstructor" || symbol.Name == "RegExpIndicesArray" || symbol.Name == "RegExpMatchArray" || symbol.Name == "RegExpExecArray" || symbol.Name == "Reflect" || symbol.Name == "JSON" || symbol.Name == "Math" || symbol.Name == "ArrayConstructor" || symbol.Name == "Promise" || symbol.Name == "PromiseLike" || symbol.Name == "PromiseConstructor" || symbol.Name == "PromiseWithResolvers") {
		for _, declaration := range symbol.Declarations {
			if file := ast.GetSourceFileOfNode(declaration); file != nil && strings.HasPrefix(file.FileName(), "bundled:///libs/") {
				t = b.c.GetAnyType()
				break
			}
		}
	}
	if len(b.c.GetConstructSignatures(t)) != 0 {
		t = b.c.GetAnyType()
	}
	t = b.c.GetBaseTypeOfLiteralType(t)
	if id := b.typeIDs[t]; id != 0 {
		return id
	}
	flags := t.Flags()
	kind := 4
	switch {
	case flags&checker.TypeFlagsNumberLike != 0:
		kind = 0
	case flags&checker.TypeFlagsStringLike != 0:
		kind = 1
	case flags&checker.TypeFlagsBooleanLike != 0:
		kind = 2
	case flags&checker.TypeFlagsVoid != 0:
		// A void annotation does not erase a JavaScript return value.
		kind = 4
	case flags&checker.TypeFlagsUnknown != 0:
		kind = 5
	case flags&checker.TypeFlagsObject != 0:
		kind = 6
	}
	if len(b.c.GetSignaturesOfType(t, checker.SignatureKindCall)) != 0 {
		kind = 4
	}
	if b.c.IsArrayType(t) || checker.IsTupleType(t) {
		kind = 7
	}
	// Scalar unions containing only undefined use the same nullable ABI as scalars.
	// A union also containing JS null deliberately remains object-backed.
	if flags&checker.TypeFlagsUnion != 0 && kind == 4 {
		number, boolean, undefined := true, true, false
		for _, part := range t.Types() {
			f := part.Flags()
			if f&checker.TypeFlagsUndefined != 0 {
				undefined = true
				continue
			}
			number = number && f&checker.TypeFlagsNumberLike != 0
			boolean = boolean && f&checker.TypeFlagsBooleanLike != 0
		}
		if undefined && number {
			kind = 0
		} else if undefined && boolean {
			kind = 2
		}
	}
	if kind != 6 && kind != 7 {
		if id := b.primitives[kind]; id != 0 {
			b.typeIDs[t] = id
			return id
		}
	}
	b.types = append(b.types, irType{kind: kind})
	id := len(b.types)
	b.typeIDs[t] = id
	if kind == 7 {
		element := b.typ(b.c.GetElementTypeOfArrayType(t))
		b.types[id-1].element = element
		return id
	}
	if kind != 6 {
		b.primitives[kind] = id
		return id
	}
	index := b.c.GetIndexInfoOfType(t, b.c.GetStringType())
	if index == nil {
		index = b.c.GetIndexInfoOfType(t, b.c.GetNumberType())
	}
	if index != nil {
		b.types[id-1].element = b.typ(index.ValueType())
	} else {
		infos := b.c.GetIndexInfosOfType(t)
		if len(infos) == 1 {
			b.types[id-1].element = b.typ(infos[0].ValueType())
		} else if len(infos) > 1 {
			b.types[id-1].element = b.typ(b.c.GetAnyType())
		}
	}
	if symbol := t.Symbol(); symbol != nil && symbol.Flags&ast.SymbolFlagsClass != 0 {
		if b.classIDs == nil {
			b.classIDs = map[*ast.Symbol]int{}
		}
		classID := b.classIDs[symbol]
		if classID == 0 {
			classID = len(b.classIDs) + 1
			b.classIDs[symbol] = classID
		}
		b.types[id-1].name = b.text("class:" + strconv.Itoa(classID))
	}
	last := 0
	for _, property := range b.c.GetPropertiesOfType(t) {
		// Symbol keys cannot be represented by the string-keyed runtime yet.
		if checker.IsKnownSymbol(property) {
			continue
		}
		name := b.text(ast.SymbolName(property))
		typ := b.typ(b.c.GetTypeOfSymbol(property))
		// Non-finite numeric reverse keys can overwrite these named members.
		// Keep their storage boxed even when the checker calls them numbers.
		if symbol := t.Symbol(); symbol != nil && symbol.Flags&ast.SymbolFlagsEnum != 0 {
			switch ast.SymbolName(property) {
			case "Infinity", "-Infinity", "NaN", "__proto__":
				typ = b.typ(b.c.GetAnyType())
			}
		}
		optional := 0
		if property.Flags&ast.SymbolFlagsOptional != 0 {
			optional = 1
		}
		b.fields = append(b.fields, irField{name: name, typ: typ, optional: optional})
		fid := len(b.fields)
		if last == 0 {
			b.types[id-1].fields = fid
		} else {
			b.fields[last-1].next = fid
		}
		last = fid
	}
	return id
}
func (b *irBuilder) symbol(n *ast.Node) int {
	s := b.c.GetSymbolAtLocation(n)
	// A parameter property has a class property symbol and a constructor-local
	// parameter symbol. Its declaration and bare reads must use the local binding.
	if s != nil && s.ValueDeclaration != nil && ast.IsParameterPropertyDeclaration(s.ValueDeclaration, s.ValueDeclaration.Parent) {
		if local := s.ValueDeclaration.Parent.Locals()[s.Name]; local != nil {
			s = local
		}
	}
	if n.Parent != nil && n.Parent.Kind == ast.KindShorthandPropertyAssignment {
		s = b.c.GetShorthandAssignmentValueSymbol(n.Parent)
	}
	return b.symbolFor(s, n.Text())
}

func (b *irBuilder) symbolFor(s *ast.Symbol, name string) int {
	if s == nil {
		return 0
	}
	if id := b.symbolIDs[s]; id != 0 {
		if id < 0 {
			panic("cyclic runtime value alias")
		}
		return id
	}
	if s.Flags&ast.SymbolFlagsAlias != 0 && !defaultSnapshot(s) {
		b.symbolIDs[s] = -1
		id := b.symbolFor(b.c.GetImmediateAliasedSymbol(s), name)
		b.symbolIDs[s] = id
		return id
	}
	b.symbols = append(b.symbols, irSymbol{name: b.text(name), typ: b.typ(b.c.GetTypeOfSymbol(s))})
	id := len(b.symbols)
	b.symbolIDs[s] = id
	return id
}

func (b *irBuilder) token(n *ast.Node, text string) int {
	pos := n.Pos()
	source := b.file.Text()
	if pos < 0 {
		pos = 0
	}
	if pos > len(source) {
		pos = len(source)
	}
	pos += len(source[pos:]) - len(strings.TrimLeftFunc(source[pos:], unicode.IsSpace))
	prefix := source[:pos]
	line := strings.Count(prefix, "\n") + 1
	column := pos - strings.LastIndex(prefix, "\n")
	b.tokens = append(b.tokens, irToken{b.text(text), line, column})
	return len(b.tokens)
}
func (b *irBuilder) node(kind int, n *ast.Node, text string) int {
	b.nodes = append(b.nodes, irNode{kind: kind, token: b.token(n, text)})
	return len(b.nodes)
}
func (b *irBuilder) unsupported(n *ast.Node) {
	id := b.token(n, n.Kind.String())
	location := b.tokens[id-1]
	panic(fmt.Sprintf("%s(%d,%d): unsupported lowering for %s", b.file.FileName(), location.line, location.column, n.Kind.String()))
}
func (b *irBuilder) list(nodes []*ast.Node) int {
	first, last := 0, 0
	for _, n := range nodes {
		id := b.lower(n)
		if id == 0 {
			continue
		}
		if first == 0 {
			first = id
		} else {
			b.nodes[last-1].next = id
		}
		last = id
	}
	return first
}

var operators = map[ast.Kind]string{
	ast.KindInstanceOfKeyword: "instanceof",
	ast.KindEqualsEqualsToken: "==", ast.KindExclamationEqualsToken: "!=",
	ast.KindAsteriskAsteriskToken: "**", ast.KindAsteriskAsteriskEqualsToken: "**=",
	ast.KindPlusToken: "+", ast.KindMinusToken: "-", ast.KindAsteriskToken: "*", ast.KindSlashToken: "/", ast.KindPercentToken: "%",
	ast.KindEqualsToken: "=", ast.KindPlusEqualsToken: "+=", ast.KindMinusEqualsToken: "-=", ast.KindAsteriskEqualsToken: "*=", ast.KindSlashEqualsToken: "/=", ast.KindPercentEqualsToken: "%=",
	ast.KindAmpersandAmpersandEqualsToken: "&&=", ast.KindBarBarEqualsToken: "||=", ast.KindQuestionQuestionEqualsToken: "??=",
	ast.KindEqualsEqualsEqualsToken: "===", ast.KindExclamationEqualsEqualsToken: "!==", ast.KindLessThanToken: "<", ast.KindGreaterThanToken: ">",
	ast.KindLessThanEqualsToken: "<=", ast.KindGreaterThanEqualsToken: ">=", ast.KindAmpersandAmpersandToken: "&&", ast.KindBarBarToken: "||",
	ast.KindExclamationToken: "!", ast.KindPlusPlusToken: "++", ast.KindMinusMinusToken: "--",
	ast.KindAmpersandToken: "&", ast.KindBarToken: "|", ast.KindCaretToken: "^", ast.KindTildeToken: "~",
	ast.KindLessThanLessThanToken: "<<", ast.KindGreaterThanGreaterThanToken: ">>", ast.KindGreaterThanGreaterThanGreaterThanToken: ">>>",
	ast.KindAmpersandEqualsToken: "&=", ast.KindBarEqualsToken: "|=", ast.KindCaretEqualsToken: "^=",
	ast.KindLessThanLessThanEqualsToken: "<<=", ast.KindGreaterThanGreaterThanEqualsToken: ">>=", ast.KindGreaterThanGreaterThanGreaterThanEqualsToken: ">>>=",
}

func (b *irBuilder) lower(n *ast.Node) int {
	if n == nil {
		return 0
	}
	if n.Kind == ast.KindForStatement && b.usingFor != n {
		initializer := n.AsForStatement().Initializer
		if initializer != nil && initializer.Kind == ast.KindVariableDeclarationList && ast.GetCombinedNodeFlags(initializer)&ast.NodeFlagsUsing != 0 {
			return b.lowerUsingFor(n)
		}
	}
	if chainNode(n) {
		if n.Kind == ast.KindPropertyAccessExpression || n.Kind == ast.KindElementAccessExpression {
			symbol := b.c.GetSymbolAtLocation(n.Expression())
			if symbol != nil && symbol.Flags&ast.SymbolFlagsAlias != 0 {
				symbol = b.c.GetAliasedSymbol(symbol)
			}
			if symbol != nil && symbol.Flags&ast.SymbolFlagsConstEnum != 0 {
				if value := b.c.GetConstantValue(n); value != nil {
					if id := b.enumLiteral(n, value); id != 0 {
						return id
					}
				}
			}
		}
		return b.optionalChain(n)
	}
	if n.Kind == ast.KindPropertyAccessExpression || n.Kind == ast.KindElementAccessExpression {
		if value := b.c.GetConstantValue(n); value != nil {
			if id := b.enumLiteral(n, value); id != 0 {
				return id
			}
		}
	}
	var row irNode
	id := 0
	switch n.Kind {
	case ast.KindInterfaceDeclaration, ast.KindTypeAliasDeclaration, ast.KindImportDeclaration, ast.KindExportDeclaration:
		return 0
	case ast.KindModuleDeclaration:
		return b.enumNamespace(n)
	case ast.KindImportEqualsDeclaration:
		return b.importEquals(n)
	case ast.KindEnumDeclaration:
		return b.enumDeclaration(n)
	case ast.KindExportAssignment:
		if n.AsExportAssignment().IsExportEquals {
			b.unsupported(n)
		}
		id = b.node(13, n, "default")
		row = b.nodes[id-1]
		row.name = b.text("default")
		row.symbol = b.symbolFor(n.Symbol(), "default")
		row.typ = b.symbols[row.symbol-1].typ
		row.constant = 1
		row.left = b.lower(n.Expression())
	case ast.KindClassDeclaration, ast.KindClassExpression:
		return b.classValue(n)
	case ast.KindArrowFunction, ast.KindFunctionExpression:
		return b.functionValue(n)
	case ast.KindMetaProperty:
		if n.AsMetaProperty().KeywordToken == ast.KindImportKeyword {
			return b.importMeta(n)
		}
		if n.AsMetaProperty().KeywordToken != ast.KindNewKeyword || n.Name().Text() != "target" || !b.thisScope {
			b.unsupported(n)
		}
		b.newTargetUsed = true
		id = b.node(58, n, "new.target")
		row = b.nodes[id-1]
		row.left = b.rawThis(n)
		row.constant = 1
		row.typ = b.typ(b.c.GetAnyType())
	case ast.KindThisKeyword:
		if !b.thisScope {
			if b.file.ExternalModuleIndicator == nil {
				b.unsupported(n)
			}
			id = b.node(20, n, "undefined")
			b.nodes[id-1].typ = b.typ(b.c.GetUndefinedType())
			break
		}
		id = b.node(58, n, "this")
		row = b.nodes[id-1]
		row.left = b.rawThis(n)
		row.typ = b.typ(b.c.GetAnyType())
	case ast.KindSuperKeyword:
		if b.superSymbol == 0 {
			b.unsupported(n)
		}
		id = b.node(59, n, "super")
		row = b.nodes[id-1]
		row.left = b.classReference(n, b.superSymbol)
		row.right = b.rawThis(n)
		row.typ = b.typ(b.c.GetAnyType())
		if b.superStatic {
			row.constant = 1
		}
	case ast.KindEmptyStatement:
		id = b.node(12, n, ";")
	case ast.KindParenthesizedExpression:
		return b.lower(n.Expression())
	case ast.KindBigIntLiteral:
		callee := b.node(66, n, "BigInt")
		b.nodes[callee-1].typ = b.typ(b.c.GetAnyType())
		literal := b.node(1, n, strings.ReplaceAll(strings.TrimSuffix(n.Text(), "n"), "_", ""))
		b.nodes[literal-1].typ = b.typ(b.c.GetStringType())
		id = b.node(6, n, "(")
		row = b.nodes[id-1]
		row.left = callee
		row.children = literal
		row.typ = b.typ(b.c.GetAnyType())
	case ast.KindNumericLiteral:
		text := strings.ReplaceAll(n.Text(), "_", "")
		if len(text) > 2 && text[0] == '0' && strings.ContainsAny(text[1:2], "xXbBoO") {
			integer, ok := new(big.Int).SetString(text, 0)
			if !ok {
				b.unsupported(n)
			}
			value, _ := new(big.Float).SetInt(integer).Float64()
			text = strconv.FormatFloat(value, 'g', -1, 64)
		}
		id = b.node(0, n, text)
	case ast.KindRegularExpressionLiteral:
		text := n.Text()
		split := strings.LastIndex(text, "/")
		callee := b.node(66, n, "RegExp")
		b.nodes[callee-1].typ = b.typ(b.c.GetAnyType())
		pattern := b.node(1, n, text[1:split])
		b.nodes[pattern-1].typ = b.typ(b.c.GetStringType())
		flags := b.node(1, n, text[split+1:])
		b.nodes[flags-1].typ = b.typ(b.c.GetStringType())
		b.nodes[pattern-1].next = flags
		id = b.node(54, n, "new")
		row = b.nodes[id-1]
		row.left = callee
		row.children = pattern
		row.typ = b.typ(b.c.GetAnyType())
	case ast.KindStringLiteral, ast.KindNoSubstitutionTemplateLiteral:
		id = b.node(1, n, n.Text())
	case ast.KindTaggedTemplateExpression:
		return b.taggedTemplate(n)
	case ast.KindTemplateExpression:
		t := n.AsTemplateExpression()
		result := b.node(1, t.Head, t.Head.Text())
		b.nodes[result-1].typ = b.typ(b.c.GetStringType())
		for _, part := range t.TemplateSpans.Nodes {
			span := part.AsTemplateSpan()
			value := b.lower(span.Expression)
			// Primitive scalar additions already perform the right string conversion.
			// Objects need the string hint, rather than addition's default hint.
			if b.types[b.nodes[value-1].typ-1].kind >= 4 {
				helper := b.node(66, part, "__templateText")
				b.nodes[helper-1].typ = b.typ(b.c.GetAnyType())
				call := b.node(6, part, "(")
				b.nodes[call-1].left = helper
				b.nodes[call-1].children = value
				b.nodes[call-1].typ = b.typ(b.c.GetStringType())
				value = call
			}
			add := b.node(7, part, "+")
			b.nodes[add-1].typ = b.typ(b.c.GetStringType())
			b.nodes[add-1].left = result
			b.nodes[add-1].right = value
			result = add
			literal := b.node(1, span.Literal, span.Literal.Text())
			b.nodes[literal-1].typ = b.typ(b.c.GetStringType())
			add = b.node(7, part, "+")
			b.nodes[add-1].typ = b.typ(b.c.GetStringType())
			b.nodes[add-1].left = result
			b.nodes[add-1].right = literal
			result = add
		}
		return result
	case ast.KindTrueKeyword, ast.KindFalseKeyword:
		id = b.node(2, n, map[bool]string{true: "true", false: "false"}[n.Kind == ast.KindTrueKeyword])
	case ast.KindIdentifier:
		if b.c.GetSymbolAtLocation(n) == b.c.GetArgumentsSymbol() {
			b.argumentsUsed = true
			id = b.node(69, n, "arguments")
			row = b.nodes[id-1]
			row.left = b.rawThis(n)
			row.typ = b.typ(b.c.GetAnyType())
			break
		}
		if symbol := b.c.GetSymbolAtLocation(n); symbol != nil && symbol.Flags&ast.SymbolFlagsAlias != 0 {
			if target := b.c.GetAliasedSymbol(symbol); target.IsExternalModule() {
				return b.moduleNamespaceReference(n, target)
			}
		}
		// Enum and namespace bodies capture their object like TypeScript's IIFE
		// parameter. References inside nested closures must use that same object.
		if binding := b.namespaceOwners[b.c.GetSymbolAtLocation(n)]; binding != 0 {
			id = b.node(3, n, n.Text())
			b.nodes[id-1].symbol = binding
			b.nodes[id-1].typ = b.symbols[binding-1].typ
			return id
		}
		if owner := b.namespaceExports[b.c.GetSymbolAtLocation(n)]; owner != nil && !namespaceLocalFunction(n, owner, b.c.GetSymbolAtLocation(n)) {
			return b.namespaceReference(n, owner)
		}
		if (n.Text() == "BigInt" || n.Text() == "Symbol" || n.Text() == "Iterator" || n.Text() == "DisposableStack" || n.Text() == "AsyncDisposableStack" || binaryBuiltinName(n.Text()) || n.Text() == "File" || n.Text() == "Blob" || n.Text() == "URL" || n.Text() == "URLSearchParams" || n.Text() == "TextEncoder" || n.Text() == "TextDecoder" || n.Text() == "ArrayBuffer" || n.Text() == "DataView" || n.Text() == "WeakRef" || n.Text() == "Date" || n.Text() == "RegExp" || n.Text() == "Reflect" || n.Text() == "JSON" || n.Text() == "escape" || n.Text() == "unescape" || n.Text() == "atob" || n.Text() == "btoa" || n.Text() == "encodeURI" || n.Text() == "encodeURIComponent" || n.Text() == "decodeURI" || n.Text() == "decodeURIComponent" || n.Text() == "isNaN" || n.Text() == "isFinite" || n.Text() == "parseInt" || n.Text() == "parseFloat" || n.Text() == "WeakMap" || n.Text() == "WeakSet" || n.Text() == "Map" || n.Text() == "Set" || n.Text() == "Promise" || n.Text() == "queueMicrotask" || n.Text() == "Array" || n.Text() == "Math" || n.Text() == "console" || n.Text() == "performance" || n.Text() == "Object" || n.Text() == "Number" || n.Text() == "String" || n.Text() == "Boolean") && b.isBuiltinIdentifier(n) {
			id = b.node(66, n, n.Text())
			b.nodes[id-1].typ = b.typ(b.c.GetAnyType())
			return id
		}
		if errorKind(n.Text()) && b.isBuiltinIdentifier(n) {
			id = b.node(53, n, n.Text())
			b.nodes[id-1].typ = b.typ(b.c.GetAnyType())
			return id
		}
		if symbol := b.c.GetSymbolAtLocation(n); symbol != nil && symbol.Flags&ast.SymbolFlagsEnumMember != 0 {
			if id := b.enumMemberReference(n, symbol); id != 0 {
				return id
			}
		}
		if (n.Text() == "NaN" || n.Text() == "Infinity") && b.isBuiltinIdentifier(n) {
			// These globals are numbers, not uninitialized program bindings.
			// The numeric ABI accepts C's NaN/Infinity spellings unchanged.
			if identifierWrite(n) {
				b.unsupported(n)
			}
			id = b.node(0, n, n.Text())
			break
		}
		if b.c.GetSymbolAtLocation(n) == b.c.GetUndefinedSymbol() {
			id = b.node(20, n, "undefined")
			break
		}
		id = b.node(3, n, n.Text())
		row = b.nodes[id-1]
		row.name = b.text(n.Text())
		row.symbol = b.symbol(n)
	case ast.KindNullKeyword:
		id = b.node(21, n, "null")
	case ast.KindConditionalExpression:
		expr := n.AsConditionalExpression()
		id = b.node(22, n, "?")
		row = b.nodes[id-1]
		row.left = b.lower(expr.Condition)
		row.right = b.lower(expr.WhenTrue)
		row.third = b.lower(expr.WhenFalse)
	case ast.KindDeleteExpression:
		operand := n.Expression()
		for operand.Kind == ast.KindParenthesizedExpression {
			operand = operand.Expression()
		}
		if chainNode(operand) {
			id = b.optionalChain(operand)
			row = b.nodes[id-1]
			row.constant = 1
			row.typ = b.typ(b.c.GetBooleanType())
		} else {
			id = b.node(8, n, "delete")
			row = b.nodes[id-1]
			row.left = b.lower(operand)
			if (operand.Kind == ast.KindPropertyAccessExpression || operand.Kind == ast.KindElementAccessExpression) && operand.Expression().Kind == ast.KindSuperKeyword {
				row.constant = 1
			}
		}
	case ast.KindTypeOfExpression:
		id = b.node(23, n, "typeof")
		row = b.nodes[id-1]
		row.left = b.lower(n.Expression())
	case ast.KindVoidExpression:
		id = b.node(20, n, "void")
		row = b.nodes[id-1]
		row.left = b.lower(n.Expression())
	case ast.KindObjectLiteralExpression:
		id = b.node(4, n, "{")
		row = b.nodes[id-1]
		row.children = b.list(n.AsObjectLiteralExpression().Properties.Nodes)
		for _, property := range n.AsObjectLiteralExpression().Properties.Nodes {
			if property.Kind == ast.KindGetAccessor || property.Kind == ast.KindSetAccessor || property.Kind == ast.KindPropertyAssignment && property.Name().Kind != ast.KindComputedPropertyName && property.Name().Text() == "__proto__" {
				row.typ = b.typ(b.c.GetAnyType())
				break
			}
		}
	case ast.KindArrayLiteralExpression:
		id = b.node(32, n, "[")
		row = b.nodes[id-1]
		row.children = b.list(n.AsArrayLiteralExpression().Elements.Nodes)
	case ast.KindOmittedExpression:
		id = b.node(20, n, "hole")
		row = b.nodes[id-1]
		row.constant = 1
		row.typ = b.typ(b.c.GetUndefinedType())
	case ast.KindSpreadElement, ast.KindSpreadAssignment:
		id = b.node(35, n, "...")
		row = b.nodes[id-1]
		row.left = b.lower(n.Expression())
	case ast.KindElementAccessExpression:
		if enumeration := b.objectEnumeration(n); enumeration >= 0 {
			if identifierWrite(n) {
				b.unsupported(n)
			}
			id = b.node(enumeration, n, "Object enumeration")
			row = b.nodes[id-1]
			row.typ = b.typ(b.c.GetTypeAtLocation(n))
			break
		}
		if mode := b.numberPredicate(n); mode >= 0 {
			if identifierWrite(n) {
				b.unsupported(n)
			}
			id = b.node(43+mode, n, "number predicate function")
			row = b.nodes[id-1]
			row.typ = b.typ(b.c.GetTypeAtLocation(n))
			break
		}
		expr := n.AsElementAccessExpression()
		if expr.QuestionDotToken != nil {
			b.unsupported(n)
		}
		id = b.node(33, n, "[]")
		row = b.nodes[id-1]
		row.left = b.lower(expr.Expression)
		row.right = b.lower(expr.ArgumentExpression)
	case ast.KindPropertyAssignment, ast.KindShorthandPropertyAssignment, ast.KindMethodDeclaration, ast.KindGetAccessor, ast.KindSetAccessor:
		if (n.Kind == ast.KindMethodDeclaration || n.Kind == ast.KindGetAccessor || n.Kind == ast.KindSetAccessor) && n.Body() == nil {
			return 0
		}
		name := n.Name()
		key := name
		computed := name.Kind == ast.KindComputedPropertyName
		if computed {
			key = name.Expression()
		}
		staticName, known := b.literalPropertyName(name)
		dynamic := !known
		id = b.node(5, n, "property")
		row = b.nodes[id-1]
		if n.Kind == ast.KindPropertyAssignment && !computed && staticName == "__proto__" {
			// A marker distinguishes prototype setters from ordinary named data.
			row.fourth = b.node(20, n, "prototype setter")
		}
		if dynamic {
			previousSuper, previousStatic := b.superSymbol, b.superStatic
			if n.Parent.Kind == ast.KindClassDeclaration || n.Parent.Kind == ast.KindClassExpression {
				b.superSymbol, b.superStatic = b.classKeySuper, b.classKeyStatic
			}
			row.right = b.lower(key)
			b.superSymbol, b.superStatic = previousSuper, previousStatic
			if !computed && name.Kind == ast.KindNumericLiteral {
				b.nodes[row.right-1].typ = b.typ(b.c.GetNumberType())
			}
		} else {
			row.name = b.text(staticName)
		}
		if n.Kind == ast.KindMethodDeclaration || n.Kind == ast.KindGetAccessor || n.Kind == ast.KindSetAccessor {
			row.left = b.functionValue(n)
			if n.Kind == ast.KindGetAccessor || n.Kind == ast.KindSetAccessor {
				marker := b.node(20, n, "accessor")
				b.nodes[marker-1].typ = b.typ(b.c.GetUndefinedType())
				if n.Kind == ast.KindGetAccessor {
					b.nodes[marker-1].constant = 1
				}
				row.third = marker
			}
			row.typ = b.typ(b.c.GetAnyType())
			if dynamic {
				row.constant = 1
			}
		} else if n.Kind == ast.KindPropertyAssignment {
			if dynamic && anonymousPropertyFunction(n.Initializer()) {
				row.constant = 1
			}
			row.left = b.lower(n.Initializer())
		} else {
			row.left = b.lower(n.Name())
		}
	case ast.KindPropertyAccessExpression:
		if n.Name().Kind == ast.KindPrivateIdentifier {
			return b.privateProperty(n)
		}
		if enumeration := b.objectEnumeration(n); enumeration >= 0 {
			if identifierWrite(n) {
				b.unsupported(n)
			}
			id = b.node(enumeration, n, "Object enumeration")
			row = b.nodes[id-1]
			row.typ = b.typ(b.c.GetTypeAtLocation(n))
			break
		}
		if mode := b.numberPredicate(n); mode >= 0 {
			if identifierWrite(n) {
				b.unsupported(n)
			}
			id = b.node(43+mode, n, "number predicate function")
			row = b.nodes[id-1]
			row.typ = b.typ(b.c.GetTypeAtLocation(n))
			break
		}
		if n.AsPropertyAccessExpression().QuestionDotToken != nil {
			b.unsupported(n)
		}

		if symbol := b.c.GetSymbolAtLocation(n.Expression()); symbol != nil && symbol.Flags&ast.SymbolFlagsAlias != 0 && b.c.GetAliasedSymbol(symbol).IsExternalModule() {
			member := b.symbol(n.Name())
			function := b.symbols[member-1].function
			// Primitive exports and functions that never read this need no
			// namespace receiver. Retain their direct binding access.
			if function != 0 {
				if b.nodes[function-1].left != 0 && b.nodes[function-1].fourth == 0 {
					return b.lower(n.Name())
				}
			} else {
				kind := b.types[b.typ(b.c.GetTypeAtLocation(n))-1].kind
				if kind == 0 || kind == 1 || kind == 2 {
					return b.lower(n.Name())
				}
			}
		}
		id = b.node(5, n, n.Name().Text())
		row = b.nodes[id-1]
		row.name = b.text(n.Name().Text())
		row.left = b.lower(n.Expression())
	case ast.KindNewExpression:
		expression := n.AsNewExpression()
		id = b.node(54, n, "new")
		row = b.nodes[id-1]
		row.typ = b.typ(b.c.GetTypeAtLocation(n))
		row.left = b.lower(expression.Expression)
		if expression.Arguments != nil {
			row.children = b.list(expression.Arguments.Nodes)
		}
	case ast.KindCallExpression:
		call := n.AsCallExpression()
		if call.Expression.Kind == ast.KindImportKeyword {
			return b.dynamicImport(n)
		}
		if call.Expression.Kind == ast.KindSuperKeyword {
			id = b.node(60, n, "super call")
			row = b.nodes[id-1]
			row.left = b.rawThis(n)
			row.children = b.list(call.Arguments.Nodes)
			row.typ = b.typ(b.c.GetAnyType())
			break
		}

		if mode := b.numberPredicate(call.Expression); mode >= 0 {
			id = b.node(39+mode, n, "number predicate")
			row = b.nodes[id-1]
			row.typ = b.typ(b.c.GetTypeAtLocation(n))
			row.children = b.list(call.Arguments.Nodes)
			break
		}
		id = b.node(6, n, "(")
		row = b.nodes[id-1]
		own := b.isBuiltinHasOwn(call.Expression)
		if call.Expression.Kind == ast.KindPropertyAccessExpression && call.Expression.Expression().Kind == ast.KindIdentifier &&
			(own || b.isBuiltinIdentifier(call.Expression.Expression()) && (call.Expression.Expression().Text() == "console" || call.Expression.Expression().Text() == "performance")) {
			owner := b.node(3, call.Expression.Expression(), call.Expression.Expression().Text())
			b.nodes[owner-1].name = b.text(call.Expression.Expression().Text())
			property := b.node(5, call.Expression, call.Expression.Name().Text())
			b.nodes[property-1].name = b.text(call.Expression.Name().Text())
			b.nodes[property-1].left = owner
			if own {
				b.nodes[property-1].constant = 1
			}
			row.left = property
		} else {
			row.left = b.lower(call.Expression)
		}
		row.children = b.list(call.Arguments.Nodes)
		if b.intrinsicVoidCall(row.left) {
			row.typ = b.intrinsicVoidType()
		}
	case ast.KindBinaryExpression:
		expr := n.AsBinaryExpression()
		if expr.OperatorToken.Kind == ast.KindInKeyword {
			id = b.node(67, n, "in")
			row = b.nodes[id-1]
			row.typ = b.typ(b.c.GetTypeAtLocation(n))
			if expr.Left.Kind == ast.KindPrivateIdentifier {
				row.left = b.privateReference(expr.Left)
				row.constant = 1
			} else {
				row.left = b.lower(expr.Left)
			}
			row.right = b.lower(expr.Right)
			break
		}
		if expr.OperatorToken.Kind == ast.KindCommaToken {
			id = b.node(31, n, ",")
			row = b.nodes[id-1]
			row.left = b.lower(expr.Left)
			row.right = b.lower(expr.Right)
			break
		}
		if expr.OperatorToken.Kind == ast.KindQuestionQuestionToken {
			id = b.node(30, n, "??")
			row = b.nodes[id-1]
			row.left = b.lower(expr.Left)
			row.right = b.lower(expr.Right)
			break
		}
		op := operators[expr.OperatorToken.Kind]
		if op == "" {
			b.unsupported(n)
		}
		kind := 7
		if expr.OperatorToken.Kind >= ast.KindFirstAssignment && expr.OperatorToken.Kind <= ast.KindLastAssignment {
			kind = 10
			left := b.c.GetBaseTypeOfLiteralType(b.c.GetTypeAtLocation(expr.Left)).Flags()
			right := b.c.GetBaseTypeOfLiteralType(b.c.GetTypeAtLocation(expr.Right)).Flags()
			addition := op == "+=" && (left&checker.TypeFlagsStringLike != 0 || (left|right)&checker.TypeFlagsAny != 0)
			logical := op == "&&=" || op == "||=" || op == "??="
			numeric := checker.TypeFlagsNumberLike | checker.TypeFlagsBigIntLike | checker.TypeFlagsAny
			if op != "=" && !addition && !logical && (left&numeric == 0 || right&numeric == 0) {
				b.unsupported(n)
			}
		}
		id = b.node(kind, n, op)
		row = b.nodes[id-1]
		row.left = b.lower(expr.Left)
		row.right = b.lower(expr.Right)
	case ast.KindPrefixUnaryExpression:
		expr := n.AsPrefixUnaryExpression()
		op := operators[expr.Operator]
		if op == "" {
			b.unsupported(n)
		}
		if op == "++" || op == "--" {
			if b.c.GetBaseTypeOfLiteralType(b.c.GetTypeAtLocation(expr.Operand)).Flags()&(checker.TypeFlagsNumberLike|checker.TypeFlagsBigIntLike|checker.TypeFlagsAny) == 0 {
				b.unsupported(n)
			}
			id = b.node(11, n, op)
			row = b.nodes[id-1]
			row.left = b.lower(expr.Operand)
			row.constant = 1
			break
		}
		id = b.node(8, n, op)
		row = b.nodes[id-1]
		row.left = b.lower(expr.Operand)
	case ast.KindPostfixUnaryExpression:
		expr := n.AsPostfixUnaryExpression()
		if b.c.GetBaseTypeOfLiteralType(b.c.GetTypeAtLocation(expr.Operand)).Flags()&(checker.TypeFlagsNumberLike|checker.TypeFlagsBigIntLike|checker.TypeFlagsAny) == 0 {
			b.unsupported(n)
		}
		id = b.node(11, n, operators[expr.Operator])
		row = b.nodes[id-1]
		row.left = b.lower(expr.Operand)
	case ast.KindAsExpression, ast.KindTypeAssertionExpression, ast.KindNonNullExpression, ast.KindSatisfiesExpression:
		id = b.node(9, n, "as")
		row = b.nodes[id-1]
		row.left = b.lower(n.Expression())
	case ast.KindFunctionDeclaration:
		return b.localFunction(n)
	case ast.KindDebuggerStatement:
		id = b.node(91, n, "debugger")
		row = b.nodes[id-1]
	case ast.KindBlock:
		id = b.node(12, n, "{")
		row = b.nodes[id-1]
		row.name = b.text("scope")
		row.children = b.blockStatements(n.AsBlock().Statements.Nodes)
	case ast.KindVariableStatement:
		return b.lower(n.AsVariableStatement().DeclarationList)
	case ast.KindVariableDeclarationList:
		declarations := n.AsVariableDeclarationList().Declarations.Nodes
		if len(declarations) == 1 {
			return b.lower(declarations[0])
		}
		id = b.node(12, n, "{")
		row = b.nodes[id-1]
		row.children = b.list(declarations)
	case ast.KindVariableDeclaration, ast.KindParameter:
		if n.Kind == ast.KindParameter && n.Name().Kind == ast.KindThisKeyword {
			return 0
		}
		if n.Kind == ast.KindVariableDeclaration {
			if result, ok := b.numberPredicateBindings(n); ok {
				return result
			}
		}
		if n.Kind == ast.KindVariableDeclaration && (n.Name().Kind == ast.KindArrayBindingPattern || n.Name().Kind == ast.KindObjectBindingPattern) {
			id = b.lower(n.Name())
			initializer := b.lower(n.Initializer())
			b.nodes[id-1].left = initializer
			return id
		}
		if n.Kind == ast.KindParameter && n.Name().Kind == ast.KindIdentifier && n.Name().Text() == "this" {
			return 0
		}
		rest := n.Kind == ast.KindParameter && n.AsParameterDeclaration().DotDotDotToken != nil
		pattern := n.Name().Kind == ast.KindArrayBindingPattern || n.Name().Kind == ast.KindObjectBindingPattern
		if n.Name().Kind != ast.KindIdentifier && !(n.Kind == ast.KindParameter && pattern) {
			b.unsupported(n)
		}
		name := "$parameter"
		if !pattern {
			name = n.Name().Text()
		}
		id = b.node(13, n, name)
		row = b.nodes[id-1]
		row.name = b.text(name)
		if pattern {
			b.symbols = append(b.symbols, irSymbol{name: row.name})
			row.symbol = len(b.symbols)
			row.right = b.lower(n.Name())
		} else {
			row.symbol = b.symbol(n.Name())
		}
		row.left = b.lower(n.Initializer())
		row.typ = b.typ(b.c.GetTypeAtLocation(n.Name()))
		if rest {
			row.third = b.node(32, n, "rest")
			b.nodes[row.third-1].typ = row.typ
			row.constant = 1
		}
		if n.Kind == ast.KindVariableDeclaration && ast.GetCombinedNodeFlags(n)&ast.NodeFlagsBlockScoped == 0 {
			row.fourth = b.node(1, n, "var")
			b.nodes[row.fourth-1].typ = b.typ(b.c.GetStringType())
		}
		if n.Kind == ast.KindParameter && (n.Initializer() != nil || n.AsParameterDeclaration().QuestionToken != nil) {
			if b.types[row.typ-1].kind != 0 && b.types[row.typ-1].kind != 2 {
				row.typ = b.typ(b.c.GetAnyType())
			}
			row.constant = 1
		}
		if pattern {
			row.typ = b.typ(b.c.GetAnyType())
		}
		b.symbols[row.symbol-1].typ = row.typ
		if n.Initializer() == nil && n.Kind == ast.KindVariableDeclaration {
			if b.types[row.typ-1].kind == 3 {
				b.unsupported(n)
			}
			row.left = b.node(20, n, "undefined")
			b.nodes[row.left-1].typ = b.typ(b.c.GetUndefinedType())
			b.nodes[row.left-1].constant = 1
		}
		if ast.GetCombinedNodeFlags(n)&ast.NodeFlagsConst != 0 {
			row.constant = 1
		}
	case ast.KindArrayBindingPattern, ast.KindObjectBindingPattern:
		id = b.node(36, n, "[")
		row = b.nodes[id-1]
		row.typ = b.typ(b.c.GetTypeAtLocation(n))
		if n.Kind == ast.KindObjectBindingPattern {
			row.constant = 1
		}
		row.children = b.list(n.Elements())
	case ast.KindBindingElement:
		if n.Name() == nil {
			id = b.node(20, n, "hole")
			b.nodes[id-1].typ = b.typ(b.c.GetUndefinedType())
			b.nodes[id-1].constant = 1
			return id
		}
		id = b.node(37, n, "binding")
		row = b.nodes[id-1]
		row.typ = b.typ(b.c.GetTypeAtLocation(n.Name()))
		row.left = b.lower(n.Initializer())
		if ast.GetCombinedNodeFlags(n)&ast.NodeFlagsBlockScoped == 0 {
			row.name = b.text("var")
		}
		if n.Name().Kind == ast.KindIdentifier {
			row.symbol = b.symbol(n.Name())
			b.symbols[row.symbol-1].typ = row.typ
		} else if n.Name().Kind == ast.KindArrayBindingPattern || n.Name().Kind == ast.KindObjectBindingPattern {
			row.right = b.lower(n.Name())
		} else {
			b.unsupported(n)
		}
		if n.AsBindingElement().DotDotDotToken != nil {
			row.constant = 1
		}
		if n.Parent.Kind == ast.KindObjectBindingPattern && n.AsBindingElement().DotDotDotToken == nil {
			key := n.AsBindingElement().PropertyName
			if key == nil {
				key = n.Name()
			}
			if key.Kind == ast.KindComputedPropertyName {
				row.third = b.lower(key.Expression())
			} else {
				row.third = b.node(1, key, key.Text())
				b.nodes[row.third-1].typ = b.typ(b.c.GetStringType())
			}
		}
	case ast.KindThrowStatement:
		id = b.node(50, n, "throw")
		row = b.nodes[id-1]
		row.left = b.lower(n.Expression())
	case ast.KindTryStatement:
		statement := n.AsTryStatement()
		id = b.node(51, n, "try")
		row = b.nodes[id-1]
		row.left = b.lower(statement.TryBlock)
		row.third = b.lower(statement.FinallyBlock)
		if statement.CatchClause != nil {
			clause := statement.CatchClause.AsCatchClause()
			wrapper := b.node(12, statement.CatchClause, "{")
			b.nodes[wrapper-1].name = b.text("scope")
			body := b.lower(clause.Block)
			if clause.VariableDeclaration != nil {
				binding := b.lower(clause.VariableDeclaration)
				value := b.node(52, clause.VariableDeclaration, "caught")
				b.nodes[value-1].typ = b.typ(b.c.GetAnyType())
				b.nodes[binding-1].left = value
				b.nodes[binding-1].name = b.text("let")
				b.catchBinding(binding)
				b.nodes[binding-1].next = body
				b.nodes[wrapper-1].children = binding
			} else {
				b.nodes[wrapper-1].children = body
			}
			row.right = wrapper
		}
	case ast.KindYieldExpression:
		if b.generatorOwner == nil || awaitOwner(n) != b.generatorOwner {
			b.unsupported(n)
		}
		id = b.node(rawGeneratorYield, n, "yield")
		row = b.nodes[id-1]
		row.left = b.lower(n.Expression())
		if n.AsYieldExpression().AsteriskToken != nil {
			row.constant = 1
		}
		row.typ = b.typ(b.c.GetAnyType())
	case ast.KindAwaitExpression:
		if b.awaitReturnOwner == nil || awaitOwner(n) != b.awaitReturnOwner {
			b.unsupported(n)
		}
		id = b.node(rawAsyncAwait, n, "await")
		row = b.nodes[id-1]
		row.left = b.lower(n.Expression())
		row.typ = b.typ(b.c.GetTypeAtLocation(n))
	case ast.KindReturnStatement:
		id = b.node(14, n, "return")
		row = b.nodes[id-1]
		row.left = b.asyncReturnExpression(n.Expression())
	case ast.KindIfStatement:
		s := n.AsIfStatement()
		id = b.node(15, n, "if")
		row = b.nodes[id-1]
		row.left = b.lower(s.Expression)
		row.right = b.lower(s.ThenStatement)
		row.third = b.lower(s.ElseStatement)
	case ast.KindWhileStatement:
		s := n.AsWhileStatement()
		id = b.node(16, n, "while")
		row = b.nodes[id-1]
		row.left = b.lower(s.Expression)
		b.beginControl(n, id, true)
		row.right = b.lower(s.Statement)
		b.endControl()
	case ast.KindDoStatement:
		s := n.AsDoStatement()
		id = b.node(24, n, "do")
		row = b.nodes[id-1]
		row.left = b.lower(s.Expression)
		b.beginControl(n, id, true)
		row.right = b.lower(s.Statement)
		b.endControl()
	case ast.KindBreakStatement, ast.KindContinueStatement:
		kind := 25
		var label *ast.Node
		if n.Kind == ast.KindContinueStatement {
			kind = 26
			label = n.AsContinueStatement().Label
		} else {
			label = n.AsBreakStatement().Label
		}
		id = b.node(kind, n, "jump")
		row = b.nodes[id-1]
		if label != nil {
			target := b.labels[label.Text()]
			row.left = target.id
			if kind == 26 {
				row.left = b.controlIDs[target.statement]
			}
		} else {
			for i := len(b.controls) - 1; i >= 0; i-- {
				if kind == 25 || b.controls[i].loop {
					row.left = b.controls[i].id
					break
				}
			}
		}
		if row.left == 0 {
			b.unsupported(n)
		}
	case ast.KindLabeledStatement:
		s := n.AsLabeledStatement()
		id = b.node(29, n, s.Label.Text())
		row = b.nodes[id-1]
		previous, exists := b.labels[s.Label.Text()]
		statement := s.Statement
		for statement.Kind == ast.KindLabeledStatement {
			statement = statement.AsLabeledStatement().Statement
		}
		b.labels[s.Label.Text()] = irLabel{id, statement}
		row.left = b.lower(s.Statement)
		if exists {
			b.labels[s.Label.Text()] = previous
		} else {
			delete(b.labels, s.Label.Text())
		}
	case ast.KindSwitchStatement:
		s := n.AsSwitchStatement()
		id = b.node(27, n, "switch")
		row = b.nodes[id-1]
		row.left = b.lower(s.Expression)
		b.beginControl(n, id, false)
		clauses := s.CaseBlock.AsCaseBlock().Clauses.Nodes
		declarations := []*ast.Node{}
		for _, clause := range clauses {
			for _, statement := range clause.AsCaseOrDefaultClause().Statements.Nodes {
				if statement.Kind == ast.KindFunctionDeclaration && statement.Body() != nil {
					declarations = append(declarations, statement)
				}
			}
		}
		first := b.list(declarations)
		row.children = b.list(clauses)
		b.endControl()
		if first != 0 {
			// The wrapper owns both hoisted functions and case-local bindings.
			row.constant = 1
			b.nodes[id-1] = row
			tail := first
			for b.nodes[tail-1].next != 0 {
				tail = b.nodes[tail-1].next
			}
			b.nodes[tail-1].next = id
			id = b.node(12, n, "{")
			row = b.nodes[id-1]
			row.name = b.text("scope")
			row.children = first
		}
	case ast.KindCaseClause, ast.KindDefaultClause:
		s := n.AsCaseOrDefaultClause()
		id = b.node(28, n, "case")
		row = b.nodes[id-1]
		row.left = b.lower(s.Expression)
		row.children = b.list(nonFunctionStatements(s.Statements.Nodes))
	case ast.KindForStatement:
		s := n.AsForStatement()
		id = b.node(17, n, "for")
		row = b.nodes[id-1]
		row.left = b.lower(s.Initializer)
		if s.Initializer != nil && s.Initializer.Kind != ast.KindVariableDeclarationList {
			statement := b.node(18, s.Initializer, ";")
			b.nodes[statement-1].left = row.left
			row.left = statement
		}
		row.right = b.lower(s.Condition)
		row.third = b.lower(s.Incrementor)
		b.beginControl(n, id, true)
		row.fourth = b.lower(s.Statement)
		b.endControl()
	case ast.KindForOfStatement, ast.KindForInStatement:
		s := n.AsForInOrOfStatement()
		if s.AwaitModifier != nil {
			if b.awaitReturnOwner == nil || awaitOwner(n) != b.awaitReturnOwner {
				b.unsupported(n)
			}
		}
		id = b.node(34, n, "of")
		if n.Kind == ast.KindForInStatement {
			b.nodes[id-1].kind = 68
		}
		row = b.nodes[id-1]
		if s.AwaitModifier != nil {
			row.third = b.node(20, n, "for await")
			b.nodes[row.third-1].typ = b.typ(b.c.GetAnyType())
		}
		if s.Initializer.Kind == ast.KindVariableDeclarationList {
			flags := ast.GetCombinedNodeFlags(s.Initializer)
			if flags&ast.NodeFlagsBlockScoped != 0 {
				row.constant = 1
			}
		}
		row.left = b.lower(s.Initializer)
		row.right = b.lower(s.Expression)
		b.beginControl(n, id, true)
		if s.Initializer.Kind == ast.KindVariableDeclarationList && ast.GetCombinedNodeFlags(s.Initializer)&ast.NodeFlagsUsing != 0 {
			row.fourth = b.lowerUsingIteration(n, row.left)
		} else {
			row.fourth = b.lower(s.Statement)
		}
		b.endControl()
	case ast.KindExpressionStatement:
		id = b.node(18, n, ";")
		row = b.nodes[id-1]
		row.left = b.lower(n.Expression())
	default:
		b.unsupported(n)
	}
	if row.token == 0 {
		row = b.nodes[id-1]
	}
	if row.typ == 0 && (row.kind <= 11 || row.kind >= 20 && row.kind <= 23 || row.kind >= 30 && row.kind <= 33) {
		row.typ = b.typ(b.c.GetTypeAtLocation(n))
	}
	if row.kind == 20 {
		row.typ = b.typ(b.c.GetUndefinedType())
	}
	b.registerUsingBinding(n, &row)
	b.contextualObject(n, &row)
	b.preserveDynamicType(n, &row)
	b.deletableLiteral(n, &row)

	b.nodes[id-1] = row
	return id
}
func exportIR(s *session) (result []byte, err error) {
	defer func() {
		if failure := recover(); failure != nil {
			err = fmt.Errorf("%v", failure)
		}
	}()
	c, release := s.program.GetTypeCheckerForFileExclusive(context.Background(), s.file)
	// Emit-resolver queries lock the checker themselves; discover modules before
	// holding its exclusive lock for IR construction.
	release()
	b := &irBuilder{c: c, file: s.file, textIDs: map[string]int{}, typeIDs: map[*checker.Type]int{}, primitives: map[int]int{}, symbolIDs: map[*ast.Symbol]int{}}
	b.controlIDs = map[*ast.Node]int{}
	b.labels = map[string]irLabel{}
	files := b.moduleFiles(s)
	b.prepareEnums(files)
	c, release = s.program.GetTypeCheckerForFileExclusive(context.Background(), s.file)
	defer release()
	b.c = c
	b.prepareDeletion(files)
	b.prepareEnumNamespaces(files)
	b.prepareDynamicModules(files)
	functions := map[*ast.Node]int{}
	last := 0
	for _, file := range files {
		b.file = file
		for _, n := range file.Statements.Nodes {
			if n.Kind != ast.KindFunctionDeclaration || n.Body() == nil {
				continue
			}
			name := "default"
			symbol := 0
			if n.Name() == nil {
				symbol = b.symbolFor(n.Symbol(), name)
			} else {
				name = n.Name().Text()
				symbol = b.symbol(n.Name())
			}
			id := b.node(19, n, name)
			typ := b.functionResultType(n)
			b.nodes[id-1].name = b.text(name)
			b.nodes[id-1].symbol = symbol
			b.nodes[id-1].typ = typ
			b.symbols[symbol-1].typ = typ
			b.symbols[symbol-1].function = id
			if !b.markSuspendingFunction(id, n) {
				b.markFunctionPrototype(id, n)
			}
			functions[n] = id
			if last == 0 {
				b.functions = id
			} else {
				b.nodes[last-1].next = id
			}
			last = id
		}
	}
	for _, file := range files {
		b.file = file
		for _, n := range file.Statements.Nodes {
			if id := functions[n]; id != 0 {
				children, body := b.functionParts(n, id)
				b.nodes[id-1].children = children
				b.nodes[id-1].left = body
			}
		}
	}
	last = 0
	for _, file := range files {
		b.file = file
		block := b.node(12, file.AsNode(), "module")
		b.nodes[block-1].constant = 1
		if last == 0 {
			b.statements = block
		} else {
			b.nodes[last-1].next = block
		}
		last = block
		previousAwait := b.awaitReturnOwner
		if b.moduleAsync[file] {
			b.awaitReturnOwner = file.AsNode()
		}
		previousUsing := b.usingStack
		if b.hasUsingDeclarations(file.Statements.Nodes) {
			b.usingStack = b.newUsingStack()
		}
		child := 0
		for _, n := range file.Statements.Nodes {
			if n.Kind == ast.KindFunctionDeclaration {
				continue
			}
			id := b.lower(n)
			if id == 0 {
				continue
			}
			if child == 0 {
				b.nodes[block-1].children = id
			} else {
				b.nodes[child-1].next = id
			}
			child = id
		}
		if b.usingStack != previousUsing {
			b.nodes[block-1].children = b.wrapUsing(file.AsNode(), b.usingStack, b.nodes[block-1].children)
		}
		b.usingStack = previousUsing
		b.awaitReturnOwner = previousAwait
	}
	b.initializeDynamicModules(files, s.file)
	b.initializeModuleNamespaces()
	b.dynamicNamespaces()
	b.normalizeDynamicBindings()
	var data bytes.Buffer
	put := func(v int) { _ = binary.Write(&data, binary.LittleEndian, int32(v)) }
	put(0x34524954)
	put(len(b.texts))
	for _, text := range b.texts {
		put(len(text))
		data.WriteString(text)
	}
	put(len(b.types))
	for _, t := range b.types {
		put(t.kind)
		put(t.name)
		put(t.fields)
		put(t.element)
	}
	put(len(b.fields))
	for _, f := range b.fields {
		put(f.name)
		put(f.typ)
		put(f.next)
		put(f.optional)
	}
	put(len(b.tokens))
	for _, t := range b.tokens {
		put(t.text)
		put(t.line)
		put(t.column)
	}
	put(len(b.nodes))
	for _, n := range b.nodes {
		for _, v := range []int{n.kind, n.token, n.name, n.typ, n.left, n.right, n.third, n.fourth, n.children, n.next, n.symbol, n.constant} {
			put(v)
		}
	}
	put(len(b.symbols))
	for _, symbol := range b.symbols {
		put(symbol.name)
		put(symbol.typ)
		put(symbol.function)
	}
	put(b.functions)
	put(b.statements)
	return data.Bytes(), nil
}
