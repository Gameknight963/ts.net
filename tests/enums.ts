enum Direction {Up,Down,Left=4,Right}
console.log(Direction.Up,Direction.Down,Direction.Left,Direction.Right,Direction[0],Direction[5]);
console.log(Object.keys(Direction).length,Object.values(Direction).length);
for(const key of Object.keys(Direction))console.log(key,(Direction as any)[key]);
enum Labels {Yes="yes",No="no"}
console.log(Labels.Yes,Labels.No,Object.keys(Labels).length);
const enum Flags {A=1,B=2,Both=A|B,Negative=-1,Zero=-0}
console.log(Flags.A,Flags.Both,Flags.Negative,1/Flags.Zero);
let effects="";
function value():number {effects+="V";return 7;}
enum Computed {A=value(),B=A+1,C=2}
console.log(Computed.A,Computed.B,Computed[7],Computed[8],effects);
enum Merged {A=1}
const initial=Merged;
enum Merged {B=2}
console.log(initial===Merged,Merged.A,Merged.B,Object.keys(Merged).length);
function local():number {enum Local {A=3,B}return Local.A+Local.B;}
console.log(local(),local());

enum Duplicate {First=1,Last=1,Negative=-1,Fraction=1.25,Zero=-0}
console.log(Duplicate[1],Duplicate[-1],Duplicate[1.25],Duplicate[0],1/Duplicate.Zero);
enum Mixed {Numeric=1,Text='text'}
console.log(Mixed.Numeric,Mixed.Text,Mixed[1],Object.keys(Mixed).length);
enum Empty {}
console.log(Object.keys(Empty).length);
function inspectEnum():number {
    console.log('observe',Object.keys(Observed).length,Object.hasOwn(Observed,'First'),(Observed as any).First);
    return 3;
}
enum Observed {First=inspectEnum(),Second=inspectEnum()+1}
console.log(Observed.First,Observed.Second,Object.keys(Observed).length);
function beforeEnum():string{return typeof Later;}
console.log(beforeEnum());
enum Later {A=1}
function runtimeZero():number{return -0;}
enum RuntimeZero {A=runtimeZero()}
console.log(1/RuntimeZero.A,RuntimeZero[0]);
const enum TextFlags {A='a',B=A,C='x'+'y',Surrogate='\uD800'}
console.log(TextFlags.A,TextFlags.B,TextFlags.C,TextFlags.Surrogate.charCodeAt(0));
enum OddNames {'hello world'=3,'\uD83D\uDE00'=4,'constructor'=5}
console.log(OddNames['hello world'],OddNames['\uD83D\uDE00'],OddNames.constructor,Object.keys(OddNames).length);
for(let i=0;i<2;i++){enum PerIteration {A=i,B=A+1}console.log(PerIteration.A,PerIteration.B);}
function typedObserve():number {
    const current=TypedObserved.A;
    console.log(current,typeof current,current===undefined);
    return 1;
}
enum TypedObserved {A=typedObserve()}
let typedValue:Direction=Direction.Left;
console.log(typedValue,typedValue+1);
( Direction as any).Up=99;
console.log(Direction.Up,Direction[0]);

enum Special {Infinity=1/0, NegativeInfinity=-1/0, NotNumber=0/0}
console.log((Special as any).Infinity, Special.NegativeInfinity, Special.NotNumber, Special[Infinity], Special[-Infinity], Special[NaN]);
enum TextEnum {First='one', Second='two'}
function text(value:TextEnum):string{return value;}
console.log(text(TextEnum.First));
enum Mixed {One=1, TextEnum='text'}
function mixed(value:Mixed):Mixed{return value;}
console.log(mixed(Mixed.One),mixed(Mixed.TextEnum));
function merged():number {enum Local {A=1} enum Local {B=2} return Local.A+Local.B;}
console.log(merged(),merged());
const captures:(()=>number)[]=[];
for(let i=0;i<3;i++){enum Local{A=i} captures.push(()=>Local.A);}
console.log(captures[0](),captures[1](),captures[2]());
function localEnumInitialization():void {
    function read():string {return typeof Local;}
    try {console.log('local-enum-before',read());}
    catch(error) {console.log('local-enum-before',(error as Error).name);}
    enum Local {A=1}
    console.log('local-enum-after',read(),Local.A);
}
localEnumInitialization();
