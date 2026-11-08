export {};
for(const name of ['en','en-US','fr','de','ja','zh-Hans','ru','ar','he','fa','ur','ps','dv','ar-Latn','en-Arab','az-Arab','az-Latn','ku-Arab','ku-Latn','und-Arab','und-Latn']) {
 const locale: any=new Intl.Locale(name);
 const result=locale.getTextInfo();
 console.log(name,result.direction,Object.keys(result).join(','),Object.getPrototypeOf(result)===Object.prototype);
 result.direction='changed';console.log('fresh',locale.getTextInfo().direction);
}
const method=(Intl.Locale.prototype as any).getTextInfo;
console.log('metadata',method.name,method.length);
for(const receiver of [undefined,null,{},'en']) {
 try {method.call(receiver);}catch(error){console.log('receiver',(error as Error).name);}
}

for(const script of ['Adlm','Arab','Armi','Avst','Chrs','Elym','Gara','Hatr','Hebr','Hung','Khar','Lydi','Mand','Mani','Mend','Merc','Mero','Narb','Nbat','Nkoo','Orkh','Ougr','Palm','Phli','Phlp','Phnx','Prti','Rohg','Samr','Sarb','Sogd','Sogo','Syrc','Thaa','Yezi']) {const locale:any=new Intl.Locale('und-'+script);console.log(script,locale.getTextInfo().direction);}
