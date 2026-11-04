// The child module path permits imports of the pinned frontend's internal packages.
// Its source remains in an unmodified, ignored checkout.
module github.com/microsoft/typescript-go/tsnetbridge

go 1.27

require github.com/microsoft/typescript-go v0.0.0

require (
	github.com/go-json-experiment/json v0.0.0-20260623181947-01eb4420fa68 // indirect
	github.com/klauspost/cpuid/v2 v2.2.10 // indirect
	github.com/zeebo/xxh3 v1.1.0 // indirect
	golang.org/x/sync v0.21.0 // indirect
	golang.org/x/sys v0.46.0 // indirect
	golang.org/x/text v0.38.0 // indirect
)

replace github.com/microsoft/typescript-go => ../../../artifacts/typescript-native/tsc
