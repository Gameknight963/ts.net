function effect(name: string) {console.log('value', name); return name;}
function dec(label: string): any {
 console.log('eval', label);
 return function(value: any, context: any) {
  console.log('apply', label, context.kind, context.name, context.static, context.private);
  context.addInitializer(function(this: any) {console.log('extra', label, this.name || this.field);});
  if (context.kind === 'field') return function(this: any, initial: any) {console.log('init', label, initial); return initial;};
  if (context.kind === 'accessor') return {init(this: any, initial: any) {console.log('init', label, initial); return initial;}};
  if (context.kind === 'class') return class extends value {static replaced = true;};
 };
}
@dec('class')
class Example {
 @dec('static-field') static sf = effect('sf');
 @dec('field') field = effect('field');
 @dec('static-method') static sm() {}
 @dec('method') method() {}
 @dec('static-accessor') static accessor sa = effect('sa');
 @dec('accessor') accessor a = effect('a');
 static {console.log('static-block', this.name);}
}
console.log('constructed-class', Example.name, Object.hasOwn(Example, 'sf'));
new Example();
