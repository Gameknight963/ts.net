function masked(value: number) { return (value & 255) % 26; }
function general(value: number, divisor: number) { return value % divisor; }
for (const value of [0,-0,-1,1,25,26,255,256,-256,2147483647,2147483648,4294967295,4294967296,9007199254740991,1e100,NaN,Infinity,-Infinity]) {
    console.log(value,masked(value),((value&255)%26)%7,((value&255)|16)%13,((value&255)^31)%13);
    const negative=(value|0)%26;
    console.log(negative,1/negative);
    for(const divisor of [0,-0,0.5,-1,26,2147483648,NaN,Infinity]) console.log(general(value,divisor));
}
console.log(1/(-0%26),1/((-26|0)%26),1/(((-1>>>0)&255)%26));
console.log(((-1>>>0)%26),(-1&-1)%26,(255&255)%26);
let order='';
const left: any={valueOf:()=>{order+='L';return 259;}};
const right: any={valueOf:()=>{order+='R';return 255;}};
function readRight(): any {order+='E';return right;}
console.log((left&readRight())%26,order);
let calls=0;
function input(): any {calls++;return -1;}
console.log(((input()&255)%26)%7,calls);
const missing: any=undefined;
console.log((missing&255)%26,((NaN|0)&255)%26);
console.log((123&255)%26%26%26%26%26%26%26%26%26%26%26%26%26%26%26%26%26%26%26%26%26%26%26%26%26%26%26%26%26%26%26%26);
