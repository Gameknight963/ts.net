import {make,attempt} from './promise-helper-values';
const deferred=make<string>();deferred.promise.then(v=>console.log('module deferred',v));deferred.resolve('ready');
attempt(()=>15).then(v=>console.log('module try',v));
attempt(()=>{throw 'module error';}).catch(e=>console.log('module error',e));
