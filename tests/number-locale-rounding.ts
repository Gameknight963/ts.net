for (const value of [1.005, 2.675, 0.125, -0.125, -0.001, -0, 1e25, 1e300, 5e-324, 1000000000000000100]) {
 for (const maximumFractionDigits of [0, 2, 5]) {
  console.log(value.toLocaleString('en-US', {maximumFractionDigits}));
 }
}
console.log((1.005).toLocaleString('en-US', {style:'decimal',minimumFractionDigits:2,maximumFractionDigits:2}));
