export {};
function positive(value:1|2|3|undefined,right:boolean):void {
    let calls=0;
    function next():boolean{calls++;return right;}
    console.log('positive',value,value && next(),calls);
    if(value && right)console.log('condition',true);else console.log('condition',false);
}
for(const value of [undefined,1,2,3] as const){positive(value,true);positive(value,false);}
function negative(value:-1|-2|undefined):void {console.log('negative',value && true);}
negative(-1);negative(-2);negative(undefined);
function text(value:'yes'|'no'|undefined):void{console.log('text',value && true,value || false);}
text('yes');text('no');text(undefined);
function zero(value:0|1|undefined):void{console.log('zero',value && true,value || false);}
zero(0);zero(1);zero(undefined);
function optional(options:Intl.DateTimeFormatOptions):void {
    let count=0;
    if(options.fractionalSecondDigits && ++count===1)console.log('option true');
    console.log('count',count);
}
optional({});optional({fractionalSecondDigits:3});
