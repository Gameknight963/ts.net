import {constructor,Values,create} from './array-constructor-values';
console.log(constructor===Array,constructor.of(4,5).join(','),Array.isArray(create()));
const values=new Values(1,2,3);console.log(values.sum(),values instanceof Array,values instanceof Values);
const {of,isArray}=Array;console.log(of(1,2).join(','),isArray(values));
