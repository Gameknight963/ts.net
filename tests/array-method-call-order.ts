export {};
const values = [1, 2, 3];
const push = values.push;
let log = '';
Object.defineProperty(values, 'push', {configurable: true, get() {log += 'get|'; return push;}});
function pushArgument(): number {
    log += 'arg|';
    Object.defineProperty(values, 'push', {configurable: true, writable: true, value: function(this: unknown) {log += 'call:' + String(this === values) + '|'; return 99;}});
    return 4;
}
console.log('captured push', values.push(pushArgument()), values.join(','), log);
log = '';
console.log('new push', values.push(5), values.join(','), log);
const items = [1, 2, 3];
const slice = items.slice;
log = '';
Object.defineProperty(items, 'slice', {configurable: true, get() {log += 'get|'; return slice;}});
function sliceArgument(): number {
    log += 'arg|';
    Object.defineProperty(items, 'slice', {configurable: true, value: function(this: unknown) {log += 'call:' + String(this === items) + '|'; return [9];}});
    return 1;
}
console.log('captured slice', items.slice(sliceArgument()).join(','), log);
log = '';
console.log('new slice', items.slice().join(','), log);
const source = [1, 2, 3];
log = '';
Object.defineProperty(source, 'includes', {configurable: true, get() {log += 'get|'; return function(this: unknown, value: number) {log += 'call:' + String(this === source) + '|'; return value === 7;};}});
console.log('custom search', source.includes((log += 'arg|', 7)), log);
