export {};
type Point = {x:number,y:number};
function add(point:Point):number {return point.x+point.y;}
function reciprocal(point:Point):number {return 1/point.x;}
function create(x:number,y:number):Point {return {x,y};}
for(const value of [0,-0,NaN,Infinity,-Infinity,5e-324,17]){
    const point=create(value,2);
    console.log(add(point),reciprocal(point),point.x===undefined,Number.isNaN(point.x));
    const alias:any=point;
    alias.x=undefined;
    console.log(add(point),reciprocal(point),point.x===undefined,Number.isNaN(point.x));
}
let reads=0;
const accessor=create(1,2);
Object.defineProperty(accessor,"x",{get(){reads++;return 11;},configurable:true});
console.log(add(accessor),reads);
const structural={get x(){reads++;return 13;},y:3};
console.log(add(structural),reads);
const missing:any={y:7};
console.log(add(missing));
const optional:{x?:number,y:number}={y:5};
Object.setPrototypeOf(optional,{x:19});
console.log(optional.x!+optional.y);
