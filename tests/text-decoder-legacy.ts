export {};
const labels=["windows-1250","windows-1251","windows-1253","windows-1254","windows-1255","windows-1256","windows-1257","windows-1258","windows-874","ibm866","koi8-r","koi8-u","iso-8859-2","iso-8859-3","iso-8859-4","iso-8859-5","iso-8859-6","iso-8859-7","iso-8859-8","iso-8859-8-i","iso-8859-13","iso-8859-15","shift_jis","euc-jp","iso-2022-jp","gbk","gb18030","big5","euc-kr"];
for(const label of labels){const decoder=new TextDecoder(label);console.log("label",label,decoder.encoding,decoder.decode(new Uint8Array([65,66,67])));}
const samples:[string,number[]][]=[
 ["windows-1251",[0xcf,0xf0,0xe8,0xe2,0xe5,0xf2]],
 ["shift_jis",[0x93,0xfa,0x96,0x7b]],
 ["euc-jp",[0xc6,0xfc,0xcb,0xdc]],
 ["iso-2022-jp",[0x1b,0x24,0x42,0x46,0x7c,0x4b,0x5c,0x1b,0x28,0x42]],
 ["gbk",[0xd6,0xd0,0xce,0xc4]],
 ["gb18030",[0x94,0x39,0xfc,0x36]],
 ["big5",[0xa4,0xa4,0xa4,0xe5]],
 ["euc-kr",[0xc7,0xd1,0xb1,0xb9]]
];
for(const [label,values] of samples){
 const bytes=new Uint8Array(values);const text=new TextDecoder(label,{fatal:true}).decode(bytes);console.log("sample",label,text);
 for(let cut=0;cut<=bytes.length;cut++){
  const decoder=new TextDecoder(label,{fatal:true});console.log("stream",label,cut,decoder.decode(bytes.subarray(0,cut),{stream:true})+decoder.decode(bytes.subarray(cut))===text);
 }
}
for(const alias of ["cp1251","latin2","latin5","tis-620","sjis","gb2312","windows-949"]){const d=new TextDecoder(alias);console.log("alias",alias,d.encoding);}

for(const label of ["shift_jis","euc-jp","gbk","big5","euc-kr"]) {
 const lead=label==="euc-jp"?0xa1:label==="euc-kr"?0xc7:0x82;
 try {new TextDecoder(label,{fatal:true}).decode(new Uint8Array([lead]));console.log("invalid accepted",label);}catch(error){console.log("fatal",label,(error as Error).name);}
 const d=new TextDecoder(label);console.log("replacement",label,Array.from(d.decode(new Uint8Array([lead])),c=>c.charCodeAt(0)).join(","));
}
