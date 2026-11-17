// Launch script that ensures ELECTRON_RUN_AS_NODE is cleared
const { spawn } = require('child_process');
const path = require('path');

const electronPath = require('electron');
const env = { ...process.env };
delete env.ELECTRON_RUN_AS_NODE;

const child = spawn(electronPath, ['.'], {
  stdio: 'inherit',
  env,
  cwd: __dirname,
});

child.on('close', (code) => process.exit(code));
