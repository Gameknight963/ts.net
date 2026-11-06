export function mapper(value: number, index: number, source: number[]): number[] {
    return [value, index + source.length + arguments.length];
}
