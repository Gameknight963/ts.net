import {lookup} from './computed-reflect-deletion-values.js';
const remove=lookup('deleteProperty');
const target={first:1,second:2};
console.log(remove(target,'first'),Object.hasOwn(target,'first'),Object.keys(target).join(','));
