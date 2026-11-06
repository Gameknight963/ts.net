import {Exported as Source} from './enum-values';
import Only = Source.Nested;
console.log(Only.read(), Only.Choice.Last, new Only.Box().value);
