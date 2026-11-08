export {};
for (const offset of [2 ** 31, 2 ** 40, Number.MAX_SAFE_INTEGER, Number.MAX_SAFE_INTEGER + 1]) {
    const buffer = new ArrayBuffer(8);
    structuredClone(buffer, {transfer: [buffer]});
    try {new DataView(buffer, offset);} catch (error) {console.log('offset', offset, (error as Error).name);}
}
for (const length of [2 ** 31, 2 ** 40, Number.MAX_SAFE_INTEGER, Number.MAX_SAFE_INTEGER + 1]) {
    const buffer = new ArrayBuffer(8);
    const converted: any = {valueOf() {structuredClone(buffer, {transfer: [buffer]}); return length;}};
    try {new DataView(buffer, 0, converted);} catch (error) {console.log('length', length, (error as Error).name);}
}
const buffer = new ArrayBuffer(8, {maxByteLength: 32});
let reads = 0;
try {new DataView(buffer, 16, {valueOf() {reads++; buffer.resize(24); return 2;}} as any);}
catch (error) {console.log('offset first', reads, buffer.byteLength, (error as Error).name);}
