export function sum(accumulator: number, value: number, index: number, source: number[]): number {
    return accumulator + value + index + source.length + arguments.length;
}
