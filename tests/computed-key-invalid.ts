const key: any = {toString: () => ({}), valueOf: () => ({})};
const result = {[key]: 1};
console.log(result);
