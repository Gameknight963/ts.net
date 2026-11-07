export {};
function factory(){return class{#value=12;static read(value:any){return value.#value;}};}
const First=factory(),Second=factory();const first=new First(),second=new Second();console.log("brands",First.read(first),Second.read(second));try{First.read(second);}catch(error){console.log("cross brand",(error as Error).name);}
