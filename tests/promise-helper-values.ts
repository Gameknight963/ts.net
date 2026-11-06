/// <reference lib="es2024.promise" />
/// <reference lib="esnext.promise" />
export function make<T>():PromiseWithResolvers<T>{return Promise.withResolvers<T>();}
export function attempt<T>(callback:()=>T):Promise<Awaited<T>>{return Promise.try(callback);}
