export {};
const object = {field0: 0,field1: 1,field2: 2,field3: 3,field4: 4,field5: 5,field6: 6,field7: 7,field8: 8,field9: 9,field10: 10,field11: 11,field12: 12,field13: 13,field14: 14,field15: 15,"": 17,"\u00e9": 18,"\u{1f600}": 19,"\ue000": 20,"1": 21,"01": 22,"field\0x": 23,"field.0": 24,costarring:31,liquid:32,extra0:100,extra1:101,extra2:102,extra3:103,extra4:104,extra5:105,extra6:106,extra7:107,extra8:108,extra9:109,extra10:110,extra11:111};
function read(target: any, key: string): any { return target[key]; }
function own(target: any, key: string): boolean { return Object.hasOwn(target, key); }
for (const key of Object.getOwnPropertyNames(object).concat(["missing", "field16", "toString"])) {
    const value = read(object, key);
    console.log(key, typeof value === "function" ? "function" : value, own(object, key));
}
console.log("order", Object.keys(object).join(","));
const inherited = Object.create(object);
console.log("prototype", read(inherited, "field15"), own(inherited, "field15"));
let conversions = 0;
const key: any = { toString() { conversions++; return "field15"; } };
console.log("coercion", (object as any)[key], conversions);
const reordered: typeof object = { ...object };
console.log("spread", read(reordered, "field15"), own(reordered, "field15"), read(reordered, "\u00e9"));

console.log("collision", read(object,"costarring"),read(object,"liquid"),own(object,"costarring"),own(object,"liquid"));

const oneCollision = {first:1,second:2,third:3,fourth:4,fifth:5,sixth:6,seventh:7,eighth:8,padding0:200,padding1:201,padding2:202,padding3:203,padding4:204,padding5:205,padding6:206,padding7:207,padding8:208,padding9:209,padding10:210,padding11:211,padding12:212,padding13:213,padding14:214,padding15:215,padding16:216,padding17:217,padding18:218,padding19:219,padding20:220,padding21:221,padding22:222,padding23:223,costarring:33};
console.log("absent collision", read(oneCollision,"costarring"),read(oneCollision,"liquid"),own(oneCollision,"liquid"));
