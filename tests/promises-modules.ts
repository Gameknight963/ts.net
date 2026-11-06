import {promise,increment,create} from './promises-values';
promise.then(increment).then(value=>console.log('module promise',value));
create().then(value=>console.log('module created',value));
console.log('module sync');
