export {};
function write(count:number):void{for(let index=0;index<count;index++)values[index&3]=index;}
function nonzero(count:number):void{for(let index=5;index<count;index++)values[index&3]=index;}
function read(count:number):number{let sum=0;for(let index=0;index<count;index++)sum+=values[index&3];return sum;}
function lengthRead():number{let sum=0;for(let index=0;index<values.length;index++)sum+=values[index];return sum;}
for(const count of [0,-1,NaN]){
    write(count);
    console.log("empty",read(count));
}
nonzero(4.5);
console.log("empty nonzero");
try{write(1);}catch(error){console.log("write before init",(error as Error).name);}
try{read(1);}catch(error){console.log("read before init",(error as Error).name);}
try{lengthRead();}catch(error){console.log("length before init",(error as Error).name);}
try{nonzero(5.5);}catch(error){console.log("nonzero before init",(error as Error).name);}
const values=[1,2,3,4].map(value=>value+1);
write(8);nonzero(5.5);
console.log("initialized",read(4),lengthRead(),values.join(","));
values[0]=13;
console.log("later write",read(4));
const direct:number[]=[1,2,3,4];
function directWrite():void{for(let index=0;index<4;index++)direct[index]=index+17;}
directWrite();console.log("direct",direct.join(","));
function otherBinding():void{for(let index=0;index<4;index++)direct[index]=later+index;}
try{otherBinding();}catch(error){console.log("other binding",(error as Error).name,direct[0]);}
const later=31;
otherBinding();console.log("other initialized",direct.join(","));
