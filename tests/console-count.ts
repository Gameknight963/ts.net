console.count();console.count(undefined);console.countReset();console.count();
console.count('jobs');console.count('other');console.count('jobs');console.countReset('jobs');console.count('jobs');
const count=console.count,reset=console.countReset;count('detached');reset('detached');count('detached');
console.countReset('jobs');console.count('jobs');
let conversions=0;const label={toString(){conversions++;return 'object-label';}};
console.count(label as any);console.count(label as any);console.log('conversions',conversions);
for(const method of [console.count,console.countReset,console.time,console.timeLog,console.timeEnd])console.log('metadata',method.name,method.length);
console.log('return',console.count('result'),console.countReset('result'));

console.count("");console.count("");console.count("__proto__");console.count("__proto__");console.count("Jobs");

[0,1].forEach(()=>console.count("callback"));
console["count"]("computed");
