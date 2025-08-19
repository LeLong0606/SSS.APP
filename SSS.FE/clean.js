const fs = require('fs');
const path = require('path');

console.log('🧹 Starting cross-platform cleanup...');

// Function to remove directory recursively
function removeDirectory(dirPath) {
    if (fs.existsSync(dirPath)) {
        console.log(`📁 Removing directory: ${dirPath}`);
        fs.rmSync(dirPath, { recursive: true, force: true });
        console.log(`✅ Directory removed: ${dirPath}`);
        return true;
    }
    return false;
}

// Function to remove file
function removeFile(filePath) {
    if (fs.existsSync(filePath)) {
        console.log(`📄 Removing file: ${filePath}`);
        fs.unlinkSync(filePath);
        console.log(`✅ File removed: ${filePath}`);
        return true;
    }
    return false;
}

try {
    // Remove node_modules directory
    const nodeModulesRemoved = removeDirectory('node_modules');
    
    // Remove package-lock.json file
    const packageLockRemoved = removeFile('package-lock.json');
    
    // Remove dist directory if it exists
    const distRemoved = removeDirectory('dist');
    
    // Remove .angular cache if it exists
    const angularCacheRemoved = removeDirectory('.angular');
    
    if (nodeModulesRemoved || packageLockRemoved || distRemoved || angularCacheRemoved) {
        console.log('');
        console.log('🎉 Cleanup completed successfully!');
        console.log('💡 Run "npm install" to reinstall dependencies');
    } else {
        console.log('ℹ️  Nothing to clean - directories and files already clean');
    }
} catch (error) {
    console.error('❌ Error during cleanup:', error.message);
    process.exit(1);
}
