export {};
const prototype: any = { get value() { return this.base + 1; }, get other() { return this.base + 2; } };
const first: any = Object.create(prototype); first.base=10;
const second: any = Object.create(prototype); second.base=20;
function read() { console.log(first.value, second.value, first.other, second.other); }
read(); read();
Object.defineProperty(prototype, 'value', {get(){return this.base+3;}, configurable:true}); read();
Object.defineProperty(prototype, 'value', {value:40, writable:true, configurable:true}); read();
Object.defineProperty(prototype, 'value', {get(){return this.base+4;}, configurable:true}); read();
delete prototype.value; read();
Object.defineProperty(prototype, 'value', {get(){return this.base+5;}, configurable:true}); read();
Object.preventExtensions(prototype); read();
Object.seal(prototype); read();
const own:any={get value(){return 60;}}; console.log(own.value,own.value); Object.defineProperty(own,'value',{value:70});console.log(own.value);
const symbol=Symbol('key');const withSymbol:any={get [symbol](){return 80;}};console.log(withSymbol[symbol],withSymbol[symbol]);delete withSymbol[symbol];console.log(withSymbol[symbol]);

const keys:any={get first(){return 1;}, get second(){return 2;}}; console.log(keys.first,keys.second); delete keys.first; console.log(keys.second,keys.second); Object.defineProperty(keys,'third',{get(){return 3;},configurable:true}); console.log(keys.second,keys.third); delete keys.third; console.log(keys.second);
const changing:any={get value(){Object.defineProperty(this,'value',{value:91,configurable:true});return 90;}};console.log(changing.value,changing.value);
const removing:any={get value(){delete this.value;return 92;}};console.log(removing.value,removing.value);
const copied:any={get value(){return this.number;},number:93};console.log(copied.value);Object.preventExtensions(copied);console.log(copied.value);Object.freeze(copied);console.log(copied.value);
const setter:any={_value:0,set value(value:number){this._value=value;},get value(){return this._value;}};setter.value=94;console.log(setter.value);Object.defineProperty(setter,'value',{get(){return 95;}});console.log(setter.value);

const dynamic:any={get value(){return 96;}};
let prefix='val';let suffix='ue';console.log(dynamic[prefix+suffix],dynamic.value,dynamic[prefix+suffix]);
const receiver:any={number:97};const inheritedGetter:any={get value(){return this.number;}};
console.log(Reflect.get(inheritedGetter,'value',receiver),Reflect.get(inheritedGetter,'value',{number:98}));

const many:any={};for(let i=0;i<10;i++)Object.defineProperty(many,'key'+i,{get(){return i;},enumerable:true,configurable:true});
console.log(Object.keys(many).join(','),Object.values(many).join(','));delete many.key1;delete many.key0;
Object.defineProperty(many,'key10',{get(){return 10;},enumerable:true,configurable:true});
console.log(Object.keys(many).join(','),Object.values(many).join(','),many.key5,many.key9);
const array:any=[];for(let i=0;i<6;i++)Object.defineProperty(array,'key'+i,{get(){return i+10;},enumerable:true,configurable:true});
Object.preventExtensions(array);console.log(array.key0,array.key5,Object.keys(array).join(','));Object.seal(array);console.log(array.key3,array.key5);
