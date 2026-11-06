import * as values from './enum-values.js';
for(const key in values)console.log('module',key);
const child:any={own:1};child.__proto__=values;
for(const key in child)console.log('module-prototype',key);
