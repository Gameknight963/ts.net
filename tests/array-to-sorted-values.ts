/// <reference lib="es2023.array" />
export function ascending(a: number, b: number): number {return a - b;}
export function sorted(values: readonly number[]): number[] {return values.toSorted(ascending);}
