export namespace Library {
 export let value = 2;
 export function increment() {return ++value;}
 export namespace Nested {export const text = "nested";}
}
export namespace Library {export const read = () => value;}
namespace Hidden {export const value = 8;}
export default Hidden;
