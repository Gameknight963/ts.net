/// <reference lib="es2024.arraybuffer" />
export {};
for (const offset of [8, 16, 2 ** 31, 2 ** 40]) {
    for (const throwing of [false, true]) {
        let effects = '';
        const length: any = {valueOf() {effects += 'L'; if (throwing) throw new Error('length'); return 0;}};
        try {const result = new Uint8Array(new ArrayBuffer(8), offset, length); console.log('ok', offset, effects, result.length);}
        catch (error) {console.log('error', offset, effects, (error as Error).name);}
    }
}
const buffer = new ArrayBuffer(8, {maxByteLength: 32});
const length: any = {valueOf() {buffer.resize(24); return 2;}};
try {const result = new Uint8Array(buffer, 16, length); console.log('grown', result.byteOffset, result.length, buffer.byteLength);}
catch (error) {console.log('grown error', (error as Error).name);}
const detached = new ArrayBuffer(8);
structuredClone(detached, {transfer: [detached]});
let reads = 0;
try {new Uint8Array(detached, 0, {valueOf() {reads++; throw new Error('length');}} as any);}
catch (error) {console.log('detached', reads, (error as Error).name);}
