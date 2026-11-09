export {};
console.log('units', Intl.supportedValuesOf('unit').join(','));
for (const key of ['calendar', 'collation', 'currency', 'numberingSystem', 'timeZone', 'unit']) {
 const values = Intl.supportedValuesOf(key as any);
 console.log('inventory', key, Array.isArray(values), values.every(value => typeof value === 'string'), values.join(',') === values.slice().sort().join(','), new Set(values).size === values.length);
 const again = Intl.supportedValuesOf(key as any); values[0] = 'modified'; values.push('modified');
 console.log('fresh', key, again.indexOf('modified') === -1);
}
for (const key of [undefined, null, 1, true, 'Unit', 'invalid', Symbol('key')]) {
 try {Intl.supportedValuesOf(key as any);}
 catch (error) {console.log('key error', typeof key, (error as Error).name);}
}
const trace: string[] = [];
console.log('coercion', Intl.supportedValuesOf({[Symbol.toPrimitive](hint: string) {trace.push(hint); return 'unit';}} as any).includes('meter'), trace.join(','));
console.log('metadata', Intl.supportedValuesOf.name, Intl.supportedValuesOf.length);
console.log('unbound', Intl.supportedValuesOf.call(null, 'unit').includes('meter'));
for (const [key, value] of [['calendar', 'gregory'], ['calendar', 'iso8601'], ['currency', 'USD'], ['numberingSystem', 'latn'], ['numberingSystem', 'arab'], ['numberingSystem', 'arabext'], ['timeZone', 'America/New_York'], ['unit', 'meter']]) console.log('known', key, Intl.supportedValuesOf(key as any).includes(value));

for (const locale of ['en-US', 'en-US-u-ca-iso8601', 'de-DE', 'ja-JP', 'th-TH']) {
 const f = new Intl.DateTimeFormat(locale, {calendar: 'iso8601', timeZone: 'UTC', year: 'numeric', month: '2-digit', day: '2-digit'});
 console.log('ISO calendar', locale, f.resolvedOptions().calendar, f.resolvedOptions().locale, f.format(new Date('2025-03-04T12:34:56Z')));
}
