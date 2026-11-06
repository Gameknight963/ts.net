import {sum} from './array-reduce-values';
console.log([1, 2, 3].reduce(sum, 0), [1, 2, 3].reduceRight(sum, 0));
