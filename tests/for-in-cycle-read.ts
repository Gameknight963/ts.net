import * as values from './for-in-cycle-values.js';
try {for(const key in values)console.log('unexpected-cycle-key',key);}catch(error){console.log('cycle-tdz',error instanceof ReferenceError);}
