export function transform(value: number, index: number, source: number[]): number {
    return value + index + source.length + arguments.length;
}
export function positive(value: number): boolean { return value > 0; }
