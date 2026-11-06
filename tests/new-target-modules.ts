import {Constructor, Parent} from './new-target-values';
import * as values from './new-target-values';
const instance = new (Constructor as any)();
console.log(instance.target === Constructor, instance.read() === values.Constructor);
class Child extends Parent {}
const child = new Child();
console.log(child.target === Child, new values.Parent().target === Parent);
