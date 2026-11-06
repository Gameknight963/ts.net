export class Vault {
    #value:string;
    constructor(value:string){this.#value=value;}
    read(other:Vault):string{return other.#value;}
    static has(value:any):boolean{return #value in value;}
}
export function create(){return class {#value=9;read():number{return this.#value;}};}
