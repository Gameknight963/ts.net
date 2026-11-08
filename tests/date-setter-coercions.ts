export {};
for (const name of ['setUTCFullYear', 'setUTCMonth', 'setUTCDate', 'setUTCHours', 'setUTCMinutes', 'setUTCSeconds', 'setUTCMilliseconds']) {
    const date: any = new Date(NaN);
    let conversions = 0;
    const value = {valueOf() {conversions++; return 1;}};
    const result = date[name](value, value, value, value);
    console.log('invalid', name, conversions, Number.isNaN(result), Number.isNaN(date.getTime()));
}
const date = new Date(Date.UTC(2024, 0, 31, 12, 30));
const order: string[] = [];
const hour = {valueOf() {order.push('hour'); date.setTime(0); return 2;}};
const minute = {valueOf() {order.push('minute'); return 3;}};
const second = {valueOf() {order.push('second'); return 4;}};
console.log('snapshot', date.setUTCHours(hour as any, minute as any, second as any), date.toISOString(), order.join(','));
const interrupted = new Date(123);
try {interrupted.setUTCMinutes({valueOf() {interrupted.setTime(456); throw new Error('conversion');}} as any);} catch (error) {console.log('throw', (error as Error).message, interrupted.getTime());}
let converted = false;
try {Date.prototype.setUTCMonth.call({} as any, {valueOf() {converted = true; return 1;}} as any);} catch (error) {console.log('receiver', (error as Error).name, converted);}
const missing: any = new Date(0);
console.log('missing', Number.isNaN(missing.setUTCSeconds()), Number.isNaN(missing.getTime()));
const ignored: any = new Date(0);
ignored.setUTCDate(2, {valueOf() {throw new Error('extra argument');}});
console.log('extra', ignored.toISOString());
