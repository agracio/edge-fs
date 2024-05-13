## edge-fs

### F# compiler for [Edge.js](https://github.com/agracio/edge-js). 

### This library is based on https://github.com/7sharp9/edge-fs all credit for original work goes to Dave Thomas. 
------

### Overview

Install edge and edge-fs modules:

``` 
npm install edge-js
npm install edge-fs
```

server.js:

```javascript
var edge = require('edge-js');

var helloFs = edge.func('fs', function () {/*
    fun input -> async { 
        return "F# welcomes " + input.ToString()
    }
*/});

helloFs('Node.js', function (error, result) {
    if (error) throw error;
    console.log(result);
});
```

Run and enjoy:

```
node server.js
F# welcomes Node.js
```

#### Supported .NET frameworks

* .NET 4.6.2
* .NET Standard 2.0
