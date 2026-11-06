import {service,maybe} from './optional-chaining-values';
import * as namespace from './optional-chaining-values';
console.log(service?.read?.(2),maybe?.(3),namespace?.service?.read?.(4),namespace.maybe?.(5));
import * as same from './optional-chaining-values';
import {receiver,update} from './optional-chaining-values';
const alias=namespace;
console.log(namespace===same,namespace===alias,namespace?.changing);
update();
console.log(alias?.changing,namespace?.default?.(),receiver?.(),namespace.receiver?.(),(namespace?.receiver)?.());
let moduleEffects='';function moduleKey():string{moduleEffects+='K';return 'maybe';}
console.log((namespace as any)?.[moduleKey()]?.(6),moduleEffects);
import * as forwarded from './optional-chaining-forward';
console.log(forwarded?.nested===namespace,forwarded?.nested?.changing,forwarded?.nested?.maybe?.(7));

console.log(namespace.receiver(),namespace.earlyReceiver());
