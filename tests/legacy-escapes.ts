function codes(value:string):string {const result:number[]=[];for(let i=0;i<value.length;i++)result.push(value.charCodeAt(i));return result.join(',');}
for(let code=0;code<65536;code+=1) {
    const value=String.fromCharCode(code);
    const escaped=escape(value);
    if(unescape(escaped)!==value)throw new Error('roundtrip');
    // Hash every code unit without generating megabytes of test output.
}
let checksum=0;
for(let code=0;code<65536;code++) {
    const encoded=escape(String.fromCharCode(code));
    for(let i=0;i<encoded.length;i++)checksum=(checksum*31+encoded.charCodeAt(i))|0;
}
console.log('all code units',checksum);
for(const value of ['', 'AZaz09@*_+-./', ' !"#$%&\'(),:;<=>?[]^`{|}~', '\u00FF\u0100\uFFFF', '\uD83D\uDE00\uD800\uDC00']) {
    console.log('escape',escape(value));console.log('roundtrip',codes(unescape(escape(value))));
}
for(const value of ['%','%0','%00','%ff','%FF','%u0000','%uD800','%uDC00','%uD83D%uDE00','%U0041','%u041','%u004G','%u%41','%%41','%2541','%u00411','%41%xx%42','a+b','%C3%A9','%u00e9','%0%41','%u000%41'])console.log('unescape',value,codes(unescape(value)));
const convert=escape as any,decode=unescape as any;
for(const value of [undefined,null,true,123,NaN,Infinity])console.log('conversion',convert(value),decode(value));
console.log('omitted',convert(),decode());
let effects='';const value={toString():string {effects+='S';return '%41"';}};
console.log('object',convert(value),decode(value),effects);
const broken={toString():string {throw new Error('conversion failed');}};
for(const method of [convert,decode])try{method(broken);}catch(error){console.log('error',(error as Error).message);}
console.log('metadata',convert.name,convert.length,decode.name,decode.length);
function shadow(escape:(value:string)=>string,unescape:(value:string)=>string):void {console.log('shadow',escape('x'),unescape('x'));}
shadow(value=>'E'+value,value=>'U'+value);
