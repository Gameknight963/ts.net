function checkedWrite(value: any) {
    const point = { x: 1 };
    point.x = value;
    return point.x;
}
console.log(checkedWrite("wrong"));
