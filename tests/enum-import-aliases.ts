import { Exported } from './enum-values';
import Imported = Exported.Nested;
console.log('imported', Imported.read(), Imported.Choice.Last);
enum Original { A=1 }
namespace Original {
    export namespace Inner {
        export let value=3;
        export function read():number {return value;}
        export class Box {value:number;constructor(){this.value=value;}}
        export interface Shape {value:number;}
    }
}
console.log('hoisted-alias', typeof Top);
import Top = Original.Inner;
enum Consumer { A=2 }
namespace Consumer {
    import Private = Original.Inner;
    export import Shared = Original.Inner;
    export import Whole = Original;
    export import Shape = Original.Inner.Shape;
    export function read():number {const shape:Shape={value:Private.read()};return shape.value;}
}
console.log(Top===Consumer.Shared, Consumer.Whole===Original, Consumer.read(), new Consumer.Shared.Box().value);
Original.Inner.value=7;
console.log(Top.read(), Consumer.Shared.read(), Consumer.read());
const saved=Original.Inner;
(Original as any).Inner={...saved,value:99,read:()=>99};
console.log(Top===saved, Consumer.Shared===saved, Consumer.read(), Original.Inner.read());
namespace Consumer {
    export function again():number {return Shared.read();}
}
console.log(Consumer.again(), Object.keys(Consumer).length, Object.hasOwn(Consumer,'Private'), Object.hasOwn(Consumer,'Shape'));



declare namespace Missing { namespace Inner { const value:number; } }
import Unused = Missing.Inner;
import Chain = Top;
console.log('chain', Chain===Top, Chain.read());
import {Snapshot,replace} from './enum-alias-values';
import * as aliases from './enum-alias-values';
console.log('module-alias', Snapshot===aliases.Snapshot, Snapshot.read());
replace();
console.log('module-snapshot', Snapshot===aliases.Snapshot, Snapshot.read(), aliases.Snapshot.read(), Exported.Nested.read());
const enum InlineFlags { One=1, Two=2 }
import FlagAlias = InlineFlags;
console.log('const-alias', FlagAlias.One, FlagAlias.Two);
namespace Consumer {
    export namespace Forward {
        console.log('local-hoisted-alias', typeof Local);
        import Local = Original.Inner;
        console.log('local-alias', Local.read());
    }
}
import ClassAlias = Original.Inner.Box;
import FunctionAlias = Original.Inner.read;
console.log('value-aliases', new ClassAlias().value, FunctionAlias());
