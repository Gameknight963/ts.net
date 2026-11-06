import * as namespace from './module-namespace-numeric-values.js';
const keys=Object.keys(namespace);
if(keys.length!==2||keys[0]!=='10'||keys[1]!=='2')throw new Error('Namespace keys must use lexical order');
console.log('PASS numeric namespace keys use lexical order');
