export function predicate(value: number, index: number, source: number[]): boolean {
    return arguments.length === 3 && value + index + source.length > 7;
}
