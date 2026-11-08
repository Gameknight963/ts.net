let calls=0;function create(){calls++;console.log("export expression",calls);return {calls};}export = create();
