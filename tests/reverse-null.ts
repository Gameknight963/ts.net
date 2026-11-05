const items: any = null;
function effect() { console.log("must not run"); return 0; }
items.reverse(effect());
