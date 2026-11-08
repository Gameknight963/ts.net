export {};
for(const name of ['indexOf','lastIndexOf','includes']) {
 const prototype:any=String.prototype,original=prototype[name];let calls=0;
 prototype[name]=function(this:unknown,needle:unknown,position:unknown){calls++;console.log('override',name,this==='text',needle,position);return name==='includes'?false:42;};
 try {
  console.log('dynamic',(String.prototype as any)[name].call('text','needle',2));
  if(name==='indexOf') console.log('direct','text'.indexOf('needle',2));
  if(name==='lastIndexOf') console.log('direct','text'.lastIndexOf('needle',2));
  if(name==='includes') console.log('direct','text'.includes('needle',2));
  console.log('boxed',(Object('text') as any)[name]('needle',2),calls);
 }finally{prototype[name]=original;}
}
const prototype:any=String.prototype,original=prototype.includes;
try{delete prototype.includes;console.log('deleted',typeof (('text' as any).includes));}finally{prototype.includes=original;}
console.log('restored','text'.includes('ext'));

const parent=Object.getPrototypeOf(prototype);
try {
 delete prototype.includes;
 Object.setPrototypeOf(prototype,{get includes(){console.log('getter receiver',typeof this);return function(this:unknown){console.log('method receiver',this==='text');return true;};}});
 console.log('inherited','text'.includes('anything'));
}finally{Object.setPrototypeOf(prototype,parent);prototype.includes=original;}
