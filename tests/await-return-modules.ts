import {awaited,arrow} from './await-return-values';
awaited(3).then(value=>console.log('module primitive',value));awaited(Promise.resolve(4)).then(value=>console.log('module promise',value));arrow().then(value=>console.log(value));console.log('module sync');
