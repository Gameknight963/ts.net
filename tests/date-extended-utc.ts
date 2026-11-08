function show(timestamp: number): void {
    const date = new Date(timestamp);
    console.log(timestamp, date.toISOString(), date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate(), date.getUTCDay(), date.getUTCHours(), date.getUTCMinutes(), date.getUTCSeconds(), date.getUTCMilliseconds());
}
for (const timestamp of [-8640000000000000, 8640000000000000, -62167219200000, -62167219200001, -62135596800001, -62135596800000, 253402300799999, 253402300800000, 253402300800001, -1, 0]) show(timestamp);
let seed = 12345;
for (let i = 0; i < 100; i++) {
    seed = (Math.imul(seed,1664525) + 1013904223) >>> 0;
    show(Math.trunc((seed / 4294967296 * 2 - 1) * 8640000000000000));
}
for (const timestamp of [-8640000000000001, 8640000000000001, NaN, Infinity]) {
    const date = new Date(timestamp);
    try { date.toISOString(); } catch(error) { console.log('invalid', (error as Error).name, Number.isNaN(date.getUTCFullYear())); }
}
console.log('json', new Date(-62167219200000).toJSON());
