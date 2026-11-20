export {};
function sum(rows:{a:number;missing:number}[]):number{let total=0;for(let i=0;i<rows.length;i++)total+=rows[i].a;return total;}
const rows:any[]=JSON.parse('['+Array.from({length:64},()=>'{"a":1,"b":2,"c":3,"d":4,"e":5}').join(',')+']');
console.log(sum(rows));
