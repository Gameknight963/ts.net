import {Vault,create} from './private-member-values';
const a=new Vault('a');const b=new Vault('b');
console.log('imported',a.read(b),Vault.has(a),Vault.has({}));
const Made=create();console.log('factory',new Made().read());
