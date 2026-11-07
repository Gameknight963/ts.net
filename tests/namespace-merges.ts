function Counter(value: number) {return value + Counter.offset;}
console.log("before namespace", Counter(1), Counter.offset);
namespace Counter {
    export let offset = 2;
    export function reset() {offset = 0;}
    export namespace Nested {export const value = 8;}
}
console.log(Counter(3), Counter.offset, Counter.Nested.value);
Counter.reset(); console.log(Counter(3));
class Box {constructor(public value: number) {} read() {return this.value + Box.offset;}}
namespace Box {
    export let offset = 4;
    export function create(n: number) {return new Box(n);}
    export class Nested {value = 9;}
}
console.log(Box.create(5).read(), Box.offset, new Box.Nested().value);
namespace Box {export const another = offset + 1;}
console.log(Box.another);

namespace Counter {export const later = offset + 10;}
console.log(Counter.later);
import Alias = Counter.Nested;
console.log(Alias.value);
console.log(Object.keys(Counter).join(","), Object.keys(Box).join(","));
const counterAlias = Counter;
const boxAlias = Box;
console.log(counterAlias === Counter, boxAlias === Box, counterAlias(4), new boxAlias(2).read());
namespace Counter {const value = 30; export const privateRead = () => value;}
namespace Counter {const value = 40; export const otherRead = () => value;}
console.log(Counter.privateRead(), Counter.otherRead());

namespace Outer {
    export function Factory(value: number): number {return value * Factory.factor;}
    export namespace Factory {
        export let factor = 3;
        export const read = () => factor;
    }
    export class Item {constructor(public value: number) {}}
    export namespace Item {
        export function create(value: number) {return new Item(value);}
        export const kind = "item";
    }
}
console.log("nested merges", Outer.Factory(4), Outer.Factory.read(), Outer.Item.create(6).value, Outer.Item.kind);
Outer.Factory.factor = 5;
console.log(Outer.Factory(2));

function identity<T>(value: T): T {return value;}
namespace identity {export const description = "generic";}
console.log(identity(7), identity("text"), identity.description);
async function task(value: number) {return value + task.offset;}
namespace task {export const offset = 2;}
task(4).then(value => console.log("async merged", value));
abstract class Base {abstract read(): number;}
namespace Base {export class Derived extends Base {read() {return 12;}}}
console.log(new Base.Derived().read());

const originalFactory = Outer.Factory;
(Outer as any).Factory = (value: number) => value + 100;
console.log("replaced export", originalFactory(3), Outer.Factory(3), originalFactory.factor);
const originalItem = Outer.Item;
(Outer as any).Item = class {value = 100;};
console.log("retained class", originalItem.create(7).value);
