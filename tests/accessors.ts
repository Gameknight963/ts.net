let effects='';
const value={stored:2,get number(){effects+='G';return this.stored;},set number(next:number){effects+='S';this.stored=next;}};
console.log(effects,Object.keys(value).length,Object.hasOwn(value,'number'));
console.log(value.number,effects);value.number=5;console.log(value.number,value.stored,effects);
value.number+=2;value.number++;console.log(value.stored,effects);
const snapshot={...value};console.log(snapshot.number,effects);value.stored=10;console.log(snapshot.number,value.number);
console.log(Object.values(value).length,Object.entries(value).length);
class BaseAccessors {
    stored=3;
    get number():number{return this.stored;}
    set number(value:number){this.stored=value;}
    static stored=4;
    static get number():number{return this.stored;}
    static set number(value:number){this.stored=value;}
}
class ChildAccessors extends BaseAccessors {
    override get number():number{return super.number+1;}
    override set number(value:number){super.number=value+2;}
    static override get number():number{return super.number+10;}
    static override set number(value:number){super.number=value+20;}
}
const child=new ChildAccessors();console.log(child.number);child.number=8;console.log(child.number,child.stored);
console.log(Object.hasOwn(child,'number'),Object.keys(child).length,Object.keys(BaseAccessors.prototype).length);
console.log(ChildAccessors.number);ChildAccessors.number=1;console.log(ChildAccessors.number,ChildAccessors.stored,BaseAccessors.stored);
const getOnly:any={get number(){return 7;}};
try{getOnly.number=8;}catch(error){console.log(error instanceof TypeError);}
const setOnly:any={set number(value:number){effects+='W'+value;}};
console.log(setOnly.number===undefined);setOnly.number=3;console.log(effects);
class GetOnly {get number(){return 9;}}
const readOnly:any=new GetOnly();try{readOnly.number=1;}catch(error){console.log(error instanceof TypeError);}
let keyCalls=0;function accessorKey():'slot'{keyCalls++;return 'slot';}
const computed={stored:1,get [accessorKey()](){return this.stored;},set [accessorKey()](next:number){this.stored=next;}};
console.log(keyCalls,(computed as any).slot);(computed as any).slot=11;console.log(computed.stored,keyCalls);
const setterOnly={set value(next:number){effects+='T'+next;}};
console.log(setterOnly.value,typeof setterOnly.value,setterOnly.value===undefined);
class GetterShadow extends BaseAccessors {override get number(){return super.number+5;}}
const getterShadow:any=new GetterShadow();try{getterShadow.number=1;}catch(error){console.log(error instanceof TypeError);}
console.log(getterShadow.number,getterShadow.stored);
class SetterShadow extends BaseAccessors {override set number(next:number){super.number=next;}}
const setterShadow=new SetterShadow();console.log(setterShadow.number===undefined);setterShadow.number=19;console.log(setterShadow.stored);
const anyBase:any=BaseAccessors;
class FieldShadow extends anyBase {number=30;}
const fieldShadow=new FieldShadow();console.log(fieldShadow.number,fieldShadow.stored,Object.hasOwn(fieldShadow,'number'));fieldShadow.number=31;console.log(fieldShadow.stored);
const replaced={get [accessorKey()](){effects+='X';return 1;},...{slot:20}};
console.log(replaced.slot,keyCalls,effects);
const replacement={...{slot:1},get [accessorKey()](){return 22;}};console.log(replacement.slot,keyCalls);
const borrowedSource={stored:40,get method(){return function(this:{stored:number}){return this.stored;};}};
console.log(borrowedSource.method());
class ThrowingAccessor {get value():number{throw new Error('getter');}set value(next:number){throw new Error('setter'+next);}}
const throwingAccessor=new ThrowingAccessor();try{console.log(throwingAccessor.value);}catch(error){console.log((error as Error).message);}
try{throwingAccessor.value=5;}catch(error){console.log((error as Error).message);}
let copies=0;
const destructuredSource={get first(){copies++;return 1;},get second(){copies++;return 2;}};
const {first,second:remaining}=destructuredSource;console.log(first,remaining,copies);
const cloneSource={get value(){copies++;return copies;}};
const cloneFirst={...cloneSource},cloneSecond={...cloneSource};console.log(cloneFirst.value,cloneSecond.value,copies);

class SpreadAccessors {value=5;get number(){return this.value+1;}}
const accessorCopy={...new SpreadAccessors()};console.log(accessorCopy.value,Object.keys(accessorCopy).length);
function protectedKey():string{return 'prototype';}
try{class InvalidAccessor {static get [protectedKey()](){return 1;}}}catch(error){console.log(error instanceof TypeError);}
try{class InvalidMethod {static [protectedKey()](){return 1;}}}catch(error){console.log(error instanceof TypeError);}
function nameKey():string{return 'name';}
class NamedAccessor {static stored='custom';static get [nameKey()](){return this.stored;}static set [nameKey()](next:string){this.stored=next;}}
const namedAccessor:any=NamedAccessor;console.log(namedAccessor.name);namedAccessor.name='changed';console.log(namedAccessor.name);
