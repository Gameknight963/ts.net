export {};
async function main():Promise<void>{
 const empty=new Blob();console.log("empty",empty.size,empty.type,await empty.text(),Object.prototype.toString.call(empty),empty instanceof Blob);
 const bytes=new Uint8Array([65,66,67,68]);
 const view=new DataView(bytes.buffer,1,2);
 const blob=new Blob(["hello ",new Blob(["world"]),"!",bytes.subarray(1,3),view],{type:"TEXT/PLAIN"});
 bytes.fill(120);
 console.log("mixed",blob.size,blob.type,await blob.text());
 const first=await blob.arrayBuffer();new Uint8Array(first).fill(0);
 console.log("copy",await blob.text(),Array.from(await blob.bytes()).join(","));
 for(const [start,end] of [[0,5],[6,11],[-4,-1],[99,100],[5,2]]) {const part=blob.slice(start,end,"TEXT/X");console.log("slice",start,end,part.size,part.type,await part.text());}
 console.log("type",new Blob([],{type:"text/\u0100"}).type,new Blob([],{type:"text/\n"}).type);
 console.log("bom",await new Blob([new Uint8Array([239,187,191,120])]).text());
 console.log("coerce",await new Blob([123 as any,null as any,undefined as any,{toString:()=>"ok"} as any]).text());
 class Child extends Blob {label():string{return "child";}}
 const child=new Child(["yes"]);console.log("subclass",child instanceof Blob,child instanceof Child,child.label(),await child.text(),child.slice() instanceof Child);
 const source={*[Symbol.iterator](){yield "a";yield new Uint8Array([98]);yield new Blob(["c"]);}};
 console.log("iterable",await new Blob(source as any).text());
 console.log("newlines",JSON.stringify(await new Blob(["a\r\nb\rc\n"],{endings:"native"}).text()));
 let effects="";const options={get endings():"transparent"{effects+="E";return "transparent";},get type():string{effects+="T";return "TEXT/X";}};
 const configured=new Blob(["ok"],options);console.log("options",effects,configured.type);
 const pending=blob.text();console.log("promise",pending instanceof Promise);await pending;
 for(const value of [null,"abc",123])try{new Blob(value as any);console.log("unexpected");}catch(error){console.log("invalid parts",(error as Error).name);}
 try{new Blob([],{endings:"wrong" as any});}catch(error){console.log("endings",(error as Error).name);}
}
main().catch(error=>console.log("failure",String(error)));
