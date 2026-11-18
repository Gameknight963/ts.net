export {};
function make(value: number): any {
    return class Captured {value = value; count: number; constructor() {this.count = arguments.length;}};
}
const Captured = make(7);
console.log('captured', new Captured().value, new Captured().count);
const Ordinary: any = function(this: any): void {
    this.count = arguments.length;
    this.target = new.target;
    (arguments as any)[0] = 99;
};
const first = new Ordinary();
const second = new Ordinary();
console.log('ordinary', first.count, second.count, second.target === Ordinary);
const seen: any[][] = [];
const Proxied: any = new Proxy(Ordinary, {
    construct(target, args, newTarget) {
        console.log('trap', args.length, newTarget === Proxied, seen.length === 0 || args !== seen[0]);
        args.push(seen.length + 10);
        seen.push(args);
        return Reflect.construct(target, args, newTarget);
    }
});
console.log('proxy', new Proxied().count, new Proxied().count, seen[0]![0], seen[1]![0]);
const Bound: any = Ordinary.bind(null);
console.log('bound', new Bound().count, new Bound().target === Ordinary);
class Base {count: number; constructor() {this.count = arguments.length;}}
class Derived extends Base {extra = 3;}
console.log('derived', new Derived().count, new Derived().extra);
console.log('native', new Array().length, new Map().size, new Set().size);
const Revoked = Proxy.revocable(Ordinary, {});
Revoked.revoke();
try {new Revoked.proxy();} catch (error) {console.log('revoked', (error as Error).name);}
