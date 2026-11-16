export {};
let events = "";
class First {
 tag = "first";
 value: number;
 constructor(value: number) {events += "F"; this.value = value;}
}
class Second {
 tag = "second";
 value: number;
 constructor(value: number) {events += "S"; this.value = value;}
}
class Implicit extends First {child = 7;}
Object.setPrototypeOf(Implicit, Second);
const implicit = new Implicit(3);
console.log("implicit", implicit.tag, implicit.value, implicit.child, implicit instanceof First, implicit instanceof Second, events);

class Explicit extends First {child = 8; constructor(value: number) {super(value);}}
Object.setPrototypeOf(Explicit, Second);
console.log("explicit", new Explicit(4).tag, events);

function* argumentsForSuper(): Generator<number> {events += "i"; yield 7;}
class Spread extends (First as any) {constructor() {super(...argumentsForSuper());}}
Object.setPrototypeOf(Spread, Second);
console.log("spread", new Spread().tag, events);

function argument(): number {events += "b"; return 9;}
class Invalid extends First {constructor() {super(argument());}}
for (const prototype of [{}, null, () => {}]) {
 Object.setPrototypeOf(Invalid, prototype);
 try {new Invalid(); console.log("invalid allowed");}
 catch (error) {console.log("invalid", (error as Error).name, events);}
}
function fail(): number {throw new Error("argument failure");}
class Throwing extends First {constructor() {super(fail());}}
Object.setPrototypeOf(Throwing, null);
try {new Throwing();} catch (error) {console.log("argument error", (error as Error).message);}

class ArrayChild extends First {child = 11;}
Object.setPrototypeOf(ArrayChild, Array);
const array: any = new ArrayChild(3);
console.log("native", Array.isArray(array), array.length, array.child, array instanceof ArrayChild);

const replacement = {tag: "replacement"};
function Replacement() {return replacement;}
class Replaced extends First {child = 12;}
Object.setPrototypeOf(Replaced, Replacement);
const replaced = new Replaced(0);
console.log("replacement", replaced.tag, replaced.child, (replaced as any) === replacement);

class ImplicitInvalid extends First {child = (events += "x", 1);}
Object.setPrototypeOf(ImplicitInvalid, null);
try {new ImplicitInvalid(argument()); console.log("implicit invalid allowed");}
catch (error) {console.log("implicit invalid", (error as Error).name, events);}

let fields = 0;
class Repeated extends First {
 child = ++fields;
 constructor() {
  super(1);
  Object.setPrototypeOf(Repeated, Second);
  try {super(2);} catch (error) {console.log("repeat", (error as Error).name, events);}
 }
}
const repeated = new Repeated();
console.log("repeat fields", repeated.tag, repeated.child, fields);

class Failing extends First {constructor(value: number) {super(value); throw new Error("base failure");}}
class Retry extends Failing {
 constructor() {
  try {super(0);} catch {
   Object.setPrototypeOf(Retry, Second);
   super(1);
  }
 }
}
console.log("retry", new Retry().tag, events);
