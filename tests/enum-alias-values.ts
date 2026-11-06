import {Exported} from './enum-values';
export import Snapshot = Exported.Nested;
export function replace():void {
    Exported.Nested={...Exported.Nested, value:90, read:()=>90};
}
