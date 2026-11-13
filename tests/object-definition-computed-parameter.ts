export {};
function invoke(method:any,target:object):object {return method(target);}
const method:string='seal';const target={value:1};console.log(invoke((Object as any)[method],target)===target,Object.isSealed(target));
