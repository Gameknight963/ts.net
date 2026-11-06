import {work,branch} from './await-statement-values';
work(4).then(v=>console.log('module work',v));branch(true).then(v=>console.log('module true',v));branch(false).then(v=>console.log('module false',v));console.log('module sync');
