const first={callField:1,keep:2};
console.log('call',Reflect.deleteProperty.call(null,first,'callField'),Object.keys(first).join(','));
const erase=Reflect.deleteProperty;
const second={applyField:3};
console.log('apply',erase.apply(undefined,[second,'applyField']),Object.keys(second).length);
const bound=erase.bind(null);
const third={boundField:4};
console.log('bind',bound(third,'boundField'),Object.keys(third).length);
const indexed={indexedCall:5};
console.log('indexed',erase['call'](undefined,indexed,'indexedCall'),Object.keys(indexed).length);
function drop(value:{dynamicCall?:number},key:string):boolean{return bound.call(null,value,key);}
const dynamic={dynamicCall:6};
console.log('dynamic',drop(dynamic,'dynamicCall'),Object.keys(dynamic).length);
const {['deleteProperty']:computed}=Reflect;
const fourth={computedMethod:7};
console.log('computed',computed(fourth,'computedMethod'),Object.keys(fourth).length);
const rebound=bound.bind(undefined);
const fifth={reboundField:8};
console.log('rebound',rebound(fifth,'reboundField'),Object.keys(fifth).length);
