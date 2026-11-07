async function asyncName(){await Promise.resolve();const values=["one","two","three"];for(let i=0;i<3;i++)values.push(String(i));return new Error(values.join("|"));}
function* generatorName(){yield 1;const values=["one","two","three"];for(let i=0;i<3;i++)values.push(String(i));return new Error(values.join("|"));}
const iterator=generatorName();iterator.next();console.log("generator",(iterator.next().value as Error).stack?.includes("generatorName"));
asyncName().then(error=>console.log("async",error.stack?.includes("asyncName")));
class Sample {error:Error;constructor(){const values=["one","two","three"];for(let i=0;i<3;i++)values.push(String(i));this.error=new Error(values.join("|"));}get current(){const values=["one","two","three"];for(let i=0;i<3;i++)values.push(String(i));return new Error(values.join("|"));}read(){return 7;}}
const sample=new Sample();console.log("constructor",sample.error.stack?.includes("Sample"));console.log("getter",sample.current.stack?.includes("current"));console.log("names",Sample.name,sample.read.name,Object.getOwnPropertyDescriptor(Sample.prototype,"current")!.get!.name);
