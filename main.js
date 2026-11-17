const { app, BrowserWindow, ipcMain, dialog, Tray, Menu } = require('electron');
const path = require('path');
const fs = require('fs');
const { spawn } = require('child_process');

let mainWindow;
let tray = null;
let CONFIG_FILE;

// Rutas por defecto
const DEFAULT_CLIENT_DIR = 'D:\\Proyectos Web Personales\\Cliente 1.39.8 for Starloco-fun 2025\\Cliente 1.39.8 for Starloco-fun 2025';
const FALLBACK_CLIENT_DIR = 'D:\\dofus-1.39.8-remastered';

const DEFAULT_CONFIG = {
  clientDir: DEFAULT_CLIENT_DIR,
  mode: '64', // '64' (16:9 Remastered) | '32' (4:3 Clásico)
  gamePath: '',
  launcherPath: ''
};

function resolveGamePaths(inputDirOrFile) {
  let baseDir = (inputDirOrFile && inputDirOrFile.trim()) ? inputDirOrFile.trim() : DEFAULT_CLIENT_DIR;

  // Si no existe la ruta configurada, probar ruta empaquetada o fallback
  if (!fs.existsSync(baseDir)) {
    const bundledGame = path.join(process.resourcesPath, 'game');
    if (fs.existsSync(bundledGame)) {
      baseDir = bundledGame;
    } else if (fs.existsSync(FALLBACK_CLIENT_DIR)) {
      baseDir = FALLBACK_CLIENT_DIR;
    }
  }

  // Si seleccionaron un ejecutable directamente, calcular la carpeta raíz
  if (fs.existsSync(baseDir) && fs.statSync(baseDir).isFile()) {
    const lower = baseDir.toLowerCase();
    if (lower.includes('retroclient')) {
      // resources/app/retroclient/Dofus.exe -> subir 3 niveles
      baseDir = path.resolve(path.dirname(baseDir), '..', '..');
    } else {
      baseDir = path.dirname(baseDir);
    }
  }

  // Candidatos para 64-bit (16:9 Panorámica Electron)
  const candidates64 = [
    path.join(baseDir, 'Dofus Retro.exe'),
    path.join(baseDir, 'DofusRetro.exe'),
    path.join(baseDir, 'Dofus.exe')
  ];

  // Candidatos para 32-bit (4:3 Standalone Flash Player clásico)
  const candidates32 = [
    path.join(baseDir, 'resources', 'app', 'retroclient', 'Dofus.exe'),
    path.join(baseDir, 'retroclient', 'Dofus.exe'),
    path.join(baseDir, 'app', 'retroclient', 'Dofus.exe'),
    path.join(baseDir, 'Dofus.exe')
  ];

  const exe64 = candidates64.find(p => fs.existsSync(p)) || candidates64[0];
  const exe32 = candidates32.find(p => fs.existsSync(p)) || candidates32[0];

  return {
    baseDir,
    exe64,
    exe32,
    exists64: fs.existsSync(exe64),
    exists32: fs.existsSync(exe32)
  };
}

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1000,
    height: 650,
    resizable: false,
    frame: false,
    transparent: false,
    icon: path.join(__dirname, 'icon.ico'),
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      contextIsolation: true,
      nodeIntegration: false,
    },
  });

  mainWindow.loadFile('index.html');

  mainWindow.on('closed', () => {
    mainWindow = null;
  });
}

function createTray() {
  const iconPath = path.join(__dirname, 'icon.ico');
  if (!fs.existsSync(iconPath)) return;

  tray = new Tray(iconPath);
  const contextMenu = Menu.buildFromTemplate([
    {
      label: 'Mostrar Launcher',
      click: () => {
        if (mainWindow) {
          if (mainWindow.isMinimized()) mainWindow.restore();
          mainWindow.show();
          mainWindow.focus();
        } else {
          createWindow();
        }
      }
    },
    { type: 'separator' },
    {
      label: 'Iniciar 64-bit (16:9 Remastered)',
      click: () => {
        loadConfigDirectly().then(cfg => {
          launchGameProcess({ mode: '64', clientDir: cfg.clientDir });
        });
      }
    },
    {
      label: 'Iniciar 32-bit (4:3 Clásico)',
      click: () => {
        loadConfigDirectly().then(cfg => {
          launchGameProcess({ mode: '32', clientDir: cfg.clientDir });
        });
      }
    },
    { type: 'separator' },
    {
      label: 'Abrir WinTabs Multi-Cuentas',
      click: () => {
        const tabsExe = getTabsExePath();
        if (tabsExe) {
          spawn(tabsExe, [], { cwd: path.dirname(tabsExe), detached: true, stdio: 'ignore' }).unref();
        }
      }
    },
    { type: 'separator' },
    {
      label: 'Salir',
      click: () => {
        app.isQuitting = true;
        app.quit();
      }
    }
  ]);

  tray.setToolTip('Dofus Galaxy Launcher');
  tray.setContextMenu(contextMenu);

  tray.on('click', () => {
    if (mainWindow) {
      if (mainWindow.isMinimized()) mainWindow.restore();
      mainWindow.show();
      mainWindow.focus();
    } else {
      createWindow();
    }
  });
}

function launchGameProcess(options) {
  return new Promise((resolve) => {
    try {
      let mode = '64';
      let clientDir = DEFAULT_CLIENT_DIR;

      if (typeof options === 'string') {
        clientDir = options;
      } else if (options && typeof options === 'object') {
        mode = options.mode || '64';
        clientDir = options.clientDir || DEFAULT_CLIENT_DIR;
      }

      const paths = resolveGamePaths(clientDir);
      const is64 = (mode === '64');
      const targetExe = is64 ? paths.exe64 : paths.exe32;
      const exists = is64 ? paths.exists64 : paths.exists32;

      if (!exists) {
        const modeName = is64 ? '64-bit (16:9 Remastered)' : '32-bit (4:3 Clásico)';
        resolve({
          success: false,
          error: `No se encontró el ejecutable de ${modeName}.\nRuta verificada: ${targetExe}\n\nPor favor configura la carpeta del cliente en Opciones.`
        });
        return;
      }

      // Directorio de trabajo según la arquitectura:
      // - 64-bit (Electron): carpeta raíz del cliente
      // - 32-bit (Flash standalone): carpeta retroclient donde está loader.swf
      const workingDir = path.dirname(targetExe);

      const child = spawn('cmd.exe', ['/c', 'start', '""', targetExe], {
        cwd: workingDir,
        detached: true,
        stdio: 'ignore',
        windowsHide: true
      });
      child.unref();

      resolve({ success: true, targetExe, mode });
    } catch (err) {
      resolve({ success: false, error: err.message });
    }
  });
}

async function loadConfigDirectly() {
  try {
    if (CONFIG_FILE && fs.existsSync(CONFIG_FILE)) {
      const data = fs.readFileSync(CONFIG_FILE, 'utf-8');
      const parsed = JSON.parse(data);
      return { ...DEFAULT_CONFIG, ...parsed };
    }
    return DEFAULT_CONFIG;
  } catch {
    return DEFAULT_CONFIG;
  }
}

function getTabsExePath() {
  const candidates = [
    path.join(process.resourcesPath, 'DofusGalaxyTabs.exe'),
    path.join(path.dirname(app.getPath('exe')), 'DofusGalaxyTabs.exe'),
    path.join(__dirname, 'DofusGalaxyTabs.exe'),
    path.join(__dirname, '..', 'DofusGalaxyTabs', 'DofusGalaxyTabs.exe'),
    path.join(process.cwd(), 'DofusGalaxyTabs.exe')
  ];

  for (const p of candidates) {
    try {
      if (fs.existsSync(p)) return p;
    } catch {}
  }
  return null;
}

function registerIpcHandlers() {
  ipcMain.handle('launch-game', async (_event, options) => {
    return await launchGameProcess(options);
  });

  ipcMain.handle('launch-tabs', async () => {
    try {
      const tabsExe = getTabsExePath();
      if (tabsExe) {
        const child = spawn(tabsExe, [], {
          cwd: path.dirname(tabsExe),
          detached: true,
          stdio: 'ignore'
        });
        child.unref();
        return { success: true };
      }
      return { success: false, error: 'No se encontró el ejecutable DofusGalaxyTabs.exe' };
    } catch (err) {
      return { success: false, error: err.message };
    }
  });

  ipcMain.handle('resolve-paths', async (_event, clientDir) => {
    return resolveGamePaths(clientDir);
  });

  ipcMain.handle('save-config', async (_event, config) => {
    try {
      const current = await loadConfigDirectly();
      const dataToSave = {
        ...current,
        clientDir: (config && config.clientDir) ? config.clientDir.trim() : current.clientDir,
        mode: (config && (config.mode === '32' || config.mode === '64')) ? config.mode : current.mode,
        launcherPath: (config && config.launcherPath) ? config.launcherPath.trim() : current.launcherPath
      };
      fs.writeFileSync(CONFIG_FILE, JSON.stringify(dataToSave, null, 2), 'utf-8');
      return { success: true, config: dataToSave };
    } catch (err) {
      return { success: false, error: err.message };
    }
  });

  ipcMain.handle('load-config', async () => {
    try {
      const cfg = await loadConfigDirectly();
      const resolved = resolveGamePaths(cfg.clientDir);
      return { success: true, config: cfg, resolved };
    } catch (err) {
      return { success: false, error: err.message, config: DEFAULT_CONFIG };
    }
  });

  ipcMain.handle('browse-file', async () => {
    if (!mainWindow) return null;
    const result = await dialog.showOpenDialog(mainWindow, {
      title: 'Seleccionar ejecutable de Dofus (Dofus Retro.exe o Dofus.exe)',
      filters: [{ name: 'Ejecutables (*.exe)', extensions: ['exe'] }],
      properties: ['openFile']
    });
    if (!result.canceled && result.filePaths.length > 0) {
      return result.filePaths[0];
    }
    return null;
  });

  ipcMain.handle('browse-folder', async () => {
    if (!mainWindow) return null;
    const result = await dialog.showOpenDialog(mainWindow, {
      title: 'Seleccionar carpeta raíz del cliente Dofus',
      properties: ['openDirectory']
    });
    if (!result.canceled && result.filePaths.length > 0) {
      return result.filePaths[0];
    }
    return null;
  });

  ipcMain.on('minimize-window', () => {
    if (mainWindow) {
      mainWindow.minimize();
    }
  });

  ipcMain.on('close-window', () => {
    if (mainWindow) {
      mainWindow.close();
    }
  });
}

// --- App lifecycle ---

app.whenReady().then(() => {
  CONFIG_FILE = path.join(app.getPath('userData'), 'launcher-config.json');
  registerIpcHandlers();
  createWindow();
  createTray();
});

app.on('window-all-closed', () => {
  app.quit();
});
