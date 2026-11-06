const invalidIndex: any = { valueOf: () => ({}), toString: () => ({}) };
console.log([1, 2].at(invalidIndex));
