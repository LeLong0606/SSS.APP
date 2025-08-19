const { exec } = require('child_process');
const fs = require('fs');
const path = require('path');

console.log('🔐 Generating SSL certificate for localhost development...');

// Check if certificates already exist
if (fs.existsSync('localhost.crt') && fs.existsSync('localhost.key')) {
    console.log('✅ SSL certificates already exist!');
    console.log('📁 Files: localhost.crt, localhost.key');
    return;
}

// Generate SSL certificate using openssl
const command = `openssl req -x509 -out localhost.crt -keyout localhost.key \
  -newkey rsa:2048 -nodes -sha256 \
  -subj '/CN=localhost' -extensions EXT -config <( \
   printf "[dn]\nCN=localhost\n[req]\ndistinguished_name = dn\n[EXT]\nsubjectAltName=DNS:localhost\nkeyUsage=keyEncipherment,dataEncipherment\nextendedKeyUsage=serverAuth")`;

exec(command, (error, stdout, stderr) => {
    if (error) {
        console.error('❌ Error generating SSL certificate:');
        console.error('This requires OpenSSL to be installed on your system.');
        console.error('');
        console.error('🔧 Alternative: Use Angular CLI with --ssl flag:');
        console.log('   npm run start:https');
        console.error('');
        console.error('🔧 Or install mkcert for easier SSL certificate generation:');
        console.error('   https://github.com/FiloSottile/mkcert');
        return;
    }
    
    console.log('✅ SSL certificate generated successfully!');
    console.log('📁 Files created: localhost.crt, localhost.key');
    console.log('');
    console.log('🚀 Now you can run:');
    console.log('   npm start        (HTTP on localhost:50503)');
    console.log('   npm run start:https (HTTPS on localhost:50503)');
});
