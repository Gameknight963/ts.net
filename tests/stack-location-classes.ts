function hasSourceLocation(error:Error,line:number){const stack=(error.stack??"").replace(/:line (\d+)/g,":$1");return stack.includes("stack-location-classes.ts:"+line);}
class Sample {
    field=new Error("field");
    static field=new Error("static field");
    constructor(){this.created=new Error("constructor");}
    created:Error;
    get error(){return new Error("getter");}
}
const sample=new Sample();console.log("classes",hasSourceLocation(sample.field,3),hasSourceLocation(Sample.field,4),hasSourceLocation(sample.created,5),hasSourceLocation(sample.error,7));
const arrow=()=>new Error("arrow");console.log("arrow",hasSourceLocation(arrow(),10));
