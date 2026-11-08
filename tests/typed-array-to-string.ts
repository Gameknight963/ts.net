const stringify = Uint8Array.prototype.toString;
console.log('identity', stringify === Array.prototype.toString, stringify === Float64Array.prototype.toString);
const values = new Uint8Array([1,2,3]);
console.log('ordinary', values.toString());
values.join = function(separator?: string): string { console.log('join receiver', this === values, arguments.length); return 'custom'; };
console.log('override', values.toString());
const generic = { join() { return 42; } };
console.log('generic', (stringify as any).call(generic));
for (const join of [null, undefined, 1, {}]) {
    console.log('fallback', stringify.call({join}));
}
console.log('array', stringify.call([4,5]));
let reads = 0;
const accessor = { get join() { reads++; return function(this: unknown) { console.log('accessor receiver', this === accessor); return 'getter'; }; } };
console.log('getter', stringify.call(accessor), reads);
const detached = new Uint8Array([1]);
structuredClone(detached.buffer,{transfer:[detached.buffer]});
try { stringify.call(detached); }
catch(error) { console.log('detached', (error as Error).name); }
(detached as any).join = () => 'detached override';
console.log('detached custom', stringify.call(detached));
console.log('metadata', stringify.name, stringify.length);
