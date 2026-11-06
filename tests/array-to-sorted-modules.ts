/// <reference lib="es2023.array" />
import {ascending, sorted} from './array-to-sorted-values';
const source = [4, 1, 3, 2];
console.log(source.toSorted(ascending).join(','), sorted(source).join(','), source.join(','));
console.log(source.toSorted(ascending).toSpliced(1, 1, 9).toReversed().join(','));
