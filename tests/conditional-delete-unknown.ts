export {};
function erase(flag:boolean,key:string):void {
    const record:{third?:number,keep:number}={third:14,keep:15};
    const untrusted=key as "absent";
    const chosen=flag?"absent":untrusted;
    console.log(delete record[chosen as keyof typeof record],Object.keys(record).join(","),record.third,record.keep);
}
erase(true,"third");
erase(false,"third");
erase(false,"keep");
