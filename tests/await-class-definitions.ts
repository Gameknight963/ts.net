export {};
const events: string[] = [];
async function key<T extends string>(name: T): Promise<T> {events.push("start " + name); await Promise.resolve(); events.push("end " + name); return name;}
class Base {read() {return 3;}}
async function base() {events.push("base start"); await Promise.resolve(); events.push("base end"); return Base;}
const fieldKey = "field", accessorKey = "accessor";
const symbol = Symbol("method");
async function symbolKey() {await Promise.resolve(); return symbol;}
class Box extends (await base()) {
    #secret = 4;
    #helper() {return this.#secret;}
    get #hidden() {return this.#secret;}
    set #hidden(value: number) {this.#secret = value;}
    update(value: number) {this.#hidden = value; return this.#hidden;}
    static #count = 2;
    static #access() {return this.#count;}
    static report() {return this.#access();}
    [await key("read")]() {return this.#helper() + super.read();}
    [fieldKey] = 7;
    accessor [accessorKey] = 8;
    get [await key("value")]() {return this.#secret;}
    set [await key("value")](value: number) {this.#secret = value;}
    [await symbolKey()]() {return 42;}
    static get [await key("label")]() {return "box";}
    static [await key("create")]() {return new Box();}
    static {events.push("static block");}
}
const box = (Box as any).create();
console.log("values", box.read(), box.field, box.accessor, (Box as any).label);
console.log("private accessor", box.update(6), box.read(), Box.report());
box.value = 12; console.log("public accessor", box.value, box.read(), box[symbol]());
box.accessor = 10; console.log("accessor", box.accessor);
console.log(events.join(","));
try {
    class Invalid extends (await Promise.resolve(1 as any)) {
        [await Promise.resolve("invalid")]() {}
    }
} catch(error) {console.log("invalid base", (error as Error).name);}
async function create(name: string) {return class {
    #value = name;
    [await key(name)]() {return this.#value;}
    static read(other: any) {return other.#value;}
};}
const First = await create("first"), Second = await create("second");
console.log("expressions", (new First() as any).first(), (new Second() as any).second());
try {(First as any).read(new Second());} catch(error) {console.log("private brand", (error as Error).name);}
const arrow = async () => class {[await key("arrow")]() {return 9;}};
const Arrow = await arrow(); console.log("arrow", (new Arrow() as any).arrow());
const object = {[await key("object")]() {return 11;}};
console.log("object", object.object());
