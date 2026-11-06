/// <reference lib="es2023.array" />
import {predicate} from './array-find-values';
const values = [1, 2, 3, 4];
console.log(values.find(predicate), values.findIndex(predicate), values.findLast(predicate), values.findLastIndex(predicate));
