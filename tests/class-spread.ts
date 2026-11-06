class SpreadRecord {x=1;y=2;}
const spreadRecord=new SpreadRecord();const spreadCopy={...spreadRecord};
console.log(spreadCopy.x,spreadCopy.y,Object.keys(spreadCopy).length);
spreadRecord.x=3;console.log(spreadCopy.x,spreadRecord.x);
class SpreadMethods {value=4;method(){return this.value;}}
const methodCopy={...new SpreadMethods()};console.log(methodCopy.value,Object.keys(methodCopy).length);
