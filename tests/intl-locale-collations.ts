/// <reference lib="esnext.intl" />
export {};
for (const tag of ['und', 'en', 'de', 'de-DE', 'es', 'es-MX', 'zh', 'zh-Hant', 'ja', 'ko', 'ar', 'fa', 'ru', 'sv', 'zz']) {
 const locale = new Intl.Locale(tag);
 const values = locale.getCollations();
 const available = Intl.supportedValuesOf('collation');
 console.log('constructible', tag, values.every(collation => Number.isFinite(new Intl.Collator(tag, {collation} as any).compare('a', 'b'))));
 console.log('inventory', tag, values.every(value => available.includes(value)), values.join(',') === values.slice().sort().join(','), values.length === new Set(values).size);
 const again = locale.getCollations(); values.push('changed');
 console.log('fresh', tag, again.indexOf('changed') === -1);
}
for (const collation of ['phonebk', 'trad', 'stroke', 'zhuyin', 'emoji', 'eor', 'foobar', 'standard', 'search']) {
 console.log('extension', collation, new Intl.Locale('de-u-co-' + collation).getCollations().join(','));
 console.log('option', collation, new Intl.Locale('zh-u-co-stroke', {collation}).getCollations().join(','));
}
console.log('empty extension', new Intl.Locale('en-u-co').getCollations().join(','));
const method = Intl.Locale.prototype.getCollations;
console.log('metadata', method.name, method.length);
for (const receiver of [null, undefined, {}, 'en-US', Intl.Locale.prototype]) {
 try {method.call(receiver);}
 catch (error) {console.log('receiver', (error as Error).name);}
}
