const literal=/ab+c/i;
console.log('literal',literal.test('ABBC'),literal.test('ac'),literal.source,literal.flags,literal.ignoreCase,literal.global,literal.lastIndex,String(literal),Object.prototype.toString.call(literal));
const capture=/(a+)(b)?/;
const matched=capture.exec('xxaaa')!;
console.log('exec',matched.length,matched[0],matched[1],matched[2],matched.index,matched.input,matched.groups,Object.keys(matched).join(','));
console.log('no match',capture.exec('xyz'));
const global=/a/g;
for(let i=0;i<4;i++)console.log('global',global.test('aba'),global.lastIndex);
const sticky=/a/y;sticky.lastIndex=1;console.log('sticky',sticky.test('ba'),sticky.lastIndex);sticky.lastIndex=0;console.log('sticky fail',sticky.test('ba'),sticky.lastIndex);
const empty=/(?:)/g;console.log('empty',empty.test('x'),empty.lastIndex,empty.test('x'),empty.lastIndex);
const plain=/a/;let effects='';(plain as any).lastIndex={valueOf(){effects+='L';return 1;}};console.log('ignored index',plain.test('a'),effects);
const conversion=/a/g;(conversion as any).lastIndex={valueOf(){effects+='I';return 1;}};
const input={toString(){effects+='S';return 'ba';}};effects='';console.log('conversion',conversion.test(input as any),conversion.lastIndex,effects);
for(const index of [-1,NaN,Infinity,100,0.9]){conversion.lastIndex=index;console.log('index',conversion.test('a'),conversion.lastIndex);}
const constructed=new RegExp('a+','mi');console.log('construct',constructed.source,constructed.flags,constructed.multiline,constructed.test('AA'));
console.log('clone',RegExp(literal)===literal,new RegExp(literal)!==literal,new RegExp(literal).flags,new RegExp(literal,'g').flags);
for(const pattern of ['', '/', '\\/', '\n', '\r', '\u2028', '\u2029'])console.log('source',new RegExp(pattern).source);
console.log('multiline',/^a$/m.test('x\na\nx'));
console.log('backreference',/(a)\1/.test('aa'),/\d+/.test('123'),/\w+/.test('word'),/a(?=b)/.test('ab'),/a(?!b)/.test('ac'));
const first=/a/g,second=/a/g;first.lastIndex=1;console.log('literal identity',first!==second,second.lastIndex);
console.log('properties',Object.keys(literal).length,Object.getOwnPropertyNames(literal).join(','));
const descriptor=Object.getOwnPropertyDescriptor(literal,'lastIndex')!;console.log('lastIndex descriptor',descriptor.writable,descriptor.enumerable,descriptor.configurable);
try{delete (literal as any).lastIndex;}catch(error){console.log('delete index',(error as Error).name);}
try{(literal as any).source='x';}catch(error){console.log('readonly source',(error as Error).name);}
for(const pattern of ['[','(','a{2,1}'])try{new RegExp(pattern);}catch(error){console.log('invalid pattern',(error as Error).name);}
for(const flags of ['gg','ii','q','uv'])try{new RegExp('a',flags);}catch(error){console.log('invalid flags',(error as Error).name);}
class Derived extends RegExp {marker=3;}
const derived=new Derived('a','g');console.log('derived',derived.marker,derived.test('a'),derived.lastIndex,derived instanceof RegExp,derived instanceof Derived);
const alternate=Reflect.construct(RegExp,['a'],Derived);console.log('reflect',alternate.test('a'),Object.getPrototypeOf(alternate)===Derived.prototype);
for(const method of [RegExp.prototype.test,RegExp.prototype.exec] as any[])try{method.call({} as any,'a');}catch(error){console.log('receiver',(error as Error).name);}
console.log('prototype',RegExp.prototype.source,RegExp.prototype.flags,RegExp.prototype.global,String(RegExp.prototype));
console.log('generic string',RegExp.prototype.toString.call({source:'a',flags:'g'} as any));
console.log('metadata',RegExp.name,RegExp.length,RegExp.prototype.test.name,RegExp.prototype.test.length,RegExp.prototype.exec.length);

