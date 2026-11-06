function identity(value: any): any { return value; }
function notANumber(): number { return 0 / 0; }
const object: any = {valueOf: () => 1};
const stringObject: any = {valueOf: () => '1'};
const values: any[] = [undefined, null, false, true, 0, -0, 1, 2, NaN, Infinity, '', '0', '1', '2', '  ', '0x10', 'invalid', [], [0], [1], [null], {}, object, stringObject];
for (let i=0;i<values.length;i++) {
    for (let j=0;j<values.length;j++) {
        const left=values[i],right=values[j];
        console.log(i,j,left == right,left != right);
    }
}
function nullableNumbers(left: number | undefined, right: number | undefined): void {
    console.log(left == right,left != right,left == null,null == left,left != undefined,undefined != left);
}
nullableNumbers(1,1);nullableNumbers(1,2);nullableNumbers(undefined,undefined);nullableNumbers(undefined,1);nullableNumbers(NaN,NaN);
function nullableBooleans(left: boolean | undefined,right: boolean | undefined): void {
    console.log(left == right,left != right,left == null,left != null);
}
nullableBooleans(true,true);nullableBooleans(true,false);nullableBooleans(undefined,undefined);
function strings(left: string,right: string): void {console.log(left == right,left != right);}
strings('2','2');strings('02','2');strings(undefined as any,undefined as any);
let effects='';
const changing: any={valueOf: (() => 1) as any};
function rhs(): number {effects+='E';changing.valueOf=()=>{effects+='L';return 4;};return 4;}
console.log(changing == rhs(),effects);
effects='';
function number(): number {effects+='N';return 4;}
console.log(number() == changing,effects);
let calls=0;
const bad: any={valueOf:()=>{calls++;return {};},toString:()=>{calls++;return {};}};
console.log(bad == bad,bad == identity({}),bad == null,bad == undefined,bad != null,calls);
const counted: any={valueOf:()=>{calls++;return 1;}};
console.log(counted == notANumber(),notANumber() == counted,calls);
function shrinking(): string {
    const values: any[]=[0,1,2,3];
    const coercer: any={valueOf:()=>{values.length=0;return 0;}};
    let result='';
    for (let i=0;i<4;i++) result+=values[i] == coercer ? 'T' : 'F';
    return result;
}
console.log(shrinking());

function indexedNullish(values: (number | undefined)[]): void {
    for (let i=0;i<8;i++) console.log(i,values[i & 7] == null,undefined != values[i & 7]);
}
const numeric: (number | undefined)[]=[undefined,NaN,-0,Infinity,1];
numeric.length=8;
indexedNullish(numeric);
const generic: any[]=[undefined,NaN,-0,Infinity,1];
indexedNullish(generic as any);
function nullableCount(count: number,seed: number): number {
    const values: (number | undefined)[]=[0,1,undefined,2,3,undefined,4,5];
    let total=seed;
    for (let i=0;i<count;i++) total+=values[(i+seed)&7] == null ? 1 : 0;
    return total;
}
console.log(nullableCount(100,0));
