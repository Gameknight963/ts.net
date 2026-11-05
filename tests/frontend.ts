function double(value: number) { return value * 2 }
function identity<T>(value: T): T { return value }
function say(value: string) { return value + "!" }
type Alias<T> = T
const count: Alias<number> = identity(double(4))
const label = identity(say("hello"))
const enabled = identity(true)
const x = 3
const point = { x }
console.log(count, label, enabled, point.x)
console.log(identity<number>(5), identity<string>("explicit"))
function readX<T extends { x: number }>(value: T) { return value.x }
console.log(readX(point))
function maybe(flag: boolean) {
    const value: number | undefined = flag ? 9 : undefined
    console.log(typeof value, value === undefined)
    if (value !== undefined) { return value + 1 }
    return 0
}
console.log(maybe(true), maybe(false))
let absent
console.log(absent, typeof absent, null, typeof null, null === undefined)
function logical(value: number, text: string) { console.log(value || 7, value && 8, text || "fallback") }
logical(0, "")
logical(3, "x")
const dynamic: any = undefined
console.log(dynamic * 2, dynamic + "!", !dynamic)
const café = { résumé: "\u0000\u03bb" }
console.log(café.résumé.length, 0xff, 0b101, 0o17, 1_000)
