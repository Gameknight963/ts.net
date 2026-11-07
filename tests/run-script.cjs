const fs = require("node:fs");
const vm = require("node:vm");
vm.runInNewContext(fs.readFileSync(process.argv[2], "utf8"), { console });
