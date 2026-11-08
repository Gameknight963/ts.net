export {};
for(const pattern of ['[[a]','[[]','[[^]','[a-z-[aeiou]]','[a-z-[b]]','[0-9-[0]]','[^a-[b]]','[a[b]','[a[bc]]','[\\[]','[a-z-\\[]']) {
 try {
  const regex=new RegExp(pattern);const inputs=['a','b','z','0','1','[',']','-','^','a]','b]','z]','0]','1]','[]','-]'];
  console.log(pattern,regex.source,inputs.map(text=>regex.test(text)).join(','));
 }catch(error){console.log(pattern,(error as Error).name);}
}
console.log('replace','a]b]z]'.replace(new RegExp('[a-z-[b]]','g'),'X'));
