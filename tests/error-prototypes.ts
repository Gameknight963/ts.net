console.log('prototypes', Error.prototype.name, TypeError.prototype.name, TypeError.prototype.message);
console.log('constructors', Error.prototype.constructor === Error, TypeError.prototype.constructor === TypeError);
console.log('inheritance', TypeError.prototype instanceof Error, TypeError.prototype instanceof TypeError, Error.prototype instanceof Error);
console.log('hidden', Object.keys(Error.prototype).length, Object.keys(TypeError.prototype).length);
const error = new TypeError('broken');
console.log('instance', error.constructor === TypeError, error instanceof Error, error instanceof TypeError, error instanceof RangeError);
console.log('own', Object.hasOwn(error, 'name'), Object.hasOwn(TypeError.prototype, 'name'), Object.hasOwn(TypeError.prototype, 'toString'));
const oldName = TypeError.prototype.name;
TypeError.prototype.name = 'Custom';
console.log('changed', error.name, error.toString(), new TypeError().name);
TypeError.prototype.name = oldName;
const fake: any = {};
fake.__proto__ = TypeError.prototype;
console.log('fake', fake instanceof Error, fake instanceof TypeError, fake.name, fake.toString());
const changed: any = new TypeError('changed');
changed.__proto__ = RangeError.prototype;
console.log('reparent', changed instanceof TypeError, changed instanceof RangeError, changed.name, changed.toString());
class Failure extends Error {
    code = 7;
    #detail = 'secret';
    constructor(message: string, cause: unknown) { super(message, {cause}); this.name = 'Failure'; }
    describe(): string { return this.message + ':' + this.code + ':' + this.#detail; }
}
const failure = new Failure('bad', error);
console.log('derived', failure instanceof Failure, failure instanceof Error, failure.constructor === Failure, failure.cause === error, failure.describe(), failure.toString());
console.log('derived-keys', Object.keys(failure).length, Object.hasOwn(failure, 'message'), typeof failure.stack);
class Specific extends TypeError {}
const specific = new Specific('specific');
console.log('specific', specific instanceof Specific, specific instanceof TypeError, specific instanceof Error, specific.name, specific.message);
class MoreSpecific extends Specific { value = 9; }
const more = new MoreSpecific('more');
console.log('deep', more instanceof MoreSpecific, more instanceof Specific, more instanceof TypeError, more.value, more.message);
more.value++;
console.log('access', more?.value, more.value += 3);
const {value: copied, message: copiedMessage} = more;
console.log('destructure', copied, copiedMessage);
for (more.value of [20, 21]) console.log('loop-write', more.value);
console.log('static-chain', (TypeError as any).__proto__ === Error, (Specific as any).__proto__ === TypeError);
class Group extends AggregateError { count(): number { return this.errors.length; } }
const group = new Group([error, failure], 'group', {cause: specific});
console.log('group', group instanceof Group, group instanceof AggregateError, group instanceof Error, group.count(), group.message, group.cause === specific);
const options: any = {};
options.__proto__ = {cause: error};
console.log('inherited-cause', new Error('cause', options as any).cause === error);
let effects = '';
const causeOptions: any = {};
causeOptions.__proto__ = {get cause(): unknown { effects += 'C'; return error; }};
const message: any = {toString(): string { effects += 'M'; return 'ordered'; }};
const ordered = new Specific(message, causeOptions);
console.log('order', effects, ordered.message, ordered.cause === error);
const families: any[] = [Error, TypeError, RangeError, ReferenceError, SyntaxError, URIError, EvalError];
for (const Family of families) {
    class Child extends Family { label = 'child'; constructor(message: string) { super(message); } }
    const item = new Child('family');
    console.log('family', item.name, item.message, item.label, item instanceof Child, item instanceof Family, item instanceof Error);
}
let setterCalls = 0;
const originalParent = (TypeError.prototype as any).__proto__;
(TypeError.prototype as any).__proto__ = {set cause(value: unknown) { setterCalls++; }};
const ownCause = new TypeError('own', {cause: error});
console.log('define-cause', setterCalls, Object.hasOwn(ownCause, 'cause'), ownCause.cause === error);
(TypeError.prototype as any).__proto__ = originalParent;
try { const missing: any = null; missing.value; } catch (caught) {
    if (caught instanceof TypeError) console.log('runtime-error', caught.constructor === TypeError, caught instanceof Error);
}
