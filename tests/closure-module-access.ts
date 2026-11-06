let moduleValue=1;
function readModule():number{return moduleValue;}
function createModuleClosure(step:number){
    return ()=>{moduleValue+=step;return {value:moduleValue,read:readModule};};
}
const moduleClosure=createModuleClosure(2);
const first=moduleClosure();const second=moduleClosure();
console.log(first.value,second.value,first.read(),first.read===second.read,first.read.name);
