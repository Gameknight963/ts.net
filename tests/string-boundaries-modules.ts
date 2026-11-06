import {starts, combine} from './string-boundaries-values';
console.log(starts.call('abc', 'b', 1), combine('a', 'b', 'c'));
const {endsWith} = String.prototype;
console.log(endsWith.call('abc', 'b', 2));
