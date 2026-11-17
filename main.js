const { app, BrowserWindow, ipcMain, dialog, Tray, Menu } = require('electron');
const path = require('path');
const fs = require('fs');
const { spawn } = require('child_process');

let mainWindow;
let tray = null;
let CONFIG_FILE;

const DEFAULT_GAME_PATH = 'D:\\dofus-1.39.8-remastered\\Dofus.exe';
const DEFAULT_CONFIG = {
  gamePath: DEFAULT_GAME_PATH,
  launcherPath: ''
};

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
    {
      label: 'Iniciar Dofus Galaxy',
      click: () => {
        loadConfigDirectly().then(cfg => {
          launchGameProcess(cfg.gamePath);
        });
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

function launchGameProcess(gamePath) {
  return new Promise((resolve) => {
    try {
      const targetPath = (gamePath && gamePath.trim()) ? gamePath.trim() : DEFAULT_GAME_PATH;

      if (!fs.existsSync(targetPath)) {
        resolve({ success: false, error: `No se encontró el ejecutable en: ${targetPath}` });
        return;
      }

      const workingDir = path.dirname(targetPath);

      // Usamos cmd.exe /c start para crear un proceso 100% independiente del Job Object de Electron.
      // De esta forma, si el launcher se minimiza o se cierra por completo, el juego NO se cerrará.
      const child = spawn('cmd.exe', ['/c', 'start', '""', targetPath], {
        cwd: workingDir,
        detached: true,
        stdio: 'ignore',
        windowsHide: true
      });
      child.unref();

      resolve({ success: true });
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
      if (!parsed.gamePath) parsed.gamePath = DEFAULT_GAME_PATH;
      return parsed;
    }
    return DEFAULT_CONFIG;
  } catch {
    return DEFAULT_CONFIG;
  }
}

function getTabsExePath() {
  const candidates = [
    // 1. En producción empaquetado (extraResources en resources/)
    path.join(process.resourcesPath, 'DofusGalaxyTabs.exe'),
    // 2. Al lado del .exe principal
    path.join(path.dirname(app.getPath('exe')), 'DofusGalaxyTabs.exe'),
    // 3. En desarrollo directo
    path.join(__dirname, 'DofusGalaxyTabs.exe'),
    // 4. En la carpeta hermana del proyecto
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
  ipcMain.handle('launch-game', async (_event, gamePath) => {
    return await launchGameProcess(gamePath);
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

  ipcMain.handle('save-config', async (_event, config) => {
    try {
      const dataToSave = {
        gamePath: (config && config.gamePath && config.gamePath.trim()) ? config.gamePath.trim() : DEFAULT_GAME_PATH,
        launcherPath: (config && config.launcherPath) ? config.launcherPath.trim() : ''
      };
      fs.writeFileSync(CONFIG_FILE, JSON.stringify(dataToSave, null, 2), 'utf-8');
      return { success: true };
    } catch (err) {
      return { success: false, error: err.message };
    }
  });

  ipcMain.handle('load-config', async () => {
    try {
      if (fs.existsSync(CONFIG_FILE)) {
        const data = fs.readFileSync(CONFIG_FILE, 'utf-8');
        const parsed = JSON.parse(data);
        if (!parsed.gamePath) parsed.gamePath = DEFAULT_GAME_PATH;
        return { success: true, config: parsed };
      }
      return { success: true, config: DEFAULT_CONFIG };
    } catch (err) {
      return { success: false, error: err.message, config: DEFAULT_CONFIG };
    }
  });

  ipcMain.handle('browse-file', async () => {
    if (!mainWindow) return null;
    const result = await dialog.showOpenDialog(mainWindow, {
      title: 'Seleccionar ejecutable de Dofus',
      filters: [{ name: 'Ejecutables (*.exe)', extensions: ['exe'] }],
      properties: ['openFile']
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
  // En Windows cerramos la app a menos que el usuario prefiera mantenerla en tray
  app.quit();
});
