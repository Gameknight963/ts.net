import {Constructor, Parent, Text, Truth} from './object-constructor-values';
const value = {field: 9};
console.log(Constructor(value) === value, new Constructor(value) === value, Object.keys(Constructor()).length);
class Child extends Parent {field = 7;}
const child = new Child(value);
console.log(child !== value, child instanceof Child, child instanceof Parent, child instanceof Object, child.target === Child, child.field);
console.log(Text(7), Truth(0), new Text('module').valueOf(), new Truth(false).valueOf());
class Label extends Text {field = 8;}
console.log(new Label('label').valueOf(), new Label('label').field, new Label('label') instanceof String);
