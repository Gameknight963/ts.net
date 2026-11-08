export {};
const target = new EventTarget();
Object.defineProperty(target, 'label', {value: 'target', enumerable: true});
Object.freeze(target);
const event = new CustomEvent('work', {detail: 42, cancelable: true});
Object.defineProperty(event, 'label', {value: 'event', enumerable: true});
Object.seal(event);
target.addEventListener('work', value => {
    console.log('listener', value.target === target, value.currentTarget === target, (value as CustomEvent).detail);
    value.preventDefault();
}, {once: true});
console.log('dispatch', target.dispatchEvent(event), event.defaultPrevented, Object.isFrozen(target), Object.isSealed(event));
console.log('properties', Object.keys(target).join(','), Object.keys(event).join(','), Reflect.set(target, 'label', 'other'), Reflect.deleteProperty(event, 'label'));
const controller = Object.freeze(new AbortController());
const signal = controller.signal;
Object.defineProperty(signal, 'label', {value: 'signal', enumerable: true});
Object.seal(signal);
signal.addEventListener('abort', () => console.log('abort event', signal.aborted, signal.reason));
controller.abort('stop');
console.log('controller', Object.isFrozen(controller), Object.isSealed(signal), signal.aborted, signal.reason, Reflect.defineProperty(signal, 'extra', {value: 1}));
const frozen = Object.freeze(new Event('fixed'));
console.log('frozen event', frozen.type, Object.isFrozen(frozen));
for (const prototype of [Event.prototype, CustomEvent.prototype, EventTarget.prototype, AbortController.prototype, AbortSignal.prototype]) {
    const tag = Object.getOwnPropertyDescriptor(prototype, Symbol.toStringTag)!;
    console.log('tag', tag.value, tag.writable, tag.enumerable, tag.configurable);
}
