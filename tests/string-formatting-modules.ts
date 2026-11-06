import {format, repeat} from './string-formatting-values';
console.log(format(' ab '), repeat.call('xy', 3));
const {padEnd, trimStart} = String.prototype;
console.log(padEnd.call('x', 3, '0'), trimStart.call('  hi '));
