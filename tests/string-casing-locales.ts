export {};
for (const locales of [['tr-TR', 'en-US'], ['zz-ZZ', 'tr-TR'], ['az-AZ-u-nu-deva'], ['lt-u-hc-h24'], []]) {
    console.log('selected', JSON.stringify(locales), 'i'.toLocaleUpperCase(locales), 'I\u0301'.toLocaleLowerCase(locales));
}
for (const locales of [['tr-TR', 'bad_locale'], ['en-US', 3], ['en-US', null]]) {
    try {'i'.toLocaleUpperCase(locales as any);}
    catch (error) {console.log('invalid', (error as Error).name);}
}
let log = '';
const list = {get length() {log += 'L'; return 3;}, get 0() {log += 'A'; return 'tr-TR';}, get 2() {log += 'B'; return {toString() {log += 'C'; return 'en-US';}};}};
console.log('getters', 'i'.toLocaleUpperCase(list as any), log);
const locale = new Intl.Locale('tr-TR-u-nu-deva');
(locale as any).toString = () => {throw new Error('must not stringify Locale');};
console.log('locale object', 'i'.toLocaleUpperCase(locale), 'I'.toLocaleLowerCase(locale));
for (const tag of ['tr-u-co-phonebk', 'tr-a-foobar', 'az-x-private']) console.log('extensions', tag, 'i'.toLocaleUpperCase(tag));
