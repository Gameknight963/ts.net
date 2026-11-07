const buffer = new ArrayBuffer(32);
const view = new DataView(buffer);
console.log(buffer.byteLength, view.byteLength, view.byteOffset, view.buffer === buffer);
console.log(ArrayBuffer.isView(view), ArrayBuffer.isView(buffer), ArrayBuffer.isView({}));
console.log(view.getUint32(0), view.getFloat64(8));
view.setUint32(0, 0x12345678);
console.log(view.getUint8(0), view.getUint8(1), view.getUint16(0), view.getUint32(0, true));
view.setInt16(4, -1234, true);
view.setInt8(6, 255);
view.setUint8(7, 258);
console.log(view.getInt16(4, true), view.getUint16(4, true), view.getInt8(6), view.getUint8(7));
view.setInt32(8, -2147483648, true);
view.setUint32(12, 4294967295);
console.log(view.getInt32(8, true), view.getUint32(12), view.getInt32(12));
view.setFloat32(16, 1.25, true);
view.setFloat64(20, -3.5);
console.log(view.getFloat32(16, true), view.getFloat64(20));
const part = new DataView(buffer, 4, 4);
console.log(part.byteOffset, part.byteLength, part.getInt16(0, true));
part.setUint8(3, 42);
console.log(view.getUint8(7));
const copy = buffer.slice(4, 8);
console.log(copy.byteLength, new DataView(copy).getUint8(3));
new DataView(copy).setUint8(3, 10);
console.log(view.getUint8(7), buffer.slice(-4).byteLength, buffer.slice(8, 2).byteLength);
console.log(new ArrayBuffer(3.9).byteLength, new ArrayBuffer(NaN).byteLength);
class Packet extends ArrayBuffer { label = 'packet'; }
const packet = new Packet(8);
console.log(packet.byteLength, packet.label, packet instanceof Packet, packet instanceof ArrayBuffer);
class Reader extends DataView<ArrayBuffer> { read() { return this.getUint8(0); } }
const reader = new Reader(buffer, 7, 1);
console.log(reader.read(), reader instanceof Reader, reader instanceof DataView);
for (const operation of [() => new ArrayBuffer(-1), () => new DataView(buffer, 33), () => new DataView(buffer, 30, 3), () => view.getUint32(30), () => part.setUint8(4, 1)]) {
    try { operation(); } catch (error) { console.log(error instanceof RangeError); }
}


view.setBigInt64(0, -1234567890123456789n, true);
console.log(view.getBigInt64(0, true).toString());
view.setBigUint64(8, 18446744073709551615n);
console.log(view.getBigUint64(8).toString(), view.getBigInt64(8).toString());
view.setBigUint64(8, -1n);
console.log(view.getBigUint64(8).toString());

const half = view as any;
half.setFloat16(0, 1.5, true);
console.log(half.getFloat16(0, true));
view.setFloat64(0, -0, true);
console.log(Object.is(view.getFloat64(0, true), -0));
view.setFloat64(0, Infinity);
console.log(view.getFloat64(0));
view.setFloat32(0, NaN);
console.log(Number.isNaN(view.getFloat32(0)));
view.setUint32(0, 4294967297);
console.log(view.getUint32(0));
let conversions = '';
const index = { valueOf() { conversions += 'i'; return 0; } };
const value = { valueOf() { conversions += 'v'; return 7; } };
(view as any).setInt16(index, value);
console.log(conversions, view.getInt16(0));
try { DataView.prototype.getUint8.call({} as DataView, 0); } catch (error) { console.log(error instanceof TypeError); }
try { (ArrayBuffer as any)(1); } catch (error) { console.log(error instanceof TypeError); }
