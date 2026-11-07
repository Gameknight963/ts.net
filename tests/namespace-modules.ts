import Hidden, {Library} from "./namespace-helper.js";
import * as All from "./namespace-helper.js";
import Alias = Library.Nested;
console.log(Library.value, Library.increment(), Library.read(), All.Library.value, Hidden.value, Alias.text);
Library.value = 9;
console.log(Library.read(), All.Library.value);
