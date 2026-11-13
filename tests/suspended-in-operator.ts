const membershipOrder: string[] = [];
const membershipKey = {toString(): string { membershipOrder.push('key'); return 'x'; }};
function membershipTarget(): Promise<object> { membershipOrder.push('target'); return Promise.resolve({x: 1}); }
class MembershipBox {
    #value = 1;
    static async contains(value: object): Promise<boolean> { return #value in await Promise.resolve(value); }
}
async function awaitedMembership(): Promise<void> {
    console.log('right', 'x' in await Promise.resolve({x: 1}), 'missing' in await Promise.resolve({x: 1}));
    console.log('left', await Promise.resolve('x') in {x: 1});
    console.log('both', await Promise.resolve('x') in await Promise.resolve({x: 1}));
    console.log('order', membershipKey as any in await membershipTarget(), membershipOrder.join(','));
    const beforeFailure = membershipOrder.length;
    try { console.log(membershipKey as any in await Promise.resolve(3 as any)); }
    catch (error) { console.log('invalid-target', Boolean(error), membershipOrder.length === beforeFailure); }
    try { console.log(membershipKey as any in await Promise.reject('target-rejected')); }
    catch (error) { console.log('rejected-target', error, membershipOrder.length === beforeFailure); }
    console.log('private', await MembershipBox.contains(new MembershipBox()), await MembershipBox.contains({}));
    try { console.log('x' in await Promise.reject('expected')); } catch (error) { console.log('reject', error); }
}
function* yieldedMembership(): Generator<unknown, void, any> {
    console.log('yield-right', 'x' in (yield 1));
    console.log('yield-left', (yield 2) in {x: 1});
    console.log('yield-both', (yield 3) in (yield 4));
}
const membership = yieldedMembership();
console.log('first', membership.next().value);
console.log('second', membership.next({x: 1}).value);
console.log('third', membership.next('x').value);
console.log('fourth', membership.next('x').value);
console.log('done', membership.next({x: 1}).done);
awaitedMembership();
