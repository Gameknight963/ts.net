let effects = '';

function target(first: number, second: number): number {return first + second;}
const functionPrototype = Object.getPrototypeOf(target);
const bind: any = target.bind;
const inherited = {
    get length() {effects += 'L'; return 99;},
    get name() {effects += 'N'; return 'inherited';}
};
Object.setPrototypeOf(inherited, functionPrototype);
delete (target as any).length;
delete (target as any).name;
Object.setPrototypeOf(target, inherited);
const bound = bind.call(target, null, 1);
console.log('inherited', bound.length, bound.name, effects, bound(2));
const original = Object.create(functionPrototype), replacement = Object.create(functionPrototype);
function lengthKey(): string {return 'length';}
function changeLength(): number {
    effects += 'L';
    Object.setPrototypeOf(Changing, replacement);
    return 3;
}
class Changing {
    static get [lengthKey()](): number {
        return changeLength();
    }
}
Object.setPrototypeOf(Changing, original);
effects = '';
const changed = bind.call(Changing, null);
console.log('captured', Object.getPrototypeOf(changed) === original, Object.getPrototypeOf(Changing) === replacement, changed.length, effects);
