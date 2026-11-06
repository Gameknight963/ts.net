export function kernel(count:number,offset:number):number {
 const Constructor:any=function(this:any,value:number):void {this.value=value;};
 let total=offset;
 for(let i=0;i<count;i++){const item=new Constructor(i) as {value:number};total+=item.value;}
 return total;
}
console.log(kernel(1,0),kernel(100000,0));
