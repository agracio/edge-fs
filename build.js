const spawn = require('child_process').spawn;
const path = require('path')
const fs = require('fs');

spawn('dotnet', ['restore', 'EdgeFs.sln'], { stdio: 'inherit', cwd: path.resolve(__dirname) })
    .on('close', function(code, signal) {
        if (code === 0) {
            spawn('dotnet', ['build', 'EdgeFs.sln', '--configuration', 'Release'], { stdio: 'inherit', cwd: path.resolve(__dirname) })
            .on('close', function(code, signal) {
                if (code === 0) {
                    copy();
                }
            })
        }
    });

function copy() {

    fs.copyFileSync('src/edge-fs/bin/Release/edge-fs.dll', 'lib/edge-fs.dll')
    fs.copyFileSync('src/edge-fs-coreclr/bin/Release/edge-fs-coreclr.dll', 'lib/edge-fs-coreclr.dll')
    fs.copyFileSync('src/edge-fs-coreclr/bin/Release/edge-fs-coreclr.deps.json', 'lib/edge-fs-coreclr.deps.json')

}

   