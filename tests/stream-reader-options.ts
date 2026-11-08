export {};
async function main() {
    let trace = '';
    const modes: any[] = [undefined, 'invalid', null, 0, Symbol('mode'),
        {toString() {trace += 'string|'; return 'invalid';}},
        {toString() {trace += 'throw|'; throw new Error('mode conversion');}},
        {[Symbol.toPrimitive](hint: string) {trace += hint + '|'; return 'invalid';}},
        {toString: undefined, valueOf() {trace += 'value|'; return 'invalid';}},
        {toString() {trace += 'undefined|'; return undefined;}}
    ];
    for (let index = 0; index < modes.length; index++) {
        trace = '';
        const response = new Response('x');
        const stream = response.body!;
        const options: any = {get mode() {trace += 'get|'; return modes[index];}};
        try {
            const reader = stream.getReader(options);
            console.log('reader', index, stream.locked, trace);
            await reader.cancel(); reader.releaseLock();
            console.log('released', stream.locked, response.bodyUsed);
        } catch (error) {console.log('error', index, (error as Error).name, (error as Error).name === 'Error' ? (error as Error).message : '', stream.locked, response.bodyUsed, trace);}
    }
    for (const options of [null, undefined, {}, 1, true, 'text']) {
        const stream = new Response('x').body!;
        try {const reader = stream.getReader(options as any); console.log('dictionary', stream.locked); await reader.cancel(); reader.releaseLock();}
        catch (error) {console.log('dictionary error', (error as Error).name, stream.locked);}
    }
    const lockedStream = new Response('x').body!;
    const owner = lockedStream.getReader(); trace = '';
    try {lockedStream.getReader({mode: {toString() {trace += 'convert|'; throw new Error('locked conversion');}}} as any);}
    catch (error) {console.log('locked order', (error as Error).name, (error as Error).message, trace, lockedStream.locked);}
    await owner.cancel(); owner.releaseLock();
    const method: any = Object.getPrototypeOf(new Response('x').body!).getReader;
    trace = '';
    try {method.call({}, {get mode() {trace += 'mode|'; return undefined;}});}
    catch (error) {console.log('receiver order', (error as Error).name, trace);}

}
main().catch(error => console.log('failure', String(error)));
