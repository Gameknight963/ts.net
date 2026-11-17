export {};
const values = JSON.parse('[1,-0,1e400,-1e400,2.5,[3,4],{"x":5},null,true,"text"]');
console.log('values', Array.isArray(values), values instanceof Array, Object.getPrototypeOf(values) === Array.prototype, values.length, 1 / values[1], values[2], values[3], JSON.stringify(values));
values[0] = 'changed'; values.push(undefined); values[4] = {value: 9};
console.log('mixed writes', values[0], values[4].value, values.pop(), Object.keys(values).join(','));
delete values[1];
console.log('hole', Object.hasOwn(values, '1'), values[1], values.length, JSON.stringify(values));
Object.defineProperty(values, '0', {value: 'locked', writable: false, enumerable: false});
try { values[0] = 8; } catch (error) { console.log('readonly', (error as Error).name); }
console.log('descriptor', values[0], Object.keys(values).join(','));
const frozen = JSON.parse('[1,2,3]'); Object.freeze(frozen);
try { frozen.push(4); } catch (error) { console.log('frozen', (error as Error).name); }
console.log('frozen values', JSON.stringify(frozen), Object.isFrozen(frozen));
const input = '[ 1, 2, 3 ]'; let setters = 0; let parsed: any; let mixedParsed: any;
Object.defineProperty(Array.prototype, '0', {set(value: any) {setters++;}, get() {return 777;}, configurable: true});
Object.defineProperty(Array.prototype, '2', {set(value: any) {setters++;}, get() {return 888;}, configurable: true});
try { parsed = JSON.parse(input); mixedParsed = JSON.parse('[1,"two",3]'); }
finally { delete (Array.prototype as any)[0]; delete (Array.prototype as any)[2]; }
console.log('own creation', setters, parsed.join(','), mixedParsed.join(','), Object.hasOwn(parsed, '0'));
const typed: number[] = JSON.parse('[1,-0,3.5]'); typed.push(4);
console.log('typed', typed.length, 1 / typed[1], typed.reduce((sum, value) => sum + value, 0));
const revived = JSON.parse('[1,2,3]', function(this: any, key: string, value: any, context?: any): any {
    if (key === '0') { this[1] = 'replacement'; return undefined; }
    console.log('reviver', key, JSON.stringify(value), context?.source);
    return value;
});
console.log('revived', revived.length, Object.hasOwn(revived, '0'), JSON.stringify(revived));
