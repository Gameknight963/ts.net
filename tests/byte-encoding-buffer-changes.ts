export {};
for (const method of ['toBase64', 'setFromBase64']) {
    for (const detachedInitially of [false, true]) {
        for (const throwing of [false, true]) {
            const buffer: any = new ArrayBuffer(4);
            const array: any = new Uint8Array(buffer, 1);
            array.set([1, 2, 3]);
            if (detachedInitially) buffer.transfer();
            const trace: string[] = [];
            const options = {
                get alphabet() {
                    trace.push('alphabet');
                    if (!detachedInitially) buffer.transfer();
                    return 'base64';
                },
                get lastChunkHandling() {trace.push('last'); if (throwing) throw new Error('option'); return 'loose';},
                get omitPadding() {trace.push('padding'); if (throwing) throw new Error('option'); return false;}
            };
            try {
                const result = method === 'toBase64' ? array.toBase64(options) : array.setFromBase64('YWJj', options);
                console.log('result', method, detachedInitially, throwing, JSON.stringify(result), trace.join(','));
            } catch (error) {console.log('error', method, detachedInitially, throwing, (error as Error).name, trace.join(','));}
        }
    }
}
const detached: any = new Uint8Array(1);
(detached.buffer as any).transfer();
for (const method of ['setFromBase64', 'setFromHex']) {
    for (const text of [1, 'bad', '']) {
        try {detached[method](text);}
        catch (error) {console.log('input order', method, typeof text, (error as Error).name);}
    }
}
