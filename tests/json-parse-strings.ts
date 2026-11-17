export {};
let checks = 0;
for (const length of [0, 1, 15, 16, 17, 31, 32, 33, 63, 64, 65, 127, 128, 1024]) {
    const plain = 'x'.repeat(length);
    for (let code = 0; code < 32; code++) {
        for (const position of [0, Math.floor(length / 2), length]) {
            const value = plain.slice(0, position) + String.fromCharCode(code) + plain.slice(position);
            if (JSON.parse(JSON.stringify(value)) !== value) throw new Error('Escaped control character changed');
            let rejected = false;
            try { JSON.parse('"' + value + '"'); }
            catch (error) { rejected = (error as Error).name === 'SyntaxError'; }
            if (!rejected) throw new Error('Raw control character accepted');
            checks++;
        }
    }
    for (const code of [34, 92, 127, 128, 255, 0x2028, 0x2029, 0xd800, 0xdc00, 0xffff]) {
        const value = plain + String.fromCharCode(code) + plain;
        if (JSON.parse(JSON.stringify(value)) !== value) throw new Error('String changed');
        checks++;
    }
    const wide = 'é界😀'.repeat(length);
    if (JSON.parse('"' + wide + '"') !== wide) throw new Error('Unicode changed');
    for (const suffix of ['', '\\', '\\u', '\\u1', '\\u12', '\\u123']) {
        let rejected = false;
        try { JSON.parse('"' + plain + suffix); }
        catch (error) { rejected = (error as Error).name === 'SyntaxError'; }
        if (!rejected) throw new Error('Unterminated string accepted');
        checks++;
    }
}
const key = 'long界'.repeat(1000), value = 'text😀'.repeat(1000);
const source = JSON.stringify({[key]: value});
const parsed = JSON.parse(source, (name: string, item: any, context?: any): any => {
    if (name === key && context.source !== JSON.stringify(value)) throw new Error('Reviver source changed');
    return item;
});
console.log('string checks', checks, parsed[key] === value);
