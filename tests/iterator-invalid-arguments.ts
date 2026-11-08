/// <reference lib="esnext.iterator" />
export {};
for (const mode of ['map', 'filter', 'flatMap', 'forEach', 'reduce', 'some', 'every', 'find', 'take', 'drop']) {
    for (const failure of ['ordinary', 'return throws', 'return invalid']) {
        const receiver: any = {
            get next() { console.log('next getter'); return () => ({done: true}); },
            get return() {
                console.log('return getter');
                return function (this: any) {
                    console.log('return call', this === receiver, arguments.length);
                    if (failure === 'return throws') throw new Error('cleanup');
                    return failure === 'return invalid' ? 1 : {};
                };
            }
        };
        console.log('case', mode, failure);
        try { (Iterator.prototype as any)[mode].call(receiver, mode === 'take' || mode === 'drop' ? -1 : 42); }
        catch (error) { console.log('error', (error as Error).name); }
    }
}
for (const mode of ['take', 'drop']) {
    const receiver = {next() {return {done: true};}, return() {console.log('conversion close'); return {};}};
    try { (Iterator.prototype as any)[mode].call(receiver, {valueOf() {throw new Error('conversion');}}); }
    catch (error) {console.log(mode, (error as Error).message);}
}
