export {};
for (const action of [Reflect, Reflect.deleteProperty]) {
    const target = {remove: 1, keep: 2};
    if (typeof action === 'function') action(target, 'remove');
    else action.deleteProperty(target, 'remove');
    console.log(Object.keys(target).join(','));
}
for (const action of [Reflect, Reflect.defineProperty]) {
    const target = {value: 1};
    if (typeof action === 'function') action(target, 'value', {get() { return 2; }});
    else action.defineProperty(target, 'value', {get() { return 3; }});
    console.log(target.value);
}
