console.log("entry");
async function run() {
 console.log("before import");
 const first = await import("./dynamic-import-values.js");
 console.log("first", first.value, first.default, first.bump(), first.value);
 const second = await import("./dynamic-import-values.js");
 console.log("cached", first === second, second.value);
 const wrapper = await import("./dynamic-import-static.js");
 console.log("shared", wrapper.shared === first);
 let failure: unknown;
 try {await import("./dynamic-import-failure.js");} catch(error) {failure = error; console.log("failed", (error as Error).message);}
 try {await import("./dynamic-import-failure.js");} catch(error) {console.log("cached failure", error === failure);}
 const missing = "./not-included.js";
 try {await import(missing);} catch(error) {console.log("missing", (error as Error).name);}
 const cycle = await import("./dynamic-import-cycle-a.js");
 console.log("cycle", cycle.read());
 let tdz: unknown;
 try {await import("./dynamic-import-tdz-a.js");} catch(error) {tdz = error; console.log("tdz", (error as Error).name);}
 try {await import("./dynamic-import-tdz-a.js");} catch(error) {console.log("tdz cached", error === tdz);}
 console.log("namespace", Object.prototype.toString.call(first), Object.getPrototypeOf(first) === null, Reflect.ownKeys(first).map(String).join(","));
 await import("./dynamic-import-values.js").then(module => console.log("callback", module.value));
 const pair = await Promise.all([import("./dynamic-import-values.js"), import("./dynamic-import-values.js")]);
 console.log("pair", pair[0] === pair[1]);
 const disposal = await import("./dynamic-import-disposal.js");
 console.log("module cleanup complete", disposal.isClosed());
 const loader = await import("./dynamic-import/loader.js");
 console.log((await loader.load()).value);
 const template = await import(`./dynamic-import-values.js`);
 console.log("template", template === first);
 const coerced = await import({toString() {console.log("specifier conversion"); return "./dynamic-import-values.js";}} as any);
 console.log("coerced", coerced === first);
 try {await import({toString() {throw "specifier failure";}} as any);} catch(error) {console.log("conversion failure", error);}
 const self = await import(import.meta.url);
 console.log("self namespace", Object.keys(self).length);
 const path = "./dynamic-import-values.js";
 const third = await import(path);
 console.log("expression", third === first, third.value);
}
run();
console.log("caller");
