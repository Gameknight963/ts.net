export {};
const format=new Intl.DateTimeFormat('en-US',{timeZone:'UTC',year:'numeric',month:'short',day:'numeric'});
function failure(label:string,action:()=>any):void{try{action();console.log(label,'unexpected');}catch(error){console.log(label,(error as Error).name);}}
for(const pair of [[undefined,0],[0,undefined],[NaN,0],[0,NaN],[Infinity,0],[0,-Infinity],[8640000000000001,0],[1n,0],[Symbol(),0]]){
    failure('invalid',()=>format.formatRange(pair[0] as any,pair[1] as any));
}
const trace:string[]=[];
const first={valueOf(){trace.push('first');return 0;}};
const second={valueOf(){trace.push('second');return 86400000;}};
console.log('coercion',format.formatRange(first as any,second as any).replace(/\s/g,''),trace.join(','));
trace.length=0;
failure('invalid converted',()=>format.formatRange({valueOf(){trace.push('first');return NaN;}} as any,second as any));
console.log('conversion order',trace.join(','));
const method=format.formatRange;
failure('receiver',()=>method.call({},0,1));
const descriptor=Object.getOwnPropertyDescriptor(Intl.DateTimeFormat.prototype,'formatRange')!;
console.log('method',method.name,method.length,descriptor.writable,descriptor.enumerable,descriptor.configurable);
console.log('equal',format.formatRange(0,0)===format.format(0));
console.log('reverse',format.formatRange(86400000,0).replace(/\s/g,''));
const frozen=Object.freeze(new Intl.DateTimeFormat('en-US',{timeZone:'UTC',year:'numeric',month:'long',day:'numeric'}));
console.log('frozen',frozen.formatRange(0,86400000).replace(/\s/g,''));
for(const cycle of ['h12','h23'] as const){
    const f=new Intl.DateTimeFormat('en-US',{timeZone:'UTC',hour:'2-digit',minute:'2-digit',hourCycle:cycle});
    console.log('cycle',cycle,f.formatRange(0,3600000).replace(/\s/g,''));
}
for(const zone of ['America/New_York','+05:30']){
    const f=new Intl.DateTimeFormat('en-US',{timeZone:zone,year:'numeric',month:'short',day:'numeric',hour:'numeric',minute:'2-digit'});
    console.log('zone',zone,f.formatRange(Date.UTC(2024,2,10,6,30),Date.UTC(2024,2,10,8,30)).replace(/\s/g,''));
}
