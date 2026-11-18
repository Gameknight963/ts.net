export {};
const text='{"field0":0,"field1":1,"field2":2,"field3":3,"field4":4,"field5":5,"field6":6,"field7":7,"field8":8,"field9":9,"field10":10,"field11":11,"field12":12,"field13":13,"field14":14,"field15":15}';
function row():any {return JSON.parse(text);}
function seeded(...values:any[]):any[] {return [row(),row(),...values];}
const changed=row();changed.field9=91;changed.field10=undefined;changed.field11=NaN;changed.field12=Infinity;
console.log('cells',JSON.stringify(seeded(changed,row())));
changed.field9=92;
console.log('changed cell',JSON.stringify(seeded(changed)));
function reordered():any {const value=row();delete value.field2;value.field2=22;return value;}
console.log('reordered',JSON.stringify(seeded(reordered(),reordered())));
const five=JSON.parse('[{"a":1,"b":2,"c":3,"d":4,"e":5},{"a":1,"b":2,"c":3,"d":4,"e":5},{"a":6,"b":7,"c":8,"d":9,"e":10}]');
five[2].e=11;
console.log('single tail',JSON.stringify(five));
console.log('integer keys',JSON.stringify(JSON.parse('[{"2":2,"1":1,"label":3},{"2":4,"1":5,"label":6},{"1":7,"2":8,"label":9}]')));
console.log('integer snapshot',JSON.stringify(JSON.parse('[{"1":1,"2":2,"3":3,"4":4,"label":5},{"1":1,"2":2,"3":3,"4":4,"label":5},{"4":8,"3":7,"2":6,"1":5,"label":9}]')));
const symbolRow=row();delete symbolRow.field15;symbolRow[Symbol('hidden')]=symbolRow;
console.log('symbols',JSON.stringify(seeded(symbolRow,row())));
const nested=row(), later=row();
nested.field8={toJSON(){nested.field15=999;nested.added=1000;later.field0=777;return 'child';}};
console.log('hook changes',JSON.stringify(seeded(nested,later,row())));
console.log('hook aftermath',nested.field15,nested.added,later.field0);
const getter=row();let reads=0;
Object.defineProperty(getter,'field8',{get(){reads++;return 88;},enumerable:true,configurable:true});
console.log('getter',JSON.stringify(seeded(getter,row())),reads);
const reentrant=row();reentrant.field8={toJSON(){return JSON.stringify(seeded(changed,row()));}};
console.log('reentrant',JSON.stringify(seeded(reentrant,row())));
console.log('gap',JSON.stringify([row(),changed],null,1));
console.log('property list',JSON.stringify([row(),changed],['field9','field0']));
let hooks=0;
Object.defineProperty(Object.prototype,'toJSON',{get(){hooks++;return undefined;},configurable:true});
let inherited='';
try {inherited=JSON.stringify(seeded(row(),row()));} finally {delete (Object.prototype as any).toJSON;}
console.log('inherited hook',inherited,hooks);
