export function make(target: object, handler: object) {
    return Proxy.revocable(target, handler);
}
