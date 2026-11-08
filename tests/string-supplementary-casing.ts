export {};
for (const [start, end] of [[0x10570, 0x105bc], [0x10d50, 0x10d85], [0x16ea0, 0x16ed3]]) {
    for (let code = start - 1; code <= end + 1; code++) {
        const text = String.fromCodePoint(code);
        console.log(code, text.toUpperCase(), text.toLowerCase(), text.toLocaleUpperCase('en-US'), text.toLocaleLowerCase('en-US'));
        console.log('sigma', (text + '\u03a3').toLowerCase());
    }
}
for (const text of ['A\ud801', '\ud801A', '\ud801\ud801', '\udc00\ud801', '\u{10597}\ud801\u{10d70}']) console.log('surrogates', JSON.stringify(text.toUpperCase()), JSON.stringify(text.toLowerCase()));
