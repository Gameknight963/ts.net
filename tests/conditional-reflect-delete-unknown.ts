export {};
function erase(first:boolean,key:string):void {
    const record:any={third:14,keep:15};
    const asserted=key as "absent";
    console.log(Reflect.deleteProperty(record,first?"absent":asserted),Object.keys(record).join(","),record.third,record.keep);
}
erase(true,"third");
erase(false,"third");
erase(false,"keep");
