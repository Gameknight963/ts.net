import {closed, run} from "./await-using-helper.js";
run().then(read => console.log("module result", closed, read()));
console.log("module caller", closed);
