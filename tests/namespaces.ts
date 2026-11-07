namespace Plain {
    const privateValue = 3;
    export let count = 1;
    export function bump() {return ++count + privateValue;}
    export class Box {constructor(public value: number) {} read() {return this.value + count;}}
    export enum State {Ready, Done}
    export interface Shape {x: number;}
    export type Alias = Shape;
    export const {x, y: renamed} = {x: 8, y: 9};
    export namespace Nested {
        export const value = 10;
        export function read() {return value + count;}
    }
}
console.log(Plain.count, Plain.bump(), Plain.count, new Plain.Box(5).read(), Plain.State.Done, Plain.x, Plain.renamed, Plain.Nested.read());
namespace Plain {export const another = count + 10;}
console.log(Plain.another);
namespace Dotted.Inner.More {export const value = 12;}
console.log(Dotted.Inner.More.value);
import Alias = Plain.Nested;
console.log(Alias.value, Alias.read());
namespace PrivateA {
 const value = "first";
 export const read = () => value;
}
namespace PrivateA {
 const value = "second";
 export const other = () => value;
}
console.log(PrivateA.read(), PrivateA.other(), Object.keys(PrivateA).join(","));
namespace Plain {export function readLater() {return after;}}
namespace Plain {export let after = 20;}
console.log(Plain.readLater());
namespace TypeOnly {export interface Shape {x: number;} export type Value = string;}
namespace ConstOnly {export const enum State {Ready = 17}}
console.log("erased", ConstOnly.State.Ready);
const shape: TypeOnly.Shape = {x: 4};
console.log(shape.x);
namespace Mixed {
    export namespace Types {export interface Shape {x: number;}}
    export namespace Constants {export const enum Value {A = 5}}
    export const value = Constants.Value.A;
}
console.log("nested erasure", Object.keys(Mixed).join(","), Mixed.value);
