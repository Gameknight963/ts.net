/// <reference lib="esnext.intl" />
export {};
for (const a of 'ABCDEFGHIJKLMNOPQRSTUVWXYZ') for (const b of 'ABCDEFGHIJKLMNOPQRSTUVWXYZ') {
 const tag = 'und-' + a + b;
 console.log(tag, JSON.stringify((new Intl.Locale(tag) as any).getWeekInfo()));
}
for (const locale of ['und', 'en', 'es', 'ar', 'fa', 'zh', 'zz', 'und-GB', 'zh-Hant', 'en-US-u-fw-mon', 'en-US-u-rg-gbzzzz', 'en-GB-u-rg-uszzzz', 'en-u-sd-gbeng', 'en-US-u-rg-zzzzzz', 'en-u-fw-fri-rg-irzzzz']) {
 console.log('locale', locale, JSON.stringify((new Intl.Locale(locale) as any).getWeekInfo()));
}
for (const firstDayOfWeek of ['mon', 'tue', 'wed', 'thu', 'fri', 'sat', 'sun']) console.log('override', firstDayOfWeek, JSON.stringify((new Intl.Locale('en-US', {firstDayOfWeek} as any) as any).getWeekInfo()));
const method = (Intl.Locale.prototype as any).getWeekInfo;
for (const receiver of [null, undefined, {}, 'en-US', Intl.Locale.prototype]) {
 try {method.call(receiver);}
 catch (error) {console.log('receiver', (error as Error).name);}
}
const locale = new Intl.Locale('en-US') as any;
const info = locale.getWeekInfo(); info.firstDay = 0; info.weekend.push(0);
console.log('fresh', JSON.stringify(locale.getWeekInfo()));
console.log('metadata', method.name, method.length);

const typedWeek: Intl.WeekInfo = new Intl.Locale('en-US').getWeekInfo();
console.log('typed week', typedWeek.firstDay, typedWeek.weekend.join(','));
