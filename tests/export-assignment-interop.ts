import factory from "./export-assignment/factory";
import Box from "./export-assignment/box";
import value from "./export-assignment/snapshot";
console.log(factory(8).read(),new Box(9).read(),value.count);
