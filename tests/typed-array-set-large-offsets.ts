export {};
const target = new Uint8Array(4);
for (const offset of [2 ** 31, 2 ** 40, Number.MAX_SAFE_INTEGER + 1, Infinity, -1, NaN, -0.5]) {
    for (const throwing of [false, true]) {
        let effects = '';
        const source: any = {get length() {effects += 'L'; if (throwing) throw new Error('length'); return 0;}};
        try {target.set(source, offset); console.log('ok', offset, throwing, effects);}
        catch (error) {console.log('error', offset, throwing, effects, (error as Error).name);}
    }
    try {target.set(null as any, offset);} catch (error) {console.log('null', offset, (error as Error).name);}
}
