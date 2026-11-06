/// <reference lib="es2023.array" />
export function copy(values: readonly number[]): number[] {return values.toReversed().with(-1, 9);}
export const source = [1, 2, 3];
