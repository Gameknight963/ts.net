export {};
const names = ['usage', 'localeMatcher', 'collation', 'numeric', 'caseFirst', 'sensitivity', 'ignorePunctuation'];
const defaults: any = {usage: 'sort', localeMatcher: 'lookup', collation: 'default', numeric: false, caseFirst: 'lower', sensitivity: 'variant', ignorePunctuation: false};
for (const throwing of ['', ...names]) {
    let log = '';
    const options: any = {};
    for (const name of names) Object.defineProperty(options, name, {get() {
        log += name + ',';
        if (name === throwing) throw new Error(name);
        if (name === 'numeric' || name === 'ignorePunctuation') return defaults[name];
        return {toString() {log += 'convert:' + name + ','; return defaults[name];}};
    }});
    try {console.log('result', new Intl.Collator('en-US', options).compare('a', 'A'));}
    catch (error) {console.log('throw', (error as Error).message);}
    console.log('order', throwing, log);
}
for (const invalid of ['usage', 'localeMatcher', 'caseFirst', 'sensitivity']) {
    let log = '';
    const options: any = {};
    for (const name of names) Object.defineProperty(options, name, {get() {log += name + ','; return name === invalid ? 'invalid' : defaults[name];}});
    try {new Intl.Collator('en-US', options);}
    catch (error) {console.log('invalid', invalid, (error as Error).name, log);}
}
let converted = false;
const value = {valueOf() {converted = true; throw new Error('boolean conversion');}};
const format = new Intl.Collator('en-US', {numeric: value, ignorePunctuation: value} as any);
console.log('boolean', converted, format.compare('a-2', 'a10'));
