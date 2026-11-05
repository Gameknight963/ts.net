const items: any = [1, 2];
items.reverse = 3;
function effect() { console.log("argument"); return 0; }
items.reverse(effect());
