import * as values from './descriptor-values.js';
const first = Object.getOwnPropertyDescriptor(values, 'count')!;
console.log('module', first.value, first.writable, first.enumerable, first.configurable, 'get' in first);
values.change();
console.log('snapshot', first.value, Object.getOwnPropertyDescriptor(values, 'count')!.value);
const descriptors = Object.getOwnPropertyDescriptors(values);
console.log('all', Object.keys(descriptors).join(','), descriptors.count.value, descriptors.change.writable, descriptors.change.configurable);
console.log('namespace accessors', (Object.prototype as any).__lookupGetter__.call(values,'count'), (Object.prototype as any).__lookupSetter__.call(values,'count'));
