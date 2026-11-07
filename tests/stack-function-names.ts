function buildError(message:string):Error {
    const pieces=[message,message.toUpperCase(),message.toLowerCase()];
    for(let index=0;index<4;index++)pieces.push(message+String(index));
    return new Error(pieces.join("|"));
}
const error=buildError("named");console.log("stack",error.stack?.includes("buildError"));
const arrowName=(value:number)=>value+1;
const object={"read value"(){return 1;},"\u96ea"(){return 2;},"\0"(){return 3;}};
console.log("names",buildError.name,arrowName.name,object["read value"].name,object["\u96ea"].name,JSON.stringify(object["\0"].name));
function outerName(){let total=0;return function innerName(value:number){total+=value;return total;};}
const inner=outerName();console.log("closure",inner.name,inner(2),inner(3));
function sameName(value:number){return value;}
{function sameName(value:number){return value+1;}console.log("shadow",sameName(3));}
console.log("outer",sameName(3));

const unicode={"\ud801\udc00"(){return 1;},"cafe\u0301"(){return 2;}};console.log("unicode",JSON.stringify(unicode["\ud801\udc00"].name),JSON.stringify(unicode["cafe\u0301"].name));

const lone={"\ud800"(){return 4;}};console.log("lone",JSON.stringify(lone["\ud800"].name),lone["\ud800"]());
