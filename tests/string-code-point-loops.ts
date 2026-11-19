export {};
function sum(text:string):number {
    let result=0;for(let i=0;i<text.length;i++)result+=text.codePointAt(i)!;return result;
}
function coalesced(text:string):number {
    let result=0;for(let i=0;i<text.length;i++)result+=text.codePointAt(i)??0;return result;
}
function mixed(text:string):number {
    let result=0;for(let i=0;i<text.length;i++)result+=text.charCodeAt(i)+text.codePointAt(i)!;return result;
}
for(const text of ['', 'abc', '\uD83D\uDE00', '\uD800x\uDC00', '\uD800\uD800\uDC00'])
    console.log('values',sum(text),coalesced(text),mixed(text));
const point=Object.getOwnPropertyDescriptor(String.prototype,'codePointAt')!;
const unit=Object.getOwnPropertyDescriptor(String.prototype,'charCodeAt')!;
try {
    String.prototype.codePointAt=function():number{return 7;};
    console.log('replacement',sum('abc'),mixed('abc'));
    let gets=0;
    Object.defineProperty(String.prototype,'codePointAt',{configurable:true,get(){gets++;return point.value;}});
    console.log('getter',sum('abc'),gets);
    gets=0;console.log('empty',sum(''),gets);
} finally {Object.defineProperty(String.prototype,'codePointAt',point);}
function readsUnit(text:string):number {
    let result=0;
    for(let i=0;i<text.length;i++){text.charCodeAt;result+=text.codePointAt(i)!;}
    return result;
}
function readsPoint(text:string):number {
    let result=0;
    for(let i=0;i<text.length;i++){text.codePointAt;result+=text.charCodeAt(i);}
    return result;
}
try {
    let gets=0;
    Object.defineProperty(String.prototype,'charCodeAt',{configurable:true,get(){
        gets++;String.prototype.codePointAt=function():number{return 7;};return unit.value;
    }});
    console.log('unused unit getter',readsUnit('abc'),gets);
} finally {
    Object.defineProperty(String.prototype,'charCodeAt',unit);Object.defineProperty(String.prototype,'codePointAt',point);
}
try {
    let gets=0;
    Object.defineProperty(String.prototype,'codePointAt',{configurable:true,get(){
        gets++;String.prototype.charCodeAt=function():number{return 9;};return point.value;
    }});
    console.log('unused point getter',readsPoint('abc'),gets);
} finally {
    Object.defineProperty(String.prototype,'charCodeAt',unit);Object.defineProperty(String.prototype,'codePointAt',point);
}
function change():void{String.prototype.codePointAt=function():number{return 7;};}
try {
    const text='abc';let result=0;
    for(let i=0;i<text.length;i++){result+=text.codePointAt(i)!;change();}
    console.log('callback',result);
} finally {Object.defineProperty(String.prototype,'codePointAt',point);}
const text='abc';let shifted=0;
for(let i=0;i<text.length;i++)shifted+=text.codePointAt(i+1)??0;
console.log('shifted',shifted);
