export {};
const rows = JSON.parse('[{"id":1,"name":"first","enabled":true,"extra":null},{"id":2,"name":null,"enabled":false,"extra":"text"},{"id":3,"name":7,"enabled":true,"extra":null},{"name":"reordered","id":4,"enabled":true,"extra":null}]');
for (const row of rows) console.log(JSON.stringify(row), Object.keys(row).join(','));
const first = rows[0];
console.log(Object.getPrototypeOf(first) === Object.prototype, 'toString' in first);
first.name = 'changed';
first.enabled = null;
first.extra = false;
console.log(JSON.stringify(first));
first.name = 9;
first.id = 'id';
console.log(JSON.stringify(first));
const changed = JSON.parse('[{"id":1,"name":"text","enabled":true}]')[0];
delete changed.name;
changed.name = 'last';
console.log(Object.keys(changed).join(','), JSON.stringify({...changed}));
const described = JSON.parse('[{"name":"text","enabled":false}]')[0];
Object.defineProperty(described, 'name', {get: () => 'getter'});
console.log(described.name, described.enabled, JSON.stringify(described));
const frozen = Object.freeze(JSON.parse('[{"name":"text","enabled":false}]')[0]);
try { frozen.enabled = true; } catch { console.log('frozen'); }
console.log(frozen.name, frozen.enabled);
const keys = Array.from({length: 32}, (_, i) => '"p' + i + '":' + (i === 31 ? '"last"' : i)).join(',');
const wide = JSON.parse('[{' + keys + '},{' + keys + '}]');
wide[0].p31 = false;
wide[1].p31 = 32;
console.log(wide[0].p30, wide[0].p31, wide[1].p31, Object.keys(wide[1]).length);
console.log(JSON.stringify(JSON.parse('[{"text":"a\\nb\\uD800","flag":true},{"text":"\\uDFFF","flag":null}]')));
for (const text of ['[{"a":"text"},{"a":tru}]', '[{"a":null},{"a":false,}]']) {
    try { JSON.parse(text); } catch { console.log('invalid'); }
}
