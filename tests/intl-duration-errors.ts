export {};
function fail(label:string,action:()=>unknown):void{try{action();console.log(label,'unexpected');}catch(error){console.log(label,(error as Error).name);}}
for(const options of [null,{style:'invalid'},{years:'numeric'},{days:'2-digit'},{hours:'long',minutes:'numeric',seconds:'long'},{hours:'numeric',minutes:'long'},{milliseconds:'2-digit'},{milliseconds:'numeric',millisecondsDisplay:'always'},{microseconds:'numeric',microsecondsDisplay:'always'},{fractionalDigits:-1},{fractionalDigits:10},{fractionalDigits:NaN},{localeMatcher:'bad'},{numberingSystem:'a'},{secondsDisplay:'invalid'}])fail('options',()=>new Intl.DurationFormat('en-US',options as any));
const f=new Intl.DurationFormat('en-US',{style:'digital'});
for(const value of [undefined,null,0,'PT1H',{},[],{hours:1,minutes:-1},{hours:1.5},{hours:Infinity},{seconds:NaN},{days:1n},{days:Symbol()},{years:4294967296},{months:4294967296},{weeks:4294967296},{days:104249991375},{seconds:9007199254740992},{hours:1e100}])fail('duration',()=>f.format(value as any));
for(const name of ['format','formatToParts','resolvedOptions'] as const){const method=f[name] as Function;fail('receiver '+name,()=>method.call({},{}));const descriptor=Object.getOwnPropertyDescriptor(Intl.DurationFormat.prototype,name)!;console.log(name,method.name,method.length,descriptor.writable,descriptor.enumerable,descriptor.configurable);}
fail('requires new',()=>(Intl.DurationFormat as any)());
const trace:string[]=[];const input={get days(){trace.push('days');return 0;},get hours(){trace.push('hours');return {valueOf(){trace.push('hour value');return 1;}};},get microseconds(){trace.push('microseconds');return 2;},get milliseconds(){trace.push('milliseconds');return 3;},get minutes(){trace.push('minutes');return 4;},get months(){trace.push('months');return 0;},get nanoseconds(){trace.push('nanoseconds');return 5;},get seconds(){trace.push('seconds');return 6;},get weeks(){trace.push('weeks');return 0;},get years(){trace.push('years');return 0;}};
console.log('read order',f.format(input as any),trace.join(','));
trace.length=0;fail('sign order',()=>f.format({get days(){trace.push('days');return 1;},get hours(){trace.push('hours');return -1;},get years(){trace.push('years');return 1;}}));console.log(trace.join(','));
trace.length=0;fail('nan order',()=>f.format({get days(){trace.push('days');return NaN;},get hours(){trace.push('hours');return 1;}}));console.log(trace.join(','));
trace.length=0;fail('option order',()=>new Intl.DurationFormat('en-US',{get localeMatcher(){trace.push('matcher');return 'bad' as any;},get numberingSystem(){trace.push('numbers');return 'latn';}}));console.log(trace.join(','));
trace.length=0;const locale={toString(){trace.push('locale');return 'en-US';}};new Intl.DurationFormat([locale as any],{get style(){trace.push('style');return 'short' as const;}});console.log(trace.join(','));
class Derived extends Intl.DurationFormat {extra=42;}
const derived=new Derived('en-US',{style:'digital'});console.log(derived instanceof Derived,derived instanceof Intl.DurationFormat,derived.extra,derived.format({seconds:1}),Object.prototype.toString.call(derived));
const frozen=Object.freeze(new Intl.DurationFormat('en-US',{style:'digital'}));console.log('frozen',frozen.format({hours:1}));
const parts=f.formatToParts({hours:1});parts[0].value='changed';console.log('fresh',f.formatToParts({hours:1})[0].value!=='changed');
console.log('supported',Intl.DurationFormat.supportedLocalesOf(['en-US','de-DE','xx-YY']).join(','));
