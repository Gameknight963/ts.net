export {};
for(const firstDayOfWeek of [undefined,0,1,2,3,4,5,6,7,'mon','MON','sun','zzz','foo-bar',null,true,false,'true','false']) {
 const value: any=new Intl.Locale('en-US-u-fw-fri-ca-gregory',{firstDayOfWeek} as any);
 console.log(value.toString(),value.firstDayOfWeek,value.calendar);
 const copy: any=new Intl.Locale(value);console.log('copy',copy.toString(),copy.firstDayOfWeek);
}
for(const tag of ['en','en-u-fw-mon','en-u-fw','en-u-fw-true','en-u-fw-unknown']) {
 const value:any=new Intl.Locale(tag);console.log('tag',value.toString(),value.firstDayOfWeek);
}
for(const firstDayOfWeek of ['',8,-1,1.5,'m','bad_value']) {
 try{new Intl.Locale('en',{firstDayOfWeek} as any);}catch(error){console.log('invalid',(error as Error).name);}
}
let events='';
const value:any=new Intl.Locale('en',{
 get calendar(){events+='calendar|';return undefined;},
 get collation(){events+='collation|';return undefined;},
 get firstDayOfWeek(){events+='first|';return {toString(){events+='text|';return '2';}};},
 get hourCycle(){events+='hour|';return undefined;}
} as any);
console.log('order',events,value.toString(),value.firstDayOfWeek);
const descriptor=Object.getOwnPropertyDescriptor(Intl.Locale.prototype,'firstDayOfWeek')!;
try{descriptor.get!.call({});}catch(error){console.log('receiver',(error as Error).name);}
