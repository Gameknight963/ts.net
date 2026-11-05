const produce: () => void = () => 73;
export function exported(): void { return produce(); }
export const result = exported();
