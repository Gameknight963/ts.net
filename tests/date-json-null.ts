export {};
function result(date: Date): string { return date.toJSON(); }
for(const timestamp of [NaN,Infinity,-Infinity,0,8640000000000000]) {
 const date=new Date(timestamp);
 const value=date.toJSON();
 console.log('direct',value,value===null,typeof value);
 console.log('returned',result(date));
 const holder={value};console.log('stored',holder.value);
 console.log('array',[value,date.toJSON()].join(','));
 if(value===null) {
  try {const dynamic: any=value; console.log(dynamic.length);}catch(error){console.log('access rejected');}
 }
}
