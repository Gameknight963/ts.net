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
