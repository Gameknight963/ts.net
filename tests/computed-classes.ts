let events='';
function key<T extends string>(name:T):T {events+=name+',';return name;}
function init(name:string,value:number):number {events+=name+',';return value;}
const names={
 get first():'first'{return key('first');},
 get method():'method'{return key('method');},
 get staticValue():'staticValue'{return key('staticValue');},
 get second():'second'{return key('second');},
 get value():'value'{return key('value');},
 get callback():'callback'{return key('callback');}
};
class Computed {
    [names.first]=init('first-init',1);
    [names.method]():number {return this.first+this.second;}
    static [names.staticValue]=init('static-init',7);
    [names.second]=init('second-init',2);
    static {events+='block,';}
}
console.log('definition',events,Computed.staticValue);
const first=new Computed();const second=new Computed();
console.log('instances',events,first.method(),second.method(),Object.keys(first)[0],Object.keys(first)[1]);
let current:'original'='original';
class CapturedKey {[current]=3;}
current='changed' as any;console.log('captured',new CapturedKey().original,(new CapturedKey() as any).changed);
function make(name:'value'){return class {[name]=5;};}
const One=make('one' as any);const Two=make('two' as any);
console.log('classes',(new One() as any).one,(new Two() as any).two,(new One() as any).two);
const methodName='computedMethod';
class Methods {
    [methodName]():number{return 6;}
    get [names.value]():number{return (this as any).stored??0;}
    set [names.value](value:number){(this as any).stored=value;}
    [names.callback]=()=>this.computedMethod();
}
const methods=new Methods();methods.value=9;
console.log('methods',methods.computedMethod(),methods.value,methods.callback(),methods.callback.name,methods.computedMethod.name);
class ContextBase {key():'outerMethod'{return 'outerMethod';}}
class Context extends ContextBase {
    fieldKey:'outerField'='outerField';
    build(){const owner=this;return class {
        [owner.fieldKey]=8;
        [super.key()]():number{return 9;}
    };}
}
const Inner=new Context().build();const inner:any=new Inner();
console.log('outer-context',inner.outerField,inner.outerMethod());
let coercions=0;
const propertyObject={toString():string{coercions++;return 'converted';}};
const propertyNames={get converted():1{coercions++;return 1;}};
class Coerced {[propertyNames.converted]=4;[propertyObject as any]():number{return 7;}}
const coerced=new Coerced();new Coerced();console.log('coercion',coercions,coerced[1],(coerced as any).converted());
const special:'__proto__'='__proto__';
class Special {[special]=6;[names.callback]=function named(){return 1;};}
const specialValue=new Special();
console.log('special',Object.hasOwn(specialValue,'__proto__'),specialValue.__proto__,specialValue.callback.name);

const nestedKey='nested';
const nestedNames={get key():'nested'{return nestedKey;}};
class ClassFields {
    [nestedNames.key]=class {static captured=this.name;};
    static [names.callback]=class {static captured=this.name;};
}
const classFields=new ClassFields();
console.log('class-names',classFields.nested.name,classFields.nested.captured,ClassFields.callback.name,ClassFields.callback.captured);
let invalidEffects='';
function invalidKey():string {invalidEffects+='key,';return 'prototype';}
try {class Invalid {static [invalidKey()](){}}}catch(error){console.log('invalid-method',error instanceof TypeError,invalidEffects);}
invalidEffects='';const invalidBase:any=3;
const badNames={get key():'field'{invalidEffects+='key,';return 'field';}};
try {class Invalid extends invalidBase {[badNames.key]=1;}}catch(error){console.log('invalid-base',error instanceof TypeError);}
const badFieldKey:'safe'='prototype' as any;
try {class Invalid {static [badFieldKey]=1;}}catch(error){console.log('invalid-field',error instanceof TypeError);}
