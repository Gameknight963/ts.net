export {};
const first={conditionalPayload:1},second={conditionalPayload:2};
function First(){return first;}
function Second(){return second;}
for(const flag of [true,false]){
 const Parent=flag?First:Second;
 class Child extends (Parent as any){conditionalPayload='text';}
 const value=new Child();
 console.log('conditional base field',value.conditionalPayload,(flag?first:second as any).conditionalPayload);
}
