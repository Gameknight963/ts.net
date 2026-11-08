export {};
const view = new DataView(new ArrayBuffer(8));
for (const offset of [2 ** 31, 2 ** 40, Number.MAX_SAFE_INTEGER, Number.MAX_SAFE_INTEGER + 1, Infinity, -1]) {
    for (const throwing of [false, true]) {
        let effects = '';
        const value: any = {valueOf() {effects += 'V'; if (throwing) throw new Error('conversion'); return 7;}};
        try {view.setUint8(offset, value);} catch (error) {console.log('set', offset, throwing, effects, (error as Error).name);}
    }
}
for (const offset of [NaN, -0.5, 1.9]) {view.setUint8(offset, 7); console.log('valid', offset, view.getUint8(offset));}
const buffer = new ArrayBuffer(8), detached = new DataView(buffer);
const value: any = {valueOf() {structuredClone(buffer, {transfer: [buffer]}); return 1;}};
try {detached.setUint8(2 ** 40, value);} catch (error) {console.log('detached', (error as Error).name);}
