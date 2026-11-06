import defaultValue,{value,arrow} from './async-functions-values';
value(1).then(result=>console.log('module function',result));arrow(1).then(result=>console.log('module arrow',result));defaultValue().then(result=>console.log('module default',result));console.log('module sync');
