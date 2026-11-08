const sample = new Event('before', { cancelable: true, composed: true });
const timestamp = sample.timeStamp;
sample.preventDefault();
console.log('init', sample.initEvent('after', true, false));
console.log('state', sample.type, sample.bubbles, sample.cancelable, sample.composed, sample.defaultPrevented, sample.cancelBubble, sample.timeStamp === timestamp);
const target = new EventTarget();
let calls = 0;
target.addEventListener('after', value => {
    calls++;
    value.initEvent('ignored', false, true);
    console.log('during', value.type, value.bubbles, value.cancelable, value.target === target, value.currentTarget === target);
});
console.log('dispatch', target.dispatchEvent(sample), calls, sample.target === target);
sample.initEvent('again', false, true);
// Node retains its internal cancellation bit here; DOM initialization clears it.
console.log('reuse', sample.type, sample.currentTarget, sample.bubbles, sample.cancelable);
target.addEventListener('again', value => value.preventDefault());
console.log('cancel', target.dispatchEvent(sample), sample.defaultPrevented);
sample.initEvent('last');
console.log('defaults', sample.bubbles, sample.cancelable, sample.defaultPrevented);
const custom = new CustomEvent('custom', { detail: { value: 3 }, composed: true });
const detail = custom.detail;
custom.initEvent('renamed', true, true);
console.log('custom', custom.type, custom.detail === detail, custom.detail.value, custom.composed);
let order = '';
(sample.initEvent as any)({ toString() { order += 'type'; return 'converted'; } }, 1, 'yes');
console.log('conversion', order, sample.type, sample.bubbles, sample.cancelable);
for (const receiver of [null, {}, 1]) {
    try { Event.prototype.initEvent.call(receiver as Event, 'x'); }
    catch (error) { console.log('receiver', (error as Error).name); }
}
try { (sample.initEvent as any)(); }
catch (error) { console.log('missing', (error as Error).name); }
console.log('metadata', Event.prototype.initEvent.name, Event.prototype.initEvent.length);
