import {transform, positive} from './array-callback-values';
const source = [-1, 2, 3];
console.log(source.map(transform).join(','), source.filter(positive).join(','));
console.log(source.every(positive), source.some(positive));
