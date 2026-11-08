import {erase,reflection} from './reflect-delete-alias-library';
const first={moduleAlias:1};
console.log('module method',erase(first,'moduleAlias'),Object.keys(first).length);
const second={moduleNamespace:2};
console.log('module namespace',reflection.deleteProperty(second,'moduleNamespace'),Object.keys(second).length);
