for(const [pattern,flags,text] of [
 ['^(?i:hello) world$','','HELLO world'],
 ['^(?i:hello) world$','','HELLO WORLD'],
 ['^(?i:a(?-i:b)c)$','','AbC'],
 ['^(?i:a(?-i:b)c)$','','ABC'],
 ['^(?-i:a)b$','i','aB'],
 ['^(?-i:a)b$','i','AB'],
 ['(?m:^a$)','','x\na\ny'],
 ['(?-m:^a$)','m','x\na\ny'],
 ['^(?s:a.b)$','','a\nb'],
 ['^(?-s:a.b)$','s','a\nb'],
 ['^(?im-s:^a.b$)$','','Axb'],
 ['^(?im-s:^a.b$)$','','A\nb'],
 ['^(?i:a)(b)\\1$','','Abb'],
 ['^(?i:(?<first>a))(b)$','','Ab'],
 ['(?i:a)|(?s:b.c)','','b\nc']
]) {
 const regex=new RegExp(pattern,flags); const match=regex.exec(text);
 console.log(pattern,flags,JSON.stringify(text),match?.join('|')??'none',match?.groups?.first??'none',regex.source,regex.flags);
}
for(const pattern of ['(?i)a','(?i-:a)','(?ii:a)','(?i-i:a)','(?-:a)','(?i--m:a)','(?g:a)','(?x:a)','(?n:a)','(?>a)','(?im','(?i a)','(?i:)']) {
 try{ const regex=new RegExp(pattern);console.log('valid',pattern,regex.test('')); }catch(error){console.log('invalid',pattern,(error as Error).name);}
}
console.log('replace','AaBb'.replace(new RegExp('(?i:a)(b)','g'),'[$1]'));
console.log('split','aXbxc'.split(new RegExp('(?i:x)')).join('|'));
console.log('indices',new RegExp('(?i:(a))(?<last>b)','d').exec('Ab')?.indices?.map(pair=>pair?.join(':')).join('|'));

console.log('literal',/^(?i:hello) world$/.test('HELLO world'),/^(?i:hello) world$/.test('HELLO WORLD'));
console.log('literal-disable',/^(?-i:a)b$/i.test('aB'),/^(?-i:a)b$/i.test('AB'));
console.log('literal-dotall',/^(?s:a.b)$/.test('a\nb'));
console.log('class',new RegExp('[()?:ims-]+').test('(?i:'));
