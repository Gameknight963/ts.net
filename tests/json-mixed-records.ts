export {};
const rows = JSON.parse('[{"id":1,"name":"first","enabled":true,"extra":null},{"id":2,"name":null,"enabled":false,"extra":"text"},{"id":3,"name":7,"enabled":true,"extra":null},{"name":"reordered","id":4,"enabled":true,"extra":null}]');
for (const row of rows) console.log(JSON.stringify(row), Object.keys(row).join(','));
const first = rows[0];
console.log(Object.getPrototypeOf(first) === Object.prototype, 'toString' in first);
first.name = 'changed';
first.enabled = null;
first.extra = false;
console.log(JSON.stringify(first));
first.name = 9;
first.id = 'id';
console.log(JSON.stringify(first));
const changed = JSON.parse('[{"id":1,"name":"text","enabled":true}]')[0];
delete changed.name;
changed.name = 'last';
console.log(Object.keys(changed).join(','), JSON.stringify({...changed}));
const described = JSON.parse('[{"name":"text","enabled":false}]')[0];
Object.defineProperty(described, 'name', {get: () => 'getter'});
console.log(described.name, described.enabled, JSON.stringify(described));
const frozen = Object.freeze(JSON.parse('[{"name":"text","enabled":false}]')[0]);
try { frozen.enabled = true; } catch { console.log('frozen'); }
console.log(frozen.name, frozen.enabled);
const keys = Array.from({length: 32}, (_, i) => '"p' + i + '":' + (i === 31 ? '"last"' : i)).join(',');
const wide = JSON.parse('[{' + keys + '},{' + keys + '}]');
wide[0].p31 = false;
wide[1].p31 = 32;
console.log(wide[0].p30, wide[0].p31, wide[1].p31, Object.keys(wide[1]).length);
console.log(JSON.stringify(JSON.parse('[{"text":"a\\nb\\uD800","flag":true},{"text":"\\uDFFF","flag":null}]')));
for (const text of ['[{"a":"text"},{"a":tru}]', '[{"a":null},{"a":false,}]']) {
    try { JSON.parse(text); } catch { console.log('invalid'); }
}

{
 type Row={id:number|undefined;name:string;flag?:boolean};
 function read(row:Row):string{return row.name.toUpperCase()+':'+(row.id===undefined?'missing':row.id);}
 type Extra={name:string;extra:string};
 function extra(row:Extra):number{return row.name.length+row.extra.length;}
 const typed=JSON.parse('[{"id":1,"name":"first","flag":true,"extra":"x"},{"id":2,"name":"second","flag":false,"extra":"y"}]') as Row[];
 const parsed:any[]=typed;
 console.log('typed records',read(parsed[0]),read(parsed[1]),extra(parsed[0]),read(parsed[0]));
 parsed[0].id=undefined;
 console.log('typed own undefined',read(parsed[0]),Object.hasOwn(parsed[0],'id'),read(parsed[1]));
 delete parsed[0].name;
 try{read(parsed[0]);console.log('typed deleted allowed');}catch(error){console.log('typed deleted',(error as Error).name);}
 console.log('typed independent row',read(parsed[1]));
 Object.setPrototypeOf(parsed[0],{name:'inherited'});
 console.log('typed inherited',read(parsed[0]));
 let gets=0;
 Object.defineProperty(parsed[1],'name',{get(){gets++;return 'getter';},configurable:true});
 console.log('typed accessor',read(parsed[1]),gets);
 const missingOptional:any=JSON.parse('{"id":3,"name":"optional"}');
 console.log('typed optional',read(missingOptional));
 const proxy=new Proxy(missingOptional,{get(target,key,receiver){gets++;return Reflect.get(target,key,receiver);}});
 const oldGets=gets;console.log('typed proxy',read(proxy),gets-oldGets);
 const frozen:any=Object.freeze(JSON.parse('{"id":4,"name":"frozen"}'));
 console.log('typed frozen',read(frozen));
 const incompatible:any=JSON.parse('{"id":5,"flag":true}');
 try{read(incompatible);console.log('typed missing allowed');}catch(error){console.log('typed missing',(error as Error).name);}
}

{
 type Wide={p0:number|undefined;p1:number|undefined;p2:number|undefined;p3:number|undefined;p4:number|undefined;p5:number|undefined;p6:number|undefined;p7:number|undefined;p8:number|undefined;p9:number|undefined;p10:number|undefined;p11:number|undefined;p12:number|undefined;p13:number|undefined;p14:number|undefined;p15:number|undefined;p16:number|undefined;p17:number|undefined;p18:number|undefined;p19:number|undefined;p20:number|undefined;p21:number|undefined;p22:number|undefined;p23:number|undefined;p24:number|undefined;p25:number|undefined;p26:number|undefined;p27:number|undefined;p28:number|undefined;p29:number|undefined;p30:number|undefined;p31:number|undefined;};
 function endpoints(value:Wide):string{return JSON.stringify({first:value.p0,last:value.p31});}
 const text="{"+Array.from({length:32},(_,i)=>JSON.stringify("p"+i)+":"+i).join(",")+"}";
 const typedWide=JSON.parse("["+text+","+text+"]") as Wide[];
 const wide:any[]=typedWide;
 console.log("typed wide",endpoints(wide[0]),endpoints(wide[1]));
 wide[0].p31=undefined;console.log("typed wide undefined",endpoints(wide[0]),endpoints(wide[1]));
}

{
 type Item={code:number;label:string};
 const mapped=JSON.parse('{"a":{"code":1,"label":"one"},"b":{"code":2,"label":"two"}}') as Record<string,Item>;
 function read(item:Item):string{return item.label+':'+item.code;}
 console.log('typed JSON dictionary',read(mapped.a),read(mapped.b));
}

{
 type Value={value:number|undefined};
 const rows=JSON.parse('[{"value":1000.5,"other":2},{"value":2000.5,"other":3}]') as Value[];
 const untyped:any[]=rows;
 function read(index:number):number|undefined{return rows[index].value;}
 console.log('single field',read(0),read(1));
 untyped[0].value=undefined;console.log('single undefined',read(0)===undefined,read(1));
 delete untyped[0].value;Object.setPrototypeOf(untyped[0],{value:7});
 console.log('single inherited',read(0),read(1));
 let gets=0;Object.defineProperty(untyped[1],'value',{get(){gets++;return 11;}});
 console.log('single accessor',read(1),gets);
 const key=['v','a','l','u','e'].join('');
 const raw:any=JSON.parse('{"value":17,"other":19}');
 console.log('equal key',raw.value,raw[key],raw.other,raw[key]);
}
