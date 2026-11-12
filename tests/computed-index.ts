const numbers: number[] = [10, 20, 30, 40];
const mixed: any[] = [10, "twenty", undefined, null];
for (let i = 0; i < 12; i++) {
    console.log(numbers[i % 4], mixed[i & 3], Object.hasOwn(mixed, i % 4));
    numbers[i % 4] += 1;
    mixed[i & 3] = i;
}
console.log(numbers[0], numbers[3], mixed[0], mixed[3]);
const mask = 3;
let unchanged = 2;
console.log(numbers[unchanged], numbers[-1 & mask], numbers[(7 & 3) ^ 1]);
console.log(numbers[(7 & 1) | 2], numbers[(4294967295 & 3)]);
const dynamic: any = numbers;
dynamic[-1] = 77;
dynamic["NaN"] = 88;
dynamic["1.5"] = 99;
console.log(numbers[-1 | 0], numbers[-1 % 4], numbers[1.5 % 4], numbers[0 % 0]);
console.log(numbers[(2147483648 | 0)], numbers[2147483648 >>> 0]);
let index = 0;
function advance() { index++; return index; }
console.log(numbers[advance() & 3], index);
numbers[advance() & 3] += advance();
console.log(numbers[2], index);
const original: number[] = [1, 2, 3, 4];
let chosen = original;
function replace() { chosen = [11, 12, 13, 14]; return 1; }
console.log(chosen[replace() & 3], original[1], chosen[1]);
function arithmetic(count: number) {
    let total = 0;
    for (let i = 1; i < count; i++) {
        total += numbers[i % 4];
        if (i === 5) break;
    }
    return total;
}
console.log(arithmetic(4.5), arithmetic(2147483648));

{
function read(value: any, key?: number): any {return value[key!];}
const object: any = {'0':'zero','NaN':'nan','Infinity':'positive','-Infinity':'negative','1e-7':'small','4294967295':'large',undefined:'missing'};
for (const key of [-0,NaN,Infinity,-Infinity,1e-7,4294967295]) console.log('numeric key', String(key), read(object,key));
console.log('missing numeric key', read(object));
const array: any = [1,2]; let reads = 0;
Object.defineProperty(array,'0',{get(){reads++;return 7;},configurable:true});
const prototype = Object.create(Array.prototype);
Object.defineProperty(prototype,'1',{get(){reads++;return 9;},configurable:true});
delete array[1]; Object.setPrototypeOf(array,prototype);
console.log('numeric accessors',read(array,-0),read(array,1),reads);
const text: any = 'A\uD800B';
for (const key of [0,1,2,-1,0.5,NaN,Infinity]) {
 const item = read(text,key); console.log('numeric string key',String(key),item===undefined?'missing':item.charCodeAt(0));
}
const bytes: any = new Uint8Array([11,12]);
for (const key of [-0,0,1,-1,NaN,Infinity,4294967295]) console.log('numeric typed key',String(key),read(bytes,key));
let effects=0;function key():number{effects++;return 0;}
try {const value:any=null;console.log(value[key()]);}catch(error){console.log('numeric null order',effects,(error as Error).name);}
}
