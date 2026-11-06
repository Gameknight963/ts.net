/// <reference lib="es2023.array" />
import {splice} from './array-to-spliced-values';
const source = [1, 2, 3];
console.log(splice(source, 7, 8).join(','), source.join(','));
const values: any[] = [1, 'two', null, undefined];
console.log(values.toSpliced(1, 1, 'changed').join('|'));
