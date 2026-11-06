import {mapper} from './array-flatten-values';
console.log([1, 2].flatMap(mapper).join(','), [[1, 2], [3, 4]].flat().join(','));
