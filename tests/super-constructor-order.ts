export {};
let events = "";
class First {tag = "first"; constructor(value: number) {events += "F";}}
class Second {tag = "second"; constructor(value: number) {events += "S";}}
function change(): number {events += "a"; Object.setPrototypeOf(Snapshot, First); return 1;}
class Snapshot extends First {constructor() {super(change());}}
Object.setPrototypeOf(Snapshot, Second);
if (new Snapshot().tag !== "second" || events !== "aS") throw new Error("Super constructor was not captured before arguments");
if (new Snapshot().tag !== "first") throw new Error("Later construction ignored the changed parent");
function* argumentsForSuper(): Generator<number> {events += "i"; Object.setPrototypeOf(Spread, First); yield 2;}
class Spread extends (First as any) {constructor() {super(...argumentsForSuper());}}
Object.setPrototypeOf(Spread, Second);
if (new Spread().tag !== "second") throw new Error("Spread arguments changed the selected constructor");
let repaired = false, rejected = false;
function repair(): number {repaired = true; Object.setPrototypeOf(Repair, First); return 3;}
class Repair extends First {constructor() {super(repair());}}
Object.setPrototypeOf(Repair, null);
try {new Repair();} catch (error) {rejected = error instanceof TypeError;}
if (!repaired || !rejected) throw new Error("Invalid constructor was not checked after argument evaluation");
if (new Repair().tag !== "first") throw new Error("Repair did not affect the next construction");
console.log("PASS super constructor capture and argument order");
