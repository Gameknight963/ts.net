import defaultValues, {values, started} from './generator-module.js';
const iterator = values(10);
console.log(started);
console.log(JSON.stringify(iterator.next()), started);
console.log(JSON.stringify(iterator.next()));
console.log(JSON.stringify(iterator.next()));
console.log([...defaultValues()].join(','), started);
export function* local() { yield* values(20); }
console.log([...local()].join(','), started);
