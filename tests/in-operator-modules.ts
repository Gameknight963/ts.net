import * as namespace from './optional-chaining-values.js';
console.log('changing' in namespace,'maybe' in namespace,'constructor' in namespace,'toString' in namespace,'__proto__' in namespace);
try{console.log((namespace as any) in {});}catch(error){console.log(error instanceof TypeError);}
