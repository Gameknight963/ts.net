for(const resizable of [false,true]){
 const buffer=resizable?new ArrayBuffer(8,{maxByteLength:16}):new ArrayBuffer(8);new Uint8Array(buffer).set([1,2,3,4,5,6,7,8]);
 const graph:any={buffer,view:new Uint8Array(buffer,2,3),data:new DataView(buffer,1,4)};graph.self=graph;
 const copied=structuredClone(graph,{transfer:[buffer]});
 console.log("clone",buffer.detached,buffer.byteLength,copied.self===copied,copied.view.buffer===copied.buffer,copied.data.buffer===copied.buffer,copied.buffer.resizable,copied.buffer.maxByteLength,Array.from(copied.view).join("|"),copied.data.getUint8(0));
}
for(const kind of ["duplicate","view","function"]){const buffer=new ArrayBuffer(4);try{structuredClone(kind==="function"?()=>{}:buffer,{transfer:kind==="duplicate"?[buffer,buffer]:kind==="view"?[new Uint8Array(buffer)] as any:[buffer]});}catch(error){console.log(kind,(error as Error).name,buffer.detached);}}
const unused=new ArrayBuffer(2);console.log("unused",structuredClone(42,{transfer:[unused]}),unused.detached);
const source=new ArrayBuffer(4);const bytes=new Uint8Array(source);const graph={buffer:source,get mutate(){bytes[0]=9;return "done";}};const copied=structuredClone(graph,{transfer:[source]});console.log("getter",copied.mutate,new Uint8Array(copied.buffer)[0],source.detached);
const first=new ArrayBuffer(4),second=new ArrayBuffer(4);try{structuredClone({get fail(){throw new Error("getter failure");}},{transfer:[first,second]});}catch(error){console.log("failure",(error as Error).message,first.detached,second.detached);}
const detached=new ArrayBuffer(1);detached.transfer();const valid=new ArrayBuffer(1);try{structuredClone({}, {transfer:[valid,detached]});}catch(error){console.log("detached-list",(error as Error).name,valid.detached);}
const transferred=new ArrayBuffer(2);let events="";function* transfers(){events+="start|";yield transferred;events+="end|";}const tracked={get value(){events+="read|";return transferred;}};const copy=structuredClone(tracked,{transfer:transfers() as any});console.log("iterable",events,transferred.detached,copy.value.byteLength);
