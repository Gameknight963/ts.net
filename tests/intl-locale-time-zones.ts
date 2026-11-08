/// <reference lib="esnext.intl" />
export {};
// These identifiers lack Windows mappings on the test host; compare the common inventory.
const unavailable = new Set(['Antarctica/Troll', 'America/Coyhaique', 'Asia/Urumqi', 'America/Ciudad_Juarez']);
function common(zones: string[] | undefined): string | undefined {return zones?.filter(zone => !unavailable.has(zone) && !zone.startsWith('SystemV/')).join(',');}

for (const a of 'ABCDEFGHIJKLMNOPQRSTUVWXYZ') for (const b of 'ABCDEFGHIJKLMNOPQRSTUVWXYZ') {
 const tag = 'und-' + a + b;
 const zones = new Intl.Locale(tag).getTimeZones();
 console.log(tag, common(zones));
 console.log('usable', tag, zones?.every(timeZone => new Intl.DateTimeFormat('en-US', {timeZone}).format(0).length > 0));
}
for (const tag of ['en', 'und', 'zz', 'en-u-rg-uszzzz', 'en-US', 'fr-CA', 'en-GB', 'en-001', 'es-419']) {
 const locale = new Intl.Locale(tag); const zones = locale.getTimeZones();
 console.log('locale', tag, common(zones));
 if (zones) {
  const again = locale.getTimeZones(); zones.push('changed');
  console.log('fresh', tag, again?.includes('changed') === false);
 }
}
const method = Intl.Locale.prototype.getTimeZones;
console.log('metadata', method.name, method.length);
for (const receiver of [null, undefined, {}, 'en-US', Intl.Locale.prototype]) {
 try {method.call(receiver);}
 catch (error) {console.log('receiver', (error as Error).name);}
}

console.log('global inventory', common(Intl.supportedValuesOf('timeZone')));
