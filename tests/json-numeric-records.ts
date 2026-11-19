const records = JSON.parse('[{"x":1,"y":2},{"x":3,"y":4},{"y":5,"x":6},{"x":"seven","y":8}]');
for (const record of records) console.log(JSON.stringify(record), Object.keys(record).join(','));
const value = JSON.parse('[{"x":1,"y":2}]')[0];
console.log(Object.getPrototypeOf(value) === Object.prototype, 'toString' in value);
value.x = 'changed';
console.log(value.x, value.y, JSON.stringify(value));
value.z = 3;
delete value.y;
value.y = 4;
console.log(Object.keys(value).join(','), JSON.stringify(value));
const described = JSON.parse('[{"x":1,"y":2}]')[0];
Object.defineProperty(described, 'x', {get: () => 9});
console.log(described.x, described.y, JSON.stringify(described));
const frozen = Object.freeze(JSON.parse('[{"x":1,"y":2}]')[0]);
try { frozen.x = 3; } catch { console.log('frozen'); }
console.log(frozen.x, frozen.y);
const inherited = JSON.parse('[{"x":1,"y":2}]')[0];
Object.setPrototypeOf(inherited, {extra: 7});
console.log(inherited.x, inherited.extra, 'extra' in inherited);
console.log(JSON.stringify(JSON.parse('[{"1":1,"0":2,"__proto__":3},{"1":4,"0":5,"__proto__":6}]')));
console.log(JSON.stringify(JSON.parse('[{"a":1,"a":2},{"a":3}]')));
console.log(JSON.stringify(JSON.parse('[{"a\\\"b":1},{"a\\\"b":2}]')));
console.log(JSON.stringify(JSON.parse('[{"a":1},{"a":2,"b":3},{"a":null},{"a":[]}]')));
console.log(JSON.parse('{"x":-0,"y":1e400}').y, Object.is(JSON.parse('{"x":-0}').x, -0));
const revived = JSON.parse('[{"x":1},{"x":2}]', (key, item) => key === 'x' ? item + 10 : item);
console.log(JSON.stringify(revived));
const wideText = '{' + Array.from({length: 32}, (_, i) => '"p' + i + '":' + i).join(',') + '}';
const wide = JSON.parse('[' + wideText + ',' + wideText + ']');
wide[0].p31 = undefined;
wide[1].p31 = 99;
console.log(wide[0].p30, wide[0].p31, 'p31' in wide[0], wide[1].p31);
console.log(Object.keys(wide[0]).length, Object.keys({...wide[1]}).length);
const constrained = JSON.parse('[{"x":1,"y":2}]')[0];
Object.preventExtensions(constrained);
try { constrained.z = 3; } catch { console.log('nonextensible'); }
constrained.x = 7;
console.log(constrained.x, Object.isExtensible(constrained));
for (const text of ['[{"x":1},{"x":01}]', '[{"x":1},{"x":2,}]', '[{"x":1},{"x":2}x]']) {
    try { JSON.parse(text); } catch { console.log('invalid'); }
}
