export {};
let text = '';
for (let code = 0; code < 128; code++) text += String.fromCharCode(code);
console.log('default', JSON.stringify(text.toLowerCase()), JSON.stringify(text.toUpperCase()));
for (const locale of ['en-US', 'tr-TR', 'az-AZ', 'lt-LT', 'el-GR']) console.log(locale, JSON.stringify(text.toLocaleLowerCase(locale)), JSON.stringify(text.toLocaleUpperCase(locale)));
