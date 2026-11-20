export {};
const direct:any={value:0,stable:1};
const alias=direct;
for(const value of [null,false,true,'text',undefined,123,1n,{nested:7},[1,2]]) {
    alias.value=value;
    console.log('direct',typeof direct.value,String(direct.value),direct.stable,Object.keys(direct).join(','));
}
const source={value:0,stable:2};
function replace(target:any,value:any):void {target.value=value;}
replace(source,'changed');
console.log('parameter',String((source as any).value),source.stable);
let reassigned:any={value:1};const first=reassigned;
reassigned={value:2};replace(reassigned,false);replace(first,null);
console.log('reassigned',String(first.value),String(reassigned.value));
function create():any {return {value:3,stable:4};}
const returned=create();returned.value='returned';console.log('return',returned.value,returned.stable);
const holder={inner:{value:5}};
const nested:any=holder.inner;nested.value='nested';console.log('nested',(holder.inner as any).value);
const container:any[]=[{value:6}];
const element:any=container[0];element.value='element';console.log('container',container[0].value);
const alternative:any=Date.now()>0?{value:7}:{value:8};
alternative.value='conditional';console.log('conditional',alternative.value);
const computed:any={value:9,stable:10};
function key():string{return 'value';}
computed[key()]='computed';console.log('computed',computed.value,computed.stable);
const numeric:any={value:10};numeric.value=11;console.log('compatible',numeric.value);
const typed={safe:12};typed.safe=13;console.log('unrelated',typed.safe);
const zero:any={value:0};zero.value=-0;console.log('zero',Object.is(zero.value,-0));
const compound:any={value:1};compound.value+='x';console.log('compound',compound.value);
const logical:any={value:0};logical.value||='fallback';console.log('logical',logical.value);
function makeAny(value:any):any{return value;}
const forwarded=makeAny({value:11});forwarded.value='forwarded';console.log('forwarded',forwarded.value);
const updated:any={value:'2'};console.log('updates',updated.value++,++updated.value,updated.value,typeof updated.value);
const entries:any[]=[{value:0},{value:1}];for(const entry of entries){entry.value='iteration';}console.log('iteration',entries[0].value,entries[1].value);
