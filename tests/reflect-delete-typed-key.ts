function remove(value: { first?: number; second?: number }, key: string): boolean {
    return Reflect.deleteProperty(value,key);
}
const value={first:1,second:2};
console.log('before',Object.keys(value).join(','));
console.log('remove',remove(value,'first'),Object.keys(value).join(','),Reflect.has(value,'first'));
console.log('again',remove(value,'first'));
value.first=3;
console.log('restore',value.first,Object.keys(value).join(','));
function erase(value:{ left?:string; right?:string },key:string):boolean {
    return Reflect['deleteProperty'](value,key);
}
const named={left:'a',right:'b'};
console.log('index method',erase(named,'right'),Reflect.get(named,'right'),Object.keys(named).join(','));
const alias=named;
console.log('alias',erase(alias,'left'),Object.keys(named).length);
function factory(){return {count:4,label:'x'};}
function drop(value:ReturnType<typeof factory>,key:string):boolean {return Reflect.deleteProperty(value,key);}
const built=factory();
console.log('factory',drop(built,'count'),Reflect.has(built,'count'),Reflect.get(built,'count'));
const reflection=Reflect;
console.log('query alias',reflection.get({unrelated:7},'unrelated'));
