export {};
const parent={inherited:42};const object={__proto__:parent,own:1};
console.log("parent",Object.getPrototypeOf(object)===parent,(object as any).inherited,Object.keys(object).join("|"),Object.hasOwn(object,"__proto__"));
const nil={__proto__:null,own:2};console.log("null",Object.getPrototypeOf(nil)===null,"toString" in nil,Object.keys(nil).join("|"));
const computed={["__proto__"]:parent,own:3};console.log("computed",Object.getPrototypeOf(computed)===Object.prototype,Object.hasOwn(computed,"__proto__"),computed.__proto__===parent);
const __proto__=parent;const shorthand={__proto__};console.log("short",Object.getPrototypeOf(shorthand)===Object.prototype,Object.hasOwn(shorthand,"__proto__"));
for(const proto of [undefined,3,"abc",true]){const value={__proto__:proto,own:4};console.log("primitive",Object.getPrototypeOf(value)===Object.prototype,Object.hasOwn(value,"__proto__"));}
let order="";function value<T>(name:string,result:T):T{order+=name;return result;}
const ordered={a:value("A",1),__proto__:value("P",parent),...value("S",{b:2}),c:value("C",3)};console.log("order",order,Object.getPrototypeOf(ordered)===parent,Object.keys(ordered).join("|"));
const copied={...object};console.log("spread",Object.getPrototypeOf(copied)===Object.prototype,Object.keys(copied).join("|"));
const quoted={"__proto__":parent,x:5};console.log("quoted",Object.getPrototypeOf(quoted)===parent,Object.hasOwn(quoted,"__proto__"));

console.log("own reads",object.own,nil.own,quoted.x);
const method={__proto__:parent,read(){return this.own;},own:7};console.log("method",method.read(),(method as any).inherited);

async function asyncObject():Promise<void>{const value={own:await Promise.resolve(8),__proto__:await Promise.resolve(parent)};console.log("await",Object.getPrototypeOf(value)===parent,value.own);}
asyncObject();
