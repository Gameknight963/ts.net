import {collect, capture, Parent} from './arguments-values';
import * as values from './arguments-values';
const result = (collect as any)(1, 2, 3);
console.log(result.length, result[0], result[2]);
console.log(capture(7)()[0], (values.collect as any)('a', 'b')[1]);
class Child extends Parent {
    saved: IArguments;
    constructor(value: number, other: number) { const saved = arguments; super(value); this.saved = saved; }
}
const child = new Child(10, 11);
console.log(child.saved.length, child.saved[1], child.read().length, child.read()[0]);
