/// <reference lib="esnext.disposable" />
import {closed, readClosed} from "./using-module-helper.js";
console.log("importer", closed, readClosed());
using held = {[Symbol.dispose]() {console.log("entry closed");}};
console.log("entry body");
