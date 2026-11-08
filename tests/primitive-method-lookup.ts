export {};
let effects = '';
const value = {
    get valueOf() {effects += 'V'; return function (this: unknown) {effects += this === value ? 'v' : '!'; return 42;};},
    get toString() {effects += 'S'; return function (this: unknown) {effects += this === value ? 's' : '!'; return 'text';};}
};
console.log('number', Number(value), effects);
effects = '';
console.log('string', String(value), effects);
const fallback = {
    get valueOf() {effects += 'V'; return function () {effects += 'v'; return {};};},
    get toString() {effects += 'S'; return function () {effects += 's'; return '7';};}
};
effects = '';
console.log('fallback', Number(fallback), effects);
for (const method of [undefined, null, 42, {}]) {
    const blocked = {valueOf() {return {};}, get toString() {console.log('get blocked'); return method;}};
    try {console.log(Number(blocked));} catch (error) {console.log('blocked', (error as Error).name);}
}
