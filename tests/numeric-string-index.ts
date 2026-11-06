const source='A\uD83D\uDE00\uD800Z';
const dynamicSource: any=source;
for(const index of [-Infinity,-4294967296,-1,-0.5,-0,0,0.5,1,2,3,4,5,4294967296,NaN,Infinity]) {
    const value=source[index];
    console.log(index,value===dynamicSource[index],value===undefined,value===undefined?0:value.charCodeAt(0));
}
for(let i=-2;i<8;i++)console.log(source[i]===dynamicSource[i]);
const missing: number=undefined as unknown as number;
console.log(source[missing],''[0]);
let calls=0;
function index():number {calls++;return 1;}
console.log(source[index()].charCodeAt(0),calls);
console.log(source[-0],source[0.5],source[4294967295]);
