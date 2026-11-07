/// <reference lib="esnext.disposable" />
function resource(name: string) {
    console.log("open", name);
    return {name, closed: false, [Symbol.dispose]() {this.closed = true; console.log("close", this.name);}};
}
namespace N {
    using r = resource("namespace");
    export const value = r.name;
    export function read() {return r.closed;}
    console.log("inside", read());
    export namespace Inner {
        using r = resource("nested");
        export const value = r.name;
    }
}
console.log("after", N.value, N.read(), N.Inner.value);
namespace N {
    using r = resource("merged");
    export const second = r.name;
}
namespace Dotted.Inner {
    using r = resource("dotted");
    export const read = () => r.closed;
}
console.log(N.second, Dotted.Inner.read());
enum E {A}
namespace E {
    using r = resource("enum namespace");
    export function isClosed() {return r.closed;}
}
console.log("enum cleanup", E.A, E.isClosed());
