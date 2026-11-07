import {record, read} from './delete-values.js';
import * as moduleValue from './delete-values.js';
console.log('before', read());
console.log('deleted', delete record.x, read(), Object.keys(record).join(','));
try {console.log(delete (moduleValue as any).record);} catch(error) {console.log('module', (error as Error).name);}
const object: any = {nested: {x: 3}};
console.log('parenthesized', delete (object?.nested).x, object.nested.x);
