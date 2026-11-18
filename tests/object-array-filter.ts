export {};
type Row = { id: number; active: boolean };
for (const length of [0, 1, 16, 17, 33, 129]) {
    const rows: Row[] = [];
    for (let index = 0; index < length; index++) rows.push({ id: index, active: index % 2 === 0 });
    const selected = rows.filter(row => row.active);
    console.log('growth', length, selected.length, selected.map(row => row.id).join(','),
        selected.length === 0 || selected[0] === rows[0]);
}
for (const mode of ['delete', 'shrink', 'getter', 'freeze']) {
    const rows: Row[] = [{id: 0, active: true}, {id: 1, active: true}, {id: 2, active: true}];
    let calls = 0, reads = 0;
    const selected = rows.filter((row, index) => {
        calls++;
        if (index === 0) {
            if (mode === 'delete') delete rows[1];
            if (mode === 'shrink') rows.length = 1;
            if (mode === 'getter') Object.defineProperty(rows, '1', {get() {reads++; return {id: 7, active: true};}, configurable: true});
            if (mode === 'freeze') Object.freeze(rows);
        }
        return row.active;
    });
    console.log(mode, selected.map(row => row.id).join(','), calls, reads);
}
const rows: Row[] = [{id: 3, active: true}, {id: 4, active: true}];
let setters = 0;
let selected: Row[];
Object.defineProperty(Array.prototype, '0', {set() {setters++;}, configurable: true});
try { selected = rows.filter(row => row.active); }
finally { delete (Array.prototype as any)[0]; }
console.log('setter', selected!.map(row => row.id).join(','), setters, Object.hasOwn(selected!, '0'));
const custom: any = rows;
const destination: Row[] = [];
custom.constructor = {[Symbol.species]: function() { return destination; }};
const observed = custom.filter((row: Row) => { console.log('visible', destination.length); return row.active; });
console.log('species', observed === destination, destination.map(row => row.id).join(','));
const locked: Row[] = [];
custom.constructor = {[Symbol.species]: function() { return locked; }};
let rejected = false;
try { custom.filter((row: Row) => { Object.freeze(locked); return row.active; }); }
catch { rejected = true; }
console.log('locked', rejected, locked.length);
const generic = {0: {id: 9, active: true}, 2: {id: 11, active: false}, length: 3};
console.log('generic', Array.prototype.filter.call(generic, (row: Row) => row.active).map(row => row.id).join(','));
