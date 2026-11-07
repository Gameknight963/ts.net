/// <reference lib="esnext.decorators" />
(Symbol as any).metadata ??= Symbol('metadata');
function mark(_value: any, context: ClassDecoratorContext | ClassMethodDecoratorContext) {
    context.metadata[String(context.name)] = context.kind;
    console.log('metadata', context.kind, String(context.name), context.metadata !== undefined);
}
@mark
class Parent { @mark parent() {} }
@mark
class Child extends Parent { @mark child() {} }
const parentMetadata = Parent[Symbol.metadata]!;
const childMetadata = Child[Symbol.metadata]!;
console.log(parentMetadata.Parent, parentMetadata.parent);
console.log(childMetadata.Child, childMetadata.parent, childMetadata.child);
console.log(Object.getPrototypeOf(childMetadata) === parentMetadata);
console.log(Object.keys(childMetadata).join(','));
let late: (() => void) | undefined;
function save(_value: any, context: ClassMethodDecoratorContext) { late = () => context.addInitializer(() => {}); }
class Saved { @save method() {} }
try { late!(); } catch (error) { console.log(error instanceof TypeError); }
function invalid(_value: any, _context: any): any { return 3; }
try { class Broken { @invalid method() {} } } catch (error) { console.log(error instanceof TypeError); }
const order: string[] = [];
function effect<T>(label: string, value: T) { order.push(label); return value; }
function decor(label: string): any { order.push('eval-' + label); return (_value: any, context: any) => { order.push('apply-' + label + ':' + String(context.name)); }; }
@decor('class')
class Computed extends effect('base', Parent) {
    @decor('first') [effect('key-first', 'first')]() { return 1; }
    @decor('second') [effect('key-second', 'second')]() { return 2; }
    static field = effect('static-field', 3);
}
console.log(order.join('|'));
console.log((new Computed() as any).first(), (new Computed() as any).second());
