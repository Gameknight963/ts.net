import {Exported as Renamed,Inlined} from "./enum-values";
import * as names from "./enum-values";
console.log(Renamed.A,Renamed.C,Renamed[4],names.Exported.B,Inlined.Value,names.Inlined.Text);
console.log(Object.keys(Renamed).length);
console.log(Renamed.state,Renamed.next(),names.Exported.next());
Renamed.state=8;
console.log(names.Exported.next(),Renamed.state);
