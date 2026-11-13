import {freeze,objectOperation} from './computed-object-definition-values.js';
const target={value:1};console.log(freeze(target)===target,Object.isFrozen(target));
const second={value:2};console.log(objectOperation('seal')(second)===second,Object.isSealed(second));
