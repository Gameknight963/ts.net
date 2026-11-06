const invalidBound: any={valueOf:()=>({}),toString:()=>({})};
console.log("abc".substring(0,invalidBound));
