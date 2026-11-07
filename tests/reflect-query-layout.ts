const reflection=Reflect;
console.log(reflection.has({},'missing'));
function work():number {
    let sum=0;
    for(let i=0;i<100000;i++){const point={x:i,y:i+1};sum+=point.x+point.y;}
    return sum;
}
console.log(work());
