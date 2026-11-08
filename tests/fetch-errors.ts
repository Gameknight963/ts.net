export {};
async function run():Promise<void>{
 const invalid:[string,()=>Promise<unknown>][]=[
  ['missing',()=>(fetch as any)()],
  ['relative',()=>fetch('/relative')],
  ['credentials',()=>fetch('https://user:pass@example.invalid/')],
  ['syntax',()=>fetch('http://[bad]')],
  ['GET body',()=>fetch('https://example.invalid/',{body:'data'})],
  ['method',()=>fetch('https://example.invalid/',{method:'TRACE'})],
  ['headers',()=>fetch('https://example.invalid/',{headers:{'bad name':'value'}})]
 ];
 for(const [name,action] of invalid){try{await action();console.log(name,'unexpected');}catch(error){console.log(name,(error as Error).name);}}
 const reason=new Error('conversion');
 try{await fetch({toString(){throw reason;}} as any);}catch(error){console.log('conversion',error===reason);}
 const pending=fetch('/invalid');console.log('before rejection');
 try{await pending;}catch{console.log('after rejection');}
}
run();
