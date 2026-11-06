/// <reference lib="es2023.array" />
export function splice(values: readonly number[], ...items: number[]): number[] {return values.toSpliced(1, 1, ...items);}
