export {};
for(const pattern of ['[]','[^]','[]a','[^]a','[[]','[]]','[^]]','[a]','[\\]]','[\\[]','[[]|[^]']) {
 try {const regex=new RegExp(pattern);console.log(pattern,regex.source,['','a',']','[','\n','\r','\u2028','\ud83d\ude00'].map(value=>regex.test(value)).join(','));}
 catch(error){console.log(pattern,(error as Error).name);}
}
console.log('negated dot','a\nb\rc\u2028d'.match(/[^]/g)?.join('|'));
console.log('optional',/[]?/.exec('abc')?.[0],/[]*/.exec('abc')?.[0]);
console.log('alternative',/[]|abc/.test('abc'),/[]|abc/.test('def'));
