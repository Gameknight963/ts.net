import { value, events } from './timers-module-helper.js';
console.log(value, events.join('|'));
const result = await new Promise<string>(resolve => setTimeout(resolve, 1, 'done'));
console.log(result);
export {};
