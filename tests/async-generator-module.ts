export let started = 0;
export async function* values(start: number) { started++; yield start; await Promise.resolve(); yield start + 1; }
export default async function* () { yield* values(3); }
