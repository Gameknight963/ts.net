export let calls = 0;
function create() { calls++; return { value: calls }; }
export default create();
