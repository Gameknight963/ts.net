import {create,merge,assign,tools} from './object-assign-values';
const first=create({value:1});console.log('module create',first.value);
const target={};const second=merge(target,{value:2});console.log('module merge',second===target,second.value);
const alias=assign({},{value:3});console.log('module alias',alias.value);
const stored=tools.copy({}, {fromStoredExport:4});console.log('module property',stored.fromStoredExport);
