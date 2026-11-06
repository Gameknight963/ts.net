import {work,branch,pattern,choose,loop} from './await-statement-values';
work(4).then(v=>console.log('module work',v));branch(true).then(v=>console.log('module true',v));branch(false).then(v=>console.log('module false',v));console.log('module sync');
pattern().then(v=>console.log('module pattern',v));
choose(1).then(v=>console.log('module switch',v));
choose(2).then(v=>console.log('module default',v));
loop().then(v=>console.log('module loop',v));
