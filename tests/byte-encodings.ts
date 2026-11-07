export {};
const U=Uint8Array as any;
for(const values of [[],[97],[97,98],[97,98,99],[0,255,254,253]]){
 const array:any=new Uint8Array(values);console.log("hex",array.toHex(),Array.from(U.fromHex(array.toHex())).join(","));
 for(const alphabet of ["base64","base64url"])for(const omitPadding of [false,true]){const text=array.toBase64({alphabet,omitPadding});console.log("base64",alphabet,omitPadding,text,Array.from(U.fromBase64(text,{alphabet})).join(","));}
}
for(const text of ["YQ==","YWI=","YWJj","YQ","YWI","YQ=","YR==","YWJ="," YW Jj ","!","A","===="]){for(const lastChunkHandling of ["loose","strict","stop-before-partial"])try{console.log("decode",text,lastChunkHandling,Array.from(U.fromBase64(text,{lastChunkHandling})).join(","));}catch(error){console.log("decode error",text,lastChunkHandling,(error as Error).name);}}
for(const size of [0,1,2,3,4,8])for(const text of [" YWJj YQ== ","YQ==","YWI=","YWJj","YQ","YWI"]){const a:any=new Uint8Array(size);console.log("set",size,text,JSON.stringify(a.setFromBase64(text)),a.toHex());}
for(const size of [0,1,2,4])for(const text of ["abcd","ABCDEF","","a","zz","00zz"]){const a:any=new Uint8Array(size);try{console.log("set hex",size,text,JSON.stringify(a.setFromHex(text)),a.toHex());}catch(error){console.log("hex error",size,text,(error as Error).name,a.toHex());}}
const original:any=new Uint8Array([1,2,3,4]);console.log("view",original.subarray(1,3).toHex(),original.subarray(1,3).toBase64());

for(const [text,size,mode] of [[" YQ\n",4,"loose"],[" YQ\n",4,"stop-before-partial"],["1+0=A-",2,"strict"],["YWJj?",3,"loose"]] as [string,number,string][]){const a:any=new Uint8Array(size);a.fill(7);try{console.log("partial",JSON.stringify(a.setFromBase64(text,{lastChunkHandling:mode})),a.toHex());}catch(error){console.log("partial error",(error as Error).name,a.toHex());}}
for(const options of [null,1,{alphabet:null},{alphabet:"bad"},{lastChunkHandling:"bad"}])try{U.fromBase64("YQ==",options);console.log("unexpected options");}catch(error){console.log("options",(error as Error).name);}
for(const method of ["toHex","toBase64","setFromHex","setFromBase64"]){const fn=(Uint8Array.prototype as any)[method];console.log("method",fn.name,fn.length);try{fn.call(new Uint8ClampedArray(2),"00");}catch(error){console.log("brand",(error as Error).name);}}
