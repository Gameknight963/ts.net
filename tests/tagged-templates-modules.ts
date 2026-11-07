import {tag,site,object} from "./tagged-template-helper.js";
import * as helpers from "./tagged-template-helper.js";
console.log(tag`a${1}b`,helpers.tag`c${2}d`);console.log(site()===site(),site()===helpers.site());console.log(object.tag`x${3}`);
console.log(String.raw`module${4}end`);
