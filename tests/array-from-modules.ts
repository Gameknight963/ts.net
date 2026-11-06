import {copy,mapped} from './array-from-values';
console.log('module copy',copy({0:'a',1:'b',length:2}).join(','));
console.log('module map',mapped([1,2]).join(','));
