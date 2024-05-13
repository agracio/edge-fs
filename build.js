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

// run('dotnet', ['restore', 'EdgeFs.sln'], function(code, signal) {
//     if (code === 0) {
//         run('dotnet', ['build', 'EdgeFs.sln'], function(code, signal) {
//             if (code === 0) {runOnSuccess}
//         });
//     }
// });

// function run(cmd, args, onClose){

// 	var params = process.env.EDGE_USE_CORECLR ? {cwd: testDir} : {};
//     var command = spawn(cmd, args, params);
//     var result = '';
//     var error = '';
//     command.stdout.on('data', function(data) {
//         result += data.toString();
//     });
//     command.stderr.on('data', function(data) {
//         error += data.toString();
//     });

//     command.on('error', function(err) {
//         console.log(error);
//         console.log(err);
//     });

//     command.on('close', function(code){
//         onClose(code, '');
// 	});
// }
function copy() {

    fs.copyFileSync('src/edge-fs/bin/Release/edge-fs.dll', 'lib/edge-fs.dll')
    fs.copyFileSync('src/edge-fs-coreclr/bin/Release/edge-fs-coreclr.dll', 'lib/edge-fs-coreclr.dll')

}

   