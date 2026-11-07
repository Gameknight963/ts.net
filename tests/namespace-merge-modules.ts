import {Counter, Box} from "./namespace-merge-helper.js";
import * as All from "./namespace-merge-helper.js";
import Alias = Box.Nested;
console.log(Counter(3), Counter.read(), Box.create(8).value, Alias.value);
Counter.offset = 9;
console.log(All.Counter(2), All.Counter.read(), All.Box === Box);
