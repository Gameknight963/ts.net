function Target(this: any, value = new.target): void {
    const target = new.target;
    console.log('function', target === undefined, target === Target, value === target);
    if (this) { this.target = target; this.saved = () => new.target; }
    function nested(): unknown { return new.target; }
    console.log('nested', nested() === undefined);
}
Target();
Target.call({});
const ordinary = new (Target as any)();
console.log('ordinary', ordinary.target === Target, ordinary.saved() === Target);
const Bound = Target.bind(null);
const bound = new (Bound as any)();
console.log('bound', bound.target === Target, bound.saved() === Target);
class Base {
    saved: () => unknown;
    constructor() {
        console.log('base', new.target === Base, new.target?.name);
        this.saved = () => new.target;
    }
}
class Derived extends Base {
    constructor() {
        console.log('before-super', new.target === Derived);
        const read = () => new.target;
        super();
        console.log('after-super', new.target === Derived, read() === Derived);
    }
}
const base = new Base();
const derived = new Derived();
console.log('capture', base.saved() === Base, derived.saved() === Derived);
class Implicit extends Base {}
console.log('implicit', new Implicit().saved() === Implicit);
class FunctionChild extends (Target as any) { constructor() { super(); } }
const child = new FunctionChild();
console.log('function-base', (child as any).target === FunctionChild, (child as any).saved() === FunctionChild);
const object = {method: function(): unknown { return new.target; }};
console.log('object', object.method() === undefined);
function nestedArrows(): () => unknown { return () => () => new.target; }
const called = nestedArrows();
const constructed = new (nestedArrows as any)();
console.log('arrows', (called() as any)() === undefined, constructed()() === nestedArrows);
function Identity(this: any): unknown { return new.target; }
const identity = new (Identity as any)();
console.log('identity', identity === Identity, Identity.call(ordinary) === undefined);
function factory(): any {
    return function Local(this: any): void { this.capture = () => new.target; };
}
const First = factory(), Second = factory();
const first = new First(), second = new Second();
console.log('per-definition', first.capture() === First, second.capture() === Second, first.capture() !== second.capture());
class ErrorChild extends Error {
    target: unknown;
    constructor() { const target = new.target; super('child'); this.target = target; }
}
class ErrorGrandchild extends ErrorChild {}
console.log('error-base', new ErrorChild().target === ErrorChild, new ErrorGrandchild().target === ErrorGrandchild);
class Replacing { constructor() { return {} as any; } }
class ReplacementChild extends Replacing {
    target: unknown;
    read: () => unknown;
    constructor() { super(); this.target = new.target; this.read = () => new.target; }
}
const replaced = new ReplacementChild();
console.log('replacement', replaced.target === ReplacementChild, replaced.read() === ReplacementChild);
class ThrowsBeforeSuper extends Base {
    constructor(stop = true) {
        console.log('uninitialized', new.target === ThrowsBeforeSuper);
        if (stop) throw new Error('stop');
        super();
    }
}
try { new ThrowsBeforeSuper(); } catch (error) { console.log('throw', error instanceof Error); }
