export let started = 0;
export function* values(start: number): Generator<number, number, unknown> {
    started++;
    yield start;
    yield start + 1;
    return start + 2;
}
export default function* () { yield* values(3); }
