export let finish: () => void;
export const gate = new Promise<void>(resolve => {finish = resolve;});
