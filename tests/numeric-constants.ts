console.log(NaN, Infinity, -Infinity, typeof NaN, typeof Infinity);
console.log(1 / Infinity, 1 / -Infinity, Infinity * 0, Infinity > 1e300);
function shadowed(NaN: number, Infinity: number): number { return NaN + Infinity; }
console.log(shadowed(3, 4));
{
    const Infinity = 7;
    const NaN = 8;
    console.log(Infinity, NaN);
}
const constants = { NaN, Infinity };
console.log(constants.NaN, constants.Infinity);
const { NaN: notANumber, Infinity: infinite } = constants;
console.log(notANumber, infinite, -infinite);
