import * as values from './descriptor-values.js';
import {remove} from './reflect-delete-values.js';
console.log('namespace',remove(values,'count'),remove(values,'missing'),values.count);
const value=Object.assign(Object.create(null),{count:2});console.log('imported method',remove(value,'count'),value.count,Object.keys(value).length);
console.log('namespace prototype',Reflect.setPrototypeOf(values,null),Reflect.setPrototypeOf(values,{}),Reflect.getPrototypeOf(values));
