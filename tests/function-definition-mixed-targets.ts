export {};
function define(target: (() => number) | {value: number}): void {
    Object.defineProperty(target, 'value', {value: 7, writable: false});
}
const record = {value: 1};
const callable = () => 3;
define(record); define(callable);
console.log(record.value, callable(), (callable as any).value);
try {record.value = 8;} catch {console.log('readonly');}
