export {};
const {MAX_SAFE_INTEGER,MIN_SAFE_INTEGER,EPSILON,MAX_VALUE,MIN_VALUE,POSITIVE_INFINITY,NEGATIVE_INFINITY,NaN:nan,parseInt:integer,parseFloat:floating}=Number;
console.log("constants",MAX_SAFE_INTEGER,MIN_SAFE_INTEGER,EPSILON,MAX_VALUE,MIN_VALUE,POSITIVE_INFINITY,NEGATIVE_INFINITY,Number.isNaN(nan));
console.log("parsing",integer("ff",16),floating("3.5suffix"),integer===Number.parseInt,floating===Number.parseFloat);
const {isInteger,MAX_SAFE_INTEGER:maximum,parseInt}=Number;
console.log("mixed",isInteger(3),maximum,parseInt("101",2),isInteger===Number.isInteger);
let calls=0;
function key():"parseInt"{calls++;return "parseInt";}
const {[key()]:computed}=Number;
console.log("computed",calls,computed("12",10),computed===Number.parseInt);
const property:"EPSILON"="EPSILON";
const {[property]:epsilon}=Number;
console.log("computed constant",epsilon===Number.EPSILON);
const {parseFloat:parse=()=>99,...rest}=Number;
console.log("rest",parse("2.25"),Object.keys(rest).join(","));
const {isFinite:finite,...others}=Number;
console.log("predicate rest",finite(2),Object.keys(others).length);
const {parseInt:{name,length}}=Number;
console.log("nested",name,length);
function read(){const {EPSILON,parseInt,isInteger}=Number;return isInteger(parseInt("12"))&&EPSILON>0;}
console.log("local",read());
