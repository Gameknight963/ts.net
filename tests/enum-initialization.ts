let saved: any;
let read: () => number;
enum Capture { Root = 0 }
namespace Capture {
    export enum Value {
        First = initialize(),
        Second = First + 1,
        Third = (() => { read = () => Value.Second; return 12; })(),
    }
    function initialize(): number {
        saved = Value;
        (Capture as any).Value = {First: 90, Second: 91, Third: 92};
        return 10;
    }
}
console.log('captured', saved.First, saved.Second, saved.Third, read!(), Capture.Value.First, Capture.Value.Second);

let partial: any;
function fail(value: any): number { partial = value; throw new Error('initializer'); }
try {
    enum Broken { First = 4, Second = fail(Broken), Third = 6 }
} catch (error) { console.log('throw', (error as Error).message); }
console.log('partial', partial.First, partial[4], Object.hasOwn(partial, 'Second'), Object.hasOwn(partial, 'Third'));

enum Collisions { A = 1, B = 1, C = 2 }
console.log('duplicate', Collisions[1], Collisions[2], Object.keys(Collisions).join(','));

const remembered: (() => number)[] = [];
function remember(value: () => number): number { remembered.push(value); return remembered.length; }
for (let i = 0; i < 3; i++) {
    enum Local { First = i, Second = remember(() => Local.First) }
    console.log('iteration', Local.First, Local.Second);
}
console.log('closures', remembered[0](), remembered[1](), remembered[2]());


