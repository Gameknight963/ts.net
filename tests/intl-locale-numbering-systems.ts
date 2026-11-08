/// <reference lib="esnext.intl" />
export {};
for (const a of 'abcdefghijklmnopqrstuvwxyz') for (const b of 'abcdefghijklmnopqrstuvwxyz') {
 const tag = a + b;
 console.log(tag, new Intl.Locale(tag).getNumberingSystems().join(','));
}
for (const a of 'ABCDEFGHIJKLMNOPQRSTUVWXYZ') for (const b of 'ABCDEFGHIJKLMNOPQRSTUVWXYZ') {
 const tag = 'ar-' + a + b;
 console.log(tag, new Intl.Locale(tag).getNumberingSystems().join(','));
}
for (const language of ['ar', 'bn', 'fa', 'ks', 'my', 'ne', 'ps', 'sd', 'ur', 'ckb', 'mni', 'sat', 'ff', 'syr', 'dv', 'lrc', 'az', 'uz', 'pa']) for (const script of ['', '-Arab', '-Deva', '-Beng', '-Mymr', '-Guru', '-Olck', '-Latn', '-Adlm']) {
 const tag = language + script;
 console.log('script', tag, new Intl.Locale(tag).getNumberingSystems().join(','));
}
for (const numberingSystem of ['latn', 'arab', 'deva', 'mathbold', 'foobar']) {
 console.log('extension', numberingSystem, new Intl.Locale('en-u-nu-' + numberingSystem).getNumberingSystems().join(','));
 console.log('option', numberingSystem, new Intl.Locale('fa-u-nu-arab', {numberingSystem}).getNumberingSystems().join(','));
}
const locale = new Intl.Locale('en-US'); const values = locale.getNumberingSystems(); values[0] = 'changed';
console.log('fresh', locale.getNumberingSystems().join(','));
const method = Intl.Locale.prototype.getNumberingSystems;
console.log('metadata', method.name, method.length);
for (const receiver of [null, undefined, {}, 'en-US', Intl.Locale.prototype]) {
 try {method.call(receiver);}
 catch (error) {console.log('receiver', (error as Error).name);}
}
