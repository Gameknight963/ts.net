const key = {value:1}, equal = {value:1};
const map = new WeakMap<object,any>([[key,'first']]);
console.log('initial',map.has(key),map.has(equal),map.get(key),map.get(equal));
console.log('chain',map.set(key,'second')===map,map.get(key),map.set(equal,undefined)===map,map.has(equal),map.get(equal));
console.log('delete',map.delete(key),map.delete(key),map.get(key),map.has(equal));
const fn=()=>1;
const array:number[]=[];
map.set(fn,'function');map.set(array,'array');
console.log('object keys',map.get(fn),map.get(array));
const set=new WeakSet([key,equal]);
console.log('set',set.has(key),set.add(key)===set,set.delete(key),set.delete(key),set.has(equal));
for(const value of [null,undefined,0,1,NaN,true,'text']) {
    console.log('primitive lookup',map.has(value as any),map.get(value as any),map.delete(value as any),set.has(value as any),set.delete(value as any));
    try{map.set(value as any,1);}catch(error){console.log('bad map key',(error as Error).name);}
    try{set.add(value as any);}catch(error){console.log('bad set key',(error as Error).name);}
}
console.log('missing',typeof (map as any).size,typeof (set as any).keys,Object.keys(map).length,Object.keys(set).length);
for(const name of ['WeakMap','WeakSet']) {
    const Constructor:any=name==='WeakMap'?WeakMap:WeakSet;
    console.log('empty',new Constructor() instanceof Constructor,new Constructor(null) instanceof Constructor);
    try{Constructor();}catch(error){console.log('requires new',name,(error as Error).name);}
    for(const input of [3,{},[3]])try{new Constructor(input);}catch(error){console.log('invalid input',name,(error as Error).name);}
    for(const method of name==='WeakMap'?['get','has','set','delete']:['has','add','delete']) {
        const action=Constructor.prototype[method];
        console.log('method',name,action.name,action.length,Object.prototype.propertyIsEnumerable.call(Constructor.prototype,method));
        for(const receiver of [{},map,set,null]) {
            if(receiver===(name==='WeakMap'?map:set))continue;
            try{action.call(receiver,key,1);}catch(error){console.log('receiver',name,method,(error as Error).name);}
        }
    }
}
let calls='';
class DerivedMap extends WeakMap<object,any> {
    marker=7;
    set(key:object,value:any):this {calls+='M';return super.set(key,value);}
}
class DerivedSet extends WeakSet<object> {
    marker=8;
    add(key:object):this {calls+='S';return super.add(key);}
}
const derivedMap=new DerivedMap([[key,1],[equal,2]]);
const derivedSet=new DerivedSet([key,equal]);
console.log('derived',calls,derivedMap.marker,derivedMap.get(equal),derivedMap instanceof WeakMap,derivedMap instanceof DerivedMap,derivedSet.marker,derivedSet.has(key),derivedSet instanceof WeakSet);
const iteratorPrototype:any=Object.getPrototypeOf(new Set().values());
let cleanupCount=0;
iteratorPrototype.return=function(this:any):any {cleanupCount++;return {};};
try{new WeakMap(new Set<any>([[key,1],[3,2]]) as any);}catch(error){console.log('map cleanup',(error as Error).name,cleanupCount);}
try{new WeakSet(new Set<any>([key,3]));}catch(error){console.log('set cleanup',(error as Error).name,cleanupCount);}
delete iteratorPrototype.return;
const alias=WeakMap;
console.log('alias',new alias([[key,8]]).get(key));
function shadow(WeakMap:any):void {console.log('shadow',new WeakMap().value);}
class Fake {value=9;}
shadow(Fake);

