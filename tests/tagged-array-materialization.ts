export {};
function copy():any[]{return [1,2,3,4].map(value=>value+1);}
const first=copy();console.log('reverse',first.reverse()===first,JSON.stringify(first));
const searched=copy();delete searched[1];searched[3]=undefined;console.log('holes',searched.indexOf(4),Object.keys(searched).join(','),JSON.stringify(searched),Object.hasOwn(searched,1),Object.hasOwn(searched,3));
const shrunk=copy();shrunk.length=2;console.log('shrink',shrunk.slice().join(','),shrunk.length);shrunk.push('mixed');console.log(JSON.stringify(shrunk));
const extended=copy();extended.length=10;console.log('extend',extended.slice().length,Object.keys(extended).join(','),JSON.stringify(extended));
const sparse=copy();sparse.length=1000000;console.log('sparse',sparse.indexOf(3),sparse.length,Object.hasOwn(sparse,999999));
const mixed:any[]=[1,'two',3,{value:4}];console.log('mixed',JSON.stringify(mixed.reverse()),mixed.slice(1).length);mixed[0]='changed';console.log(JSON.stringify(mixed));
const nan=copy();nan[0]=NaN;nan[1]=-0;console.log('numbers',nan.includes(NaN),nan.indexOf(NaN),Object.is(nan[1],-0),JSON.stringify(nan));
const result=copy();const alias=result;result.slice();result[0]='text';delete result[2];result.push({value:8});console.log('identity',alias===result,JSON.stringify(alias));
