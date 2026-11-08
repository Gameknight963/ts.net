export {};
for (let code = 0x1f00; code <= 0x1fff; code++) {
    const text = String.fromCharCode(code);
    console.log(code, text.toUpperCase(), text.toLocaleUpperCase('en-US'));
}
for (const code of [0x390, 0x3b0, 0x1e96, 0x1e97, 0x1e98, 0x1e99, 0x1e9a, 0x587, 0xfb13, 0xfb14, 0xfb15, 0xfb16, 0xfb17]) {
    const text = 'a' + String.fromCharCode(code) + 'z';
    console.log('other', code, text.toUpperCase(), text.toLocaleUpperCase('en-US'));
}
