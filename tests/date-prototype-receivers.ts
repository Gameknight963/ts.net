export {};
const prototype=Date.prototype;
for(const method of ['getTime','valueOf','toString','toISOString','toJSON','setTime','getUTCFullYear']) {
 try{console.log(method,(prototype as any)[method](0));}catch(error){console.log(method,(error as Error).name);}
}
console.log('brand',Object.prototype.toString.call(prototype),prototype instanceof Date,Object.getPrototypeOf(prototype)===Object.prototype);
try{new Date(prototype);}catch(error){console.log('copy',(error as Error).name);}
const clone=structuredClone(prototype);console.log('clone',clone instanceof Date,Object.keys(clone).length,Object.getPrototypeOf(clone)===Object.prototype);
const fake=Object.create(Date.prototype);
try{Date.prototype.getTime.call(fake);}catch(error){console.log('fake',(error as Error).name);}
