const invalidCharacter: any = {valueOf:()=>({}),toString:()=>({})};
console.log("abc".codePointAt(invalidCharacter));
