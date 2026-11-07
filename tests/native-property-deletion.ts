function recreate(owner: any, key: string): void {
    const original = owner[key];
    console.log(key, 'before', Object.hasOwn(owner, key), Object.prototype.propertyIsEnumerable.call(owner, key));
    owner[key] = original;
    console.log(key, 'overwrite', Object.keys(owner).includes(key));
    console.log(key, 'delete', delete owner[key], Object.hasOwn(owner, key));
    owner[key] = original;
    let found = false;
    for (const name in owner) if (name === key) found = true;
    console.log(key, 'recreated', Object.hasOwn(owner, key), Object.keys(owner).includes(key), Object.prototype.propertyIsEnumerable.call(owner, key), found);
}
recreate(Array.prototype, 'pop');
recreate(Math, 'sin');
recreate(Promise.prototype, 'catch');
recreate(Number, 'isInteger');
recreate(Number.prototype, 'toFixed');
recreate(String.prototype, 'trim');
recreate(Boolean.prototype, 'valueOf');
try { delete (Math as any).PI; } catch (error) { console.log('constant', (error as Error).name); }
console.log('constant retained', Math.PI > 3);
try { delete (String.prototype as any).length; } catch (error) { console.log('string length', (error as Error).name); }
