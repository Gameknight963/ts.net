export {};
class Parent {
    constructor(public prefix: string) {}
    async key() {await Promise.resolve(); return this.prefix;}
}
class Outer extends Parent {
    constructor(public value: number) {super("inner");}
    async create() {
        const captured = this.value;
        return class {[await super.key()]() {return captured;}};
    }
    async arrow() {
        const prefix = this.prefix;
        const create = async () => class {[await Promise.resolve(prefix)]() {return 17;}};
        return await create();
    }
}
const outer = new Outer(5);
const Inner = await outer.create(), Arrow = await outer.arrow();
console.log("context", (new Inner() as any).inner(), (new Arrow() as any).inner());
try {class Broken {[await Promise.reject<string>("key rejection")]() {}}} catch(error) {console.log("key rejected", error);}
let evaluated = false;
try {
    class Broken extends (await Promise.reject<typeof Parent>("base rejection")) {
        [(evaluated = true, "unused")]() {}
    }
} catch(error) {console.log("base rejected", error, evaluated);}
const events: string[] = [];
const coercion = {[Symbol.toPrimitive](hint: string) {events.push(hint); return "coerced";}};
class Coerced {[await Promise.resolve(coercion as any)]() {return 19;}}
const method = (new Coerced() as any).coerced;
console.log("coercion", events.join(","), method.name, method());
class Numeric {[await Promise.resolve(7)]() {return "seven";}}
console.log("numeric", (new Numeric() as any)[7]());
