enum Tree { Root = 1 }
namespace Tree {
    export namespace Branch {
        let secret = 10;
        export let count = 2;
        export function next(): number { return secret + ++count + Tree.Root; }
        export enum Kind { First = 3, Second }
        export namespace Leaf {
            export const label = 'leaf';
            export function read(): number { return count + Kind.Second; }
        }
    }
}
const branch = Tree.Branch;
console.log(Tree.Root, branch.count, branch.next(), branch.Kind[4], branch.Leaf.label, branch.Leaf.read());
namespace Tree {
    export namespace Branch {
        export function later(): number { return count + extra; }
        export let extra = 7;
    }
}
console.log(branch === Tree.Branch, branch.later(), Object.keys(branch).length, Object.keys(branch)[0], Object.keys(branch)[4]);
branch.count = 20;
console.log(Tree.Branch.next(), branch.Leaf.read(), branch.later());


namespace Tree.Branch.Twig {
    export let size = 5;
    export function grow(): number { return ++size; }
}
console.log(Tree.Branch.Twig.grow(), Tree.Branch.Twig.size);
namespace Tree {
    namespace Hidden { export let value = 17; }
    export function hidden(): number { return Hidden.value; }
    export namespace Timing {
        console.log('before', typeof (Timing as any).read, read());
        export let value = 9;
        export function read(): number { return typeof value === 'undefined' ? -1 : value; }
        console.log('after', typeof (Timing as any).read, read());
    }
}
console.log(Tree.hidden(), Object.hasOwn(Tree, 'Hidden'));
namespace Tree {
    export namespace Timing {
        const local = read;
        Timing.read = () => 55;
        console.log('local-function', read(), Timing.read(), local === read);
    }
}
console.log('published-function', Tree.Timing.read());
for (const key of Object.keys(branch)) console.log('branch-key', key);
namespace Tree {
    export namespace Patterns {
        export let [first, second = first + 1, ...tail] = [4, undefined, 8, 9];
        export let {x: renamed, missing = second + 1, ...rest} = {x: 10, y: 11} as {x:number, y:number, missing?:number};
        export let {nested: [inner]} = {nested: [12]};
        export function sum():number {return first + second + renamed + missing + inner;}
    }
}
console.log('patterns', Tree.Patterns.sum(), Tree.Patterns.tail.length, Tree.Patterns.rest.y);
Tree.Patterns.first = 20;
console.log('patterns-live', Tree.Patterns.sum(), Object.keys(Tree.Patterns).length);
namespace Tree {
    export namespace FunctionScope {
        export function read():number {return 3;}
        const original=read;
        FunctionScope.read=()=>4;
        console.log('function-scope', read(), FunctionScope.read(), original===read);
    }
}

let capturedEnum: any;
namespace Tree {
    export enum Replaced {
        First = replaceEnum(),
        Second = First + 1,
    }
}
function replaceEnum(): number {
    capturedEnum = Tree.Replaced;
    (Tree as any).Replaced = {First: 31, Second: 32};
    return 21;
}
console.log('enum-capture', capturedEnum.First, capturedEnum.Second, capturedEnum[22], Tree.Replaced.First, Tree.Replaced.Second);
