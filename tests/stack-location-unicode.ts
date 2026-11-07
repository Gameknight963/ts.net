const greeting="🙂"; const failure=new Error("unicode");
console.log("unicode",failure.stack?.replace(/:line (\d+)/g,":$1").includes("stack-location-unicode.ts:1"));
function optimizedLoop(limit:number):Error {
    let total=0;
    for(let i=0;i<limit;i++)total+=i;
    return new Error(String(total));
}
console.log("loop",/stack-location-unicode.ts:(6|8)(?:\D|$)/.test((optimizedLoop(10000).stack??"").replace(/:line (\d+)/g,":$1")));
