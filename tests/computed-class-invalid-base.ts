let touched=0;
const keys={get value():'value'{touched++;return 'value';}};
let rejected=false;
try {const invalid:any=3;class Invalid extends invalid {[keys.value]=1;}}
catch(error){rejected=error instanceof TypeError;}
console.log(rejected,touched);
