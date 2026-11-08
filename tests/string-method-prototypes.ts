export {};
for(const name of ['at','slice','substring','charAt','charCodeAt','codePointAt']) {
 const prototype:any=String.prototype,method:any=prototype[name];
 console.log('metadata',name,method.name,method.length,Object.prototype.hasOwnProperty.call(prototype,name));
 for(const position of [undefined,0,1,-1,99])console.log('call',name,position,method.call('abcdef',position,3));
 let events='';const receiver:any={toString(){events+='text|';return 'abcdef';}},position:any={valueOf(){events+='index|';return 1;}};
 console.log('order',name,method.call(receiver,position),events);
 prototype[name]=function(this:unknown){console.log('receiver',name,this==='abcdef');return name==='charCodeAt'||name==='codePointAt'?123:'override';};
 try {
  if(name==='at')console.log('override','abcdef'.at(1));
  if(name==='slice')console.log('override','abcdef'.slice(1,3));
  if(name==='substring')console.log('override','abcdef'.substring(1,3));
  if(name==='charAt')console.log('override','abcdef'.charAt(1));
  if(name==='charCodeAt')console.log('override','abcdef'.charCodeAt(1));
  if(name==='codePointAt')console.log('override','abcdef'.codePointAt(1));
  console.log('boxed',(Object('abcdef') as any)[name](1));
  delete prototype[name];console.log('deleted',typeof ('abcdef' as any)[name]);
 }finally{prototype[name]=method;}
}
