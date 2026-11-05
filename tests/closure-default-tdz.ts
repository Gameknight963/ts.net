function run(first: () => number = () => second, second: number = first()) { return second; }
run();
