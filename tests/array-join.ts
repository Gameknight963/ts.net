console.log('basic', [1, 2, 3].join(), [1, 2, 3].join('|'), [1, 2, 3].join(''));
console.log('missing', [1, undefined, null, , 5].join('-'), [].join('x'));
console.log('nested', [[1, 2], [3, 4]].join('|'));
const join: any = [1].join;
console.log('metadata', join.name, join.length);
console.log('generic', join.call({0: 'a', 2: 'c', length: 3}, '/'), join.call('abc', '-'), join.call(4));
console.log('length', join.call({0: 'x', 1: 'y', length: 1.9}), join.call({length: -1}));
function args(): string { return join.call(arguments, ':'); }
console.log('arguments', (args as any)(1, 2, 3));
let effects = '';
const source = {
    get length(): number { effects += 'L'; return 2; },
    get 0(): string { effects += 'A'; return 'a'; },
    get 1(): string { effects += 'B'; return 'b'; }
};
const separator: any = {toString(): string { effects += 'S'; return '|'; }};
console.log('order', join.call(source, separator), effects);
const changing: any[] = [1, 2, 3];
const first = {toString(): string { changing.length = 1; return 'first'; }};
changing[0] = first;
console.log('snapshot-length', changing.join('|'));
const growing: any[] = [1];
growing[0] = {toString(): string { growing.push(2); return 'one'; }};
console.log('growth', growing.join('|'), growing.length);
const cycle: any[] = [1]; cycle.push(cycle); cycle.push(3);
console.log('cycle', cycle.join('|'), '' + cycle);
const left: any[] = [1], right: any[] = [2]; left.push(right); right.push(left);
console.log('mutual', left.join('|'), right.join('|'));
const sparse: any[] = []; sparse.length = 3;
(sparse as any).__proto__ = {1: 'inherited'};
console.log('inherited', join.call(sparse, '|'));
for (const value of [null, undefined]) {
    try { join.call(value); } catch (error) { console.log('invalid', error instanceof TypeError); }
}
let conversions = 0;
const emptySeparator: any = {toString(): string { conversions++; return '-'; }};
console.log('empty', join.call({length: 0}, emptySeparator), conversions);
const failure: any[] = [{toString(): string { throw 'failed'; }}];
try { failure.join(); } catch (error) { console.log('failure', error); }
failure[0] = 'recovered';
console.log('cleanup', failure.join());
