const fs = require('node:fs');
const rows = [];
const buffer = new ArrayBuffer(8), view = new DataView(buffer);
function add(text) {
    const value = +text;
    view.setFloat64(0, value);
    rows.push(Buffer.from(text, 'utf16le').toString('hex') + '\t' + (Number.isNaN(value) ? 'nan' : view.getBigUint64(0).toString(16).padStart(16, '0')));
}
for (const text of ['', '+', '-', '.', '+.', '0', '-0', '-0.0', '0e99999', '-0e99999', 'Infinity', '+Infinity', '-Infinity', 'infinity', 'INF', 'NaN', 'nan', '0x', '0b', '0o', '+0x1', '-0b1', '0x1g', '0b2', '0o8', '1_000', '1,000', '.5', '5.', '1e', '1e+', '1e-3', '1e309', '1e-400', '2.2250738585072014e-308', '4.9406564584124654e-324']) add(text);
for (let code=0; code<=65535; code++) add(String.fromCharCode(code) + '1' + String.fromCharCode(code));
let state = 0x173a9531;
function random() { state ^= state << 13; state ^= state >>> 17; state ^= state << 5; return state >>> 0; }
for (let i=0; i<4000; i++) {
    const radix = [2,8,16][i%3], prefix = {2:'0b',8:'0o',16:'0x'}[radix];
    let digits = '';
    const length = random()%1100 + 1;
    for (let j=0; j<length; j++) digits += (random()%radix).toString(radix);
    add(prefix + digits);
    if (i%10===0) add(prefix + digits + 'z');
}
for (let i=0; i<2000; i++) add((random()%2 ? '-' : '+') + random() + '.' + random() + 'e' + ((random()%800)-400));
for (const width of [53,54,63,64,100,1023,1024]) {
    const base = 1n << BigInt(width);
    for (const offset of [-3n,-2n,-1n,0n,1n,2n,3n]) {
        for (const radix of [2,8,16]) add(({2:'0b',8:'0o',16:'0x'})[radix]+(base+offset).toString(radix));
    }
}
fs.writeFileSync(process.argv[2], rows.join('\n')+'\n');
