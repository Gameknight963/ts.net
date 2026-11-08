export {};
for(const pattern of ['\\k','\\k<name>','[\\k]','[\\k<name>]','(a)\\k<name>','\\k+']) {
 try {const regex=new RegExp(pattern);console.log(pattern,regex.source,regex.test('k<name>'),regex.test('ak<name>'),regex.test('kkk'));}
 catch(error){console.log(pattern,(error as Error).name);}
}
for(const pattern of ['(?<name>a)\\k<name>','\\k<name>(?<name>a)','(?<name>a)\\k<missing>','(?<name>a)\\k','(?<name>a)[\\k]']) {
 try {const regex=new RegExp(pattern);console.log('named',pattern,regex.test('aa'),regex.test('a'));}
 catch(error){console.log('named',pattern,(error as Error).name);}
}
console.log('escaped',new RegExp('\\\\k<name>').test('\\k<name>'));
console.log('lookbehind',new RegExp('(?<=a)\\k').test('ak'));
console.log('fake name',new RegExp('\\(\\?<name>\\k').test('(?<name>k'));
