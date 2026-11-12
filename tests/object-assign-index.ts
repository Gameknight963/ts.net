export {};
console.log('computed assign', Object['assign']({}, {throughIndex: 1}).throughIndex);
const namespaceAlias = Object;
console.log('computed namespace alias', namespaceAlias['assign']({}, {throughNamespaceAlias: 2}).throughNamespaceAlias);
const target = {};
const copied = Object['assign'](target, {throughTargetVariable: 3});
console.log('computed target variable', copied === target, copied.throughTargetVariable);
const merged = Object['assign']({}, {throughFirst: 4}, {throughSecond: 5});
console.log('computed multiple sources', merged.throughFirst, merged.throughSecond);
