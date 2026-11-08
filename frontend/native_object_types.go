package main

import (
	"github.com/microsoft/typescript-go/internal/ast"
	"github.com/microsoft/typescript-go/internal/checker"
	"strings"
)

func nativeObjectType(t *checker.Type) bool {
	if t == nil {
		return false
	}
	// Anonymous records returned by native library APIs use runtime property
	// storage, just like their named interfaces.
	if symbol := t.Symbol(); symbol != nil {
		for _, declaration := range symbol.Declarations {
			if declaration.Kind == ast.KindTypeLiteral {
				if file := ast.GetSourceFileOfNode(declaration); file != nil && strings.HasPrefix(file.FileName(), "bundled:///libs/") {
					return true
				}
			}
		}
	}
	if symbol := t.Symbol(); symbol != nil && (errorKind(symbol.Name) || collectionType(symbol.Name) || strings.HasSuffix(symbol.Name, "DecoratorContext") || symbol.Name == "ClassAccessorDecoratorTarget" || symbol.Name == "ClassAccessorDecoratorResult" || symbol.Name == "PropertyDescriptor" || symbol.Name == "TypedPropertyDescriptor" || symbol.Name == "IArguments" || symbol.Name == "Object" || symbol.Name == "Number" || symbol.Name == "String" || symbol.Name == "Boolean" || symbol.Name == "Function" || symbol.Name == "FunctionConstructor" || symbol.Name == "Console" || symbol.Name == "Performance" || symbol.Name == "Symbol" || symbol.Name == "SymbolConstructor" || symbol.Name == "BigInt" || symbol.Name == "BigIntConstructor" || symbol.Name == "ImportMeta" || symbol.Name == "DisposableStack" || symbol.Name == "DisposableStackConstructor" || symbol.Name == "AsyncDisposableStack" || symbol.Name == "AsyncDisposableStackConstructor" || binaryBuiltinName(symbol.Name) || binaryBuiltinName(strings.TrimSuffix(symbol.Name, "Constructor")) || symbol.Name == "Crypto" || symbol.Name == "AesKeyAlgorithm" || symbol.Name == "JsonWebKey" || symbol.Name == "KeyAlgorithm" || symbol.Name == "HmacKeyAlgorithm" || symbol.Name == "CryptoKey" || symbol.Name == "CryptoKeyPair" || symbol.Name == "EcKeyAlgorithm" || symbol.Name == "SubtleCrypto" || symbol.Name == "DOMException" || symbol.Name == "Event" || symbol.Name == "CustomEvent" || symbol.Name == "EventTarget" || symbol.Name == "AbortController" || symbol.Name == "AbortSignal" || symbol.Name == "File" || symbol.Name == "Blob" || symbol.Name == "URL" || symbol.Name == "URLSearchParams" || symbol.Name == "Headers" || symbol.Name == "FormData" || symbol.Name == "FormDataIterator" || symbol.Name == "Response" || symbol.Name == "Request" || symbol.Name == "ReadableStreamDefaultController" || symbol.Name == "CountQueuingStrategy" || symbol.Name == "ByteLengthQueuingStrategy" || symbol.Name == "WritableStream" || symbol.Name == "WritableStreamDefaultWriter" || symbol.Name == "WritableStreamDefaultController" || symbol.Name == "ReadableStream" || symbol.Name == "ReadableStreamDefaultReader" || symbol.Name == "ReadableStreamBYOBReader" || symbol.Name == "HeadersIterator" || symbol.Name == "TextEncoder" || symbol.Name == "TextDecoder" || symbol.Name == "TextEncoderEncodeIntoResult" || symbol.Name == "SharedArrayBuffer" || symbol.Name == "SharedArrayBufferConstructor" || symbol.Name == "ArrayBuffer" || symbol.Name == "ArrayBufferConstructor" || symbol.Name == "DataView" || symbol.Name == "DataViewConstructor" || symbol.Name == "FinalizationRegistry" || symbol.Name == "FinalizationRegistryConstructor" || symbol.Name == "WeakRef" || symbol.Name == "WeakRefConstructor" || symbol.Name == "TemplateStringsArray" || symbol.Name == "Date" || symbol.Name == "DateConstructor" || symbol.Name == "RegExp" || symbol.Name == "RegExpConstructor" || symbol.Name == "RegExpIndicesArray" || symbol.Name == "RegExpMatchArray" || symbol.Name == "RegExpExecArray" || symbol.Name == "Intl" || symbol.Name == "Locale" || symbol.Name == "WeekInfo" || symbol.Name == "TextInfo" || symbol.Name == "NumberFormat" || symbol.Name == "NumberFormatPart" || symbol.Name == "NumberRangeFormatPart" || symbol.Name == "ResolvedNumberFormatOptions" || symbol.Name == "DateTimeFormat" || symbol.Name == "ResolvedDateTimeFormatOptions" || symbol.Name == "DateTimeFormatPart" || symbol.Name == "PluralRules" || symbol.Name == "ResolvedPluralRulesOptions" || symbol.Name == "RelativeTimeFormat" || symbol.Name == "ResolvedRelativeTimeFormatOptions" || symbol.Name == "ListFormat" || symbol.Name == "ResolvedListFormatOptions" || symbol.Name == "DisplayNames" || symbol.Name == "ResolvedDisplayNamesOptions" || symbol.Name == "Segmenter" || symbol.Name == "Segments" || symbol.Name == "SegmentData" || symbol.Name == "ResolvedSegmenterOptions" || symbol.Name == "SegmentIterator" || symbol.Name == "Collator" || symbol.Name == "ResolvedCollatorOptions" || symbol.Name == "Reflect" || symbol.Name == "JSON" || symbol.Name == "Atomics" || symbol.Name == "Math" || symbol.Name == "ArrayConstructor" || symbol.Name == "Promise" || symbol.Name == "PromiseLike" || symbol.Name == "PromiseConstructor" || symbol.Name == "PromiseWithResolvers") {
		for _, declaration := range symbol.Declarations {
			if file := ast.GetSourceFileOfNode(declaration); file != nil && strings.HasPrefix(file.FileName(), "bundled:///libs/") {
				return true
			}
		}
	}

	return false
}
