export {};
async function fields(data:FormData):Promise<string>{const result:string[]=[];for(const [name,value] of data){if(typeof value==="string")result.push(JSON.stringify(name)+":"+JSON.stringify(value));else result.push(JSON.stringify(name)+":"+JSON.stringify([value.name,value.type,Array.from(new Uint8Array(await value.arrayBuffer()))]));}return result.join("|");}
async function main():Promise<void>{
 const source=new FormData();source.append("a","one");source.append("a","two");source.append("empty","");source.append('q"\n',"x\ny");
 source.append("file",new File([new Uint8Array([0,65,255,13,10])],"\u00fc.txt",{type:"application/x-test"}));
 const response=new Response(source);const clone=response.clone();console.log("roundtrip",await fields(await response.formData()),response.bodyUsed);
 console.log("clone",await fields(await clone.formData()));
 const request=new Request("https://example.com/",{method:"POST",body:source});console.log("request",await fields(await request.formData()));
 console.log("empty",Array.from(await new Response(new FormData()).formData()).length);
 async function parse(body:string,type='multipart/form-data; boundary="test"'):Promise<void>{const response=new Response(body,{headers:{"content-type":type}});try{console.log("custom",await fields(await response.formData()),response.bodyUsed);}catch(error){console.log("error",(error as Error).name,response.bodyUsed);}}
 await parse('--test\r\nContent-Disposition: form-data; name="a"\r\n\r\none\r\n--test\r\nContent-Disposition: form-data; name="f"; filename="folder/file.txt"\r\nContent-Type: Text/Plain; charset=utf-8\r\n\r\nfile\r\n--test--\r\n');
 await parse('--test\r\ncontent-disposition: form-data; name="x"\r\n\r\nnot-a-boundary\r\nb\r\n--test--\r\nepilogue');
 await parse('--test\r\nContent-Disposition: form-data; name="f"; filename="a.txt"\r\nContent-Transfer-Encoding: base64\r\n\r\nYWJj\r\n--test--');
 await parse('--test\r\nContent-Disposition: attachment; name="ignored"\r\n\r\nx\r\n--test--');
 await parse('--test--');
 for(const body of ['', '--test', '--test\r\n', '--test\r\nBad Header\r\n\r\nx\r\n--test--', '--test\r\nContent-Disposition: form-data; name="x"\r\n\r\nx'])await parse(body);
 await parse('--test--','multipart/form-data');
}
main().catch(error=>console.log("failure",String(error)));
