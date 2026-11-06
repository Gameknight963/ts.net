import * as namespace from './module-namespace-order-values.js';
import * as same from './module-namespace-order-values.js';
import {update} from './module-namespace-order-values.js';
console.log(namespace===same);
for(const key of Object.keys(namespace))console.log(key.length,key.charCodeAt(0),typeof (namespace as any)[key]);
update();console.log(namespace.changing);
try{(namespace as any).changing=3;}catch(error){console.log(error instanceof TypeError);}
try{(namespace as any).added=3;}catch(error){console.log(error instanceof TypeError);}
console.log(Object.hasOwn(namespace,'added'));
