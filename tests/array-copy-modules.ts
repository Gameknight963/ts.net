/// <reference lib="es2023.array" />
import {copy, source} from './array-copy-values';
console.log(copy(source).join(','), source.join(','));
const reversed = source.toReversed;
console.log(reversed.call(source).join(','), source.with.apply(source, [1, 7]).join(','));
