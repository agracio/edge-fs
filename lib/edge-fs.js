var path = require('path');

process.env.EDGE_FS_TOOLS = path.join(__dirname, 'tools')
exports.getCompiler = function () {
	return process.env.EDGE_FS_NATIVE || ( process.env.EDGE_USE_CORECLR ? path.join(__dirname, 'edge-fs-coreclr.dll') : path.join(__dirname, 'edge-fs.dll'));
};

// exports.getBootstrapDependencyManifest = function() {
// 	return path.join(__dirname, 'edge-fs-coreclr.deps.json');
// }
