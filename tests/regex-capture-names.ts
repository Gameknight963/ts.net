export {};
for (const name of ['$value', '_name', 'caf\\u00e9', '\\u{10400}', '\\u0061', 'a\\u200c', 'a\\u30fb', 'a\\uff65', 'a\\u0301', '\\u005f', '\\uD800\\uDC00']) {
    const expression = new RegExp('(?<' + name + '>a)(b)\\k<' + name + '>', 'd');
    const match = expression.exec('aba')!;
    const key = Object.keys(match.groups!)[0];
    console.log('capture', key, match.join('|'), match.groups![key], match.indices!.groups![key] === match.indices![1]);
    console.log('replace', 'aba'.replace(expression, '$<' + key + '>:$2'));
    'aba'.replace(expression, (...args: any[]) => {console.log('callback', args[1], args[2], args[5][key]); return '';});
}
for (const name of ['', '1name', 'a-b', 'a b', '\\u30fb', '\\uff65', '\\u200c', '\\u0301', '\\uD800', '\\u002d', '\\u{}', '\\u{110000}']) {
    try {new RegExp('(?<' + name + '>a)'); console.log('unexpected');}
    catch (error) {console.log('invalid', (error as Error).name);}
}

console.log('literal', /(?<$value>a)\k<$value>/.exec('aa')!.groups!.$value);
console.log('escaped forward', new RegExp('\\k<a>(?<\\u0061>a)').test('a'));
const collision = /(?<$value>a)(?<tsnetNamed002400760061006C00750065>b)/.exec('ab')!;
console.log('collision', collision.groups!.$value, collision.groups!.tsnetNamed002400760061006C00750065, collision.join('|'));
const recompiled: any = /(?<old>a)/;
recompiled.compile('(?<$new>b)', 'd');
const updated = recompiled.exec('b');
console.log('recompiled', Object.keys(updated.groups).join(','), updated.groups.$new, updated.indices.groups.$new === updated.indices[1]);
const cloned = structuredClone(recompiled);
console.log('cloned', cloned.exec('b').groups.$new);

for (const flags of ['d', 'du']) {
    for (const name of ['\u{10940}', '\\u{10940}', '\u{1E6C0}', 'a\u00B7', 'a\u200C', 'a\u200D', 'a\u30FB']) {
        const expression = new RegExp('(?<' + name + '>a)\\k<' + name + '>', flags);
        const match = expression.exec('aa')!;
        const key = Object.keys(match.groups!)[0];
        console.log('identifier name', flags, key, match.groups![key], match.indices!.groups![key] === match.indices![1]);
    }
    for (const name of ['\u2E2F', '\u{1FAEA}', 'a\u{1F3FB}', '\u200Cname', 'a\uD800']) {
        try {new RegExp('(?<' + name + '>a)', flags); console.log('unexpected identifier', flags);}
        catch (error) {console.log('invalid identifier', flags, (error as Error).name);}
    }
}

// Sample across Unicode, plus the boundaries of surrogate encodings.
for (const flags of ['', 'u']) {
    let accepted = 0, rejected = 0, checksum = 0;
    const points = [0, 36, 48, 65, 95, 0x2E2F, 0xD7FF, 0xD800, 0xDBFF, 0xDC00, 0xDFFF, 0xE000, 0xFFFF, 0x10000, 0x10FFFF];
    for (let point = 128; point <= 0x10FFFF; point += 997) points.push(point);
    for (const point of points) {
        const character = String.fromCodePoint(point);
        for (const prefix of ['', 'a']) {
            for (const spelling of [character, '\\u{' + point.toString(16) + '}']) {
                try {
                    const expression = new RegExp('(?<' + prefix + spelling + '>x)', flags);
                    const match = expression.exec('x')!;
                    const key = Object.keys(match.groups!)[0];
                    if (key !== prefix + character || match.groups![key] !== 'x') throw new Error('Incorrect capture name');
                    accepted++;
                    checksum = (checksum + point) % 1000000007;
                } catch (error) {
                    if ((error as Error).name !== 'SyntaxError') throw error;
                    rejected++;
                }
            }
        }
    }
    console.log('identifier samples', flags, accepted, rejected, checksum);
}
